# AER for Coding-Agent Harnesses

AER can sit below a coding-agent harness as its structured context and event representation. The goal is not to turn AER into an agent framework. The agent loop, permissions, sandbox, runtime and model remain application concerns. AER provides a compact, typed representation for the data those components exchange.

The design is informed by the Decoding AI coding-agent course, which separates the small model/tool loop from the larger harness concerns: context management, permissions, durable session state, tool results, skills, subagents, observability and evaluation. The course currently implements context compaction, append-only replay logs, explicit permission decisions and bounded turn boundaries. AER adopts the representation-level parts of those ideas without importing a Python runtime or an LLM dependency. citeturn0search0turn0search7

## What AER adds

### 1. Agent frames

`AerAgentFrame` is a typed envelope for:

- user and assistant messages;
- tool calls and tool results;
- observations;
- checkpoints;
- errors and completion events;
- optional parent/call relationships;
- optional input/output token accounting;
- truncation state and short summaries.

The payload remains `AerValue`. There is no second serialization model to maintain.

Example:

```csharp
var call = AerAgentFrame.ToolCall(
    sequence: 12,
    id: "call-12",
    tool: "search_repo",
    arguments: AerValue.Object(new Dictionary<string, AerValue>
    {
        ["query"] = AerValue.String("AerParser")
    }));

var result = AerAgentFrame.ToolResult(
    sequence: 13,
    id: "call-12-result",
    tool: "search_repo",
    result: AerValue.Array(results),
    summary: "3 matching files");
```

The same frames can be emitted as AER-AI for model context or AER-B frames for a runtime/event stream.

### 2. Tool-result compaction

Coding agents often receive very large command, search, test or repository outputs. The course's microcompaction strategy keeps the tool call/result relationship intact while eliding old tool-result bodies. AER exposes the same representation-level operation through `AerAgentContext.MicroCompact`.

It is deterministic and does not call a model. Older tool-result payloads are removed while identity, tool name, sequence, parent, summary and truncation state remain. This makes it suitable for gateways and SDKs as well as agent runtimes.

```csharp
var compacted = AerAgentContext.MicroCompact(
    frames,
    keepRecentFrames: 20);
```

For a full semantic summary, the agent runtime can use its own summarizer and store the result as a `Checkpoint` frame. AER does not prescribe a model or provider.

### 3. Durable event boundaries

The existing AER-B length-prefixed stream is a natural transport for agent events. One frame is one independently decodable AER value. This means a runtime can persist or forward events without concatenating JSON documents or inventing another event payload format.

AER Patch can be used when the runtime needs state deltas instead of complete snapshots.

### 4. MCP profile

`application/aer; profile=agent` is available in `Aer.Mcp`. Existing JSON, Text, Ai and Binary profiles remain unchanged.

```csharp
var payload = AerMcpProfileEncoder.EncodeAgent(resultFrame);
var contentType = AerMcpProfileEncoder.ContentType(AerMcpProfile.Agent);
```

This is deliberately opt-in. JSON remains the compatibility default.

## Harness mapping

| Coding-agent concern | Harness responsibility | AER contribution |
|---|---|---|
| Model/tool loop | Agent runtime | Typed tool-call/result frames |
| Permissions | Policy/runtime | `status`, `error`, metadata fields in frames |
| Context window | Runtime/provider | Compact representation and frame projection |
| Microcompaction | Runtime | `AerAgentContext.MicroCompact` |
| Full LLM compaction | Runtime/model | Store result as a `Checkpoint` frame |
| Session replay | Runtime/storage | AER frames over append-only AER-B stream |
| Steering/follow-up | Harness | User/observation frames with sequence/order |
| Subagents | Harness | `parent` frame relationship |
| Observability | Harness | Sequence, ids, status and token accounting |
| Evaluation | Benchmark harness | Stable frame representation and exact byte/token measurement |
| Sandbox | Runtime | AER carries results; it does not execute commands |

## Design constraints

AER intentionally does not copy the course's Python implementation of permissions, sandboxing, durable runtime or model orchestration. Those components need operating-system, provider and application context that does not belong in a serialization library.

AER also does not claim that character reduction equals token reduction. Exact tokenizer measurements remain the benchmark authority, consistent with the project's AI effectiveness benchmark.

## Recommended architecture

```text
Coding Agent
     |
     +-- model loop
     +-- permissions
     +-- sandbox
     +-- runtime / replay
     +-- skills / subagents
     |
     v
AER Agent Profile
     |
     +-- compact typed frames
     +-- tool-result microcompaction
     +-- checkpoints
     +-- patches / deltas
     |
     +---- AER-AI -> model context / MCP
     +---- AER Text -> logs / debugging
     +---- AER-B -> runtime / event transport
     +---- JSON -> compatibility boundary
```

The key boundary is that the canonical data model remains independent of the agent implementation. This keeps AER useful for non-agent applications while making it much easier to plug into Claude-Code-style, MCP-based or custom coding-agent harnesses.
