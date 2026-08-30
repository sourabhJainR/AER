# AER Interoperability

The canonical compatibility rule is simple:

```text
same canonical value + same AER-B version => same wire bytes
```

The implementation matrix is:

| Language | Text | Tables | Schema | Patch | Binary | Streaming | Tests |
|---|---:|---:|---:|---:|---:|---:|---:|
| .NET | Yes | Yes | Yes | Yes | Yes | Yes | Yes |
| Python | Yes | Yes | Yes | Yes | Yes | Yes | Yes |
| TypeScript | Yes | Yes | Yes | Yes | Yes | Yes | Yes |
| Go | Yes | Yes | Yes | Yes | Yes | Yes | Yes |

Use the frozen vectors in `conformance/binary/v1.json` as the compatibility authority. A future AER-B wire change must use a new major/versioned vector set rather than changing v1 vectors in place.
