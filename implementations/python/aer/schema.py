from dataclasses import dataclass
from typing import Optional
from .core import AerValue, AerKind

@dataclass(frozen=True)
class Field:
    name: str
    kind: AerKind
    required: bool = False
    minimum: Optional[float] = None
    maximum: Optional[float] = None

@dataclass(frozen=True)
class Schema:
    name: str
    fields: dict[str, Field]

    def validate(self, value: AerValue) -> list[str]:
        if value.kind != AerKind.OBJECT:
            return [f'{self.name}: expected object']
        data = value.data
        errors: list[str] = []
        for name, field in self.fields.items():
            if name not in data:
                if field.required:
                    errors.append(f'{name}: required')
                continue
            current = data[name]
            if current.kind != field.kind:
                errors.append(f'{name}: expected {field.kind.name}')
                continue
            if field.minimum is not None and isinstance(current.data, (int, float)) and current.data < field.minimum:
                errors.append(f'{name}: below minimum')
            if field.maximum is not None and isinstance(current.data, (int, float)) and current.data > field.maximum:
                errors.append(f'{name}: above maximum')
        return errors
