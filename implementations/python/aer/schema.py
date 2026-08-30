from dataclasses import dataclass
from typing import Optional
from .core import AerValue, AerKind

@dataclass(frozen=True)
class Field:
    name: str
    kind: AerKind
    required: bool = False
    unit: Optional[str] = None
    minimum: Optional[float] = None
    maximum: Optional[float] = None
    meaning: Optional[str] = None

@dataclass(frozen=True)
class Schema:
    name: str
    fields: dict[str, Field]
    def validate(self, value: AerValue) -> list[str]:
        if value.kind != AerKind.OBJECT: return [f'{self.name}: expected object, got {value.kind.name}']
        data=value.data; errors=[]
        for name, field in self.fields.items():
            if name not in data:
                if field.required: errors.append(f'{self.name}.{name}: required field is missing')
                continue
            current=data[name]
            if field.kind != current.kind and not (field.kind==AerKind.DECIMAL and current.kind==AerKind.INT):
                errors.append(f'{self.name}.{name}: expected {field.kind.name}, got {current.kind.name}'); continue
            if field.minimum is not None and isinstance(current.data,(int,float)) and current.data < field.minimum: errors.append(f'{self.name}.{name}: below minimum')
            if field.maximum is not None and isinstance(current.data,(int,float)) and current.data > field.maximum: errors.append(f'{self.name}.{name}: above maximum')
        return errors
