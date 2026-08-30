# AER Conformance Suite

This directory contains machine-readable test vectors used to verify parser/writer behavior across implementations.

Each vector should define:

```json
{
  "id": "basic-object-001",
  "text": "name:Sourabh\nage:45\nactive:true\n",
  "canonical": {
    "name": "Sourabh",
    "age": 45,
    "active": true
  }
}
```

Future vectors should also include expected AER-B bytes once the binary wire format is frozen.

Implementations should run every vector as:

```text
AER text -> parse -> canonical value -> serialize -> canonical equivalent
```

Invalid vectors should assert a stable AER error category rather than relying on exception message text.
