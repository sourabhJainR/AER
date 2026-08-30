import json
from pathlib import Path
from aer import decode

def test_frozen_binary_vectors():
    data=json.loads(Path(__file__).parents[2].joinpath('../../conformance/binary/v1.json').resolve().read_text())
    for vector in data['vectors']:
        value=decode(bytes.fromhex(vector['hex']))
        assert value is not None, vector['id']
