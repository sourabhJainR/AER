namespace Aer;

public sealed record AerOptimizationOptions(bool PromoteUniformObjectArraysToTables = true, int MinimumRowsForTable = 2, bool RemoveNullObjectFields = false);

public static class AerOptimizer
{
    public static AerValue Optimize(AerValue value, AerOptimizationOptions? options = null)
    {
        options ??= new();
        return OptimizeValue(value, options);
    }

    private static AerValue OptimizeValue(AerValue v, AerOptimizationOptions o) => v.Kind switch
    {
        AerKind.Object => OptimizeObject((IReadOnlyDictionary<string, AerValue>)v.Data!, o),
        AerKind.Array => OptimizeArray((IReadOnlyList<AerValue>)v.Data!, o),
        AerKind.Table => AerValue.Table(((AerTable)v.Data!).Validate()),
        _ => v
    };

    private static AerValue OptimizeObject(IReadOnlyDictionary<string, AerValue> source, AerOptimizationOptions o)
    {
        var result = new Dictionary<string, AerValue>(source.Count, StringComparer.Ordinal);
        foreach (var pair in source)
        {
            var optimized = OptimizeValue(pair.Value, o);
            if (o.RemoveNullObjectFields && optimized.Kind == AerKind.Null) continue;
            result[pair.Key] = optimized;
        }
        return AerValue.Object(result);
    }

    private static AerValue OptimizeArray(IReadOnlyList<AerValue> source, AerOptimizationOptions o)
    {
        var optimized = source.Select(v => OptimizeValue(v, o)).ToArray();
        if (!o.PromoteUniformObjectArraysToTables || optimized.Length < o.MinimumRowsForTable || optimized.Any(v => v.Kind != AerKind.Object)) return AerValue.Array(optimized);
        var objects = optimized.Select(v => (IReadOnlyDictionary<string, AerValue>)v.Data!).ToArray();
        var columns = objects[0].Keys.ToArray();
        if (objects.Any(x => x.Count != columns.Length || x.Keys.Any(k => !objects[0].ContainsKey(k)))) return AerValue.Array(optimized);
        var rows = objects.Select(x => (IReadOnlyList<AerValue>)columns.Select(c => x[c]).ToArray()).ToArray();
        return AerValue.Table(new AerTable(columns, rows));
    }
}
