# AER P0 Adoption Implementation

## Implemented in this phase

- `.aer` canonical text extension and `.aerb` binary extension.
- Stable media-type/profile contract in `spec/media-types.md`.
- Zero-SDK CLI contract under `tools/aer-cli`.
- CLI commands for convert, validate, format and benchmark.
- Editor-neutral LSP contract under `tools/aer-lsp`.
- VS Code-compatible extension manifest, syntax grammar and CLI-backed commands.
- JSON Schema metadata contract for tooling interoperability.
- HTTP content-negotiation contract.
- GitHub language integration metadata and repository guidance.
- Incremental adoption guidance that preserves JSON fallback.

## Important compatibility decision

AER AI is a profile of `.aer`, not a new file extension. This keeps editor support simple and avoids creating multiple dialects that tooling must learn.

## P0 acceptance criteria

1. A developer can install/use the CLI without adding an AER SDK to the application.
2. JSON can be converted to AER and AER can be converted back to JSON.
3. `.aer` is recognized as a structured text document by the VS Code integration.
4. Validation and deterministic formatting are available from the editor.
5. HTTP clients can negotiate AER using standard `Accept` semantics.
6. MCP remains backward compatible through JSON fallback.
7. AER can be introduced at a representation boundary and rolled back without changing domain models.

## Next P0 hardening before merge

- Package the CLI as self-contained native executables for Windows, Linux and macOS.
- Add a real LSP implementation backed by the AER parser rather than only the contract.
- Add VSIX build/package validation in CI.
- Add JSON Schema-driven completion/diagnostics where a schema is supplied.
- Add GitHub Actions workflow for CLI/editor artifacts.
- Add a browser playground using the same core/WASM or service implementation.
