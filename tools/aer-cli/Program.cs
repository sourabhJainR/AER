using System.Text.Json;
using Aer;

static void Usage()
{
    Console.Error.WriteLine("AER CLI");
    Console.Error.WriteLine("  aer convert <file|-> --to aer|json");
    Console.Error.WriteLine("  aer validate <file|->");
    Console.Error.WriteLine("  aer fmt <file|->");
    Console.Error.WriteLine("  aer benchmark <file|->");
}

if (args.Length == 0) { Usage(); return 2; }

var command = args[0].ToLowerInvariant();
var input = args.Length > 1 ? args[1] : "-";
var text = input == "-" ? Console.In.ReadToEnd() : File.ReadAllText(input);

try
{
    switch (command)
    {
        case "convert":
        {
            if (args.Length < 4 || args[2] != "--to") { Usage(); return 2; }
            var target = args[3].ToLowerInvariant();
            if (target == "aer")
            {
                var value = AerValue.FromJson(JsonDocument.Parse(text).RootElement);
                Console.Write(AER.Serialize(value));
                return 0;
            }
            if (target == "json")
            {
                var value = AER.Deserialize(text);
                Console.WriteLine(value.ToJsonElement().GetRawText());
                return 0;
            }
            Usage(); return 2;
        }
        case "validate":
            _ = AER.Deserialize(text);
            Console.WriteLine("valid");
            return 0;
        case "fmt":
            Console.Write(AER.Serialize(AER.Deserialize(text)));
            return 0;
        case "benchmark":
        {
            var value = AerValue.FromJson(JsonDocument.Parse(text).RootElement);
            var json = text;
            var aer = AER.Serialize(value);
            var ai = AerAiAdapter.Encode(value).Payload;
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                jsonBytes = System.Text.Encoding.UTF8.GetByteCount(json),
                aerBytes = System.Text.Encoding.UTF8.GetByteCount(aer),
                aerAiBytes = System.Text.Encoding.UTF8.GetByteCount(ai),
                aerReductionPct = Reduction(json, aer),
                aerAiReductionPct = Reduction(json, ai)
            }, new JsonSerializerOptions { WriteIndented = true }));
            return 0;
        }
        default:
            Usage(); return 2;
    }
}
catch (Exception ex) when (ex is FormatException or JsonException or InvalidOperationException or ArgumentException)
{
    Console.Error.WriteLine($"error: {ex.Message}");
    return 1;
}

static double Reduction(string baseline, string candidate)
{
    var b = System.Text.Encoding.UTF8.GetByteCount(baseline);
    var c = System.Text.Encoding.UTF8.GetByteCount(candidate);
    return b == 0 ? 0 : Math.Round((b - c) * 100.0 / b, 2);
}
