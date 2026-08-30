using System.Text.Json;
using Aer;

if (args.Length == 0)
{
    PrintHelp();
    return 0;
}

try
{
    switch (args[0].ToLowerInvariant())
    {
        case "convert" when args.Length >= 3:
            return ConvertFile(args[1], args[2]);
        case "optimize" when args.Length >= 2:
            return OptimizeFile(args[1]);
        case "validate" when args.Length >= 2:
            return ValidateFile(args[1]);
        case "inspect" when args.Length >= 2:
            return InspectFile(args[1]);
        case "version":
            Console.WriteLine("AER CLI 0.1.0");
            return 0;
        default:
            PrintHelp();
            return 2;
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine($"AER error: {ex.Message}");
    return 1;
}

static int ConvertFile(string inputPath, string outputPath)
{
    var source = File.ReadAllText(inputPath);
    var value = Path.GetExtension(inputPath).Equals(".aer", StringComparison.OrdinalIgnoreCase)
        ? AER.Deserialize(source)
        : AerValue.FromJson(JsonSerializer.Deserialize<JsonElement>(source));

    if (Path.GetExtension(outputPath).Equals(".aer", StringComparison.OrdinalIgnoreCase))
        File.WriteAllText(outputPath, AerWriter.Write(AerDocument.Create(value)));
    else
        File.WriteAllText(outputPath, JsonSerializer.Serialize(value.ToJsonElement(), new JsonSerializerOptions { WriteIndented = true }));
    return 0;
}

static int OptimizeFile(string inputPath)
{
    var source = File.ReadAllText(inputPath);
    var value = AER.Deserialize(source);
    Console.WriteLine(AerWriter.Write(AerDocument.Create(AER.Optimize(value))));
    return 0;
}

static int ValidateFile(string inputPath)
{
    var source = File.ReadAllText(inputPath);
    _ = AER.Deserialize(source);
    Console.WriteLine("valid");
    return 0;
}

static int InspectFile(string inputPath)
{
    var source = File.ReadAllText(inputPath);
    var value = AER.Deserialize(source);
    Console.WriteLine($"kind={value.Kind}");
    Console.WriteLine($"characters={source.Length}");
    Console.WriteLine($"binaryBytes={AER.ToBinary(value).Length}");
    return 0;
}

static void PrintHelp()
{
    Console.WriteLine("AER CLI");
    Console.WriteLine("  aer convert <input.json|input.aer> <output.aer|output.json>");
    Console.WriteLine("  aer optimize <input.aer>");
    Console.WriteLine("  aer validate <input.aer>");
    Console.WriteLine("  aer inspect <input.aer>");
    Console.WriteLine("  aer version");
}
