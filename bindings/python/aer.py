"""Minimal Python AER text emitter for interoperability experiments."""
import json
from typing import Any


def scalar(value: Any) -> str:
    if value is None:
        return "-"
    if isinstance(value, bool):
        return str(value).lower()
    if isinstance(value, (int, float)):
        return str(value)
    if isinstance(value, str):
        return json.dumps(value) if any(c in value for c in ',:\n"{}[]') else value
    return json.dumps(value, separators=(",", ":"))


def encode(value: dict[str, Any], indent: str = "") -> str:
    lines: list[str] = []
    for key, item in value.items():
        if isinstance(item, list) and item and all(isinstance(x, dict) for x in item):
            columns: list[str] = []
            for row in item:
                for column in row:
                    if column not in columns:
                        columns.append(column)
            lines.append(f"{indent}{key}[{len(item)}]{{{','.join(columns)}}}:")
            for row in item:
                lines.append(indent + "  " + ",".join(scalar(row.get(c)) for c in columns))
        elif isinstance(item, dict):
            lines.append(f"{indent}{key}:")
            lines.extend(encode(item, indent + "  ").splitlines())
        elif isinstance(item, list):
            lines.append(f"{indent}{key}[{len(item)}]:" + ",".join(scalar(x) for x in item))
        else:
            lines.append(f"{indent}{key}:{scalar(item)}")
    return "\n".join(lines)
