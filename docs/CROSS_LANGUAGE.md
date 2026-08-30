# AER Cross-Language Parity

AER maintains independent implementations for .NET, Python, TypeScript and Go. They share the same canonical value kinds and AER-B v1 wire contract.

## Parity matrix

| Capability | .NET | Python | TypeScript | Go |
|---|---|---|---|---|
| Scalars | Yes | Yes | Yes | Yes |
| Nested objects | Yes | Yes | Yes | Yes |
| Arrays | Yes | Yes | Yes | Yes |
| Tables | Yes | Yes | Yes | Yes |
| References | Yes | Yes | Yes | Yes |
| Typed date/time | Yes | Yes | Yes | Yes |
| Typed duration | Yes | Yes | Yes | Yes |
| AER-H text writer | Yes | Yes | Yes | Yes |
| AER-H text parser | Yes | Yes | Yes | Yes |
| Schema validation | Yes | Yes | Yes | Yes |
| Patch operations | Yes | Yes | Yes | Yes |
| AER-B v1 encode/decode | Yes | Yes | Yes | Yes |
| AERF streaming frames | Yes | Yes | Yes | Yes |
| Frozen binary vectors | Yes | Yes | Yes | Yes |
| Property tests | Yes | Yes | Yes | Yes |
| Native fuzz entry point | Planned/CI fuzz job | Property/fuzz harness | Property/fuzz harness | Go fuzz test |

## Wire contract

AER-B v1 is:

```text
4 bytes: AERB
1 byte : version 1
N bytes: tagged value payload
```

AERF frames are:

```text
4 bytes: AERF
1 byte : version 1
4 bytes: payload length (uint32 little-endian)
N bytes: one complete AER-B payload
```

## Testing strategy

Every implementation must pass frozen AER-B vectors, table/text round trips, schema validation, patch mutations, streaming frames, deterministic property tests, and malformed/truncated-input tests.

The authoritative frozen vectors live under `conformance/binary/v1.json`. A wire-format change requires a new versioned specification and vector set; vectors must never be silently regenerated in place.

## Release parity gate

Before an AER release is called interoperable, each implementation must be able to produce AER-B and decode payloads produced by the other implementations. The resulting canonical values must be semantically identical and known vectors must remain byte-for-byte stable.
