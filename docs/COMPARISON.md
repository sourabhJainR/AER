# AER Comparison Matrix

AER is designed for a specific gap: one logical representation that can serve humans, AI systems and machine-to-machine workloads. It should complement mature formats rather than claim universal superiority.

## Capability comparison

| Capability | JSON | Protobuf | MessagePack | CBOR | XML | YAML | CSV | Markdown | TOON | AER |
|---|---|---|---|---|---|---|---|---|---|---|
| Human readable | Yes | Low | Low | Low | Medium | Yes | Yes | Yes | Yes | Yes |
| Dynamic/no schema required | Yes | No | Yes | Yes | Yes | Yes | Yes | Yes | Yes | Yes |
| Strong native typing | Limited | Yes | Moderate | Moderate | Moderate | Moderate | No | No | Low | Yes |
| Repetitive-record compaction | No | Yes | Partial | Partial | No | Partial | Yes | No | Yes | Yes |
| Explicit table representation | No | Partial | No | No | No | No | Yes | No | Yes | Yes |
| Binary encoding | No | Yes | Yes | Yes | No | No | No | No | No | Yes |
| AI-specific profile | No | No | No | No | No | No | No | No | Yes | Yes |
| Optional semantic metadata | Partial | Partial | Partial | Partial | Attributes | Partial | No | Text | Limited | Yes |
| Adaptive representation | No | Schema/codegen driven | No | No | No | No | No | No | Limited | Yes |
| Incremental patch model | External | External | External | External | External | External | External | External | External | Native profile planned |
| Canonical model across text/binary/AI | No | Binary-centric | Binary-centric | Binary-centric | Text-centric | Text-centric | Table-centric | Text-centric | Text-centric | Yes |
| MCP integration profile | External | External | External | External | External | External | External | External | External | Yes |

## JSON vs AER

JSON is an excellent general-purpose interchange format. AER's intended advantage is structural efficiency for repeated data and the ability to use the same canonical model for human text, AI text and binary transport.

### JSON

```json
{
  "employees": [
    {"id":101,"name":"Amit","role":"Engineer","level":4},
    {"id":102,"name":"Priya","role":"Manager","level":5},
    {"id":103,"name":"Rahul","role":"Engineer","level":3}
  ]
}
```

### AER

```text
employees[3]{id,name,role,level}:
  101,Amit,Engineer,4
  102,Priya,Manager,5
  103,Rahul,Engineer,3
```

AER removes repeated object keys and punctuation from repeated records while retaining the schema directly beside the data.

## Protobuf vs AER

Protobuf remains a strong choice for stable, typed, high-throughput service contracts with generated code. AER adds value where schema flexibility, human inspection, AI context and a single text/binary model matter.

| Area | Protobuf | AER |
|---|---|---|
| Wire efficiency | Excellent | Target: high |
| Generated code | Excellent | Optional |
| Human inspection | Poor | Excellent in AER-H |
| AI prompt representation | Not designed for it | Native AER-AI profile |
| Schema evolution | Excellent | Versioned schema model |
| Dynamic structures | Less convenient | Native |
| Repetitive tables | Binary efficient | Explicit table form + optimizer |
| Browser/debug friendliness | Low | High |
| Binary/text same model | No | Yes |
| Migration from JSON | Requires schema/codegen | Direct canonical conversion |

AER should not claim to beat Protobuf on bytes or latency until the benchmark suite demonstrates it on equivalent schemas and workloads.

## MessagePack and CBOR vs AER

MessagePack and CBOR are strong binary encodings. AER's distinct value is the unified representation model plus human and AI profiles. AER-B is intended to provide a comparable binary path while AER-H and AER-A solve use cases binary formats do not target directly.

## XML vs AER

XML is expressive and mature, particularly for document-oriented ecosystems and namespace-heavy contracts. AER deliberately avoids verbose angle-bracket structure and focuses on compact structured data, tables and machine-oriented interchange.

## YAML vs AER

YAML is convenient for configuration and human-edited documents, but AER intentionally uses a narrower deterministic grammar. AER prioritizes predictable parsing and stable canonicalization over YAML's broader authoring flexibility.

## CSV vs AER

CSV is excellent for rectangular tables but weak for nested objects, typing, references and mixed structures. AER includes an explicit table representation while retaining nested objects and typed values.

## Markdown vs AER

Markdown is a presentation/document format, not a deterministic data interchange format. AER borrows the useful human readability property but keeps a strict data grammar.

## TOON vs AER

TOON is an important comparison point for LLM-oriented tabular data. AER adopts the useful idea of declaring repetitive record fields once, then extends the model with optional schema metadata, richer types, references, binary encoding and infrastructure integration.

AER should be measured against TOON with the same tokenizer, datasets and prompt construction. No fixed percentage advantage should be claimed without benchmark evidence.

## The AER advantage

The intended AER value proposition is the combination:

```text
                One canonical model
                        |
        +---------------+---------------+
        |               |               |
      AER-H           AER-A           AER-B
     humans            AI            machines
        |               |               |
        +---------------+---------------+
                        |
                    AER Schema
                        |
                   AER Optimizer
```

This allows an application to keep its domain model stable while selecting the best representation for the consumer.

## What AER should win on

1. Developer readability compared with binary-first protocols.
2. Compact repetitive structured text compared with ordinary JSON/YAML/XML.
3. AI-oriented representation compared with general-purpose serialization formats.
4. Easy JSON migration because both map to the canonical object/array/value model.
5. A single architecture spanning text, AI and binary representations.
6. Adaptive table/vector representation without forcing the application developer to hand-optimize every payload.
7. Optional semantic metadata for units, constraints and field meaning.

## What AER should not promise without evidence

- Always smaller than Protobuf.
- Always faster than MessagePack.
- Always fewer tokens than TOON for every model/tokenizer.
- Universal replacement for JSON.
- Universal replacement for XML or YAML in document/configuration ecosystems.

Those are benchmark questions, not marketing assumptions.
