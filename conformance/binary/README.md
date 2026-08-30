# Frozen AER-B vectors

`v1.json` is the interoperability fixture for the AER-B v1 wire contract.

Implementations must:

1. Decode every vector without semantic loss.
2. Match known encode vectors byte-for-byte.
3. Reject malformed/truncated payloads.
4. Preserve object key/value types and array order.

Adding a v1 vector is allowed; changing an existing vector is a breaking change and requires a new major binary version or an explicit AEP.
