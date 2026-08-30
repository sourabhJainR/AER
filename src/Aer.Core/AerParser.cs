using System.Globalization;
using System.Text;

namespace Aer;

public static class AerParser
{
    public static AerDocument Parse(string text, AerParseOptions? options = null)
    {
        if (text is null) throw new ArgumentNullException(nameof(text));
        options ??= new AerParseOptions(); options.Validate();
        if (Encoding.UTF8.GetByteCount(text) > options.MaxDocumentBytes) throw new AerFormatException("AER006", $"Document exceeds {options.MaxDocumentBytes} bytes.");
        var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        if (lines.Length > options.MaxLines) throw new AerFormatException("AER006", $"Document exceeds {options.MaxLines} lines.");
        var meaningful = lines.Where(l => !string.IsNullOrWhiteSpace(l) && !l.TrimStart().StartsWith('#')).ToList();
        var index = 0; var version = 1;
        var directives = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        while (index < meaningful.Count && meaningful[index].TrimStart().StartsWith('@'))
        {
            var p = meaningful[index].Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            if (p.Length == 2 && string.Equals(p[0], "@aer", StringComparison.OrdinalIgnoreCase) && int.TryParse(p[1], NumberStyles.None, CultureInfo.InvariantCulture, out var v))
            { if (v != 1) throw new AerFormatException("AER001", $"Unsupported AER version {v}."); version = v; }
            else if (p.Length == 2 && IsDirectiveName(p[0][1..])) directives[p[0][1..]] = p[1];
            else throw new AerFormatException("AER002", $"Invalid directive on line {index + 1}: {meaningful[index]}");
            index++;
        }
        if (index >= meaningful.Count) throw new AerFormatException("AER002", "AER document contains no root value.");
        var root = ParseBlock(meaningful, ref index, CountIndent(meaningful[index]), 0, options);
        if (index != meaningful.Count) throw new AerFormatException("AER002", $"Unexpected content on line {index + 1}.");
        return new AerDocument(version, root, directives);
    }

    private static AerValue ParseBlock(IReadOnlyList<string> lines, ref int index, int indent, int depth, AerParseOptions options)
    {
        if (depth > options.MaxDepth) throw new AerFormatException("AER007", $"Maximum nesting depth {options.MaxDepth} exceeded.");
        var obj = new Dictionary<string, AerValue>(StringComparer.Ordinal);
        while (index < lines.Count)
        {
            var raw = lines[index]; var currentIndent = CountIndent(raw);
            if (currentIndent < indent) break;
            if (currentIndent > indent) throw new AerFormatException("AER002", $"Unexpected indentation on line {index + 1}.");
            var line = raw.Trim();
            if (line.StartsWith('@')) throw new AerFormatException("AER002", $"Directive is only allowed before the root on line {index + 1}.");
            var colon = FindUnquoted(line, ':');
            if (colon < 1) throw new AerFormatException("AER002", $"Expected key:value on line {index + 1}: {line}");
            var keySpec = line[..colon].Trim(); var rest = line[(colon + 1)..].Trim(); ValidateKey(keySpec, index + 1);
            if (TryParseTableHeader(keySpec, rest, out var tableName, out var count, out var columns))
            {
                EnsureNewKey(obj, tableName, index + 1); EnsureCollectionLimit(count, options, tableName); index++;
                var rows = new List<IReadOnlyList<AerValue>>(count);
                for (var r = 0; r < count; r++)
                {
                    if (index >= lines.Count || CountIndent(lines[index]) <= indent) throw new AerFormatException("AER004", $"Unexpected end of table {tableName}.");
                    var cells = SplitCsv(lines[index++].Trim());
                    if (cells.Count != columns.Count) throw new AerFormatException("AER004", $"{tableName}: expected {columns.Count} cells but found {cells.Count}.");
                    rows.Add(cells.Select(c => ParseScalar(c, options)).ToArray());
                }
                obj[tableName] = AerValue.Table(new AerTable(columns, rows).Validate()); continue;
            }
            if (TryParseCountedArray(keySpec, rest, out var arrayName, out var values))
            {
                EnsureNewKey(obj, arrayName, index + 1); EnsureCollectionLimit(values.Count, options, arrayName);
                obj[arrayName] = AerValue.Array(values.Select(v => ParseScalar(v, options)).ToArray()); index++; continue;
            }
            if (rest.Length == 0)
            {
                EnsureNewKey(obj, keySpec, index + 1); index++;
                obj[keySpec] = index < lines.Count && CountIndent(lines[index]) > indent
                    ? ParseBlock(lines, ref index, CountIndent(lines[index]), depth + 1, options)
                    : AerValue.Object(new Dictionary<string, AerValue>());
            }
            else { EnsureNewKey(obj, keySpec, index + 1); obj[keySpec] = ParseScalar(rest, options); index++; }
        }
        return AerValue.Object(obj);
    }

    private static void EnsureNewKey(IReadOnlyDictionary<string, AerValue> obj, string key, int line) { if (obj.ContainsKey(key)) throw new AerFormatException("AER003", $"Duplicate key '{key}' on line {line}."); }
    private static void EnsureCollectionLimit(int count, AerParseOptions options, string name) { if (count < 0 || count > options.MaxCollectionItems) throw new AerFormatException("AER006", $"Collection '{name}' exceeds {options.MaxCollectionItems} items."); }
    private static bool IsDirectiveName(string name) => name.Length > 0 && name.All(c => char.IsLetterOrDigit(c) || c is '_' or '-');
    private static void ValidateKey(string key, int line) { if (key.Length == 0 || key.Any(char.IsControl) || key.Contains('\t')) throw new AerFormatException("AER002", $"Invalid key on line {line}."); }

    private static bool TryParseCountedArray(string spec, string rest, out string name, out IReadOnlyList<string> values)
    {
        name = spec; values = Array.Empty<string>(); var open = spec.LastIndexOf('['); var close = spec.EndsWith(']') ? spec.Length - 1 : -1;
        if (open <= 0 || close <= open || !int.TryParse(spec[(open + 1)..close], NumberStyles.None, CultureInfo.InvariantCulture, out var count)) return false;
        name = spec[..open]; values = SplitCsv(rest); if (values.Count != count) throw new AerFormatException("AER004", $"{name}: declared {count} items but found {values.Count}."); return true;
    }
    private static bool TryParseTableHeader(string spec, string rest, out string name, out int count, out IReadOnlyList<string> columns)
    {
        name = spec; count = 0; columns = Array.Empty<string>(); var open = spec.LastIndexOf('['); var mid = spec.IndexOf("]{", StringComparison.Ordinal);
        if (open <= 0 || mid < open || !spec.EndsWith('}') || rest.Length != 0) return false;
        if (!int.TryParse(spec[(open + 1)..mid], NumberStyles.None, CultureInfo.InvariantCulture, out count)) return false;
        name = spec[..open]; columns = SplitCsv(spec[(mid + 2)..^1]); if (columns.Count == 0 || columns.Any(string.IsNullOrWhiteSpace)) throw new AerFormatException("AER004", $"{name}: table requires non-empty columns."); return true;
    }
    private static AerValue ParseScalar(string text, AerParseOptions options)
    {
        text = text.Trim(); if (text.Length > options.MaxScalarLength) throw new AerFormatException("AER006", $"Scalar exceeds {options.MaxScalarLength} characters.");
        if (text == "-") return AerValue.Null; if (text.Equals("true", StringComparison.OrdinalIgnoreCase)) return AerValue.Bool(true); if (text.Equals("false", StringComparison.OrdinalIgnoreCase)) return AerValue.Bool(false); if (text.StartsWith("@") && text.Length > 1) return AerValue.Reference(text[1..]);
        if (text.StartsWith("b64\"", StringComparison.Ordinal)) { if (!text.EndsWith('"')) throw new AerFormatException("AER005", "Invalid base64 scalar."); try { return AerValue.Bytes(Convert.FromBase64String(text[4..^1])); } catch (FormatException) { throw new AerFormatException("AER005", "Invalid base64 scalar."); } }
        if (text.StartsWith("dt\"", StringComparison.Ordinal)) { if (!text.EndsWith('"') || !DateTimeOffset.TryParse(text[3..^1], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dto)) throw new AerFormatException("AER005", "Invalid datetime scalar."); return AerValue.DateTime(dto); }
        if (text.StartsWith("dur\"", StringComparison.Ordinal)) { if (!text.EndsWith('"') || !TimeSpan.TryParse(text[5..^1], CultureInfo.InvariantCulture, out var duration)) throw new AerFormatException("AER005", "Invalid duration scalar."); return AerValue.Duration(duration); }
        if (text.Length >= 2 && text[0] == '"' && text[^1] == '"') return AerValue.String(Unescape(text[1..^1]));
        if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l)) return AerValue.Int(l); if (decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var m)) return AerValue.Decimal(m); return AerValue.String(text);
    }
    private static List<string> SplitCsv(string input)
    {
        var result = new List<string>(); var start = 0; var quoted = false; var escaped = false;
        for (var i = 0; i < input.Length; i++) { var c = input[i]; if (escaped) { escaped = false; continue; } if (c == '\\' && quoted) { escaped = true; continue; } if (c == '"') quoted = !quoted; else if (c == ',' && !quoted) { result.Add(input[start..i].Trim()); start = i + 1; } }
        if (quoted || escaped) throw new AerFormatException("AER005", "Unterminated quoted scalar."); result.Add(input[start..].Trim()); return result;
    }
    private static string Unescape(string s) => s.Replace("\\n", "\n").Replace("\\\"", "\"").Replace("\\\\", "\\");
    private static int CountIndent(string s) => s.TakeWhile(c => c == ' ').Count();
    private static int FindUnquoted(string s, char target) { var quoted = false; var escaped = false; for (var i = 0; i < s.Length; i++) { var c = s[i]; if (escaped) { escaped = false; continue; } if (c == '\\' && quoted) { escaped = true; continue; } if (c == '"') quoted = !quoted; else if (c == target && !quoted) return i; } return -1; }
}
