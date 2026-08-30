using System.Buffers.Binary;
using System.Text;

namespace Aer;

public sealed record AerBinaryOptions(int MaxPayloadBytes = 16 * 1024 * 1024, int MaxDepth = 128, int MaxCollectionItems = 1_000_000, int MaxStringBytes = 4 * 1024 * 1024)
{
    public void Validate()
    {
        if (MaxPayloadBytes <= 0) throw new ArgumentOutOfRangeException(nameof(MaxPayloadBytes));
        if (MaxDepth < 0) throw new ArgumentOutOfRangeException(nameof(MaxDepth));
        if (MaxCollectionItems < 0) throw new ArgumentOutOfRangeException(nameof(MaxCollectionItems));
        if (MaxStringBytes <= 0) throw new ArgumentOutOfRangeException(nameof(MaxStringBytes));
    }
}

public static class AerBinary
{
    private static readonly byte[] Magic = "AERB"u8.ToArray();

    public static byte[] Encode(AerValue value)
    {
        using var ms = new MemoryStream();
        ms.Write(Magic); ms.WriteByte(1); WriteValue(ms, value, 0);
        return ms.ToArray();
    }

    public static AerValue Decode(ReadOnlySpan<byte> data, AerBinaryOptions? options = null)
    {
        options ??= new AerBinaryOptions(); options.Validate();
        if (data.Length < 5 || data.Length > options.MaxPayloadBytes || !data[..4].SequenceEqual(Magic) || data[4] != 1)
            throw new AerFormatException("AER009", "Invalid AER binary header, version, or payload size.");
        var offset = 5;
        var value = ReadValue(data, ref offset, 0, options);
        if (offset != data.Length) throw new AerFormatException("AER010", "Trailing bytes after AER binary value.");
        return value;
    }

    private static void WriteValue(Stream s, AerValue v, int depth)
    {
        if (depth > 128) throw new InvalidOperationException("AER value nesting exceeds encoding limit.");
        s.WriteByte((byte)v.Kind);
        switch (v.Kind)
        {
            case AerKind.Null: break;
            case AerKind.Bool: s.WriteByte((byte)((bool)v.Data! ? 1 : 0)); break;
            case AerKind.Int: WriteInt64(s, (long)v.Data!); break;
            case AerKind.Float: WriteInt64(s, BitConverter.DoubleToInt64Bits((double)v.Data!)); break;
            case AerKind.Decimal: WriteString(s, ((decimal)v.Data!).ToString(System.Globalization.CultureInfo.InvariantCulture)); break;
            case AerKind.String: WriteString(s, (string)v.Data!); break;
            case AerKind.Bytes: WriteBytes(s, (byte[])v.Data!); break;
            case AerKind.DateTime: WriteInt64(s, ((DateTimeOffset)v.Data!).UtcTicks); break;
            case AerKind.Duration: WriteInt64(s, ((TimeSpan)v.Data!).Ticks); break;
            case AerKind.Reference: WriteString(s, (string)v.Data!); break;
            case AerKind.Array: var a = (IReadOnlyList<AerValue>)v.Data!; WriteInt64(s, a.Count); foreach (var x in a) WriteValue(s, x, depth + 1); break;
            case AerKind.Object: var o = (IReadOnlyDictionary<string, AerValue>)v.Data!; WriteInt64(s, o.Count); foreach (var p in o) { WriteString(s, p.Key); WriteValue(s, p.Value, depth + 1); } break;
            case AerKind.Table: var t = (AerTable)v.Data!; WriteInt64(s, t.Columns.Count); foreach (var c in t.Columns) WriteString(s, c); WriteInt64(s, t.Rows.Count); foreach (var row in t.Rows) foreach (var x in row) WriteValue(s, x, depth + 1); break;
            default: throw new NotSupportedException(v.Kind.ToString());
        }
    }

    private static AerValue ReadValue(ReadOnlySpan<byte> data, ref int o, int depth, AerBinaryOptions options)
    {
        if (depth > options.MaxDepth) throw new AerFormatException("AER007", $"Maximum nesting depth {options.MaxDepth} exceeded.");
        var kind = (AerKind)ReadByte(data, ref o);
        return kind switch
        {
            AerKind.Null => AerValue.Null,
            AerKind.Bool => ReadBool(data, ref o),
            AerKind.Int => AerValue.Int(ReadInt64(data, ref o)),
            AerKind.Float => AerValue.Float(BitConverter.Int64BitsToDouble(ReadInt64(data, ref o))),
            AerKind.Decimal => ParseDecimal(ReadString(data, ref o, options)),
            AerKind.String => AerValue.String(ReadString(data, ref o, options)),
            AerKind.Bytes => AerValue.Bytes(ReadBytes(data, ref o, options)),
            AerKind.DateTime => AerValue.DateTime(new DateTimeOffset(ReadInt64(data, ref o), TimeSpan.Zero)),
            AerKind.Duration => AerValue.Duration(TimeSpan.FromTicks(ReadInt64(data, ref o))),
            AerKind.Reference => AerValue.Reference(ReadString(data, ref o, options)),
            AerKind.Array => ReadArray(data, ref o, depth, options),
            AerKind.Object => ReadObject(data, ref o, depth, options),
            AerKind.Table => ReadTable(data, ref o, depth, options),
            _ => throw new AerFormatException("AER009", $"Unsupported binary kind {(byte)kind}.")
        };
    }

    private static AerValue ReadBool(ReadOnlySpan<byte> d, ref int o) { var b = ReadByte(d, ref o); if (b > 1) throw new AerFormatException("AER009", "Invalid boolean value."); return AerValue.Bool(b == 1); }
    private static AerValue ParseDecimal(string s) { if (!decimal.TryParse(s, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var value)) throw new AerFormatException("AER009", "Invalid decimal value."); return AerValue.Decimal(value); }
    private static AerValue ReadArray(ReadOnlySpan<byte> d, ref int o, int depth, AerBinaryOptions p) { var n = ReadCount(d, ref o, p); var a = new AerValue[n]; for (var i = 0; i < n; i++) a[i] = ReadValue(d, ref o, depth + 1, p); return AerValue.Array(a); }
    private static AerValue ReadObject(ReadOnlySpan<byte> d, ref int o, int depth, AerBinaryOptions p) { var n = ReadCount(d, ref o, p); var m = new Dictionary<string, AerValue>(n, StringComparer.Ordinal); for (var i = 0; i < n; i++) { var key = ReadString(d, ref o, p); if (!m.TryAdd(key, ReadValue(d, ref o, depth + 1, p))) throw new AerFormatException("AER003", $"Duplicate object key '{key}'."); } return AerValue.Object(m); }
    private static AerValue ReadTable(ReadOnlySpan<byte> d, ref int o, int depth, AerBinaryOptions p) { var c = ReadCount(d, ref o, p); var cols = new string[c]; var seen = new HashSet<string>(StringComparer.Ordinal); for (var i = 0; i < c; i++) { cols[i] = ReadString(d, ref o, p); if (!seen.Add(cols[i])) throw new AerFormatException("AER003", $"Duplicate table column '{cols[i]}'."); } var r = ReadCount(d, ref o, p); var rows = new List<IReadOnlyList<AerValue>>(r); for (var i = 0; i < r; i++) { var row = new AerValue[c]; for (var j = 0; j < c; j++) row[j] = ReadValue(d, ref o, depth + 1, p); rows.Add(row); } return AerValue.Table(new AerTable(cols, rows).Validate()); }
    private static int ReadCount(ReadOnlySpan<byte> d, ref int o, AerBinaryOptions p) { var n = ReadInt64(d, ref o); if (n < 0 || n > p.MaxCollectionItems || n > int.MaxValue) throw new AerFormatException("AER006", "Binary collection exceeds configured limits."); return (int)n; }
    private static void WriteInt64(Stream s, long v) { Span<byte> b = stackalloc byte[8]; BinaryPrimitives.WriteInt64LittleEndian(b, v); s.Write(b); }
    private static long ReadInt64(ReadOnlySpan<byte> d, ref int o) { Ensure(d, o, 8); var v = BinaryPrimitives.ReadInt64LittleEndian(d[o..]); o += 8; return v; }
    private static void WriteString(Stream s, string v) => WriteBytes(s, Encoding.UTF8.GetBytes(v));
    private static string ReadString(ReadOnlySpan<byte> d, ref int o, AerBinaryOptions p) => Encoding.UTF8.GetString(ReadBytes(d, ref o, p));
    private static void WriteBytes(Stream s, byte[] b) { WriteInt64(s, b.Length); s.Write(b); }
    private static byte[] ReadBytes(ReadOnlySpan<byte> d, ref int o, AerBinaryOptions p) { var n = ReadInt64(d, ref o); if (n < 0 || n > p.MaxStringBytes || n > int.MaxValue) throw new AerFormatException("AER006", "Binary byte/string value exceeds configured limit."); Ensure(d, o, (int)n); var b = d[o..(o + (int)n)].ToArray(); o += (int)n; return b; }
    private static byte ReadByte(ReadOnlySpan<byte> d, ref int o) { Ensure(d, o, 1); return d[o++]; }
    private static void Ensure(ReadOnlySpan<byte> d, int o, int n) { if (o < 0 || n < 0 || o > d.Length - n) throw new AerFormatException("AER008", "Truncated AER binary payload."); }
}
