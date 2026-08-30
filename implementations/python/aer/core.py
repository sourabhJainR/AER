from __future__ import annotations
from dataclasses import dataclass
from enum import IntEnum
import base64, datetime as dt, struct
from typing import Any

class AerKind(IntEnum):
    NULL=0; BOOL=1; INT=2; FLOAT=3; DECIMAL=4; STRING=5; BYTES=6; DATETIME=7; DURATION=8; ARRAY=9; OBJECT=10; TABLE=11; REFERENCE=12

@dataclass(frozen=True)
class AerValue:
    kind: AerKind
    data: Any

class AerError(ValueError): pass

def from_obj(v: Any) -> AerValue:
    if v is None: return AerValue(AerKind.NULL,None)
    if isinstance(v,AerValue): return v
    if isinstance(v,bool): return AerValue(AerKind.BOOL,v)
    if isinstance(v,int): return AerValue(AerKind.INT,v)
    if isinstance(v,float): return AerValue(AerKind.FLOAT,v)
    if isinstance(v,(bytes,bytearray)): return AerValue(AerKind.BYTES,bytes(v))
    if isinstance(v,dt.datetime): return AerValue(AerKind.DATETIME,v)
    if isinstance(v,dt.timedelta): return AerValue(AerKind.DURATION,v)
    if isinstance(v,str): return AerValue(AerKind.STRING,v)
    if isinstance(v,dict): return AerValue(AerKind.OBJECT,{str(k):from_obj(x) for k,x in v.items()})
    if isinstance(v,(list,tuple)): return AerValue(AerKind.ARRAY,[from_obj(x) for x in v])
    raise TypeError(f'Unsupported value: {type(v).__name__}')

def _esc(s:str)->str:
    return s.replace('\\','\\\\').replace('"','\\"').replace('\n','\\n')

def _scalar(v:AerValue)->str:
    k=v.kind; x=v.data
    if k==AerKind.NULL:return '-'
    if k==AerKind.BOOL:return 'true' if x else 'false'
    if k==AerKind.INT:return str(x)
    if k==AerKind.FLOAT:return repr(x)
    if k==AerKind.DECIMAL:return str(x)
    if k==AerKind.STRING:
        return x if x and all(c not in x for c in ',:\n') and x not in ('-','true','false') and not x.startswith(('@','dt"','dur"','b64"')) else '"'+_esc(x)+'"'
    if k==AerKind.BYTES:return 'b64"'+base64.b64encode(x).decode()+'"'
    if k==AerKind.DATETIME:return 'dt"'+x.isoformat()+'"'
    if k==AerKind.DURATION:return 'dur"'+str(x)+'"'
    if k==AerKind.REFERENCE:return '@'+x
    raise AerError('non-scalar')

def dumps(value:Any, header:bool=True)->str:
    v=from_obj(value)
    lines=['@aer 1'] if header else []
    def emit(name,x,indent=0):
        p=' '*indent
        if x.kind==AerKind.OBJECT:
            lines.append(f'{p}{name}:')
            for k,val in x.data.items(): emit(k,val,indent+2)
        elif x.kind==AerKind.ARRAY:
            if all(y.kind not in (AerKind.OBJECT,AerKind.ARRAY,AerKind.TABLE) for y in x.data): lines.append(f'{p}{name}[{len(x.data)}]:'+','.join(_scalar(y) for y in x.data))
            else:
                lines.append(f'{p}{name}:')
                for i,val in enumerate(x.data): emit(str(i),val,indent+2)
        else: lines.append(f'{p}{name}:{_scalar(x)}')
    if v.kind==AerKind.OBJECT:
        for k,val in v.data.items(): emit(k,val)
    else: emit('value',v)
    return '\n'.join(lines)+'\n'

def _split(s):
    out=[]; start=0; q=False; esc=False
    for i,c in enumerate(s):
        if esc: esc=False; continue
        if c=='\\' and q: esc=True; continue
        if c=='"': q=not q
        elif c==',' and not q: out.append(s[start:i].strip()); start=i+1
    out.append(s[start:].strip()); return out

def _unq(s): return s[1:-1].replace('\\n','\n').replace('\\"','"').replace('\\\\','\\')
def _scalar_parse(s):
    if s=='-': return AerValue(AerKind.NULL,None)
    if s.lower()=='true': return AerValue(AerKind.BOOL,True)
    if s.lower()=='false': return AerValue(AerKind.BOOL,False)
    if s.startswith('@'): return AerValue(AerKind.REFERENCE,s[1:])
    if s.startswith('b64"') and s.endswith('"'): return AerValue(AerKind.BYTES,base64.b64decode(s[4:-1]))
    if s.startswith('dt"') and s.endswith('"'): return AerValue(AerKind.DATETIME,dt.datetime.fromisoformat(s[3:-1]))
    if s.startswith('dur"') and s.endswith('"'): return AerValue(AerKind.DURATION,dt.timedelta.fromisoformat(s[5:-1]))
    if len(s)>=2 and s[0]=='"' and s[-1]=='"': return AerValue(AerKind.STRING,_unq(s))
    try:return AerValue(AerKind.INT,int(s))
    except ValueError: 
        try:return AerValue(AerKind.FLOAT,float(s))
        except ValueError:return AerValue(AerKind.STRING,s)

def loads(text:str)->AerValue:
    lines=[x for x in text.replace('\r\n','\n').replace('\r','\n').split('\n') if x.strip() and not x.lstrip().startswith('#')]
    while lines and lines[0].lstrip().startswith('@'): lines.pop(0)
    if not lines: raise AerError('empty AER document')
    obj={}
    def block(pos,indent):
        out={}
        while pos<len(lines):
            raw=lines[pos]; ind=len(raw)-len(raw.lstrip(' '))
            if ind<indent: break
            if ind>indent: raise AerError('invalid indentation')
            line=raw.strip(); pos+=1
            if ':' not in line: raise AerError(f'invalid line: {line}')
            key,rest=line.split(':',1); rest=rest.strip()
            if '[' in key and key.endswith(']') and rest:
                n=int(key[key.rfind('[')+1:-1]); name=key[:key.rfind('[')]; vals=_split(rest)
                if len(vals)!=n: raise AerError('array count mismatch')
                out[name]=AerValue(AerKind.ARRAY,[_scalar_parse(v) for v in vals]); continue
            if not rest:
                child,pos=block(pos, ind+2) if pos<len(lines) and len(lines[pos])-len(lines[pos].lstrip(' '))>ind else ({},pos)
                out[key]=AerValue(AerKind.OBJECT,child); continue
            out[key]=_scalar_parse(rest)
        return out,pos
    d,_=block(0,0); return AerValue(AerKind.OBJECT,d)

def encode(value:Any)->bytes:
    v=from_obj(value); return b'AERB'+bytes([1])+_enc(v)
def _enc(v):
    k=v.kind; b=bytes([k])
    if k==AerKind.NULL:return b
    if k==AerKind.BOOL:return b+bytes([1 if v.data else 0])
    if k==AerKind.INT:return b+struct.pack('<q',v.data)
    if k==AerKind.FLOAT:return b+struct.pack('<d',v.data)
    if k==AerKind.DECIMAL:return b+_packstr(str(v.data))
    if k==AerKind.STRING:return b+_packstr(v.data)
    if k==AerKind.BYTES:return b+_packbytes(v.data)
    if k==AerKind.DATETIME:return b+struct.pack('<q',int(v.data.timestamp()*10_000_000))
    if k==AerKind.DURATION:return b+struct.pack('<q',v.data.days*864000000000+v.data.seconds*10_000_000+v.data.microseconds*10)
    if k==AerKind.REFERENCE:return b+_packstr(v.data)
    if k==AerKind.ARRAY:return b+struct.pack('<q',len(v.data))+b''.join(_enc(x) for x in v.data)
    if k==AerKind.OBJECT:return b+struct.pack('<q',len(v.data))+b''.join(_packstr(k)+_enc(x) for k,x in v.data.items())
    raise AerError('binary table encoding requires structured table API')
def _packbytes(x):return struct.pack('<q',len(x))+x
def _packstr(x):return _packbytes(x.encode())

def decode(data:bytes)->AerValue:
    if len(data)<5 or data[:4]!=b'AERB' or data[4]!=1: raise AerError('invalid AERB header')
    v,_= _dec(data,5); return v
def _read(data,o,n):
    if o+n>len(data): raise AerError('truncated AERB payload')
    return data[o:o+n],o+n
def _dec(d,o):
    raw,o=_read(d,o,1); k=AerKind(raw[0])
    if k==AerKind.NULL:return AerValue(k,None),o
    if k==AerKind.BOOL:
        x,o=_read(d,o,1); return AerValue(k,x[0]!=0),o
    if k==AerKind.INT:
        x,o=_read(d,o,8); return AerValue(k,struct.unpack('<q',x)[0]),o
    if k==AerKind.FLOAT:
        x,o=_read(d,o,8); return AerValue(k,struct.unpack('<d',x)[0]),o
    if k in (AerKind.DECIMAL,AerKind.STRING,AerKind.REFERENCE):
        x,o=_read_str(d,o); return AerValue(k,x),o
    if k==AerKind.BYTES:
        x,o=_read_blob(d,o); return AerValue(k,x),o
    if k==AerKind.ARRAY:
        n,o=_read_i64(d,o); a=[]
        for _ in range(n): x,o=_dec(d,o); a.append(x)
        return AerValue(k,a),o
    if k==AerKind.OBJECT:
        n,o=_read_i64(d,o); m={}
        for _ in range(n): key,o=_read_str(d,o); x,o=_dec(d,o); m[key]=x
        return AerValue(k,m),o
    raise AerError(f'unsupported kind {k}')
def _read_i64(d,o):
    x,o=_read(d,o,8); return struct.unpack('<q',x)[0],o
def _read_str(d,o):
    x,o=_read_blob(d,o); return x.decode(),o
def _read_blob(d,o):
    n,o=_read_i64(d,o); x,o=_read(d,o,n); return x,o
