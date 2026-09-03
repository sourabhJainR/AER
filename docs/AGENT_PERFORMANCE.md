# AER Agent Context Performance and Cache Profile

## Design goal

AER should make agent context cheaper to serialize, move, persist and present to an inference runtime without pretending to implement model-side attention. KV tensors remain owned by the inference engine. AER provides the stable data-plane primitives that make KV reuse practical:

```text
Agent transcript
      |
      v
Deterministic pages
      |
      +--> content hash ----> page cache / KV cache key
      |
      +--> prefix hash -----> prefix reuse validation
      |
      +--> token estimate ---> context budget
      |
      v
AER-AI / AER-B / JSON
```

This is intentionally analogous to paged KV-cache systems: context is split into reusable pages, immutable prefixes are addressable by stable hashes, and only the changed tail needs to be rebuilt. The actual attention implementation, GPU memory management and provider-specific KV tensor format remain outside AER.

## What is implemented

### 1. Token-budgeted context pages

`AerAgentContextPager.BuildPages` groups complete frames into deterministic pages using a caller-supplied tokenizer estimate. Frames are never split. An oversized frame becomes a single page instead of being silently truncated.

The estimator is deliberately injected because tokenization is model/tokenizer specific. AER must not hard-code a character-to-token ratio and call it an AI benchmark.

### 2. Stable content and prefix hashes

Every page receives:

- `ContentHash`: SHA-256 of its AER-B encoded frames.
- `PrefixHash`: SHA-256 chain of all prior page hashes plus the current page.
- `PageId`: deterministic human-debuggable identifier derived from the page index and content hash.

The hashes make cache reuse deterministic and provider-neutral.

### 3. Prefix reuse planning

`PlanReuse(previous, current)` compares page content hashes from the beginning and returns:

- reusable page count
- reusable token estimate
- total current token estimate

A normal coding-agent turn should therefore keep a stable prefix and mutate only the tail whenever possible.

### 4. Scoped KV cache keys

`ComputeCacheKey(page, modelScope)` includes the profile version, model scope, content hash and prefix hash. A runtime can map this key to its own KV-cache implementation without making AER aware of provider-specific tensor layouts.

## Token economy strategy

Use the following order because it preserves information before deleting it:

1. Reuse unchanged pages from the previous context.
2. Keep system/instruction and stable repository facts in pinned early pages.
3. Microcompact old tool-result bodies while retaining identity, correlation and summaries.
4. Prefer structured AER values over JSON strings nested inside tool results.
5. Use a tokenizer-specific estimator when choosing page boundaries and context budgets.
6. Perform semantic LLM summarization only at an explicit checkpoint boundary.
7. Keep the newest tool interactions unmodified until a context boundary is reached.

Do not optimize character count in isolation. The acceptance metric is successful task completion per input token, plus latency and cost.

## Runtime integration pattern

A runtime can maintain two structures:

```text
Immutable page store
  page hash -> serialized AER page

Model KV cache
  scoped page key -> provider KV handle
```

On each turn:

1. Build the new page plan.
2. Compare it with the previous plan.
3. Reuse the unchanged prefix pages and their provider KV handles.
4. Encode only new/changed pages.
5. Run the model on the reused prefix plus the new tail.
6. Append the resulting Agent frames.
7. Periodically microcompact and checkpoint.

The AER library itself remains deterministic and side-effect free for these operations.

## Quality and regression gates

Performance changes must not weaken correctness. The reference conformance suite should always cover:

- canonical AER round trips
- agent frame validation
- 64-bit sequence handling
- correlation and retry metadata
- binary stream round trips
- microcompaction identity preservation
- checkpoint round trips
- deterministic page boundaries
- deterministic page hashes
- full prefix reuse
- partial prefix reuse after tail mutation
- cache-key scoping
- rejection of invalid page metadata

For production readiness add property/fuzz tests for arbitrary valid frame sequences and malformed page metadata. Benchmark both cold and warm contexts and report:

- task success rate
- tool-call validity
- input tokens
- output tokens
- serialized bytes
- cache hit/page reuse ratio
- end-to-end latency
- model cost

A benchmark that only reports byte reduction is insufficient because a shorter context can still reduce answer quality.

## Non-goals

AER does not:

- store provider KV tensors
- allocate GPU memory
- implement attention kernels
- decide model-specific cache eviction
- choose a tokenizer
- perform LLM summarization
- execute tools
- enforce permissions

Those responsibilities stay in the inference/runtime layer. This separation keeps AER small, portable and regression-resistant while still making it an excellent substrate for high-performance agent runtimes.
