# AER Release Parity Gate

A release is ready to publish only when all language implementations pass the following gates:

- .NET, Python, TypeScript and Go build cleanly.
- All frozen AER-B v1 vectors decode successfully.
- Each implementation encodes the frozen table vector byte-for-byte identically.
- Text table/nested-object round trips pass.
- Schema required/type/min/max validation passes.
- Patch add/replace/remove operations pass.
- AERF multi-frame round trips pass.
- Deterministic property tests pass.
- Fuzz entry points execute without panics/crashes on malformed binary data.
- Package build artifacts are generated successfully.

AER 1.x does not require source-level API equality between languages. It requires semantic and wire-level interoperability.
