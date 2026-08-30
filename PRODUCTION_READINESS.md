# AER Production Readiness

## Status

AER is a strong production candidate for deterministic serialization and an improving candidate for AI-context workloads. It should not be called production-proven for AI task utility until model-backed paired evaluations are published.

## Current engineering assessment

| Area | Previous | Current target | Evidence in repository |
|---|---:|---:|---|
| AER core production hardening | 8.5/10 | 9.0/10 | bounded parsing/binary checks, deterministic conformance, property/fuzz suite |
| CI/release discipline | 8.5/10 | 9.0/10 | build, conformance, integration, fuzz, benchmark, package consumer gates |
| MCP integration surface | 5.5/10 | 8.0/10 | concrete capability parsing, deterministic negotiation, payload encoder, integration contracts |
| Orchestrator integration | 4.0/10 | 7.5/10 | standalone deterministic context planner with priority and budget enforcement |
| Real-world benchmark coverage | 6.5/10 | 8.0/10 | multi-workload corpus, fidelity gates, runtime measurements, reproducible artifacts |
| Exact AI/token effectiveness | 3.5/10 | 6.5/10 | exact `o200k_base` tokenizer run in CI; model task utility remains open |
| Production evidence | 7.0/10 | 8.0/10 | reproducible reports, package consumer, integration and fuzz gates |
| Overall readiness | 6.5/10 | 8.0/10 | strong serialization production candidate; AI claims still require model-backed evidence |

The current target is deliberately conservative. Token savings are not treated as proof of better model behavior.

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
- The repository includes a deterministic property-style suite covering text and binary round trips across 2,000 generated cases.

### MCP and orchestration

- MCP integration must negotiate representation rather than assume every client understands AER.
- JSON remains the fallback for unsupported clients.
- AER MCP APIs remain SDK-neutral and preserve host transport boundaries.
- Orchestration policy must remain deterministic and independent of model/provider calls.
- Context budgets must fail clearly when required context cannot fit.

### Operations

- Library APIs remain side-effect free.
- Adapters must preserve cancellation, error and request boundaries of their host frameworks.
- Logs/telemetry in host applications must not expose data payloads by default.
- The core library has no mandatory telemetry or external service dependency.

### Packaging

- NuGet metadata includes license, repository, description and README.
- Release builds are deterministic and warnings are errors.
- Package contents are validated in CI.
- A package smoke-consumer test verifies a fresh consumer can restore and use the package without repository internals.

### Verification

- Valid conformance vectors pass.
- Negative/malformed-input suite passes.
- Benchmark smoke test passes.
- AI effectiveness benchmark passes its fidelity checks.
- Exact tokenizer measurement runs in CI using `o200k_base` through the pinned tiktoken adapter.
- Fuzz/property testing covers parser/codec round trips before stable release.
- At least two independent implementations should pass the frozen conformance vectors before 1.0.

## AI effectiveness evidence ladder

AER claims must be workload- and tokenizer-specific. Byte reduction is not an AI effectiveness claim.

1. Exact token count: JSON vs AER Text vs AER AI for a named tokenizer.
2. Paired model evaluation: same task, model, prompt, permissions, repository/data snapshot and verification.
3. Task utility: correctness, tool-call accuracy, completion rate, clarification turns and regression rate.
4. Economics: input/output tokens, latency and total model/tool cost.
5. Reproduction: frozen corpus, configuration, raw results, hashes and statistical methodology.

The repository currently establishes level 1 and the serialization/fidelity portions of level 5. Levels 2-4 remain required before making strong claims that AER improves AI outcomes.

## Release rule

Do not call a version "production ready" because the benchmark is favorable. Release readiness requires the compatibility, safety, determinism, packaging and verification gates to pass together. For AI-specific claims, publish model-backed paired evidence separately from serialization readiness.
