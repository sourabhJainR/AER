namespace Aer;

public enum AerTypeKind { Any, Null, Bool, Int, Float, Decimal, String, Bytes, DateTime, Duration, Array, Object, Table, Reference }

public sealed record AerField(string Name, AerTypeKind Type = AerTypeKind.Any, bool Required = false, string? Unit = null, double? Min = null, double? Max = null, string? Meaning = null);

public sealed record AerSchema(string Name, IReadOnlyDictionary<string, AerField> Fields)
{
    public IReadOnlyList<string> Validate(AerValue value)
    {
        var errors = new List<string>();
        if (value.Kind != AerKind.Object)
        {
            errors.Add($"{Name}: expected object, got {value.Kind}");
            return errors;
        }
        var obj = (IReadOnlyDictionary<string, AerValue>)value.Data!;
        foreach (var field in Fields.Values)
        {
            if (!obj.TryGetValue(field.Name, out var v))
            {
                if (field.Required) errors.Add($"{Name}.{field.Name}: required field is missing");
                continue;
            }
            if (!Matches(field.Type, v.Kind)) errors.Add($"{Name}.{field.Name}: expected {field.Type}, got {v.Kind}");
            if (field.Min.HasValue && TryNumber(v, out var n) && n < field.Min.Value) errors.Add($"{Name}.{field.Name}: {n} < min {field.Min.Value}");
            if (field.Max.HasValue && TryNumber(v, out n) && n > field.Max.Value) errors.Add($"{Name}.{field.Name}: {n} > max {field.Max.Value}");
        }
        return errors;
    }

    private static bool Matches(AerTypeKind expected, AerKind actual) => expected == AerTypeKind.Any || expected switch
    {
        AerTypeKind.Null => actual == AerKind.Null,
        AerTypeKind.Bool => actual == AerKind.Bool,
        AerTypeKind.Int => actual == AerKind.Int,
        AerTypeKind.Float => actual == AerKind.Float,
        AerTypeKind.Decimal => actual is AerKind.Decimal or AerKind.Int,
        AerTypeKind.String => actual == AerKind.String,
        AerTypeKind.Bytes => actual == AerKind.Bytes,
        AerTypeKind.DateTime => actual == AerKind.DateTime,
        AerTypeKind.Duration => actual == AerKind.Duration,
        AerTypeKindKind.Array => actual == AerKind.Array,
        AerTypeKind.Object => actual == AerKind.Object,
        AerTypeKind.Table => actual == AerKind.Table,
        AerTypeKind.Reference => actual == AerKind.Reference,
        _ => false
    };

    private static bool TryNumber(AerValue value, out double number)
    {
        switch (value.Data)
        {
            case long l: number = l; return true;
            case double d: number = d; return true;
            case decimal m: number = (double)m; return true;
            default: number = 0; return false;
        }
    }
}
