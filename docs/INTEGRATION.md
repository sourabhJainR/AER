# Integrating AER into an existing platform

## Pattern A: edge adapter

No domain code changes.

```text
Existing service -> object model -> AER adapter -> client
                                  |
                                  +-> JSON fallback
```

Use this first when API compatibility is important.

## Pattern B: canonical representation

Make `AerValue` the internal serialization-neutral boundary for AI/MCP orchestration.

```text
Service -> domain model -> AerValue -> profile -> transport
```

This avoids maintaining separate business serializers for JSON, AER and binary.

## Pattern C: AER-native services

For new internal services:

```text
Contract/schema
      |
      v
Domain
      |
      v
AER canonical model
      |
      +--> HTTP AER
      +--> MCP AER-A
      +--> AER-B event
      +--> JSON compatibility
```

## ASP.NET Core

The production package should implement:

```csharp
services.AddAER(options =>
{
    options.EnableText = true;
    options.EnableBinary = true;
    options.EnableAiProfile = true;
});
```

Then support content negotiation such as:

```text
Accept: application/aer
Accept: application/aer; profile=ai
Accept: application/aer; profile=binary
```

The exact media-type registration should be finalized with IANA-compatible conventions before public standardization.

## Event systems

Use AER-B for stable, typed, high-volume events and AER-H for fixture files. Version schemas independently of consumer deployment versions.

## AI pipelines

Use AER-A between retrieval, orchestration and tool layers when records repeat. Keep natural-language explanations outside the structured payload so the model can clearly separate data from narration.

## Caching

Cache after canonicalization. Do not create separate cache keys for JSON and AER forms of the same semantic data.

## Observability

Track representation choice and compression/size metrics without logging full payloads. Compare AER against the previous format before enabling it globally.

## Safe migration sequence

1. Add AER adapter and compatibility tests.
2. Benchmark real payloads.
3. Enable AER for non-breaking read paths.
4. Add schema validation to selected contracts.
5. Enable AER-A for AI/MCP responses.
6. Enable AER-B for internal high-volume traffic.
7. Promote selected contracts to AER-first.

## Rollback

Every migration should support a configuration switch that returns JSON. The canonical domain model must remain independent of the selected wire format so rollback does not require business changes.
