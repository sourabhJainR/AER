# Implementation notes

Python, TypeScript and Go use language-native data structures and APIs. They do not depend on the .NET implementation at runtime.

The common interoperability points are:

- `AerKind` numeric tag assignments.
- AER-B v1 header and tagged payload structure.
- AERF v1 streaming frame structure.
- AER table column/row semantics.
- Schema field type/required/range semantics.
- Patch path and operation semantics.
- Frozen binary conformance vectors.

This separation keeps the implementations independently testable and reduces the risk of a single reference implementation hiding interoperability defects.
