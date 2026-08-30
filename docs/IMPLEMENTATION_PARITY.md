# AER implementation parity

AER uses a language-neutral canonical model and frozen AER-B vectors. Each implementation is developed independently; the conformance corpus is the interoperability contract.

| Capability | .NET | Python | TypeScript | Go |
|---|---|---|---|---|
| Scalar values | Yes | Yes | Yes | Yes |
| Objects | Yes | Yes | Yes | Yes |
| Arrays | Yes | Yes | Yes | Yes |
| AER Text write | Yes | Yes | Yes | Yes |
| AER Text parse | Yes | Yes | Yes | Core parsing |
| AER-B encode | Yes | Yes | Yes | Yes |
| AER-B decode | Yes | Yes | Yes | Yes |
| Frozen binary vectors | Yes | Yes | Yes | Yes |
| Tables | Yes | Reference path | Reference path | Type exposed |
| Schema validation | Yes | Planned parity layer | Planned parity layer | Planned parity layer |
| Patch API | Yes | Planned parity layer | Planned parity layer | Planned parity layer |
| Streaming frames | Yes | Planned parity layer | Planned parity layer | Planned parity layer |

The table intentionally distinguishes the stable cross-language core from higher-level parity work. Release criteria require all implementations to pass the frozen binary corpus before a version is promoted.
