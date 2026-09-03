# AER + MCP

## Principle

Keep the tool implementation independent of transport representation.

```text
MCP tool
  -> domain result
  -> AerValue
  -> validate
  -> optimize
  -> select profile
  -> MCP result
```

AER is a payload representation, not a replacement for MCP framing, authorization or tool safety.

## Implemented integration surface

`Aer.Mcp` now provides three transport-neutral building blocks:

- `AerMcpCapabilities` parses advertised AER profiles.
- `AerMcpNegotiator.SelectProfile` deterministically selects the best supported profile.
- `AerMcpNegotiator.Encode` creates an `AerMcpPayload` containing the selected content type and encoded text/binary payload.

The adapter intentionally does not depend on a specific MCP SDK. This keeps the package usable across MCP SDK versions and lets the host map the payload into the SDK's current result/content types.

Supported profile identifiers:

```text
aer.text.v1
aer.ai.v1
aer.binary.v1
```

JSON remains the compatibility fallback.

## Compatibility-first integration

For an existing MCP server, add a response negotiation layer. Keep JSON as the default for clients without AER capability.

```text
Request capabilities
    |
    +-- AER supported? -- yes --> negotiated AER profile
    |
    +-- no --------------------> JSON
```

Do not invent a new MCP wire-level capability field. Map the AER profile identifiers into the capability mechanism supported by the MCP SDK/version used by the host.

## Example

```csharp
var capabilities = AerMcpCapabilities.FromProfiles(
    new[] { "aer.ai.v1", "aer.text.v1" });

var payload = AerMcpNegotiator.Encode(
    domainResult,
    capabilities,
    preferred: AerMcpProfile.Ai);

// payload.ContentType -> MCP SDK content metadata
// payload.Text        -> text content for AER text/AI profiles
// payload.Binary      -> binary-capable host transport when supported
```

## Tool result example

Domain result:

```json
{
  "status":"success",
  "items":[
    {"id":101,"name":"Amit","score":0.92},
    {"id":102,"name":"Priya","score":0.87}
  ]
}
```

AER-A:

```text
status:success
items[2]{id,name,score}:
  101,Amit,0.92
  102,Priya,0.87
```

This is especially useful when a tool returns many repeated records to an LLM.

## Resource example

A large MCP resource can expose an AER text representation for inspection and an AER-B representation for application clients. The resource URI remains the source of identity; AER references remain local identifiers unless the application explicitly maps them.

## Rollout

Start with one high-volume, repetitive-output tool. Capture baseline token counts and latency, add AER-A behind a client capability flag, verify semantic equality against JSON, then widen adoption.

The repository now has an integration contract suite covering AI preference, JSON fallback and orchestrator interaction in CI.

## Important boundary

AER does not replace MCP protocol framing or authorization. Authentication, authorization, tool safety and transport semantics remain owned by the MCP/server stack. The binary profile also requires a host transport/content mechanism that can carry binary data; the adapter does not pretend that every MCP client can consume arbitrary binary content.
