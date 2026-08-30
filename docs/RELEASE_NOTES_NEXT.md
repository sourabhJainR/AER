# Next release

This increment completes the cross-language parity layer for the reference implementations.

Highlights:

- Python table/schema/patch/streaming support.
- TypeScript table/schema/patch/streaming support with full nested parser.
- Go table/schema/patch/streaming support with full nested parser.
- AER-B v1 table frozen vector and exact encode/decode assertions.
- Deterministic property tests in all three independent implementations.
- Native Go fuzz target for malformed AER-B input.
- Cross-language parity and release-gate documentation.

The release gate still requires CI to pass on the final commit before a versioned package release is published.
