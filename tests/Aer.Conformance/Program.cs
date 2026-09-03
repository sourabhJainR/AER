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
    ("agent-negative-sequence", () => AerAgentFrame.FromValue(AER.Deserialize("kind:user\nseq:-1\nid:x"))),
    ("agent-invalid-attempt", () => AerAgentFrame.FromValue(AER.Deserialize("kind:tool_call\nseq:1\nid:x\nname:grep\nattempt:0"))),
};

foreach (var (id, test) in negative)
{
    try { test(); Console.Error.WriteLine($"FAIL {id}: malformed input was accepted"); failures++; }
    catch (AerFormatException) { Console.WriteLine($"PASS {id}"); }
}

var emptyRoot = AerValue.Object(new Dictionary<string, AerValue>());
var emptyRootText = AER.Serialize(emptyRoot);
if (!string.Equals(emptyRootText, "{}\n", StringComparison.Ordinal))
{
    Console.Error.WriteLine($"FAIL empty-root-object: unexpected encoding '{emptyRootText}'");
    failures++;
}
else
{
    var emptyRootRoundTrip = AER.Deserialize(emptyRootText);
    if (emptyRootRoundTrip.Kind != AerKind.Object || ((IReadOnlyDictionary<string, AerValue>)emptyRootRoundTrip.Data!).Count != 0)
    {
        Console.Error.WriteLine("FAIL empty-root-object: roundtrip changed the root value");
        failures++;
    }
    else Console.WriteLine("PASS empty-root-object");
}
var call = AerAgentFrame.ToolCall(
    1,
    "call-1",
    "search_repo",
    AerValue.Object(new Dictionary<string, AerValue> { ["query"] = AerValue.String("AerParser") }),
    turnId: "turn-1",
    attempt: 1);
var result = AerAgentFrame.ToolResult(
    2,
    "result-1",
    "search_repo",
    AerValue.Array(new[] { AerValue.String("src/Aer.Core/AerParser.cs") }),
    summary: "1 match",
    relatedId: "call-1",
    turnId: "turn-1",
    retryable: true,
    attempt: 1);
var transcript = new AerAgentTranscript();
transcript.Append(AerAgentFrame.Text(AerAgentFrameKind.TurnStarted, 0, "turn-start", "", turnId: "turn-1") with { Boundary = "before_model" });
transcript.Append(call);
transcript.Append(result);

var binary = AerAgent.EncodeBinaryFrames(transcript.Frames);
var decoded = AerAgent.DecodeBinaryFrames(binary);
if (decoded.Count != 3 || decoded[1].Kind != AerAgentFrameKind.ToolCall || decoded[2].RelatedId != "call-1" || !decoded[2].Retryable || decoded[2].Attempt != 1)
{
    Console.Error.WriteLine("FAIL agent-binary-roundtrip"); failures++;
}
else Console.WriteLine("PASS agent-binary-roundtrip");

var longSequence = AerAgentFrame.Text(AerAgentFrameKind.Observation, 5_000_000_000L, "large-seq", "ok");
var longRoundTrip = AerAgentFrame.FromValue(longSequence.ToValue());
if (longRoundTrip.Sequence != 5_000_000_000L)
{
    Console.Error.WriteLine("FAIL agent-long-sequence-roundtrip"); failures++;
}
else Console.WriteLine("PASS agent-long-sequence-roundtrip");

var compacted = AerAgentContext.MicroCompact(transcript.Frames, keepRecentFrames: 1);
if (compacted.Count != 3 || compacted[1].Data is null || compacted[2].Data is null)
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
if (compactedToolOnly[1].Data is not null || !compactedToolOnly[1].Truncated || compactedToolOnly[1].Kind != AerAgentFrameKind.ToolResult || compactedToolOnly[2].Data is null)
{
    Console.Error.WriteLine("FAIL agent-tool-result-microcompaction"); failures++;
}
else Console.WriteLine("PASS agent-tool-result-microcompaction");

var checkpoint = AerAgentContext.Checkpoint(5, "cp-1", "Repository explored and tests identified", turnId: "turn-1");
var checkpointRoundTrip = AerAgentFrame.FromValue(checkpoint.ToValue());
if (checkpointRoundTrip.Kind != AerAgentFrameKind.Checkpoint || checkpointRoundTrip.Boundary != "context_boundary")
{
    Console.Error.WriteLine("FAIL agent-checkpoint-roundtrip"); failures++;
}
else Console.WriteLine("PASS agent-checkpoint-roundtrip");

var pageFrames = new[]
{
    AerAgentFrame.Text(AerAgentFrameKind.User, 10, "p-user", "inspect repository"),
    call with { Sequence = 11, Id = "p-call" },
    result with { Sequence = 12, Id = "p-result", RelatedId = "p-call" },
    AerAgentFrame.Text(AerAgentFrameKind.Assistant, 13, "p-assistant", "found the parser"),
    AerAgentFrame.Text(AerAgentFrameKind.User, 14, "p-follow", "now check tests")
};
var pages = AerAgentContextPager.BuildPages(pageFrames, targetTokens: 4, tokenEstimator: _ => 2);
if (pages.Count != 3 || pages[0].StartSequence != 10 || pages[0].EndSequence != 11 || pages[2].EndSequence != 14 || !pages[0].Pinned)
{
    Console.Error.WriteLine("FAIL agent-paged-context-boundaries"); failures++;
}
else Console.WriteLine("PASS agent-paged-context-boundaries");

var pagesAgain = AerAgentContextPager.BuildPages(pageFrames, targetTokens: 4, tokenEstimator: _ => 2);
var reuse = AerAgentContextPager.PlanReuse(pages, pagesAgain);
if (reuse.ReusedPrefixPages != pages.Count || reuse.ReusedTokens != reuse.TotalTokens || !reuse.HasReusablePrefix)
{
    Console.Error.WriteLine("FAIL agent-page-cache-reuse"); failures++;
}
else Console.WriteLine("PASS agent-page-cache-reuse");

var changedTail = pageFrames.Select(f => f.Id == "p-follow" ? f with { Data = AerValue.String("check tests and benchmarks") } : f).ToArray();
var changedPages = AerAgentContextPager.BuildPages(changedTail, targetTokens: 4, tokenEstimator: _ => 2);
var partialReuse = AerAgentContextPager.PlanReuse(pages, changedPages);
if (partialReuse.ReusedPrefixPages != 2 || partialReuse.ReusedTokens != 8 || partialReuse.TotalTokens != 10)
{
    Console.Error.WriteLine("FAIL agent-page-partial-reuse"); failures++;
}
else Console.WriteLine("PASS agent-page-partial-reuse");

var cacheKey1 = AerAgentContextPager.ComputeCacheKey(pages[0], "model-v1");
var cacheKey2 = AerAgentContextPager.ComputeCacheKey(pages[0], "model-v2");
if (cacheKey1.Length != 64 || cacheKey1 == cacheKey2 || cacheKey1 != AerAgentContextPager.ComputeCacheKey(pages[0], "model-v1"))
{
    Console.Error.WriteLine("FAIL agent-page-cache-key"); failures++;
}
else Console.WriteLine("PASS agent-page-cache-key");

return failures == 0 ? 0 : 1;
