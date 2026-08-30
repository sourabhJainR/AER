import json
from pathlib import Path
from aer import decode, encode

def test_frozen_binary_vectors():
    path = Path(__file__).resolve().parents[3] / 'conformance' / 'binary' / 'v1.json'
    data = json.loads(path.read_text())
    for vector in data['vectors']:
        payload = bytes.fromhex(vector['hex'])
        value = decode(payload)
        assert value is not None, vector['id']

def test_encode_known_vectors():
    cases = [
        ('null', None, '414552420100'),
        ('mixed-array', [1, 'x', None], '41455242010903000000000000000201000000000000000501000000000000007800'),
    ]
    for name, value, expected in cases:
        assert encode(value).hex() == expected, name
