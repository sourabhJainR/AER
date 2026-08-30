# AER AI Effectiveness Benchmark

## Purpose

AER's AI profile exists to reduce context overhead without reducing task quality. This benchmark measures the complete tradeoff rather than claiming that smaller payloads are automatically better.

## Required comparison set

Every published AER AI result should compare the same canonical payload against:

- JSON
- AER Text
- AER AI
- TOON when an equivalent reference encoder is available
- any repository-native format already used by the target workload

Binary benchmarks are reported separately from LLM-context benchmarks.

## Real-world workload classes

Use representative structured payloads from:

1. MCP tool/resource responses.
2. Repository metadata and dependency inventories.
3. Issue/Jira records and acceptance criteria.
4. API responses with 10/100/1,000/10,000 records.
5. Analytics/FP&A-style tabular data with repeated dimensions and measures.
6. Nested configuration and policy documents.
7. Irregular records where table optimization should not trigger.
8. Mixed-type values, nulls, Unicode, timestamps, decimals, references and large strings.

Do not benchmark only a synthetic uniform table. AER's adaptive behavior must be measured across uniform, semi-structured and irregular inputs.

## Metrics

### Representation

- UTF-8 bytes.
- UTF-16 characters.
- Structural overhead ratio.
- Compression ratio relative to JSON.

### AI context economics

Use exact tokenizer adapters when available. Never present a character or whitespace heuristic as an exact token count.

Report:

- input tokens;
- output tokens when applicable;
- tokens saved versus JSON;
- percentage saved versus JSON;
- tokenizer name and version;
- model family where relevant.

### Fidelity and safety

- parse success rate;
- canonical round-trip equality;
- schema validation success;
- invalid-input rejection rate;
- deterministic output stability;
- semantic equivalence after optimization.

### Runtime

- encode latency;
- decode latency;
- allocations/GC where the benchmark tool can measure them;
- throughput;
- p50/p95/p99 where the harness supports repeated measurements.

### AI task utility

For model-backed experiments, measure task quality on the same tasks with each representation:

- answer correctness;
- tool-call correctness;
- retrieval precision/recall where applicable;
- task completion rate;
- regression/error rate;
- number of clarification turns;
- total model tokens;
- latency;
- total cost.

The primary effectiveness metric is:

`verified task value / total model + tool cost`

Token reduction alone is not an effectiveness win if task quality or verification falls.

## Paired experimental design

For credible comparisons:

1. Hold the canonical data constant.
2. Hold the model, temperature/reasoning settings, system prompt, task, tool permissions and timeout constant.
3. Randomize or counterbalance representation order where model-backed evaluation is used.
4. Run multiple trials per task when stochasticity matters.
5. Record exact model/provider versions and benchmark commit.
6. Store raw results and an aggregate report.
7. Compare paired tasks, not unrelated averages.

External benchmark practice increasingly emphasizes like-for-like configurations, task-level outcome, cost and token measurement; AER should follow the same discipline. citeturn283250search1turn283250search2

## Baseline and promotion policy

A change to AER's encoder, optimizer, grammar or AI profile must compare against a frozen baseline.

A candidate is not publishable as an improvement when it:

- improves bytes but reduces fidelity;
- saves tokens but lowers task correctness;
- improves speed but increases invalid output;
- wins only on one hand-picked payload;
- changes the corpus, tokenizer or model while claiming a direct improvement;
- relies on estimated token counts presented as exact.

## Benchmark tiers

### Tier 0: deterministic CI

Fast checks on fixed fixtures:

- byte counts;
- canonical equality;
- parser validity;
- deterministic encoding;
- optimizer losslessness.

### Tier 1: local performance

Repeated encode/decode measurements over the full fixed corpus.

### Tier 2: exact tokenizer evaluation

Run with approved tokenizer adapters and report per-tokenizer results.

### Tier 3: model utility evaluation

Run the same engineering or MCP tasks with controlled model settings and collect quality/cost/latency metrics.

### Tier 4: external reproduction

Publish corpus, harness version, model/provider configuration, tokenizer version, results and statistical methodology so a third party can reproduce the claim.

## Reporting standard

Never report only “AER is X% smaller”. Publish a table containing:

`workload | format | bytes | tokens | parse/fidelity | encode p50 | decode p50 | task success | total cost`

Also report median and distribution, not only a favorable mean.

## Practical AI coding benchmark

For the Adaptive AI Coding Orchestrator, use a paired task suite where the only changed variable is the structured context representation:

```text
same task
same repository
same agent
same model
same permissions
same verification
        |
        +--> JSON context
        +--> AER AI context
```

Measure:

- time-to-proven-change;
- human clarification turns;
- repository exploration/tool calls;
- total model tokens;
- verification failures;
- regression failures;
- accepted output rate.

This directly measures whether AER improves the engineering workflow rather than merely compressing text.

## Result integrity

Every benchmark result must include:

`benchmark_version, corpus_version, aer_commit, runtime, tokenizer/model, configuration, timestamp, raw-result-hash`

Do not overwrite historical benchmark results. Append a new run and compare it to the frozen baseline.
