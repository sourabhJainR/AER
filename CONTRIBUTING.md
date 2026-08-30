# Contributing to AER

AER is an open-source data representation project. Contributions should preserve four properties: deterministic parsing, lossless semantics, readable text and efficient machine/AI representation.

## Development

The reference implementation targets .NET 9.

```bash
dotnet restore src/Aer.Core/Aer.Core.csproj
dotnet build src/Aer.Core/Aer.Core.csproj -c Release
```

## Before a pull request

- Keep the canonical model format-neutral.
- Avoid format-specific business logic in applications.
- Add or update examples for syntax changes.
- Add conformance vectors for parser/writer changes.
- Add benchmark coverage for performance-sensitive changes.
- Do not add undocumented syntax.
- Do not claim a performance improvement without measurements.

## Specification changes

Any change to the AER grammar, canonical model, binary wire format, schema semantics or profile behavior must include a documentation update and an AEP proposal when it is not a bug fix.

## Commit and PR guidance

Prefer small, reviewable commits. A PR should explain compatibility impact and whether existing conformance vectors continue to pass.

## Compatibility rule

AER 1.x should remain forward-compatible where possible, but unknown mandatory features must fail explicitly. Backward incompatible changes belong in a new major version.
