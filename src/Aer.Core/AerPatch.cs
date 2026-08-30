namespace Aer;

/// <summary>Supported incremental change operations.</summary>
public enum AerPatchOp { Add, Replace, Remove }

/// <summary>A path-based mutation against a canonical AER value.</summary>
public sealed record AerPatchOperation(AerPatchOp Op, string Path, AerValue? Value = null);

/// <summary>Applies deterministic path-based patches to objects and arrays.</summary>
public static class AerPatch
{
    public static AerValue Apply(AerValue root, IEnumerable<AerPatchOperation> operations)
    {
        var current = root;
        foreach (var operation in operations)
            current = ApplyOne(current, operation);
        return current;
    }

    private static AerValue ApplyOne(AerValue root, AerPatchOperation operation)
    {
        var segments = operation.Path.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length == 0) throw new AerFormatException("AER002", "Patch path cannot be empty.");
        return Mutate(root, segments, 0, operation);
    }

    private static AerValue Mutate(AerValue node, string[] segments, int index, AerPatchOperation op)
    {
        if (index == segments.Length - 1)
            return ApplyLeaf(node, segments[index], op);

        var segment = segments[index];
        return node.Kind switch
        {
            AerKind.Object => MutateObject((IReadOnlyDictionary<string,AerValue>)node.Data!, segment, segments, index, op),
            AerKind.Array => MutateArray((IReadOnlyList<AerValue>)node.Data!, segment, segments, index, op),
            _ => throw new AerFormatException("AER005", $"Path segment '{segment}' cannot traverse {node.Kind}.")
        };
    }

    private static AerValue MutateObject(IReadOnlyDictionary<string,AerValue> source, string key, string[] segments, int index, AerPatchOperation op)
    {
        if (!source.TryGetValue(key, out var child)) throw new AerFormatException("AER005", $"Path does not exist: /{string.Join('/', segments.Take(index + 1))}");
        var map = new Dictionary<string,AerValue>(source, StringComparer.Ordinal) { [key] = Mutate(child, segments, index + 1, op) };
        return AerValue.Object(map);
    }

    private static AerValue MutateArray(IReadOnlyList<AerValue> source, string segment, string[] segments, int index, AerPatchOperation op)
    {
        if (!int.TryParse(segment, out var i) || i < 0 || i >= source.Count) throw new AerFormatException("AER005", $"Invalid array index '{segment}'.");
        var list = source.ToList();
        list[i] = Mutate(list[i], segments, index + 1, op);
        return AerValue.Array(list);
    }

    private static AerValue ApplyLeaf(AerValue node, string segment, AerPatchOperation op)
    {
        if (node.Kind == AerKind.Object)
        {
            var map = new Dictionary<string,AerValue>((IReadOnlyDictionary<string,AerValue>)node.Data!, StringComparer.Ordinal);
            switch (op.Op)
            {
                case AerPatchOp.Add:
                case AerPatchOp.Replace:
                    if (op.Value is null) throw new AerFormatException("AER002", "Patch value is required.");
                    map[segment] = op.Value;
                    return AerValue.Object(map);
                case AerPatchOp.Remove:
                    if (!map.Remove(segment)) throw new AerFormatException("AER005", $"Field does not exist: {segment}");
                    return AerValue.Object(map);
            }
        }
        if (node.Kind == AerKind.Array && int.TryParse(segment, out var i))
        {
            var list = ((IReadOnlyList<AerValue>)node.Data!).ToList();
            if (op.Op == AerPatchOp.Remove && i >= 0 && i < list.Count) { list.RemoveAt(i); return AerValue.Array(list); }
            if (op.Value is not null && i >= 0 && i < list.Count) { list[i] = op.Value; return AerValue.Array(list); }
        }
        throw new AerFormatException("AER005", $"Cannot apply patch '{op.Op}' at '{segment}'.");
    }
}
