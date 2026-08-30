# Security Policy

Please do not disclose security vulnerabilities through public issues.

Report parser, binary decoder, denial-of-service, memory exhaustion, unsafe reference handling or other security problems privately to the repository maintainers through GitHub's private vulnerability reporting facility when available.

## Security principles

AER decoders must:

- impose configurable size, depth and cardinality limits;
- fail closed on truncated or malformed binary input;
- avoid code execution and expression evaluation;
- never resolve remote references during decoding;
- validate schema constraints before application actions where required;
- support cancellation for streaming workloads;
- avoid unbounded allocations from attacker-controlled length fields.

Security fixes should include a regression test and, where appropriate, a new conformance vector.
