# AER Adoption Guide

AER is easiest to adopt incrementally. Keep the existing application/domain model and insert AER at representation boundaries.

## 1. Existing JSON API

```text
Controller -> domain object -> AER adapter -> application/aer
                           \-> JSON compatibility
```

Start with read-only endpoints. Keep JSON as the default until clients advertise AER.

## 2. ASP.NET Core

The repository contains `Aer.AspNetCore`.

```csharp
app.MapGet("/employees", () => AerResults.Text(repository.GetEmployees()));
app.MapGet("/ai/employees", () => AerResults.Ai(repository.GetEmployees()));
```

For mature production integration, add content negotiation so the selected profile is driven by the request `Accept` header.

## 3. MCP

Use `Aer.Mcp` at the tool/resource response boundary.

```text
MCP tool -> domain result -> AER profile selector -> client
                                      |
                         +------------+------------+
                         |            |            |
                        JSON        AER-AI       AER-B
```

Recommended default:

- JSON for legacy clients.
- AER-AI where the immediate consumer is an LLM/agent.
- AER-B for trusted high-throughput binary-aware services.

## 4. AI and RAG

Use AER-AI for structured retrieval results, tool outputs and intermediate agent state where repeated fields create token overhead.

```text
retriever -> domain records -> AER optimizer -> AER-AI -> model context
```

Keep semantic metadata selective. Add meaning, units or constraints when they improve model interpretation.

## 5. Events and Kafka

Do not rewrite the event platform. Add AER at the producer/consumer serialization boundary.

```text
Producer -> domain event -> AER-B -> Kafka -> AER-B -> Consumer
                                   |
                                   +-> AER text for replay/debug fixtures
```

Use schema versions in the event envelope and reject incompatible versions explicitly.

## 6. Redis and caches

Hash canonical AER values instead of serialized text so formatting changes do not invalidate cache keys. AER is the representation layer, not the cache itself.

## 7. gRPC and service-to-service

Keep gRPC/Protobuf when generated contracts and established tooling are the priority. Use AER-B for selected dynamic or AI-oriented internal traffic where one model must also be inspectable as AER text or consumed by AI tooling.

## 8. Migration stages

### Stage 0: observe

Measure existing payloads.

### Stage 1: dual output

Expose JSON and AER side by side.

### Stage 2: AI/MCP

Use AER-AI for new agent and tool flows.

### Stage 3: selected internal services

Use AER-B where benchmark results justify it.

### Stage 4: AER-first contracts

Adopt AER for new interfaces where its combined text/AI/binary model has a clear operational benefit.

## 9. Rollback

Every adoption should have a format switch or gateway configuration that returns traffic to JSON/Protobuf without changing business logic.

## 10. Adoption checklist

- Benchmark the real payload.
- Verify semantic round-trip.
- Configure parser/resource limits.
- Add conformance tests.
- Add observability for profile/size/latency.
- Maintain a JSON fallback.
- Roll out to a small traffic percentage.
- Compare error/latency/token metrics before expanding.
