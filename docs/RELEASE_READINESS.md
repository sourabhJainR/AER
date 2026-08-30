# Release readiness

AER is ready for a public compatibility release only when all gates below are green.

- .NET core, ASP.NET Core and MCP builds pass.
- Python, TypeScript and Go implementations compile/test independently.
- Frozen AER-B vectors decode in every implementation.
- Known binary encode vectors match byte-for-byte.
- Text round-trip conformance passes.
- Tokenizer-specific AI benchmarks run and record exact tokenizer metadata.
- NuGet, npm and PyPI package builds succeed locally and in release CI.
- Security, governance, versioning and license documentation are present.
- No universal performance claim is published without reproducible benchmark evidence.
