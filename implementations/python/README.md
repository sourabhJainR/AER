# aer-format

Python reference implementation of AER 1.x text and AER-B codecs.

```python
from aer import dumps, loads, encode, decode
payload = {'name': 'Sourabh', 'age': 45, 'active': True}
text = dumps(payload)
value = loads(text)
binary = encode(value)
round_trip = decode(binary)
```

The binary implementation follows `conformance/binary/v1.json` exactly for the supported canonical value kinds. Release packaging targets PyPI as `aer-format`.
