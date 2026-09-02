using System.Text.Json;
using Aer;

var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
var directory = Path.Combine(root, "conformance", "valid");
var files = Directory.GetFiles(directory, "*.json", SearchOption.TopDirectoryOnly);
var failures = 0;

foreach (var file in files)
{
    using var doc = JsonDocument.Parse(File.ReadAllText(file));
    var test = doc.RootElement;
    var id = test.GetProperty("id").GetString() ?? Path.GetFileName(file);
    var text = test.GetProperty("text").GetString() ?? string.Empty;
    var expected = test.GetProperty("canonical").GetRawText();

    try
    {
        var value = AER.Deserialize(text);
        var actual = value.ToJsonElement().GetRawText();
        using var expectedDoc = JsonDocument.Parse(expected);
        using var actualDoc = JsonDocument.Parse(actual);
        if (!JsonElement.DeepEquals(expectedDoc.RootElement, actualDoc.RootElement))
        {
            Console.Error.WriteLine($"FAIL {id}: canonical mismatch"); failures++;
        }
        else Console.WriteLine($"PASS {id}");
    }
    catch (Exception ex) { Console.Error.WriteLine($"FAIL {id}: {ex.Message}"); failures++; }
}

var negative = new (string Id, Action Test)[]
{
    ("duplicate-key", () => AER.Deserialize("a: 1\na: 2")),
    ("invalid-base64", () => AER.Deserialize("data: b64\"not-base64!\"")),
    ("array-count-mismatch", () => AER.Deserialize("items[2]: 1")),
    ("unsupported-version", () => AER.Deserialize("@aer 99\na: 1")),
    ("binary-trailing-bytes", () => AerBinary.Decode(AerBinary.Encode(AerValue.Int(1)).Concat(new byte[] { 0xFF }).ToArray())),
    ("binary-invalid-bool", () => AerBinary.Decode(new byte[] { (byte)'A', (byte)'E', (byte)'R', (byte)'B', 1, (byte)AerKind.Bool, 2 })),
    ("agent-unknown-kind", () => AerAgentFrame.FromValue(AER.Deserialize("kind:unknown\nseq:1\nid:x"))),
    ("agent-invalid-sequence", () => { var t = new AerAgentTranscript(); t.Append(AerAgentFrame.Text(AerAgentFrameKind.User, 2, "a", "x")); t.Append(AerAgentFrame.Text(AerAgentFrameKind.User, 1, "b", "y")); }),
};

foreach (var (id, test) in negative)
{
    try { test(); Console.Error.WriteLine($"FAIL {id}: malformed input was accepted"); failures++; }
    catch (AerFormatException) { Console.WriteLine($"PASS {id}"); }
}

var call = AerAgentFrame.ToolCall(
    1,
    "call-1",
    "search_repo",
    AerValue.Object(new Dictionary<string, AerValue> { ["query"] = AerValue.String("AerParser") }));
var result = AerAgentFrame.ToolResult(
    2,
    "result-1",
    "search_repo",
    AerValue.Array(new[] { AerValue.String("src/Aer.Core/AerParser.cs") }),
    summary: "1 match");
var transcript = new AerAgentTranscript();
transcript.Append(call);
transcript.Append(result);

var binary = AerAgent.EncodeBinaryFrames(transcript.Frames);
var decoded = AerAgent.DecodeBinaryFrames(binary);
if (decoded.Count != 2 || decoded[0].Kind != AerAgentFrameKind.ToolCall || decoded[1].Name != "search_repo")
{
    Console.Error.WriteLine("FAIL agent-binary-roundtrip"); failures++;
}
else Console.WriteLine("PASS agent-binary-roundtrip");

var compacted = AerAgentContext.MicroCompact(transcript.Frames, keepRecentFrames: 1);
if (compacted.Count != 2 || compacted[0].Data is null || compacted[1].Data is null)
{
    Console.Error.WriteLine("FAIL agent-microcompaction-boundary"); failures++;
}
else Console.WriteLine("PASS agent-microcompaction-boundary");

var toolOnly = new[]
{
    call,
    result with { Sequence = 3, Id = "result-2", Summary = "old result" },
    result with { Sequence = 4, Id = "result-3" }
};
var compactedToolOnly = AerAgentContext.MicroCompact(toolOnly, keepRecentFrames: 1);
if (compactedToolOnly[1].Data is not null || !compactedToolOnly[1].Truncated || compactedToolOnly[2].Data is null)
{
    Console.Error.WriteLine("FAIL agent-tool-result-microcompaction"); failures++;
}
else Console.WriteLine("PASS agent-tool-result-microcompaction");

return failures == 0 ? 0 : 1;
