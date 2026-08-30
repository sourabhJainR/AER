from __future__ import annotations
from dataclasses import dataclass
from enum import IntEnum
import base64, datetime as dt, struct
from typing import Any

class AerKind(IntEnum):
    NULL=0; BOOL=1; INT=2; FLOAT=3; DECIMAL=4; STRING=5; BYTES=6; DATETIME=7; DURATION=8; ARRAY=9; OBJECT=10; TABLE=11; REFERENCE=12

@dataclass(frozen=True)
class AerTable:
    columns: tuple[str, ...]
    rows: tuple[tuple['AerValue', ...], ...]
    def __post_init__(self):
        if len(set(self.columns)) != len(self.columns) or not self.columns:
            raise ValueError('AER table requires unique non-empty columns')
        width=len(self.columns)
        if any(len(r)!=width for r in self.rows): raise ValueError('AER table row width mismatch')

@dataclass(frozen=True)
class AerValue:
    kind: AerKind
    data: Any

class AerError(ValueError): pass

def from_obj(v: Any) -> AerValue:
    if isinstance(v,AerValue): return v
    if v is None: return AerValue(AerKind.NULL,None)
    if isinstance(v,bool): return AerValue(AerKind.BOOL,v)
    if isinstance(v,int) and not isinstance(v,bool): return AerValue(AerKind.INT,v)
    if isinstance(v,float): return AerValue(AerKind.FLOAT,v)
    if isinstance(v,dt.datetime): return AerValue(AerKind.DATETIME,v)
    if isinstance(v,dt.timedelta): return AerValue(AerKind.DURATION,v)
    if isinstance(v,(bytes,bytearray)): return AerValue(AerKind.BYTES,bytes(v))
    if isinstance(v,str): return AerValue(AerKind.STRING,v)
    if isinstance(v,dict): return AerValue(AerKind.OBJECT,{str(k):from_obj(x) for k,x in v.items()})
    if isinstance(v,(list,tuple)): return AerValue(AerKind.ARRAY,tuple(from_obj(x) for x in v))
    raise TypeError(f'Unsupported value: {type(v).__name__}')

def table(columns: list[str]|tuple[str,...], rows: list[list[Any]]|tuple[tuple[Any,...],...]) -> AerValue:
    return AerValue(AerKind.TABLE,AerTable(tuple(columns),tuple(tuple(from_obj(x) for x in r) for r in rows)))

def reference(name:str)->AerValue: return AerValue(AerKind.REFERENCE,name)

def _esc(s:str)->str: return s.replace('\\','\\\\').replace('"','\\"').replace('\n','\\n')
def _scalar(v:AerValue)->str:
    k,x=v.kind,v.data
    if k==AerKind.NULL:return '-'
    if k==AerKind.BOOL:return 'true' if x else 'false'
    if k==AerKind.INT:return str(x)
    if k==AerKind.FLOAT:return repr(x)
    if k==AerKind.DECIMAL:return str(x)
    if k==AerKind.STRING:
        if x and all(c not in x for c in ',:\n') and x not in ('-','true','false') and not x.startswith(('@','dt"','dur"','b64"')): return x
        return '"'+_esc(x)+'"'
    if k==AerKind.BYTES:return 'b64"'+base64.b64encode(x).decode()+'"'
    if k==AerKind.DATETIME:return 'dt"'+x.astimezone(dt.timezone.utc).isoformat().replace('+00:00','Z')+'"'
    if k==AerKind.DURATION:return 'dur"'+str(x)+'"'
    if k==AerKind.REFERENCE:return '@'+x
    raise AerError('non-scalar')

def dumps(value:Any, header:bool=True)->str:
    v=from_obj(value); lines=['@aer 1'] if header else []
    def emit(name,x,indent=0):
        p=' '*indent
        if x.kind==AerKind.OBJECT:
            lines.append(f'{p}{name}:')
            for k,val in x.data.items(): emit(k,val,indent+2)
        elif x.kind==AerKind.TABLE:
            t=x.data; lines.append(f"{p}{name}[{len(t.rows)]}{{{','.join(t.columns)}}}:")
            for r in t.rows: lines.append(' '*(indent+2)+','.join(_scalar(z) for z in r))
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

def _split(s:str)->list[str]:
    out=[]; start=0; q=False; esc=False
    for i,c in enumerate(s):
        if esc: esc=False; continue
        if c=='\\' and q: esc=True; continue
        if c=='"': q=not q
        elif c==',' and not q: out.append(s[start:i].strip()); start=i+1
    out.append(s[start:].strip()); return out

def _scalar_parse(s:str)->AerValue:
    if s=='-': return AerValue(AerKind.NULL,None)
    if s.lower()=='true': return AerValue(AerKind.BOOL,True)
    if s.lower()=='false': return AerValue(AerKind.BOOL,False)
    if s.startswith('@') and len(s)>1:return reference(s[1:])
    if s.startswith('b64"') and s.endswith('"'):return AerValue(AerKind.BYTES,base64.b64decode(s[4:-1],validate=True))
    if s.startswith('dt"') and s.endswith('"'):return AerValue(AerKind.DATETIME,dt.datetime.fromisoformat(s[3:-1].replace('Z','+00:00')))
    if s.startswith('dur"') and s.endswith('"'):
        raw=s[5:-1]
        return AerValue(AerKind.DURATION,dt.timedelta(seconds=float(raw[:-1]) if raw.endswith('s') else float(dt.timedelta.fromisoformat(raw).total_seconds())))
    if len(s)>=2 and s[0]=='"' and s[-1]=='"':return AerValue(AerKind.STRING,s[1:-1].replace('\\n','\n').replace('\\"','"').replace('\\\\','\\'))
    try:return AerValue(AerKind.INT,int(s,10))
    except ValueError: pass
    try:return AerValue(AerKind.FLOAT,float(s)) if any(c in s for c in '.eE') else AerValue(AerKind.DECIMAL,s)
    except ValueError:return AerValue(AerKind.STRING,s)

def loads(text:str)->AerValue:
    lines=[x for x in text.replace('\r\n','\n').replace('\r','\n').split('\n') if x.strip() and not x.lstrip().startswith('#')]
    while lines and lines[0].lstrip().startswith('@'): lines.pop(0)
    if not lines: raise AerError('empty AER document')
    def block(pos:int,indent:int):
        out={}
        while pos<len(lines):
            raw=lines[pos]; ind=len(raw)-len(raw.lstrip(' '))
            if ind<indent: break
            if ind>indent: raise AerError('invalid indentation')
            line=raw.strip(); pos+=1
            if ':' not in line: raise AerError(f'invalid line: {line}')
            key,rest=line.split(':',1); rest=rest.strip()
            tm=key.rfind(']{'); op=key.rfind('[')
            if op>0 and tm>op and key.endswith('}') and not rest:
                n=int(key[op+1:tm]); name=key[:op]; cols=tuple(c.strip() for c in key[tm+2:-1].split(',')); rows=[]
                for _ in range(n):
                    if pos>=len(lines): raise AerError('unexpected end of table')
                    row=lines[pos].strip(); pos+=1; vals=_split(row)
                    if len(vals)!=len(cols): raise AerError('table column count mismatch')
                    rows.append(tuple(_scalar_parse(v) for v in vals))
                out[name]=AerValue(AerKind.TABLE,AerTable(cols,tuple(rows))); continue
            am=key.rfind('[')
            if am>0 and key.endswith(']') and rest:
                n=int(key[am+1:-1]); vals=_split(rest)
                if len(vals)!=n: raise AerError('array count mismatch')
                out[key[:am]]=AerValue(AerKind.ARRAY,tuple(_scalar_parse(v) for v in vals)); continue
            if not rest:
                if pos<len(lines) and len(lines[pos])-len(lines[pos].lstrip(' '))>ind: child,pos=block(pos,ind+2)
                else: child={}
                out[key]=AerValue(AerKind.OBJECT,child)
            else: out[key]=_scalar_parse(rest)
        return out,pos
    d,_=block(0,0); return AerValue(AerKind.OBJECT,d)

_TICKS_EPOCH=621355968000000000
def _ticks(x:dt.datetime)->int:
    u=x if x.tzinfo else x.replace(tzinfo=dt.timezone.utc); u=u.astimezone(dt.timezone.utc)
    return _TICKS_EPOCH+int(u.timestamp()*10_000_000)

def encode(value:Any)->bytes:
    return b'AERB'+bytes([1])+_enc(from_obj(value))
def _pack_i64(n:int)->bytes:return struct.pack('<q',n)
def _packstr(s:str)->bytes:
    b=s.encode(); return _pack_i64(len(b))+b
def _enc(v:AerValue)->bytes:
    k,x=v.kind,v.data; b=bytes([int(k)])
    if k==AerKind.NULL:return b
    if k==AerKind.BOOL:return b+bytes([1 if x else 0])
    if k==AerKind.INT:return b+_pack_i64(x)
    if k==AerKind.FLOAT:return b+struct.pack('<d',x)
    if k in (AerKind.DECIMAL,AerKind.STRING,AerKind.REFERENCE):return b+_packstr(str(x))
    if k==AerKind.BYTES:return b+_pack_i64(len(x))+x
    if k==AerKind.DATETIME:return b+_pack_i64(_ticks(x))
    if k==AerKind.DURATION:return b+_pack_i64(x.days*864000000000+x.seconds*10_000_000+x.microseconds*10)
    if k==AerKind.ARRAY:return b+_pack_i64(len(x))+b''.join(_enc(y) for y in x)
    if k==AerKind.OBJECT:return b+_pack_i64(len(x))+b''.join(_packstr(key)+_enc(y) for key,y in x.items())
    if k==AerKind.TABLE:
        t=x; return b+_pack_i64(len(t.columns))+b''.join(_packstr(c) for c in t.columns)+_pack_i64(len(t.rows))+b''.join(_enc(y) for r in t.rows for y in r)
    raise AerError('unsupported binary kind')

def decode(data:bytes,max_items:int=1_000_000)->AerValue:
    if len(data)<5 or data[:4]!=b'AERB' or data[4]!=1: raise AerError('invalid AERB header')
    v,o=_dec(data,5,max_items)
    if o!=len(data): raise AerError('trailing AERB payload')
    return v
def _read(d:bytes,o:int,n:int):
    if n<0 or o<0 or o>len(d)-n: raise AerError('truncated AERB payload')
    return d[o:o+n],o+n
def _i64(d,o): x,o=_read(d,o,8); return struct.unpack('<q',x)[0],o
def _str(d,o): n,o=_i64(d,o); x,o=_read(d,o,n); return x.decode(),o
def _dec(d,o,limit):
    raw,o=_read(d,o,1); k=AerKind(raw[0])
    if k==AerKind.NULL:return AerValue(k,None),o
    if k==AerKind.BOOL:x,o=_read(d,o,1);return AerValue(k,x[0]!=0),o
    if k==AerKind.INT:x,o=_i64(d,o);return AerValue(k,x),o
    if k==AerKind.FLOAT:x,o=_read(d,o,8);return AerValue(k,struct.unpack('<d',x)[0]),o
    if k in (AerKind.DECIMAL,AerKind.STRING,AerKind.REFERENCE):x,o=_str(d,o);return AerValue(k,x),o
    if k==AerKind.BYTES:n,o=_i64(d,o);x,o=_read(d,o,n);return AerValue(k,x),o
    if k==AerKind.DATETIME:n,o=_i64(d,o);return AerValue(k,dt.datetime.fromtimestamp((n-_TICKS_EPOCH)/10_000_000,tz=dt.timezone.utc)),o
    if k==AerKind.DURATION:n,o=_i64(d,o);return AerValue(k,dt.timedelta(microseconds=n/10)),o
    if k==AerKind.ARRAY:n,o=_i64(d,o); n=int(n); 
    elif k==AerKind.OBJECT:n,o=_i64(d,o); n=int(n)
    else:n=None
    if k==AerKind.ARRAY:
        if n<0 or n>limit:raise AerError('invalid array length')
        a=[]
        for _ in range(n):z,o=_dec(d,o,limit);a.append(z)
        return AerValue(k,tuple(a)),o
    if k==AerKind.OBJECT:
        if n<0 or n>limit:raise AerError('invalid object length')
        m={}
        for _ in range(n):key,o=_str(d,o);z,o=_dec(d,o,limit);m[key]=z
        return AerValue(k,m),o
    if k==AerKind.TABLE:
        c,o=_i64(d,o); c=int(c)
        if c<=0 or c>limit:raise AerError('invalid column count')
        cols=[]
        for _ in range(c):x,o=_str(d,o);cols.append(x)
        r,o=_i64(d,o);r=int(r)
        if r<0 or r>limit:raise AerError('invalid row count')
        rows=[]
        for _ in range(r):
            row=[]
            for _ in range(c):z,o=_dec(d,o,limit);row.append(z)
            rows.append(tuple(row))
        return AerValue(k,AerTable(tuple(cols),tuple(rows))),o
    raise AerError(f'unsupported AER kind {k}')
