using System.Security.Cryptography;
using System.Text;

namespace Aer;

/// <summary>Computes a stable SHA-256 hash from the canonical AER value.</summary>
public static class AerHash
{
    /// <summary>Returns a SHA-256 hash over deterministic canonical text.</summary>
    public static string Sha256(AerValue value)
    {
        var canonical = Canonicalize(value);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    }

    private static string Canonicalize(AerValue value) => value.Kind switch
    {
        AerKind.Null => "n",
        AerKind.Bool => (bool)value.Data! ? "b1" : "b0",
        AerKind.Int => $"i{(long)value.Data!}",
        AerKind.Float => $"f{BitConverter.DoubleToInt64Bits((double)value.Data!):x16}",
        AerKind.Decimal => $"d{((decimal)value.Data!).ToString(System.Globalization.CultureInfo.InvariantCulture)}",
        AerKind.String => "s" + Convert.ToBase64String(Encoding.UTF8.GetBytes((string)value.Data!)),
        AerKind.Bytes => "x" + Convert.ToBase64String((byte[])value.Data!),
        AerKind.DateTime => "t" + ((DateTimeOffset)value.Data!).ToUniversalTime().ToString("O"),
        AerKind.Duration => "u" + ((TimeSpan)value.Data!).Ticks,
        AerKind.Reference => "r" + (string)value.Data!,
        AerKind.Array => "a[" + string.Join(',', ((IReadOnlyList<AerValue>)value.Data!).Select(Canonicalize)) + "]",
        AerKind.Object => "o{" + string.Join(',', ((IReadOnlyDictionary<string,AerValue>)value.Data!).OrderBy(x => x.Key, StringComparer.Ordinal).Select(x => Convert.ToBase64String(Encoding.UTF8.GetBytes(x.Key)) + ":" + Canonicalize(x.Value))) + "}",
        AerKind.Table => "q{" + string.Join(',', ((AerTable)value.Data!).Columns.Select(c => Convert.ToBase64String(Encoding.UTF8.GetBytes(c)))) + "}|" + string.Join(';', ((AerTable)value.Data!).Rows.Select(r => "[" + string.Join(',', r.Select(Canonicalize)) + "]")),
        _ => throw new ArgumentOutOfRangeException(nameof(value.Kind))
    };
}
