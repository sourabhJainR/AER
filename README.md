# AER

Adaptive Efficient Representation

AER is a compact, deterministic, human-readable and AI-oriented data representation system for APIs, services, LLM context, MCP tools, configuration, events and high-volume data transfer.

AER is designed as one logical model with multiple physical representations:

- AER Text: readable source and API/debug format.
- AER AI: token-aware text profile for LLM prompts and tool responses.
- AER Binary: compact typed wire format for high-throughput service-to-service traffic.
- AER Schema: optional types, constraints, units and semantic meaning.
- AER Optimizer: automatically converts repetitive structures into compact tables and other efficient forms.
- AER Patch: incremental state changes for synchronization.
- AER MCP: integration profile for Model Context Protocol tools/resources.

The repository is Apache-2.0 licensed.

## Design goals

AER is optimized for five things at the same time:

1. Low representation overhead.
2. Easy visual inspection and editing.
3. Deterministic parsing and validation.
4. Efficient LLM token usage.
5. A clean path from human-readable data to binary transport.

AER is not intended to claim that every workload is smaller or faster than every competing format. Performance must be established with representative benchmarks. The project therefore treats JSON, MessagePack, Protobuf, CBOR, YAML, XML and TOON as comparison points rather than assumptions.

## Quick start

The current reference implementation targets .NET 9.

```csharp
using Aer;

var data = new Dictionary<string, object?>
{
    ["name"] = "Sourabh",
    ["age"] = 45,
    ["active"] = true,
    ["skills"] = new[] { "AI", "C#", ".NET" }
};

var text = AER.Serialize(data);
Console.WriteLine(text);

var roundTrip = AER.Deserialize(text);
var binary = AER.ToBinary(data);
var fromBinary = AER.FromBinary(binary);
```

Typical result:

```text
name:Sourabh
age:45
active:true
skills[3]:AI,C#,.NET
```

## Core syntax

### Scalars

```text
name:Sourabh
age:45
active:true
score:97.5
note:"hello, world"
empty:-
```

`-` is the canonical null marker in text mode.

### Nested objects

```text
user:
  id:42
  name:Sourabh
  active:true
  address:
    city:Hyderabad
    country:India
```

### Arrays

Uniform scalar arrays use a declared length:

```text
tags[5]:AI,ML,RAG,LLM,MCP
```

### Tables

Uniform records become a table. Field names are declared once:

```text
employees[3]{id,name,role,level}:
  101,Amit,Engineer,4
  102,Priya,Manager,5
  103,Rahul,Engineer,3
```

This is the primary compact form for large repetitive record sets.

### References

```text
company:
  id:C1
  name:Planful

employees[2]{id,name,company}:
  E1,Sourabh,@C1
  E2,Priya,@C1
```

### Typed literals

```text
created:dt"2026-08-30T09:00:00+05:30"
latency:dur"00:00:00.042"
payload:b64"SGVsbG8="
```

### Directives

```text
@aer 1
@encoding utf8
@null -
```

Directives are metadata and do not change the canonical data model.

## Schema

AER schemas are optional. Use them when a service needs contracts, validation or semantic hints.

```text
@schema User
  id:int required
  name:str required
  age:int min=0 max=150
  confidence:float min=0 max=1
```

Equivalent .NET model:

```csharp
var schema = new AerSchema("User", new Dictionary<string, AerField>
{
    ["id"] = new("id", AerTypeKind.Int, Required: true),
    ["name"] = new("name", AerTypeKind.String, Required: true),
    ["age"] = new("age", AerTypeKind.Int, Min: 0, Max: 150),
    ["confidence"] = new("confidence", AerTypeKind.Float, Min: 0, Max: 1)
});
```

Schemas can carry:

- type
- required/optional
- minimum and maximum
- unit
- human/AI meaning
- table column definitions
- future enum/pattern/reference constraints

## Adaptive optimizer

AER does not require callers to decide how data should be compacted.

The optimizer recursively evaluates the canonical model:

```text
object
  -> nested object when irregular
  -> array when values are heterogeneous
  -> table when objects share columns
  -> scalar vector when values are uniform
```

Example input model:

```text
employees:
  - {id:101,name:Amit,role:Engineer}
  - {id:102,name:Priya,role:Manager}
  - {id:103,name:Rahul,role:Engineer}
```

Optimized AER:

```text
employees[3]{id,name,role}:
  101,Amit,Engineer
  102,Priya,Manager
  103,Rahul,Engineer
```

The optimizer is lossless: values are preserved; only representation changes.

## AI profile

AER-AI is a profile, not a second data model. The same canonical value is emitted using the most token-efficient valid AER text form.

Example:

```text
@schema Stock{symbol:str,price:decimal,change:float,volume:int}

status:success
timestamp:dt"2026-08-30T09:00:00+05:30"
stocks[2]@Stock:
  NVDA,178.42,2.31,54321000
  AMD,247.18,-1.42,32100000
```

AI metadata can be supplied only where needed:

```text
confidence:0.92 @range[0,1] @meaning="forecast confidence"
revenue:125000000 @currency=USD
```

Recommended policy:

- Keep core tool responses schema-light for small objects.
- Use schema declarations for repeated records.
- Use tables for repetitive tool output.
- Use references for repeated entities.
- Keep verbose semantic annotations outside hot paths unless the model benefits from them.

## Binary codec

AER-B is a deterministic tagged binary representation of the same canonical model.

Binary structure:

```text
magic: AERB
version: 1
value: tagged recursive payload
```

Supported value kinds include null, boolean, integer, floating point, decimal, string, bytes, date/time, duration, array, object, table and reference.

```csharp
byte[] payload = AerBinary.Encode(value);
AerValue value2 = AerBinary.Decode(payload);
```

Binary mode is intended for service-to-service traffic where bytes, allocation rate and parser cost matter more than source readability.

## MCP integration

AER fits naturally at the MCP tool/resource boundary without requiring an all-at-once migration.

### Existing MCP setup

Keep your existing JSON MCP contract and add an AER response profile:

```text
MCP Client
   |
   | JSON request
   v
MCP Server
   |
   | canonical domain object
   v
AER adapter
   |
   +--> JSON for legacy clients
   +--> AER-AI for LLM clients
   +--> AER-B for binary-aware clients
```

A practical negotiation field can be:

```text
aer.profile = text | ai | binary
```

or an HTTP/MCP transport capability such as:

```text
Accept: application/aer; profile=ai
```

Do not force every MCP client to understand AER on day one. The server can expose JSON as the compatibility default and AER as an opt-in capability.

### New MCP setup

For an AER-first server:

```text
Tool implementation
      |
      v
Canonical AER value
      |
      +---- validator
      +---- optimizer
      +---- AI adapter
      +---- binary codec
      |
      v
MCP transport adapter
```

Typical tool response:

```text
@aer 1
status:success
result[3]{id,name,status}:
  1,Amit,ready
  2,Priya,busy
  3,Rahul,ready
```

The server can still expose a JSON translation for clients that do not advertise AER.

### MCP deployment modes

1. Compatibility mode: existing JSON MCP server plus AER adapter.
2. Dual mode: JSON and AER available per request/capability.
3. AER-native: canonical data and all internal tool boundaries use AER; JSON is an edge translation only.
4. Binary transport: AER-B between trusted services, AER-A at the LLM boundary.

## Infrastructure integration

### API gateways

Add AER at the edge rather than rewriting business services:

```text
Client -> Gateway -> existing API -> domain model
                  |
                  +-> AER encoder/decoder
```

This provides a low-risk rollout.

### .NET services

Recommended package split:

```text
Aer.Core
Aer.AspNetCore
Aer.Mcp
Aer.Json
Aer.Benchmarks
```

`Aer.Core` should contain the canonical model, schema, parser, writer, optimizer and binary codec. Framework adapters should remain separate.

### Event streaming

AER-B is suited to events where producers and consumers already agree on a schema. AER text is useful for diagnostics, replay fixtures and event snapshots.

### Caches

Use canonical hashing above the canonical model so the same data has the same cache key regardless of JSON/AER text formatting.

### Databases

Do not store AER as a replacement for relational or analytical storage. Store the domain data in the database and use AER at the API, event, cache and AI boundaries where representation overhead matters.

## Migration strategy

A recommended staged rollout:

### Stage 0: benchmark

Create representative payloads from real systems and compare:

- JSON compact
- JSON pretty
- Protobuf
- MessagePack
- CBOR
- YAML
- XML where relevant
- TOON for LLM-oriented structured data
- AER text
- AER AI
- AER binary

Measure:

- bytes on wire
- compressed bytes
- encode time
- decode time
- allocations
- peak memory
- LLM token count
- generated-token error rate
- schema validation cost

### Stage 1: read-only AER

Expose AER on a small set of GET/tool responses. Keep JSON as default.

### Stage 2: internal boundaries

Use AER inside new services, MCP tool results and AI orchestration boundaries.

### Stage 3: binary service traffic

Adopt AER-B between high-volume services with stable schemas.

### Stage 4: AER-native contracts

Move selected high-volume or AI-heavy interfaces to AER-first contracts and keep compatibility adapters for older clients.

## Backward compatibility

AER should coexist with JSON rather than requiring a flag day migration.

Recommended conversion boundary:

```text
JSON <-> Canonical AER Model <-> AER Text
                                  |
                                  +-> AER AI
                                  +-> AER Binary
```

This avoids multiple independent parsers and prevents format-specific business logic.

## Security and robustness

Production deployments should enforce limits at the parser boundary:

- maximum document bytes
- maximum nesting depth
- maximum table rows
- maximum columns
- maximum string length
- maximum decoded bytes
- numeric range checks
- schema validation where required
- cancellation/timeouts for streaming parsers

AER parsers must not execute code, evaluate expressions or resolve remote references during normal decoding.

MCP deployments should treat tool payloads as untrusted input even when AER is used. Validation belongs before domain actions.

## Error model

Use stable machine-readable error categories in framework adapters:

```text
AER001 Invalid header
AER002 Invalid syntax
AER003 Invalid scalar
AER004 Invalid table shape
AER005 Schema violation
AER006 Document too large
AER007 Nesting depth exceeded
AER008 Binary payload truncated
AER009 Unsupported version
```

Application layers should map parser errors to their own transport error contract rather than leaking parser internals.

## Project structure

```text
AER/
├── src/
│   └── Aer.Core/
│       ├── Aer.cs
│       ├── AerValue.cs
│       ├── AerDocument.cs
│       ├── AerSchema.cs
│       ├── AerParser.cs
│       ├── AerWriter.cs
│       ├── AerOptimizer.cs
│       ├── AerBinary.cs
│       └── AerAiAdapter.cs
├── docs/
│   ├── SPECIFICATION.md
│   ├── ARCHITECTURE.md
│   ├── MCP.md
│   ├── INTEGRATION.md
│   └── BENCHMARKS.md
├── examples/
│   ├── basic.aer
│   ├── nested.aer
│   ├── table.aer
│   ├── ai.aer
│   └── patch.aer
├── tests/
└── LICENSE
```

## Example comparison

JSON:

```json
{
  "employees": [
    {"id":101,"name":"Amit","role":"Engineer","level":4},
    {"id":102,"name":"Priya","role":"Manager","level":5},
    {"id":103,"name":"Rahul","role":"Engineer","level":3}
  ]
}
```

AER:

```text
employees[3]{id,name,role,level}:
  101,Amit,Engineer,4
  102,Priya,Manager,5
  103,Rahul,Engineer,3
```

AER's advantage here comes from removing repeated structural syntax while keeping the data obvious to a human and explicit to a parser.

## What AER is and is not

AER is:

- a data representation
- an optional schema system
- an adaptive optimizer
- a text parser/writer
- a typed binary codec
- an AI-facing profile
- an integration pattern for MCP and APIs

AER is not:

- a database
- a query language in the core specification
- a replacement for every serialization format
- a claim that binary AER always beats Protobuf or that AI AER always beats every token encoding

## Roadmap to production maturity

The current repository establishes the core reference direction. The next production milestones are:

1. Freeze the AER 1.0 grammar and canonical model.
2. Add exhaustive parser/fuzzer/property tests.
3. Add configurable parser limits and cancellation support.
4. Add a canonical hashing specification.
5. Add enum/pattern/schema references and table typing.
6. Add AER patch format and streaming frames.
7. Add AER JSON/YAML/CSV import/export adapters.
8. Add ASP.NET Core formatters and content negotiation.
9. Add MCP SDK integration and capability negotiation.
10. Add Python, TypeScript and Go implementations from the same conformance suite.
11. Add benchmark corpus and CI regression thresholds.
12. Publish a versioned specification and conformance test suite.

## License

Apache License 2.0. See [LICENSE](LICENSE).
