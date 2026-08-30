# AER AI benchmark protocol

The AI benchmark measures AER against JSON using the same logical payload and the same tokenizer. It reports counts rather than claiming a universal percentage win.

Supported harness profiles:

- `cl100k_base` via `tiktoken`
- `o200k_base` via `tiktoken`
- GPT-2 tokenizer via `transformers`

Example:

```bash
pip install -e '.[ai]'
python benchmarks/ai/token_benchmark.py --tokenizer tiktoken --tokenizer o200k --out results.json
```

Every result records:

- Python/runtime metadata
- source payload size
- JSON text
- AER text
- tokenizer-specific JSON token count
- tokenizer-specific AER token count

Future benchmark revisions should add representative MCP tool responses, RAG chunks, agent state, and multi-turn context packing. Results should be published with the exact tokenizer/library version and benchmark corpus hash.
