using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Aer;

var baseDirectory = AppContext.BaseDirectory;
var repoRoot = Path.GetFullPath(Path.Combine(baseDirectory, "../../../../.."));
var corpusPath = Path.Combine(repoRoot, "benchmarks", "corpus", "workloads.json");
var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
var corpus = JsonSerializer.Deserialize<Corpus>(File.ReadAllText(corpusPath), jsonOptions)
    ?? throw new InvalidOperationException("Benchmark corpus is invalid.");
if (corpus.Workloads is null || corpus.Workloads.Count == 0)
    throw new InvalidOperationException("Benchmark corpus contains no workloads.");

const int warmup = 50;
const int iterations = 500;
var results = new List<BenchmarkResult>(corpus.Workloads.Count);

foreach (var workload in corpus.Workloads)
{
    if (string.IsNullOrWhiteSpace(workload.Id) || workload.Value.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        throw new InvalidOperationException($"Benchmark workload '{workload.Id}' is invalid.");

    var value = AerValue.FromJson(workload.Value);
    var jsonText = JsonSerializer.Serialize(workload.Value);
    var jsonBytes = Encoding.UTF8.GetByteCount(jsonText);
    var aerText = AER.Serialize(value);
    var aerAi = AerAiAdapter.Encode(value).Payload;
    var aerBinary = AER.ToBinary(value);

    var expected = JsonSerializer.SerializeToElement(workload.Value);
    var textRoundTrip = AER.Deserialize(aerText).ToJsonElement();
    var binaryRoundTrip = AER.FromBinary(aerBinary).ToJsonElement();

    results.Add(new BenchmarkResult(
        workload.Id,
        workload.Category,
        jsonBytes,
        Encoding.UTF8.GetByteCount(aerText),
        Encoding.UTF8.GetByteCount(aerAi),
        aerBinary.Length,
        Ratio(Encoding.UTF8.GetByteCount(aerText), jsonBytes),
        Ratio(Encoding.UTF8.GetByteCount(aerAi), jsonBytes),
        JsonElement.DeepEquals(expected, textRoundTrip),
        JsonElement.DeepEquals(expected, binaryRoundTrip),
        Measure(warmup, iterations, () => _ = AER.Serialize(value)),
        Measure(warmup, iterations, () => _ = AER.Deserialize(aerText)),
        Measure(warmup, iterations, () => _ = AER.ToBinary(value)),
        Measure(warmup, iterations, () => _ = AER.FromBinary(aerBinary))));
}

if (results.Any(r => !r.TextRoundTrip || !r.BinaryRoundTrip))
    throw new InvalidOperationException("One or more effectiveness workloads failed canonical round-trip fidelity checks.");

var report = new BenchmarkReport(
    "1.1",
    typeof(AER).Assembly.GetName().Version?.ToString() ?? "unknown",
    DateTimeOffset.UtcNow,
    corpus.Version,
    results);

var outputDirectory = Path.Combine(repoRoot, "artifacts", "benchmarks");
Directory.CreateDirectory(outputDirectory);
var outputPath = Path.Combine(outputDirectory, "ai-effectiveness.json");
File.WriteAllText(outputPath, JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine(JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));

static double Ratio(int numerator, int denominator) => denominator == 0 ? 0 : Math.Round((double)numerator / denominator, 6);

static double Measure(int warmup, int iterations, Action action)
{
    for (var i = 0; i < warmup; i++) action();
    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();
    var timer = Stopwatch.StartNew();
    for (var i = 0; i < iterations; i++) action();
    timer.Stop();
    return Math.Round(timer.Elapsed.TotalMilliseconds / iterations, 6);
}

public sealed record Corpus(string Version, IReadOnlyList<Workload> Workloads);
public sealed record Workload(string Id, string Category, string Description, JsonElement Value);
public sealed record BenchmarkResult(
    string Workload,
    string Category,
    int JsonBytes,
    int AerTextBytes,
    int AerAiBytes,
    int AerBinaryBytes,
    double AerTextToJsonRatio,
    double AerAiToJsonRatio,
    bool TextRoundTrip,
    bool BinaryRoundTrip,
    double EncodeTextMsPerOp,
    double DecodeTextMsPerOp,
    double EncodeBinaryMsPerOp,
    double DecodeBinaryMsPerOp);
public sealed record BenchmarkReport(
    string BenchmarkVersion,
    string AerRuntimeVersion,
    DateTimeOffset TimestampUtc,
    string CorpusVersion,
    IReadOnlyList<BenchmarkResult> Results);
