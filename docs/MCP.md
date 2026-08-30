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

## Compatibility-first integration

For an existing MCP server, add a response negotiation layer. Keep JSON as the default for clients without AER capability.

```text
Request capabilities
    |
    +-- AER supported? -- yes --> AER-A or AER-B
    |
    +-- no --------------------> JSON
```

Suggested capability names:

```text
aer.text.v1
aer.ai.v1
aer.binary.v1
```

The exact MCP capability field should be aligned with the MCP SDK/version used by the server rather than inventing a wire-level field that conflicts with the protocol.

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

## Server architecture

```text
                     +----------------+
                     | MCP request    |
                     +-------+--------+
                             |
                             v
                     +---------------+
                     | Tool handler  |
                     +-------+-------+
                             |
                             v
                     +---------------+
                     | AerValue       |
                     +--+---+---+----+
                        |   |   |
                        |   |   +--> JSON fallback
                        |   +-------> AER-A
                        +-----------> AER-B
```

## Rollout

Start with one high-volume, repetitive-output tool. Capture baseline token counts and latency, add AER-A behind a client capability flag, verify semantic equality against JSON, then widen adoption.

## Important boundary

AER does not replace MCP's protocol framing or authorization. It is a payload representation used inside an MCP integration. Authentication, authorization, tool safety and transport semantics remain owned by the MCP/server stack.
