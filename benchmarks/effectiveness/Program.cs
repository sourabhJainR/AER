using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Aer;

var baseDirectory = AppContext.BaseDirectory;
var repoRoot = Path.GetFullPath(Path.Combine(baseDirectory, "../../../../.."));
var corpusPath = Path.Combine(repoRoot, "benchmarks", "corpus", "workloads.json");
var corpus = JsonSerializer.Deserialize<Corpus>(File.ReadAllText(corpusPath))
    ?? throw new InvalidOperationException("Benchmark corpus is invalid.");

const int warmup = 50;
const int iterations = 500;
var results = new List<BenchmarkResult>(corpus.Workloads.Count);

foreach (var workload in corpus.Workloads)
{
    var value = AerValue.FromJson(workload.Value);
    var jsonText = JsonSerializer.Serialize(workload.Value);
    var jsonBytes = Encoding.UTF8.GetByteCount(jsonText);
    var aerText = AER.Serialize(value);
    var aerAi = AerAiAdapter.Encode(value).Payload;
    var aerBinary = AER.ToBinary(value);

    var expected = JsonSerializer.SerializeToElement(workload.Value);
    var textRoundTrip = AER.Deserialize(aerText).ToJsonElement();
    var binaryRoundTrip = AER.FromBinary(aerBinary).ToJsonElement();

    var encodeText = Measure(warmup, iterations, () => _ = AER.Serialize(value));
    var decodeText = Measure(warmup, iterations, () => _ = AER.Deserialize(aerText));
    var encodeBinary = Measure(warmup, iterations, () => _ = AER.ToBinary(value));
    var decodeBinary = Measure(warmup, iterations, () => _ = AER.FromBinary(aerBinary));

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
        encodeText,
        decodeText,
        encodeBinary,
        decodeBinary));
}

var report = new BenchmarkReport(
    "1.0",
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
