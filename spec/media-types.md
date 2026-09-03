# AER Media Types and File Extensions

## Canonical identifiers

| Representation | File extension | Media type | Purpose |
|---|---|---|---|
| AER Text | `.aer` | `application/aer` | Human-readable structured data |
| AER AI | `.aer` | `application/aer; profile="ai"` | LLM/context optimized representation |
| AER Binary | `.aerb` | `application/aer-binary` | Compact binary transport/storage |

AER AI is a profile of the `.aer` format, not a separate grammar or mandatory file extension. This keeps editor and repository integration simple.

## HTTP content negotiation

Clients may advertise support with:

```http
Accept: application/aer
```

or:

```http
Accept: application/aer; profile="ai"
```

Servers must continue to support JSON when requested or when the client does not advertise AER support:

```http
Accept: application/json
```

A server must not return AER solely because the request contains an unrelated `Accept` wildcard. Normal HTTP content negotiation rules apply.

## Compatibility

- `.aer` is UTF-8 text.
- `.aerb` is binary and must not be opened as text.
- The profile parameter selects representation behavior; it does not change semantic meaning.
- JSON remains the interoperability fallback.
- AER parsers must reject unsupported mandatory versions/profiles rather than silently interpreting them.
