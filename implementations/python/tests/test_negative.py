import pytest
from aer import decode

def test_binary_truncation_rejected():
    with pytest.raises(ValueError): decode(bytes.fromhex('41455242010a0100000000000000'))

def test_bad_magic_rejected():
    with pytest.raises(ValueError): decode(b'XXXX\x01\x00')
