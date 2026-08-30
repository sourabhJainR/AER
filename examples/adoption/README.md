# Adoption Examples

These examples illustrate incremental use of AER without replacing existing application/domain models.

## API

```text
Domain object -> AER-H/AER-A -> HTTP response
```

## MCP

```text
Tool -> canonical value -> profile selector -> JSON/AER-AI/AER-B
```

## RAG

```text
retriever -> repeated records -> optimizer -> AER-AI -> model context
```

## Events

```text
producer -> AER-B -> broker -> AER-B -> consumer
```

## Migration

Start with dual output and retain JSON fallback until measurements prove AER is a better fit for the selected interface.
