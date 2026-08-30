# Advanced AER Capabilities

## Canonical hashing

`AerHash.Sha256(value)` computes a stable SHA-256 identifier from the canonical value model. Object key order does not change the hash; array order and table column order do.

```csharp
var hash = AerHash.Sha256(value);
```

Use the hash for cache keys, deduplication and integrity checks. Do not use it as an authentication mechanism without a separate signature scheme.

## Patching

AER supports deterministic path-based object/array mutations:

```csharp
var updated = AerPatch.Apply(value, new[]
{
    new AerPatchOperation(AerPatchOp.Replace, "/user/role", AerValue.String("Manager")),
    new AerPatchOperation(AerPatchOp.Remove, "/user/temporaryFlag")
});
```

Patch paths are local data paths. They do not resolve remote resources.

## Streaming

`AerStream` wraps AER-B payloads in length-prefixed frames:

```text
AERF | version | uint32 payloadLength | AER-B payload
```

```csharp
var frame = AerStream.EncodeFrame(value);
foreach (var item in AerStream.DecodeFrames(frame))
{
    // process item
}
```

The decoder enforces a frame size limit and rejects invalid magic, unsupported versions and truncated payloads.

## Production note

Patch and streaming semantics should be frozen and covered by conformance vectors before being included in a final AER 1.0 wire-compatibility guarantee.
