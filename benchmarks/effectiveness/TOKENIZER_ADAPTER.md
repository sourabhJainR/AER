# Tokenizer Adapter Contract

The core AER repository intentionally has no tokenizer dependency. Exact LLM token measurements are supplied by external adapters.

## Input

The adapter receives one UTF-8 JSON object per line on stdin:

```json
{"id":"case-001","text":"employees[2]{id,name}: 1,Amit 2,Priya"}
```

## Output

It must emit one JSON object per input line:

```json
{"id":"case-001","tokens":12,"tokenizer":"example-tokenizer","version":"1.0"}
```

Rules:

- `tokens` must be a non-negative integer.
- `tokenizer` and `version` identify the exact tokenizer implementation.
- No heuristic character-to-token conversion may be reported as exact tokenizer output.
- The adapter must be deterministic for identical input and tokenizer version.
- Errors must be returned as structured records or non-zero exit status; never silently emit zero.

## Controlled comparison

Run the same corpus through JSON, AER Text and AER AI representations. Keep the task, model, system prompt, reasoning configuration, tool permissions and repository snapshot constant. Record the tokenizer name/version alongside every result.

Approved adapters may be implemented in Python, TypeScript, Go or .NET without changing the AER core.
