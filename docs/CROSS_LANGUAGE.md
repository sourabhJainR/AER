# Cross-language interoperability

AER interoperability is driven by the canonical model and frozen AER-B vectors, not by copying one implementation.

## Python

```python
from aer import dumps, loads, encode, decode
v = {'name': 'Sourabh', 'age': 45, 'active': True}
b = encode(v)
assert decode(b).data['name'].data == 'Sourabh'
```

## TypeScript

```ts
import { dumps, loads, encode, decode } from '@aer-format/core';
const v = { name: 'Sourabh', age: 45, active: true };
const b = encode(v);
const roundTrip = decode(b);
```

## Go

```go
v := aer.From(map[string]aer.Value{"name": aer.From("Sourabh"), "age": aer.From(45)})
b := aer.Encode(v)
_, err := aer.Decode(b)
```

The conformance rule is byte-for-byte equality for known AER-B fixtures and semantic equality for decoded values. Implementations may use different APIs internally.
