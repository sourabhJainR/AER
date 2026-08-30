# Cross-language implementation status

The reference implementations expose the following feature-equivalent surfaces:

| Feature | .NET | Python | TypeScript | Go |
|---|---|---|---|---|
| AER scalar model | Yes | Yes | Yes | Yes |
| Nested object parser/writer | Yes | Yes | Yes | Yes |
| Scalar arrays | Yes | Yes | Yes | Yes |
| Tables | Yes | Yes | Yes | Yes |
| Typed date/time | Yes | Yes | Yes | Yes |
| Typed duration | Yes | Yes | Yes | Yes |
| References | Yes | Yes | Yes | Yes |
| Schema type/required/range metadata | Yes | Yes | Yes | Yes |
| Patch add/replace/remove | Yes | Yes | Yes | Yes |
| AER-B encode/decode | Yes | Yes | Yes | Yes |
| AERF frame encode/decode | Yes | Yes | Yes | Yes |
| Frozen vector tests | Yes | Yes | Yes | Yes |
| Property testing | Yes | Yes | Yes | Yes |
| Fuzzing | CI-ready | CI-ready | CI-ready | Native Go fuzz |

The goal is semantic and wire parity, not identical source APIs. Each implementation should be able to consume the same frozen AER-B vectors and produce the same bytes for the same canonical value.
