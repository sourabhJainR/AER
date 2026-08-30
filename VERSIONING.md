# AER Versioning

AER uses semantic versioning for implementations and explicit document/profile versions for data compatibility.

## Compatibility

- `1.x` documents must remain readable by conforming `1.x` implementations unless they use an explicitly negotiated optional feature.
- Unknown mandatory directives or binary tags must fail with an explicit unsupported-feature error.
- Optional metadata may be ignored only where the specification marks it non-semantic.
- Binary wire tags are never silently reinterpreted.

## Version fields

Text documents may begin with:

```text
@aer 1
```

AER-B begins with the `AERB` magic followed by its wire version.

## Release numbering

- Patch: compatible bug/security fixes.
- Minor: backward-compatible syntax/profile additions.
- Major: incompatible canonical model or wire changes.

AER 1.0 should be frozen only after the conformance suite, fuzzing, benchmarks and at least two independent implementations pass.
