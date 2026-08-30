using System.Diagnostics;
using System.Text.Json;
using Aer;

var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../.."));
var sample = Path.Combine(root, "examples", "benchmark", "sample.json");
var json = File.ReadAllText(sample);
var element = JsonSerializer.Deserialize<JsonElement>(json);
var value = AerValue.FromJson(element);

const int iterations = 5000;
var aerText = AER.Serialize(value);
var aerBinary = AER.ToBinary(value);

Measure("JSON", iterations, () => _ = JsonSerializer.SerializeToUtf8Bytes(element));
Measure("AER Text", iterations, () => _ = AER.Serialize(value));
Measure("AER Binary", iterations, () => _ = AER.ToBinary(value));

Console.WriteLine();
Console.WriteLine($"JSON bytes={JsonSerializer.SerializeToUtf8Bytes(element).Length}");
Console.WriteLine($"AER text bytes={System.Text.Encoding.UTF8.GetByteCount(aerText)}");
Console.WriteLine($"AER binary bytes={aerBinary.Length}");

static void Measure(string name, int iterations, Action action)
{
    for (var i = 0; i < 250; i++) action();
    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();
    var sw = Stopwatch.StartNew();
    for (var i = 0; i < iterations; i++) action();
    sw.Stop();
    Console.WriteLine($"{name,-12} {sw.Elapsed.TotalMilliseconds,10:F2} ms total  {sw.Elapsed.TotalNanoseconds / iterations,10:F0} ns/op");
}
