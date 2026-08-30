# AER status

The repository now contains independent reference implementations for .NET, Python, TypeScript and Go with a shared AER-B v1 wire contract and AERF v1 frame contract.

The current parity work covers text parsing/writing, tables, schema validation, patches, binary encoding/decoding, streaming frames, frozen binary vectors and property/fuzz testing.

The final release gate is CI success on the complete cross-language matrix. Long-running fuzz campaigns remain a nightly/release-hardening activity.
