using System.Text.Json;

namespace Aer;

public static class AER
{
    public static string Serialize(object? value, AerWriteOptions? options = null) => AerWriter.Write(AerDocument.Create(AerValue.FromObject(value)), options);
    public static AerValue Deserialize(string text) => AerParser.Parse(text).Root;
    public static byte[] ToBinary(object? value) => AerBinary.Encode(AerValue.FromObject(value));
    public static AerValue FromBinary(ReadOnlySpan<byte> bytes) => AerBinary.Decode(bytes);
    public static string ToAi(object? value, AerSchema? schema = null, AerAiOptions? options = null) => AerAiAdapter.Encode(AerValue.FromObject(value), schema, options).Payload;
    public static string ToJson(object? value, bool indented = false) => JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = indented });
    public static AerValue Optimize(object? value) => AerOptimizer.Optimize(AerValue.FromObject(value));
}
