# @aer-format/core

Node.js/TypeScript reference implementation for AER text and AER-B.

```ts
import { dumps, loads, encode, decode } from '@aer-format/core';
const text = dumps({ name: 'Sourabh', age: 45, active: true });
const value = loads(text);
const bytes = encode(value);
const roundTrip = decode(bytes);
```

The package is structured for npm publication and keeps the wire contract aligned with `conformance/binary/v1.json`.
