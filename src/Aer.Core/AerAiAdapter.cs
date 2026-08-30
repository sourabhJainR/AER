namespace Aer;

public sealed record AerAiOptions(bool IncludeSchema = true, bool IncludeMeaning = true, int? MaxCharacters = null);

public sealed record AerAiEnvelope(string Format, string Payload, int Characters, string? Schema = null);

public static class AerAiAdapter
{
    public static AerAiEnvelope Encode(AerValue value, AerSchema? schema = null, AerAiOptions? options = null)
    {
        options ??= new();
        var optimized = AerOptimizer.Optimize(value);
        var payload = AerWriter.Write(AerDocument.Create(optimized), new AerWriteOptions(Pretty: false, IncludeHeader: false));
        if (options.MaxCharacters is int max && payload.Length > max) throw new InvalidOperationException($"AER payload is {payload.Length} characters; configured maximum is {max}.");
        var schemaText = options.IncludeSchema && schema is not null ? string.Join("\n", schema.Fields.Values.Select(FormatField)) : null;
        return new AerAiEnvelope("aer-ai/1", payload, payload.Length, schemaText);
    }

    public static string BuildToolResponse(AerValue value, string? instruction = null)
    {
        var envelope = Encode(value);
        return instruction is null ? envelope.Payload : instruction.TrimEnd() + "\n\n" + envelope.Payload;
    }

    private static string FormatField(AerField f)
    {
        var meta = new List<string>();
        if (f.Unit is not null) meta.Add("unit=" + f.Unit);
        if (f.Min is not null) meta.Add("min=" + f.Min.Value);
        if (f.Max is not null) meta.Add("max=" + f.Max.Value);
        if (f.Meaning is not null) meta.Add("meaning=\"" + f.Meaning.Replace("\"", "\\\"") + "\"");
        return f.Name + ":" + f.Type + (f.Required ? "!" : "") + (meta.Count == 0 ? "" : " @" + string.Join(" @", meta));
    }
}
