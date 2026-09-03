# AER for VS Code

This extension is the first editor integration for AER. It is designed around the same language-server contract used by other editors.

## UX goals

- Recognize `.aer` automatically.
- Syntax highlighting and folding.
- Format document.
- Validate document and show diagnostics.
- Convert JSON to AER and AER to JSON from the command palette.
- Show byte/token benchmark information for the current document.
- Keep AER AI as a representation profile rather than introducing another file type.

## Compatibility

Cursor, Windsurf and other VS Code-compatible editors should be able to reuse the extension and language server without an AER-specific fork where their extension model permits it.

## Commands

```text
AER: Format Document
AER: Validate Document
AER: Convert to JSON
AER: Convert JSON to AER
AER: Show Benchmark
```
