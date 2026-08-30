using System.Globalization;
using System.Text.Json;

namespace Aer;

public enum AerKind { Null, Bool, Int, Float, Decimal, String, Bytes, DateTime, Duration, Array, Object, Table, Reference }

public sealed record AerValue(AerKind Kind, object? Data)
{
    public static AerValue Null => new(AerKind.Null, null);
    public static AerValue Bool(bool value) => new(AerKind.Bool, value);
    public static AerValue Int(long value) => new(AerKind.Int, value);
    public static AerValue Float(double value) => new(AerKind.Float, value);
    public static AerValue Decimal(decimal value) => new(AerKind.Decimal, value);
    public static AerValue String(string value) => new(AerKind.String, value);
    public static AerValue Bytes(byte[] value) => new(AerKind.Bytes, value);
    public static AerValue DateTime(DateTimeOffset value) => new(AerKind.DateTime, value);
    public static AerValue Duration(TimeSpan value) => new(AerKind.Duration, value);
    public static AerValue Array(IReadOnlyList<AerValue> values) => new(AerKind.Array, values);
    public static AerValue Object(IReadOnlyDictionary<string, AerValue> values) => new(AerKind.Object, values);
    public static AerValue Table(AerTable table) => new(AerKind.Table, table.Validate());
    public static AerValue Reference(string id) => new(AerKind.Reference, id);

    public JsonElement ToJsonElement(JsonSerializerOptions? options = null)
    {
        object? value = Kind switch
        {
            AerKind.Null => null,
            AerKind.Bool => Data,
            AerKind.Int => Data,
            AerKind.Float => Data,
            AerKind.Decimal => Data,
            AerKind.String => Data,
            AerKind.Bytes => Convert.ToBase64String((byte[])Data!),
            AerKind.DateTime => ((DateTimeOffset)Data!).ToString("O", CultureInfo.InvariantCulture),
            AerKind.Duration => ((TimeSpan)Data!).ToString("c", CultureInfo.InvariantCulture),
            AerKind.Reference => new Dictionary<string, object?> { ["$ref"] = Data },
            AerKind.Array => ((IReadOnlyList<AerValue>)Data!).Select(v => v.ToJsonElement(options)).ToArray(),
            AerKind.Object => ((IReadOnlyDictionary<string, AerValue>)Data!).ToDictionary(k => k.Key, v => v.Value.ToJsonElement(options)),
            AerKind.Table => ((AerTable)Data!).Rows.Select(r => r.Select(v => v.ToJsonElement(options)).ToArray()).ToArray(),
            _ => throw new InvalidOperationException($"Unsupported kind {Kind}")
        };
        return JsonSerializer.SerializeToElement(value, options);
    }

    public static AerValue FromObject(object? value)
    {
        if (value is null) return Null;
        if (value is AerValue av) return av;
        if (value is AerTable table) return Table(table);
        if (value is IDictionary<string, object?> map)
            return Object(map.ToDictionary(k => k.Key, v => FromObject(v.Value)));

        return value switch
        {
            string s => String(s),
            bool b => Bool(b),
            byte by => Int(by),
            short sh => Int(sh),
            int i => Int(i),
            long l => Int(l),
            float f => Float(f),
            double d => Float(d),
            decimal m => Decimal(m),
            DateTime dt => DateTime(new DateTimeOffset(dt)),
            DateTimeOffset dto => DateTime(dto),
            TimeSpan ts => Duration(ts),
            byte[] bytes => Bytes(bytes),
            System.Collections.IEnumerable values => Array(values.Cast<object?>().Select(FromObject).ToArray()),
            _ => FromJson(JsonSerializer.SerializeToElement(value))
        };
    }

    public static AerValue FromJson(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Null => Null,
        JsonValueKind.True => Bool(true),
        JsonValueKind.False => Bool(false),
        JsonValueKind.String => String(element.GetString() ?? string.Empty),
        JsonValueKind.Number when element.TryGetInt64(out var l) => Int(l),
        JsonValueKind.Number when element.TryGetDecimal(out var m) => Decimal(m),
        JsonValueKind.Number => Float(element.GetDouble()),
        JsonValueKind.Array => Array(element.EnumerateArray().Select(FromJson).ToArray()),
        JsonValueKind.Object => element.TryGetProperty("$ref", out var r)
            ? Reference(r.GetString() ?? string.Empty)
            : Object(element.EnumerateObject().ToDictionary(p => p.Name, p => FromJson(p.Value))),
        _ => throw new FormatException($"Unsupported JSON token: {element.ValueKind}")
    };
}
