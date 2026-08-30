# AER Language Server

AER uses the Language Server Protocol so one implementation can serve VS Code-compatible editors and other LSP clients.

Planned/implemented contract:

- diagnostics and syntax errors
- document formatting
- completion
- hover metadata
- document symbols/folding
- JSON Schema validation
- commands for JSON <-> AER conversion
- token/size benchmark preview

The server must never modify source documents during validation. Formatting is explicit or controlled by the editor's format-on-save setting.
