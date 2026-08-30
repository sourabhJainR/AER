import json
from pathlib import Path
from aer import decode, encode, table, AerKind

VECTOR_HEX='41455242010b02000000000000000200000000000000696404000000000000006e616d650100000000000000020100000000000000050400000000000000416d6974'

def test_frozen_binary_vector_exact():
    root=Path(__file__).resolve().parents[3]
    vectors=json.loads((root/'conformance'/'binary'/'v1.json').read_text())['vectors']
    for item in vectors: assert decode(bytes.fromhex(item['hex'])) is not None
    x=table(['id','name'], [[1,'Amit']])
    assert encode(x).hex()==VECTOR_HEX
    decoded=decode(bytes.fromhex(VECTOR_HEX))
    assert decoded.kind==AerKind.TABLE
    assert decoded.data.columns==('id','name')
