# AER Roadmap

## Phase 1: specification

- Freeze AER 1.0 canonical model and grammar.
- Define canonicalization and hashing.
- Freeze AER-B wire format.
- Publish conformance vectors.

Status: substantially implemented. Continue expanding frozen vectors and independent implementations.

## Phase 2: proof

- Implement executable benchmarks.
- Compare JSON, Protobuf, MessagePack, CBOR, YAML, XML, CSV, Markdown and TOON.
- Measure bytes, compressed bytes, latency, allocations, memory and exact LLM tokens.
- Publish reproducible benchmark results.

Status: executable multi-workload benchmarks and exact `o200k_base` token measurement are now CI-gated. Next priority is broader tokenizer coverage and direct TOON comparison.

## Phase 3: adoption

- Publish the .NET package.
- Add AER CLI.
- Add ASP.NET Core integration.
- Add Python and TypeScript implementations.
- Add playground/converter tooling.

Status: .NET, CLI, ASP.NET Core and cross-language implementations exist. Continue package polish and consumer examples.

## Phase 4: AI ecosystem

- Add MCP integration.
- Add agent/tool/RAG examples.
- Add AI benchmark corpus.
- Support content negotiation and profile selection.
- Add deterministic orchestrator context planning.

Status: MCP negotiation and payload APIs plus a standalone orchestrator planning layer are implemented. Next priority is a real MCP server/client interoperability example and model-backed paired evaluation.

## Phase 5: enterprise

- Add Go and Rust implementations.
- Add Kafka/event integration.
- Add Redis/cache helpers.
- Add observability and security hardening.
- Add compatibility and migration tooling.

Status: cross-language parity is in progress. Prioritize independent conformance, fuzzing and production migration tooling before adding many integrations.

## Phase 6: ecosystem

- AEP governance.
- Independent implementations.
- Conformance certification.
- Versioned releases and long-term compatibility policy.
- External reproduction of AI benchmark claims.

The key milestone before 1.0 is not another feature. It is independent conformance plus reproducible evidence that AER-A improves or preserves real AI task outcomes at acceptable cost.
