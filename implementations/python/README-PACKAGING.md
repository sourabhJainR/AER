# Python package

Package name: `aer-format`

Build:

```bash
python -m pip install --upgrade build
python -m build
```

The package metadata is in `pyproject.toml`; runtime dependencies are intentionally minimal. AI tokenizer dependencies are optional under the `ai` extra and test dependencies under the `test` extra.
