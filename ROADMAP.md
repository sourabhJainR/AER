# AER Roadmap

## Phase 1: specification

- Freeze AER 1.0 canonical model and grammar.
- Define canonicalization and hashing.
- Freeze AER-B wire format.
- Publish conformance vectors.

## Phase 2: proof

- Implement executable benchmarks.
- Compare JSON, Protobuf, MessagePack, CBOR, YAML, XML, CSV, Markdown and TOON.
- Measure bytes, compressed bytes, latency, allocations, memory and LLM tokens.
- Publish reproducible benchmark results.

## Phase 3: adoption

- Publish the .NET package.
- Add AER CLI.
- Add ASP.NET Core integration.
- Add Python and TypeScript implementations.
- Add playground/converter tooling.

## Phase 4: AI ecosystem

- Add MCP SDK integration.
- Add agent/tool/RAG examples.
- Add AI benchmark corpus.
- Support content negotiation and profile selection.
- Add the AER Agent profile for typed tool calls/results, lifecycle events, checkpoints and context projection.
- Add AER Agent conformance vectors for lifecycle, correlation, retry, replay and compaction semantics.
- Add JSON interoperability vectors for Agent frames.
- Add Python, TypeScript and Go Agent Profile parity against the same vectors.
- Benchmark AER Agent context against JSON and other agent-context representations on real coding tasks.
- Measure task success, tool-call validity, context tokens, latency and cost; do not use character reduction as a proxy for AI effectiveness.

## Phase 5: enterprise

- Add Go and Rust implementations.
- Add Kafka/event integration.
- Add Redis/cache helpers.
- Add observability and security hardening.
- Add compatibility and migration tooling.

## Phase 6: ecosystem

- AEP governance.
- Independent implementations.
- Conformance certification.
- Versioned releases and long-term compatibility policy.
