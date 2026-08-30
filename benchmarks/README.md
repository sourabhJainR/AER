# AER Benchmark Lab

The benchmark lab is designed to compare identical logical payloads across representations.

## Required targets

- JSON compact
- Protobuf
- MessagePack
- CBOR
- YAML
- XML
- CSV where applicable
- TOON
- AER Text
- AER AI
- AER Binary

## Required metrics

```text
raw bytes
compressed bytes
encode time
decode time
allocations
peak memory
records/sec
LLM token count
```

## Dataset families

`small-object.json`, `nested-api.json`, `table-100.json`, `table-10000.json`, `telemetry-100000.json`, `mcp-tool.json`, `rag-context.json`.

All formats must represent the same logical value. Schema-bearing formats must report schema size separately and, where useful, include a warm-schema measurement.

## Token benchmark

Token measurements must be performed with the tokenizer used by the target model. Character count is not a substitute for token count.

## Acceptance thresholds

Do not hard-code a universal AER win. A release should track regressions against the previous AER version and publish the complete matrix so users can see where each format wins.
