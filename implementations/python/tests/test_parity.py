import random
from aer import *

def test_table_roundtrip():
    x=table(['id','name'], [[1,'Amit'],[2,'Priya']])
    assert decode(encode(x))==x
    assert loads(dumps(x)).kind==AerKind.OBJECT

def test_schema_patch_stream():
    x=from_obj({'user':{'id':1,'name':'Amit'}})
    schema=Schema('User', {'id':Field('id',AerKind.INT,True,minimum=0), 'name':Field('name',AerKind.STRING,True)})
    assert schema.validate(x)==['User.id: required field is missing','User.name: required field is missing']
    patched=apply(x,[PatchOperation(PatchOp.REPLACE,'/user/name',from_obj('Priya'))])
    assert patched.data['user'].data['name'].data=='Priya'
    frames=encode_frame(x)+encode_frame(patched)
    assert decode_frames(frames)[1]==patched

def random_value(rng, depth=0):
    if depth>2: return from_obj(rng.choice([None,True,False,rng.randint(-20,20),round(rng.random()*10,3),'x','a,b']))
    t=rng.randrange(7)
    if t<4:return random_value(rng,3)
    if t==4:return AerValue(AerKind.ARRAY,tuple(random_value(rng,depth+1) for _ in range(rng.randrange(4))))
    m={f'k{i}':random_value(rng,depth+1) for i in range(rng.randrange(4))}
    return AerValue(AerKind.OBJECT,m)

def test_property_roundtrip_deterministic():
    rng=random.Random(20260830)
    for _ in range(250):
        x=random_value(rng)
        assert decode(encode(x))==x
        assert loads(dumps(x))==x if x.kind==AerKind.OBJECT else True
