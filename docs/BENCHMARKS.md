# AER Benchmark Plan

AER claims should be evidence-driven. Run all comparisons on identical logical payloads and record both raw and compressed sizes.

## Format comparison

AER is not intended to replace every existing format. Its design target is the intersection of human readability, compact structured data, strong typing, deterministic parsing, AI context efficiency, adaptive representation and a path to binary transport.

| Capability | JSON | Protobuf | MessagePack | CBOR | XML | YAML | CSV | Markdown | TOON | AER |
|---|---|---|---|---|---|---|---|---|---|---|
| Human-readable source | Yes | Low | Low | Low | Yes | Yes | Yes | Yes | Yes | Yes |
| Compact text representation | Medium | N/A | N/A | N/A | Low | Medium | High | Low | High | Target: High |
| Compact binary representation | No | Yes | Yes | Yes | No | No | No | No | No | Yes |
| Strong native typing | Limited | Yes | Limited | Yes | Limited | Limited | No | No | Limited | Yes |
| Schema optional | Yes | No, schema normally required | Yes | Yes | Yes | Yes | No | No | Yes | Yes |
| Repeated-record table form | No | Schema-based | No | No | No | No | Yes | No | Yes | Yes |
| Native references | Convention | Convention | Convention | Convention | Yes | Convention | No | No | Limited | Yes |
| Semantic metadata | Limited | Limited | Limited | Tags | Attributes | Limited | No | Text | Limited | Native/optional |
| Deterministic grammar | Yes | Yes | Yes | Yes | Yes | No | Yes | No | Yes | Yes |
| Streaming friendly | Yes | Yes | Yes | Yes | Yes | Partial | Yes | No | Yes | Yes |
| AI-focused profile | No | No | No | No | No | No | No | No | Yes | Yes |
| Adaptive representation | No | No | No | No | No | No | No | No | Limited | Yes |
| Human + AI + binary from one model | No | Partial | Partial | Partial | No | No | No | No | Text-focused | Yes |

### JSON vs AER

JSON is an excellent general-purpose interchange format with a huge ecosystem. AER's advantage is not that JSON is inadequate; it is that JSON repeats structural syntax for every record and has a relatively small native type system.

Example JSON:

```json
{
  "employees": [
    {"id":101,"name":"Amit","role":"Engineer","level":4},
    {"id":102,"name":"Priya","role":"Manager","level":5},
    {"id":103,"name":"Rahul","role":"Engineer","level":3}
  ]
}
```

Equivalent AER:

```text
employees[3]{id,name,role,level}:
  101,Amit,Engineer,4
  102,Priya,Manager,5
  103,Rahul,Engineer,3
```

AER advantages for this workload:

- Field names are declared once for the repeated record set.
- The shape is explicit to the parser and easy to inspect.
- The same logical value can be emitted as AER Text, AER AI or AER Binary.
- Schema, units, ranges and semantic meaning can be attached without changing the core data model.
- An optimizer can choose table form automatically when repetition makes it beneficial.

Trade-off: JSON has broader tooling and interoperability today. AER needs adapters and a mature ecosystem before it can be a practical default everywhere.

### Protobuf vs AER

Protobuf remains a strong choice for high-performance, schema-driven binary RPC. AER is designed for a broader boundary: humans, AI systems, APIs and binary services can share one canonical representation.

| Area | Protobuf | AER |
|---|---|---|
| Primary strength | Efficient schema-driven binary serialization | One canonical model across human, AI and binary forms |
| Contract model | `.proto` schema | Optional AER Schema |
| Human editing | Poor | Strong in AER Text |
| Binary wire format | Strong | AER-B |
| LLM prompt/tool representation | Requires another representation | AER-AI is native |
| Adaptive table representation | Not a core concept | Core optimizer capability |
| Semantic units/meaning | Usually external conventions | Schema metadata can carry them |
| Legacy JSON coexistence | Common via generated gateways | Explicit JSON adapter is part of design |
| Best fit | Stable internal RPC contracts | API/AI/MCP boundaries and mixed human/machine workflows |

AER should not claim to beat Protobuf on binary size or throughput until measured. The intended advantage is architectural: avoid maintaining separate JSON, AI-text and binary data models for the same payload.

### MessagePack and CBOR vs AER

MessagePack and CBOR are compact binary formats. They are strong when binary size and serialization efficiency matter, but their normal wire representation is not meant to be edited by humans or placed directly into an LLM prompt.

AER's differentiator is the multi-profile approach:

```text
Canonical AER model
      |
      +--> AER Text  -> humans/debugging/configuration
      +--> AER AI    -> LLM context/MCP payloads
      +--> AER Binary-> service-to-service transport
```

### XML vs AER

XML provides mature schemas, namespaces, mixed content and extensive enterprise tooling. AER intentionally chooses a much smaller structural language for data interchange.

AER's target advantages are lower structural overhead, simpler parsing, easier human editing and a direct AI/binary path. XML remains preferable where XML-specific ecosystem features are a hard requirement.

### YAML vs AER

YAML is pleasant for configuration but has a broad and complex language surface. AER intentionally keeps the core grammar deterministic and data-focused. AER also treats table encoding and binary conversion as first-class concerns.

### CSV vs AER

CSV is extremely compact for flat rectangular data, but has weak typing, no native nested structure and no standard representation for references or semantic metadata. AER's table mode targets the same repeated-record efficiency while retaining typed and nested values.

AER should not try to beat CSV for the simplest possible flat table where CSV already fits perfectly.

### Markdown vs AER

Markdown is excellent for prose and human documentation, but it is not a strict data serialization format. AER is intended for data that must be parsed deterministically and transported between systems while remaining readable.

### TOON vs AER

TOON is a highly relevant comparison for LLM-oriented structured data because it also uses compact tabular representations and field declarations. AER's broader goal is to retain the same kind of compact text option while adding typed values, optional schemas, semantic metadata, references, adaptive optimization and a binary profile under one canonical model.

Do not treat token savings over TOON as established until the same benchmark corpus and tokenizer are used. TOON is the format AER should benchmark particularly carefully for LLM workloads.

## AER advantages in one view

AER is designed around seven differentiators:

1. **One canonical model, multiple representations.** The application data model does not change when switching between human-readable, AI-oriented and binary forms.
2. **Adaptive structure.** Repetitive object arrays can be promoted to compact tables automatically instead of forcing every caller to hand-design the compact form.
3. **Optional schema with semantic information.** Types, required fields, ranges, units and meaning can travel with the data when required and disappear from hot paths when not required.
4. **AI-native profile.** AER-AI can optimize structure for LLM context while keeping the same semantic model used by the service.
5. **Binary escape hatch.** AER-B provides a typed binary representation when human readability is no longer the main requirement.
6. **MCP/API fit.** AER can sit at tool/resource and API boundaries without forcing an all-at-once migration away from JSON.
7. **Lossless optimization.** Optimization changes representation, not business meaning.

## Where AER should win

The highest-value target workloads are expected to be:

- MCP tool responses with repeated records.
- LLM retrieval context containing structured business data.
- API responses with large repetitive object arrays.
- Configuration that needs both human editing and strong validation.
- Event payloads that need text fixtures plus binary service transport.
- Systems currently maintaining separate DTO/JSON/LLM/binary formatting layers.

## Where established formats may remain better

AER should not force itself into workloads where an existing format is already the clear fit:

- Protobuf for mature schema-first binary RPC ecosystems.
- MessagePack/CBOR where standardized compact binary encoding is the primary concern.
- CSV for simple flat tabular interchange.
- XML for XML-native enterprise ecosystems and document-oriented features.
- YAML for teams that specifically need YAML's configuration ecosystem.
- Markdown for prose and documentation.

## Formats to benchmark

- JSON compact
- JSON pretty
- Protobuf
- MessagePack
- CBOR
- YAML
- XML where applicable
- CSV where applicable
- TOON
- AER Text
- AER AI
- AER Binary

## Workloads

1. Small object: 5-20 fields.
2. Nested API response.
3. Uniform 100-row table.
4. Uniform 10,000-row table.
5. Highly repetitive telemetry.
6. Heterogeneous configuration.
7. MCP tool response.
8. LLM retrieval context.
9. Patch/update stream.
10. Large binary-containing record.

## Metrics

```text
raw bytes
compressed bytes
encode ns/op
decode ns/op
allocations/op
peak memory
records/sec
LLM token count
LLM output validity
schema validation ns/op
```

## Fairness rules

- Same semantic data.
- Same compression setting when compression is compared.
- Release builds.
- Warmup before timing.
- Multiple iterations and confidence intervals.
- Separate cold-start and steady-state results.
- Never compare a schema-bearing format to a schema-free format without reporting the difference.
- Use the same tokenizer and tokenization settings for LLM comparisons.
- Report schema bytes separately when a schema is transmitted rather than assumed to exist out-of-band.
- Report both optimized and non-optimized AER results.

## CI gates

The benchmark suite should fail a release when AER regresses beyond agreed thresholds for throughput, allocation count or representation size.

The initial repository does not publish benchmark numbers. Numbers should be generated from the executable benchmark suite rather than estimated in documentation.
