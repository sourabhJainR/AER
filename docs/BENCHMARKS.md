# AER Benchmark Plan

AER claims should be evidence-driven. Run all comparisons on identical logical payloads and record both raw and compressed sizes.

## Formats

- JSON compact
- JSON pretty
- Protobuf
- MessagePack
- CBOR
- YAML
- XML where applicable
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

## CI gates

The benchmark suite should fail a release when AER regresses beyond agreed thresholds for throughput, allocation count or representation size.

The initial repository does not publish benchmark numbers. Numbers should be generated from the executable benchmark suite rather than estimated in documentation.
