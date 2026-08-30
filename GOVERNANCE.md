# AER Governance

AER is governed through open technical discussion, reproducible evidence and documented compatibility rules.

## Maintainers

Maintainers are responsible for reviewing code, approving specification changes, managing releases and protecting compatibility.

## AEP process

AER Enhancement Proposals are required for non-trivial changes to:

- syntax or grammar;
- canonical types;
- schema semantics;
- binary wire format;
- AI profile behavior;
- interoperability requirements.

An AEP should include motivation, alternatives, syntax/examples, compatibility impact, implementation plan and conformance vectors.

## Release policy

- Patch releases fix compatible defects.
- Minor releases add backward-compatible features.
- Major releases may change the canonical model or wire compatibility.

The AER specification is the source of truth; implementation behavior must conform to it.
