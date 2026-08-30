# AER Production Architecture

## System shape

```text
                     +------------------+
                     | Application data |
                     +--------+---------+
                              |
                              v
                    +-------------------+
                    | AER canonical IR  |
                    +----+---------+----+
                         |         |
                 +-------+         +--------+
                 v                          v
        +----------------+          +----------------+
        | Schema/Validate|          | Adaptive       |
        | constraints    |          | Optimizer      |
        +-------+--------+          +-------+--------+
                |                           |
                +------------+--------------+
                             v
                    +-------------------+
                    | Profile selection |
                    +---+-------+---+---+
                        |       |   |
                        v       v   v
                      Text      AI  Binary
                        |       |   |
                        +---+---+---+
                            |
                            v
                         Transport
```

## Module boundaries

### Aer.Core

Owns the canonical value tree, schema model, parser, writer, optimizer, binary codec and AI adapter. It must not depend on ASP.NET Core, MCP SDKs or application domain libraries.

### Aer.AspNetCore

Provides content negotiation, input/output formatters, streaming and typed model binding for ASP.NET Core. This should be a separate package so non-Web applications do not carry Web dependencies.

### Aer.Mcp

Provides MCP result/resource adapters and capability negotiation. Tool implementations should produce the canonical AER value first and let the adapter choose JSON, AER-A or AER-B.

### Aer.Json

Bidirectional JSON conversion. JSON compatibility is an edge concern, not the canonical model.

### Aer.Benchmarks

Contains stable representative corpora and regression thresholds. Benchmarks must run against release builds and record allocation counts, bytes, throughput and latency.

## Threading and allocations

A production implementation should prefer span-based parsing, pooled buffers and streaming writers for large payloads. Public APIs should avoid global mutable state. Schema objects should be immutable and reusable across requests.

## DoS controls

Make limits explicit in `AerReaderOptions` and `AerBinaryOptions`:

```text
MaxBytes
MaxDepth
MaxStringLength
MaxArrayItems
MaxTableRows
MaxTableColumns
MaxFieldCount
```

The parser should reject input before allocating proportional memory where possible.

## Trust boundaries

Treat all inbound AER data as untrusted. Parsing is data-only. References are identifiers and must not trigger external lookups. Semantic metadata is not executable.

## Canonical hashing

The long-term design should canonicalize into a stable byte sequence and hash that sequence with SHA-256 or another explicitly selected cryptographic algorithm. Formatting differences must not change the semantic hash.

## Observability

Framework adapters should expose counters for:

- aer_encode_total
- aer_decode_total
- aer_encode_bytes
- aer_decode_bytes
- aer_validation_failures
- aer_parse_failures
- aer_optimizer_promotions
- aer_payload_tokens_estimate
- aer_payload_duration

Do not log sensitive payloads by default.

## Deployment patterns

### Existing infrastructure

```text
                existing services
                      |
        +-------------+-------------+
        |                           |
   JSON edge                    AER adapter
        |                           |
      clients                  AI/MCP clients
```

### New platform

```text
Domain -> AER IR -> validation -> optimization -> profile -> transport
                                  |
                                  +-> audit/metrics
```

## Backward compatibility

Use adapters rather than parallel domain serializers. Keep JSON at public boundaries until client support is established. AER can be introduced per endpoint, service, event type or MCP tool.

## Production checklist

Before calling an implementation production-ready, require:

- parser property tests
- malformed-input fuzzing
- golden cross-version fixtures
- round-trip tests for every type
- resource-limit tests
- binary truncation tests
- benchmark regression gates
- compatibility tests against JSON and Protobuf where relevant
- MCP interoperability tests
- package signing and provenance
- semantic versioning policy
