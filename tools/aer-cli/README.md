# AER CLI

The `aer` CLI is the zero-SDK entry point for converting, validating, formatting and benchmarking structured data.

## Commands

```text
aer convert input.json --to aer
aer convert input.aer --to json
aer validate input.aer
aer fmt input.aer
aer benchmark input.json
```

The CLI must preserve canonical semantics across JSON and AER. It is intentionally separate from the core library so applications do not need a CLI dependency.

## Exit codes

- `0` success
- `1` invalid input or validation failure
- `2` usage error

## Adoption rule

Every command must work with files and stdin/stdout so AER can be introduced into existing Unix, CI and editor workflows without application changes.
