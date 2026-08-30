import json
from pathlib import Path
from aer import decode, AerKind, AerTable

def test_all_frozen_binary_vectors():
    root=Path(__file__).resolve().parents[3]
    vectors=json.loads((root/'conformance'/'binary'/'v1.json').read_text())['vectors']
    for item in vectors:
        value=decode(bytes.fromhex(item['hex']))
        assert value is not None, item['id']
        if item['id']=='table-basic':
            assert value.kind==AerKind.TABLE
            assert value.data.columns==('id','name')
            assert value.data.rows[0][0].data==1
            assert value.data.rows[0][1].data=='Amit'
