# AEP-0002: AER Agent Profile

Status: Draft

## Problem statement

AI agent harnesses exchange a recurring set of structured events: turns, user input, model output, tool calls, tool results, observations, approvals, questions, retries, checkpoints, compaction and completion. Most implementations define these events inside one runtime and serialize their payloads independently, which makes replay, cross-language transport and context projection harder.

## Motivation

AER already provides a canonical typed value model, adaptive text representation, AI profile, binary framing, patches and MCP integration. An Agent Profile allows these capabilities to be reused by coding agents and other tool-using agents without making AER responsible for model execution, permissions, sandboxing or orchestration.

The design is informed by the Decoding AI coding-agent harness architecture, especially its explicit action/observation loop, safe steering boundaries, append-only session state, typed events, retry behavior and deterministic microcompaction. The profile adopts only representation-level semantics. citeturn0search0turn0search1

## Proposed design

An Agent frame is an AER object with required fields:

```text
kind
seq
id
```

Optional fields:

```text
data
name
status
summary
parent
related
turn
boundary
truncated
retryable
attempt
input_tokens
output_tokens
```

`data` is always an ordinary canonical `AerValue`. Tool arguments and results are not encoded as JSON strings inside AER.

### Frame kinds

```text
session
turn_started
user
steering
assistant
tool_call
permission_requested
permission_result
tool_result
observation
user_question
follow_up
checkpoint
context_compacted
context_microcompacted
error
abort
turn_finished
done
```

Unknown kinds are rejected by the reference implementation until a future profile version explicitly defines extension behavior.

## Correlation rules

- `seq` provides total ordering within a transcript.
- `id` uniquely identifies the frame.
- `related` links an event to the action or request it answers, such as a tool result to its tool call.
- `parent` identifies a parent agent/subagent or parent execution context.
- `turn` groups frames belonging to one model turn.
- `boundary` describes a safe runtime boundary such as `before_model` or `context_boundary`; it does not cause execution.

## Retry rules

`attempt` is a positive one-based attempt number. `retryable=true` records that the runtime may retry the represented operation. AER does not choose retry limits or perform retries.

## Context projection

AER defines deterministic microcompaction for old tool-result payloads. The operation may remove `data` while retaining event identity and metadata and setting `truncated=true`.

Semantic summarization is outside AER. A runtime may create a `checkpoint` frame containing an LLM-generated summary and optional structured state.

## Binary compatibility

Agent frames are ordinary AER values and can therefore be transported through AER-B length-prefixed frames. Adding optional fields is backward-compatible. New required fields or changed field types require an AEP and profile versioning decision.

## AI profile impact

Agent frames may be emitted through AER-AI. The profile should remain schema-light for small events and use table/adaptive forms for repetitive tool results and observations. Exact token savings must be established using the project's pinned tokenizer benchmark rather than character count.

## Security impact

Agent frames are data only. They do not execute commands, resolve references, invoke models, apply permissions or access the network. Implementations must continue to enforce parser/resource limits and must treat tool output as untrusted data.

## Alternatives considered

### Put the complete coding-agent runtime into AER

Rejected. This would couple the representation layer to OS execution, model providers, user interaction and deployment infrastructure.

### Use a JSON event schema only

Rejected as the primary representation because it duplicates field names and loses the benefit of AER's canonical model, adaptive encoding and binary stream.

### Use positional tool-call/result pairing

Rejected. Explicit `related` IDs are more robust for replay, transport and subagent concurrency.

### Encode every internal runtime detail

Rejected. Only portable protocol semantics belong in the profile. Provider-specific tracing, sandbox internals and UI state remain outside the profile.

## Conformance vectors

The reference suite should cover:

1. Every frame kind round-trip.
2. Unknown kind rejection.
3. Required `kind`, `seq` and `id` validation.
4. 64-bit sequence round-trip.
5. Tool call/result correlation using `related`.
6. Parent and turn grouping.
7. Retry/attempt validation.
8. Token-count validation.
9. Binary frame round-trip.
10. Microcompaction preserving tool-result identity.
11. Checkpoint round-trip.
12. Multi-turn replay with steering, follow-up, permission pause, retry and abort events.

## Migration strategy

The Agent Profile is additive. Existing AER Text, AER-AI, AER-B, JSON and MCP profiles remain valid. A runtime can begin by wrapping existing tool calls/results as Agent frames, then add lifecycle events and checkpoints as needed.

JSON remains the compatibility fallback for clients that do not advertise AER support.
