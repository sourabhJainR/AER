using System.Buffers.Binary;
using System.Text;

namespace Aer;

public static class AerBinary
{
    private static readonly byte[] Magic = "AERB"u8.ToArray();

    public static byte[] Encode(AerValue value)
    {
        using var ms = new MemoryStream();
        ms.Write(Magic);
        ms.WriteByte(1);
        WriteValue(ms, value);
        return ms.ToArray();
    }

    public static AerValue Decode(ReadOnlySpan<byte> data)
    {
        if (data.Length < 5 || !data[..4].SequenceEqual(Magic) || data[4] != 1) throw new FormatException("Invalid AER binary header or version.");
        var offset = 5;
        return ReadValue(data, ref offset);
    }

    private static void WriteValue(Stream s, AerValue v)
    {
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
            case AerKind.Array:
                var a = (IReadOnlyList<AerValue>)v.Data!; WriteInt64(s, a.Count); foreach (var x in a) WriteValue(s, x); break;
            case AerKind.Object:
                var o = (IReadOnlyDictionary<string, AerValue>)v.Data!; WriteInt64(s, o.Count); foreach (var p in o) { WriteString(s, p.Key); WriteValue(s, p.Value); } break;
            case AerKind.Table:
                var t = (AerTable)v.Data!; WriteInt64(s, t.Columns.Count); foreach (var c in t.Columns) WriteString(s, c); WriteInt64(s, t.Rows.Count); foreach (var row in t.Rows) foreach (var x in row) WriteValue(s, x); break;
            default: throw new NotSupportedException(v.Kind.ToString());
        }
    }

    private static AerValue ReadValue(ReadOnlySpan<byte> data, ref int o)
    {
        var kind = (AerKind)ReadByte(data, ref o);
        return kind switch
        {
            AerKind.Null => AerValue.Null,
            AerKind.Bool => AerValue.Bool(ReadByte(data, ref o) != 0),
            AerKind.Int => AerValue.Int(ReadInt64(data, ref o)),
            AerKind.Float => AerValue.Float(BitConverter.Int64BitsToDouble(ReadInt64(data, ref o))),
            AerKind.Decimal => AerValue.Decimal(decimal.Parse(ReadString(data, ref o), System.Globalization.CultureInfo.InvariantCulture)),
            AerKind.String => AerValue.String(ReadString(data, ref o)),
            AerKind.Bytes => AerValue.Bytes(ReadBytes(data, ref o)),
            AerKind.DateTime => AerValue.DateTime(new DateTimeOffset(ReadInt64(data, ref o), TimeSpan.Zero)),
            AerKind.Duration => AerValue.Duration(TimeSpan.FromTicks(ReadInt64(data, ref o))),
            AerKind.Reference => AerValue.Reference(ReadString(data, ref o)),
            AerKind.Array => ReadArray(data, ref o),
            AerKind.Object => ReadObject(data, ref o),
            AerKind.Table => ReadTable(data, ref o),
            _ => throw new FormatException($"Unsupported binary kind {kind}.")
        };
    }

    private static AerValue ReadArray(ReadOnlySpan<byte> d, ref int o) { var n = checked((int)ReadInt64(d, ref o)); var a = new AerValue[n]; for (var i = 0; i < n; i++) a[i] = ReadValue(d, ref o); return AerValue.Array(a); }
    private static AerValue ReadObject(ReadOnlySpan<byte> d, ref int o) { var n = checked((int)ReadInt64(d, ref o)); var m = new Dictionary<string, AerValue>(n); for (var i = 0; i < n; i++) m[ReadString(d, ref o)] = ReadValue(d, ref o); return AerValue.Object(m); }
    private static AerValue ReadTable(ReadOnlySpan<byte> d, ref int o) { var c = checked((int)ReadInt64(d, ref o)); var cols = new string[c]; for (var i = 0; i < c; i++) cols[i] = ReadString(d, ref o); var r = checked((int)ReadInt64(d, ref o)); var rows = new List<IReadOnlyList<AerValue>>(r); for (var i = 0; i < r; i++) { var row = new AerValue[c]; for (var j = 0; j < c; j++) row[j] = ReadValue(d, ref o); rows.Add(row); } return AerValue.Table(new AerTable(cols, rows)); }

    private static void WriteInt64(Stream s, long v) { Span<byte> b = stackalloc byte[8]; BinaryPrimitives.WriteInt64LittleEndian(b, v); s.Write(b); }
    private static long ReadInt64(ReadOnlySpan<byte> d, ref int o) { Ensure(d, o, 8); var v = BinaryPrimitives.ReadInt64LittleEndian(d[o..]); o += 8; return v; }
    private static void WriteString(Stream s, string v) => WriteBytes(s, Encoding.UTF8.GetBytes(v));
    private static string ReadString(ReadOnlySpan<byte> d, ref int o) => Encoding.UTF8.GetString(ReadBytes(d, ref o));
    private static void WriteBytes(Stream s, byte[] b) { WriteInt64(s, b.Length); s.Write(b); }
    private static byte[] ReadBytes(ReadOnlySpan<byte> d, ref int o) { var n = checked((int)ReadInt64(d, ref o)); Ensure(d, o, n); var b = d[o..(o + n)].ToArray(); o += n; return b; }
    private static byte ReadByte(ReadOnlySpan<byte> d, ref int o) { Ensure(d, o, 1); return d[o++]; }
    private static void Ensure(ReadOnlySpan<byte> d, int o, int n) { if (o < 0 || n < 0 || o > d.Length - n) throw new FormatException("Truncated AER binary payload."); }
}
