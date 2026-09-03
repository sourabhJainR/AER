# AER for Coding-Agent Harnesses

AER can sit below a coding-agent harness as its structured context and event representation. The goal is not to turn AER into an agent framework. The agent loop, permissions, sandbox, runtime and model remain application concerns. AER provides a compact, typed representation for the data those components exchange.

This document records a focused review of the Decoding AI articles **Building a Coding Agent From Scratch: Harness Architecture** and **The Bare-Bones Coding Agent Loop**, and separates ideas that materially improve AER from ideas that should remain outside a serialization/representation layer. The articles describe a small ReAct-style model/tool loop surrounded by context management, permission gating, sandboxing, memory, skills, subagents, event streaming, durable sessions and observability. citeturn0search0turn0search1

## Findings at a glance

| Concept from Decode | AER decision | Reason |
|---|---|---|
| Typed action/observation events | Adopt | Directly improves the agent data boundary and avoids JSON-in-JSON tool payloads. |
| Explicit turn boundaries | Adopt | Makes replay, steering and safe context projection unambiguous. |
| Steering / follow-up / cooperative abort | Adopt as event semantics | AER can transport the control intent without implementing the queue/runtime. |
| Tool-call correlation | Adopt | `related` provides explicit call/result pairing for replay and tracing. |
| Retry attempt and retryable result | Adopt | Makes transient failures machine-readable instead of burying retry intent in text. |
| Context microcompaction | Adopt | Deterministic representation-level reduction is a strong fit for AER. |
| Semantic LLM compaction | Represent checkpoint only | Summarization requires a model/runtime and must not enter AER Core. |
| Append-only session log | Adapt | AER-B frames provide a compact event stream; storage remains a runtime choice. |
| Typed event sink | Adopt concept | AER Agent frames form a transport-neutral event contract. |
| Token accounting | Adopt | Useful for AI cost/quality telemetry and benchmarking. |
| Permission gate | Skip implementation | Policy requires application/security context. AER can carry permission events/results. |
| Sandbox | Skip implementation | OS/container execution is outside a data format. |
| LSP | Skip implementation | Language-aware code intelligence belongs to the coding-agent tool layer. |
| Model/provider abstraction | Skip implementation | Provider selection is an agent/runtime concern; AER remains model-neutral. |
| Skills/memory files | Skip implementation | These are harness features. AER can serialize their structured state if needed. |
| TUI | Skip | Presentation layer. |
| Max-step loop control | Skip | Agent runtime policy, not representation semantics. |
| Database-backed queue | Skip | AER should not impose infrastructure; append-only frames work with any durable store. |

The important conclusion is that AER should absorb the **protocol semantics** around an agent loop, not the execution machinery around that loop.

## What the articles reveal that matters to AER

### 1. The useful abstraction is action -> observation -> next action

The coding-agent loop is deliberately small: the model selects an action, the harness executes it, the result becomes an observation, and the next model request sees that observation. The course explicitly treats the agent loop as ReAct and puts most engineering value in the surrounding harness. citeturn1view1

For AER, this means tool calls and observations should be first-class structured records rather than arbitrary text. That is the strongest direct fit.

AER now models:

```text
ToolCall
   |
   | related/correlation id
   v
ToolResult
   |
   v
Observation
   |
   v
next model request
```

### 2. Turn boundaries are more important than a generic event list

Decode distinguishes a model-request boundary from a would-stop/turn boundary. Steering input is accepted only at a safe model boundary, never in the middle of a tool call; follow-ups wait until the turn would end; cooperative abort stops at a boundary and preserves history. citeturn1view0turn2view2

AER does not implement this queueing behavior, but it now has explicit `TurnStarted`, `TurnFinished`, `Steering`, `FollowUp` and `Abort` frame kinds plus a compact `boundary` field. This gives a runtime enough information to preserve those semantics across a transport or replay log.

The distinction is important:

```text
AER:        describes what happened / what was requested
Harness:    decides when the request is safe to execute
```

### 3. Tool-call/result correlation must survive compaction

The course treats tool calls and results as a pair and its microcompaction removes old result bodies while retaining enough structure to understand the previous interaction. AER's first implementation already retained IDs, names, sequence and truncation state. The review strengthened this with an explicit `related` identifier so a result can point back to its originating call without relying on positional assumptions.

This matters when events are persisted, streamed, reordered by a transport, or projected into a smaller context.

### 4. Retry semantics should not be inferred from prose

Decode uses bounded output/tool retries and turns recoverable tool failures into model-visible retry observations. Its file editing path also rejects ambiguous matches rather than silently changing multiple locations. citeturn1view1turn3view1

AER cannot enforce tool retry policy, but it can preserve the facts required to implement it consistently. Agent frames therefore support:

```text
attempt:   1, 2, 3, ...
retryable: true/false
status:    application-defined result state
```

This allows a harness to distinguish:

```text
tool failed, retry is sensible
```

from:

```text
tool failed, retrying is pointless
```

without parsing natural-language error messages.

### 5. Semantic compaction and deterministic microcompaction are different

The article's headline compaction pattern is `[summary, *tail]`: old context is summarized while recent interactions remain fresh. It also describes microcompaction that clears old tool-result bodies. citeturn1view0turn1view2

Only the second operation belongs in AER Core. AER's `MicroCompact` is deterministic and model-free. For semantic compaction, AER now provides a `Checkpoint` frame carrying the externally generated summary and optional structured state.

This gives us a clean split:

```text
AER Core
  old tool result -> deterministic elision

Agent runtime
  old conversation -> LLM-generated summary
                         |
                         v
                    AER Checkpoint
```

This prevents AER from acquiring a model dependency or silently making expensive inference calls.

### 6. Session logs map naturally to AER-B

Decode persists completed turns in append-only JSONL and can resume a session from that log. It records enough state to reconstruct the conversation after interruption. citeturn2view2

AER already has a length-prefixed binary stream where each frame is independently decodable. The right adaptation is therefore not another session-log format. It is:

```text
session runtime
      |
      v
AER Agent frames
      |
      v
AER-B append-only stream
      |
      +--> file
      +--> database blob/log
      +--> message bus
      +--> remote event stream
```

Storage, retention and checkpointing remain runtime responsibilities.

### 7. Typed events are a strong boundary for UI and remote clients

Decode uses a typed event union such as `TurnStarted`, `ToolCallStarted`, `ToolResult`, `PermissionRequested`, `AskUserRequested`, `TaskListUpdated`, `ContextCompacted` and `AgentError`; the TUI consumes these events rather than having tools know about the UI. citeturn2view1

AER adopts this separation at the wire/data level. The expanded Agent frame kinds cover the lifecycle/control events that are meaningful outside one specific UI implementation.

AER intentionally does not prescribe rendering.

### 8. Token usage belongs in the evidence path

The course records model/tool traces and token usage through OpenTelemetry/Opik, including input, cached input, reasoning and completion counts. citeturn2view1

AER already had AI token measurement infrastructure. The agent profile now carries optional input/output token counts at frame level so an external observability system can correlate usage with a turn or tool interaction. These fields remain optional because not every provider exposes the same accounting.

## Improvements incorporated in this review

### Agent lifecycle vocabulary

Added frame kinds:

```text
TurnStarted
Steering
PermissionRequested
PermissionResult
UserQuestion
FollowUp
ContextCompacted
ContextMicrocompacted
Abort
TurnFinished
```

These complement the existing session/user/assistant/tool/observation/checkpoint/error/done model.

### Correlation and replay metadata

Added:

```text
related   -> action/result correlation
turn      -> turn grouping
boundary  -> safe injection/checkpoint boundary
attempt   -> retry attempt number
retryable -> whether a failure may be retried
```

The existing `seq`, `id` and `parent` fields remain the primary ordering, identity and subagent relationship mechanisms.

### Stronger validation

Agent frames now validate:

- non-negative sequence;
- non-empty identity;
- positive attempt numbers;
- non-negative token counts;
- required tool names for tool calls/results;
- supported frame kinds.

The sequence parser also correctly retains the full 64-bit AER integer range rather than narrowing the sequence to a 32-bit integer.

### Safer microcompaction

Old tool-result bodies are removed while preserving:

```text
kind
id
seq
name
parent
related
turn
summary
truncated
retryable
attempt
```

The frame remains a `ToolResult`; compaction never changes the event's identity or meaning. This is important for replay and trace correlation.

### Explicit semantic checkpoint

`AerAgentContext.Checkpoint(...)` provides a deterministic envelope for a runtime-generated summary/state snapshot. AER does not generate the summary.

## Concepts deliberately not imported

### Permissions

Decode has an explicit allow/ask/deny permission layer and pauses a turn for human approval. This is valuable architecture, but implementing it in AER would mix policy with serialization. AER only carries permission request/result events and status metadata. citeturn0view0

### Sandboxing

The course correctly isolates bash and file operations in Docker/remote sandboxes. AER should never execute commands or know whether a tool is running on a host, container or remote worker. citeturn2view1

### LSP

LSP-based syntax and symbol feedback is a strong coding-agent technique because it gives cheap feedback after edits. It is not a representation concern. AER can carry LSP findings as observations, but an LSP client/index belongs in the tool layer. citeturn1view0

### Memory and skills

The course uses project instruction and memory files plus on-demand skills to control context growth. These are useful harness mechanisms, but forcing an AER memory database or skill engine would make AER much less general. AER should remain capable of representing the resulting structured state without owning its lifecycle. citeturn0view0

### Model providers

Decode keeps provider knowledge behind a model factory so the loop is independent of Gemini, OpenRouter or self-hosted models. This is good architecture, but AER should go one layer lower: it should remain provider-neutral and simply transport the resulting structured context/events. citeturn1view2

### TUI and queues

The steering queue and single-flight runner are excellent runtime designs. AER now has the event vocabulary needed to transport those intents, but it does not implement an in-memory queue, terminal UI or concurrency scheduler. This keeps the core deterministic and reusable.

### Max-step limits

The article intentionally avoids a fixed maximum-step setting and treats context pressure as the more meaningful boundary. This is a reasonable harness philosophy, but no step policy should be encoded in AER. The representation can expose sequence and context/checkpoint metadata while the runtime chooses its stopping policy. citeturn1view2

## Current AER vs Decode: where AER is already stronger

The comparison should not be framed as AER versus a coding-agent harness because they solve different layers.

| Capability | Decode harness | AER |
|---|---|---|
| Agent execution loop | Yes | Intentionally no |
| Tool execution | Yes | No |
| Permissions | Yes | Event representation only |
| Sandbox | Yes | No |
| LSP | Yes | No |
| TUI | Yes | No |
| Session persistence | JSONL runtime log | AER-B event stream + runtime storage |
| Typed agent data boundary | Python/Pydantic model | Canonical cross-representation `AerValue` |
| Tool result compaction | Yes | Deterministic representation-level version |
| Semantic checkpoint | Runtime behavior | Portable checkpoint envelope |
| Binary event transport | Not the core focus | Native AER-B |
| Text/AI/binary from one model | No | Yes |
| Cross-language conformance target | Not the focus | Existing AER architecture |
| JSON interoperability | Via framework/tool models | Existing JSON adapter |
| MCP integration | Harness-specific | Existing MCP profile + Agent profile |

The important AER advantage is that the agent event model does not have to become the agent runtime. It can be shared between .NET, Python, TypeScript, Go, MCP servers, gateways and remote workers.

## Recommended agent architecture with AER

```text
                    Agent Runtime / Harness
                              |
             +----------------+----------------+
             |                |                |
          Model loop       Tool layer      Policy/runtime
             |                |                |
             +----------------+----------------+
                              |
                       AER Agent Profile
                              |
          +-----------------+------------------+
          |                 |                  |
       Context          Event stream       Checkpoints
       projection          |                  |
          |                |                  |
        AER-AI           AER-B              AER-B
          |                |                  |
       model/MCP       replay/transport    durable state
```

## Next meaningful work

The next step should not be another collection of agent features. The high-value work is to prove the representation under real workloads:

1. Add canonical AER Agent conformance vectors for every lifecycle event.
2. Add JSON equivalents so an agent can round-trip AER Agent <-> JSON.
3. Add Python/TypeScript/Go Agent Profile implementations with the same vectors.
4. Add replay fixtures containing multi-turn tool failures, retries, steering, permission pauses, compaction and resume.
5. Benchmark JSON, AER Text and AER-AI for the same coding-agent transcripts with exact tokenizer measurements.
6. Measure task success, tool-call validity, context size, latency and cost rather than character count alone.
7. Add streaming tests for partial/truncated frames and resume-from-checkpoint behavior.

These are evidence-producing improvements. They are more valuable than adding agent-runtime features to AER merely because the course contains them.
