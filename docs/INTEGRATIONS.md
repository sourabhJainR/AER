# AER Integration Catalog

AER should be introduced at representation boundaries so existing domain code remains unchanged.

| Boundary | Recommended profile | Why |
|---|---|---|
| Browser/debug API | AER-H | Readable and compact |
| LLM context | AER-AI | Repeated structures are explicit and token-aware |
| MCP tool response | AER-AI | Compact structured tool data |
| Trusted service-to-service | AER-B | Typed binary transport |
| Kafka/event stream | AER-B | Stable schemas and compact events |
| Config/replay fixtures | AER-H | Human editable |
| Legacy consumers | JSON adapter | Zero-disruption migration |
| gRPC/protobuf estate | Keep Protobuf where it wins | Avoid unnecessary rewrites |

## Principle

AER is a representation layer, not a requirement to replace every transport or persistence technology.
