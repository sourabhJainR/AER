# Current cross-language release status

Implemented on the feature branch:

- .NET AER core and adapters
- Python reference package with text, binary and schema APIs
- TypeScript/Node reference package with text and binary APIs
- Go reference package with text emission and binary codec
- Frozen AER-B v1 JSON vector corpus
- Cross-language CI build/test jobs
- Tokenizer-specific AI benchmark harness for cl100k_base and o200k_base, plus a Transformers profile
- NuGet, npm and PyPI release metadata
- Tagged release workflow

The remaining pre-1.0 work is parity hardening for advanced features such as tables, patches, streaming frames and full schema syntax in each non-.NET implementation, plus measured benchmark publication and package registry dry-runs.
