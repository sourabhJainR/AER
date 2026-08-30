# AER Orchestration

`Aer.Orchestration` provides a transport- and model-independent context planning layer for AI agents.

It does not call an LLM and does not own MCP transport. Its job is deterministic context selection:

```text
candidate context
      |
      v
required/priority ordering
      |
      v
character budget
      |
      v
AER-AI encoding
      |
      v
agent/orchestrator
```

The host remains responsible for tool execution, model calls, permissions, retries, cancellation and verification.

## Example

```csharp
var planner = new AerOrchestrator();
var plan = planner.Plan(
    new AerOrchestrationContext("task-42", Repository: "repo", Snapshot: "abc123"),
    candidates,
    maxCharacters: 12000);

var context = planner.EncodePlan(plan);
```

The same planner can be used by coding agents, MCP clients, retrieval pipelines and other orchestrators without taking a dependency on a particular agent SDK.
