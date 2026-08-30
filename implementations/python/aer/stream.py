from .core import AerValue, encode, decode
import struct
MAGIC=b'AERF'; VERSION=1; HEADER=9

def encode_frame(value:AerValue)->bytes:
    payload=encode(value)
    return MAGIC+bytes([VERSION])+struct.pack('<I',len(payload))+payload

def decode_frames(data:bytes,max_frame_bytes:int=16*1024*1024):
    offset=0
    while offset<len(data):
        if len(data)-offset<HEADER: raise ValueError('AER008 truncated frame header')
        if data[offset:offset+4]!=MAGIC or data[offset+4]!=VERSION: raise ValueError('AER009 invalid frame header')
        n=struct.unpack('<I',data[offset+5:offset+9])[0]
        if n>max_frame_bytes: raise ValueError('AER006 frame exceeds configured size limit')
        end=offset+HEADER+n
        if end>len(data): raise ValueError('AER008 truncated frame payload')
        yield decode(data[offset+HEADER:end])
        offset=end
