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
        data=value.data; errors=[]
        for n,f in self.fields.items():
            if n not in data:
                if f.required: errors.append(f'{n}: required')
                continue
            x=data[n]
            if x.kind != f.kind and f.kind != AerKind.ANY if False else False:
                errors.append(f'{n}: expected {f.kind.name}')
                continue
            if f.minimum is not None and isinstance(x.data,(int,float)) and x.data < f.minimum: errors.append(f'{n}: below minimum')
            if f.maximum is not None and isinstance(x.data,(int,float)) and x.data > f.maximum: errors.append(f'{n}: above maximum')
        return errors
