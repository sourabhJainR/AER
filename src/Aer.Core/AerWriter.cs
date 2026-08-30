using System.Globalization;
using System.Text;

namespace Aer;

public sealed record AerWriteOptions(bool Pretty = true, bool IncludeHeader = false, bool CompactArrays = true, int IndentSize = 2);

public static class AerWriter
{
    public static string Write(AerDocument document, AerWriteOptions? options = null)
    {
        options ??= new();
        var sb = new StringBuilder();
        if (options.IncludeHeader) sb.AppendLine("@aer 1");
        if (document.Directives is not null)
            foreach (var d in document.Directives) sb.Append('@').Append(d.Key).Append(' ').AppendLine(d.Value);
        WriteValue(sb, null, document.Root, 0, options);
        return sb.ToString().TrimEnd() + "\n";
    }

    private static void WriteValue(StringBuilder sb, string? key, AerValue value, int indent, AerWriteOptions o)
    {
        var pad = new string(' ', indent * o.IndentSize);
        switch (value.Kind)
        {
            case AerKind.Object:
                if (key is not null) sb.Append(pad).Append(key).AppendLine(":");
                foreach (var pair in (IReadOnlyDictionary<string, AerValue>)value.Data!) WriteValue(sb, pair.Key, pair.Value, indent + (key is null ? 0 : 1), o);
                break;
            case AerKind.Array:
                var values = (IReadOnlyList<AerValue>)value.Data!;
                if (o.CompactArrays && values.All(IsScalar)) sb.Append(pad).Append(key).Append('[').Append(values.Count).Append("]: ").AppendLine(string.Join(',', values.Select(ScalarText)));
                else
                {
                    sb.Append(pad).Append(key).AppendLine(":");
                    foreach (var v in values) WriteValue(sb, "-", v, indent + 1, o);
                }
                break;
            case AerKind.Table:
                var table = (AerTable)value.Data!;
                sb.Append(pad).Append(key).Append('[').Append(table.Rows.Count).Append("]{").Append(string.Join(',', table.Columns)).AppendLine("}:");
                foreach (var row in table.Rows) sb.Append(pad).Append(new string(' ', o.IndentSize)).AppendLine(string.Join(',', row.Select(ScalarText)));
                break;
            default:
                sb.Append(pad).Append(key).Append(':').AppendLine(ScalarText(value));
                break;
        }
    }

    private static bool IsScalar(AerValue v) => v.Kind is not (AerKind.Array or AerKind.Object or AerKind.Table);

    private static string ScalarText(AerValue value) => value.Kind switch
    {
        AerKind.Null => "-",
        AerKind.Bool => ((bool)value.Data!).ToString().ToLowerInvariant(),
        AerKind.Int => ((long)value.Data!).ToString(CultureInfo.InvariantCulture),
        AerKind.Float => ((double)value.Data!).ToString("R", CultureInfo.InvariantCulture),
        AerKind.Decimal => ((decimal)value.Data!).ToString(CultureInfo.InvariantCulture),
        AerKind.String => QuoteIfNeeded((string)value.Data!),
        AerKind.Bytes => "b64\"" + Convert.ToBase64String((byte[])value.Data!) + "\"",
        AerKind.DateTime => "dt\"" + ((DateTimeOffset)value.Data!).ToString("O", CultureInfo.InvariantCulture) + "\"",
        AerKind.Duration => "dur\"" + ((TimeSpan)value.Data!).ToString("c", CultureInfo.InvariantCulture) + "\"",
        AerKind.Reference => "@" + value.Data,
        _ => throw new FormatException("Nested value requires a block representation.")
    };

    private static string QuoteIfNeeded(string s)
    {
        if (s.Length > 0 && s.All(c => !char.IsWhiteSpace(c) && c is not ':' and not ',' and not '#' and not '"')) return s;
        return '"' + s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n") + '"';
    }
}
