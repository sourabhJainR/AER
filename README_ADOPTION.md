# AER Adoption

Start here for integrating AER into an existing system.

- [Comparison](docs/COMPARISON.md)
- [Adoption guide](docs/ADOPTION.md)
- [Integration catalog](docs/INTEGRATIONS.md)
- [MCP](docs/MCP.md)
- [Specification](docs/SPECIFICATION.md)
- [Benchmarks](docs/BENCHMARKS.md)
- [Benchmark lab](benchmarks/README.md)
- [CLI](src/Aer.Cli/Program.cs)
- [Playground](playground/index.html)

Recommended first experiment:

```bash
aer convert payload.json payload.aer
aer inspect payload.aer
aer optimize payload.aer
```

For AI/MCP workloads, compare JSON and AER-AI token counts on the same payload and tokenizer before changing production defaults.
