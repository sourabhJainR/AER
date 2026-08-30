# Property and fuzz testing

AER uses deterministic property tests plus malformed-input fuzz entry points.

## Properties

For generated values:

```text
Decode(Encode(x)) == x
DecodeFrames(EncodeFrame(x)) == [x]
Loads(Dumps(x)) == x
```

For schemas:

```text
valid values produce zero validation errors
required/type/range violations are deterministic
```

For patches:

```text
ApplyPatch(x, ops) produces the expected canonical value
```

## Fuzz targets

- AER-B decoder with arbitrary bytes.
- AERF frame decoder with arbitrary bytes.
- Text parser with malformed indentation, table counts and quoted strings.
- Schema validation with generated values.
- Patch application with invalid paths and indexes.

Go includes a native `testing.F` fuzz target. Python and TypeScript use deterministic property loops in the default CI path; dedicated long-running fuzz jobs should be enabled for release/nightly workflows.

Fuzzing must remain bounded by parser limits so malformed input cannot cause unbounded allocation or recursion.
