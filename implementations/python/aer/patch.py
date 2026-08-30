from dataclasses import dataclass
from enum import Enum
from .core import AerValue, AerKind

class PatchOp(str, Enum): ADD='add'; REPLACE='replace'; REMOVE='remove'
@dataclass(frozen=True)
class PatchOperation:
    op: PatchOp
    path: str
    value: AerValue|None = None

def apply(root: AerValue, operations: list[PatchOperation]) -> AerValue:
    current=root
    for operation in operations: current=_apply_one(current,operation)
    return current

def _apply_one(root,op):
    parts=[p for p in op.path.split('/') if p]
    if not parts: raise ValueError('AER patch path cannot be empty')
    return _mutate(root,parts,0,op)

def _mutate(node,parts,index,op):
    if index==len(parts)-1: return _leaf(node,parts[index],op)
    key=parts[index]
    if node.kind==AerKind.OBJECT:
        data=dict(node.data)
        if key not in data: raise ValueError(f'path does not exist: /{key}')
        data[key]=_mutate(data[key],parts,index+1,op)
        return AerValue(AerKind.OBJECT,data)
    if node.kind==AerKind.ARRAY:
        i=int(key); data=list(node.data); data[i]=_mutate(data[i],parts,index+1,op); return AerValue(AerKind.ARRAY,tuple(data))
    raise ValueError(f'cannot traverse {node.kind.name}')

def _leaf(node,key,op):
    if node.kind==AerKind.OBJECT:
        data=dict(node.data)
        if op.op in (PatchOp.ADD,PatchOp.REPLACE):
            if op.value is None: raise ValueError('patch value required')
            data[key]=op.value
        elif op.op==PatchOp.REMOVE:
            if key not in data: raise ValueError(f'field does not exist: {key}')
            del data[key]
        return AerValue(AerKind.OBJECT,data)
    if node.kind==AerKind.ARRAY:
        i=int(key); data=list(node.data)
        if op.op==PatchOp.REMOVE: del data[i]
        elif op.value is not None: data[i]=op.value
        else: raise ValueError('patch value required')
        return AerValue(AerKind.ARRAY,tuple(data))
    raise ValueError('patch target is not mutable')
