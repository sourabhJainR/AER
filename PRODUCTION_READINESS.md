# AER Production Readiness

## Status

AER is a production-candidate serialization library. The core safety and release gates below are required before a stable 1.x release is declared.

## Required gates

### Compatibility

- Semantic versioning is followed.
- Text and binary versions are explicit.
- Unknown mandatory features fail closed.
- Binary tags are never silently reinterpreted.
- Conformance vectors remain frozen and versioned.

### Input safety

- Parser input limits are configurable and bounded.
- Binary decoding limits cover payload size, nesting, collections and string/byte values.
- Malformed booleans, decimals, directives, duplicate keys/columns and trailing bytes are rejected deterministically.
- Streaming frames enforce independent frame limits and propagate binary limits.

### Determinism

- Equivalent input produces deterministic output.
- Canonical round-trip tests use structural equality.
- Benchmark reports identify AER commit/runtime and corpus version.

### Operations

- Library APIs remain side-effect free.
- Adapters must preserve cancellation, error and request boundaries of their host frameworks.
- Logs/telemetry in host applications must not expose data payloads by default.
- The core library has no mandatory telemetry or external service dependency.

### Packaging

- NuGet metadata includes license, repository, description and README.
- Release builds are deterministic and warnings are errors.
- Package contents are validated in CI.
- A package smoke-consumer test should be added before 1.0 to verify a fresh consumer can restore and use the package without repository internals.

### Verification

- Valid conformance vectors pass.
- Negative/malformed-input suite passes.
- Benchmark smoke test passes.
- AI effectiveness benchmark passes its fidelity checks.
- Fuzz/property testing is required before 1.0 for parser, binary codec, streaming decoder and optimizer.
- At least two independent implementations should pass the frozen conformance vectors before 1.0.

## Effectiveness measurement

AER claims must be workload- and tokenizer-specific. Byte reduction is not an AI effectiveness claim.

For AI workloads report:

`workload -> representation -> exact tokens -> task quality -> verification -> latency -> total cost`

Paired JSON/AER experiments must hold model, task, prompt, tool permissions and repository snapshot constant.

## Release rule

Do not call a version "production ready" because the benchmark is favorable. Release readiness requires the compatibility, safety, determinism, packaging and verification gates above to pass together.
