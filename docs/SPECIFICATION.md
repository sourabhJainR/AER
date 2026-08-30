# AER 1.0 Draft Specification

## 1. Abstract

AER (Adaptive Efficient Representation) defines a lossless logical data model and multiple encodings for human, AI and machine consumers. The canonical model is independent of text or binary syntax.

## 2. Canonical model

A value is exactly one of:

`null | bool | int | float | decimal | string | bytes | datetime | duration | array | object | table | reference`

An object is an unordered set of unique string keys. An array is an ordered sequence. A table is an ordered sequence of rows sharing an ordered column list. A reference is an application-visible identifier and carries no network resolution semantics.

## 3. Text grammar

Informal grammar:

```text
document     = directive* root
root         = statement+
statement    = field | table | array
field        = key ":" scalar
             | key ":" newline indent block
array        = key "[" integer "]:" csv
 table       = key "[" integer "]{" columns "}:" newline rows
columns      = key ("," key)*
row          = scalar ("," scalar)*
scalar       = null | bool | number | quoted | typed | reference | bare
null         = "-"
bool         = "true" | "false"
reference    = "@" identifier
typed        = "b64\"...\"" | "dt\"...\"" | "dur\"...\""
```

The definitive 1.0 grammar should be maintained in machine-readable EBNF alongside the conformance suite before declaring the specification final.

## 4. Canonicalization

For deterministic hashing and signatures:

- object keys are sorted lexicographically by UTF-8 code units
- table column order is preserved
- array order is preserved
- integers have one canonical decimal form
- floats use IEEE-754 binary64 and round-trip text form
- strings are UTF-8
- datetime uses RFC 3339 / round-trip precision
- bytes use raw bytes in binary and base64 in text

## 5. Null and missing

A missing object field is different from `null`.

```text
x:-
```

means x exists and is null. An absent x is not represented.

## 6. Schema

Schemas are metadata over canonical values. Schema validation must be deterministic and side-effect free.

Supported foundational constraints:

- type
- required
- min/max
- unit
- meaning

Future versions may add enum, regex/pattern, one-of/all-of and references without changing existing scalar syntax.

## 7. Security limits

Every implementation must expose configurable limits for maximum bytes, nesting depth, array/table cardinality and scalar length. Implementations must fail closed on truncated or malformed binary input.

## 8. Versioning

The major version is part of the document header when a header is emitted. Unknown mandatory features must produce an explicit unsupported-version error rather than being silently ignored.

## 9. Binary format

AER-B starts with:

```text
4 bytes: ASCII AERB
1 byte : version = 1
N bytes: tagged value payload
```

Each value has a one-byte type tag followed by type-specific data. Length-prefixed strings/bytes/collections use unsigned-safe bounded lengths in the normative format. The current reference implementation uses checked signed 64-bit lengths internally and must be aligned with the final unsigned wire specification before 1.0.

## 10. Profiles

AER-H prioritizes readability. AER-A prioritizes model-token efficiency. AER-B prioritizes wire and parsing efficiency. All profiles represent the same canonical model and must round-trip without semantic loss.
