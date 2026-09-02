namespace Aer;

/// <summary>Canonical event kinds used at an AI agent/tool boundary.</summary>
public enum AerAgentFrameKind
{
    Session,
    User,
    Assistant,
    ToolCall,
    ToolResult,
    Observation,
    Checkpoint,
    Error,
    Done
}

/// <summary>
/// A deterministic, typed frame for agent transcripts, tool calls/results and checkpoints.
/// The payload stays an <see cref="AerValue"/> so the same frame can be emitted as AER-AI, AER-B,
/// or ordinary JSON without maintaining a second agent-specific data model.
/// </summary>
public sealed record AerAgentFrame(
    AerAgentFrameKind Kind,
    long Sequence,
    string Id,
    AerValue? Data = null,
    string? Name = null,
    string? Status = null,
    string? Summary = null,
    string? ParentId = null,
    bool Truncated = false,
    int? InputTokens = null,
    int? OutputTokens = null)
{
    /// <summary>Creates a user/assistant/observation frame with a text payload.</summary>
    public static AerAgentFrame Text(AerAgentFrameKind kind, long sequence, string id, string text, string? summary = null)
        => new(kind, sequence, id, AerValue.String(text), Summary: summary);

    /// <summary>Creates a tool-call frame. Arguments are kept typed rather than JSON strings.</summary>
    public static AerAgentFrame ToolCall(long sequence, string id, string tool, AerValue arguments, string? parentId = null)
        => new(AerAgentFrameKind.ToolCall, sequence, id, arguments, Name: tool, ParentId: parentId);

    /// <summary>Creates a tool-result frame. Large outputs can be represented by a summary and marked truncated.</summary>
    public static AerAgentFrame ToolResult(long sequence, string id, string tool, AerValue result, bool truncated = false, string? summary = null, string? parentId = null)
        => new(AerAgentFrameKind.ToolResult, sequence, id, result, Name: tool, Summary: summary, ParentId: parentId, Truncated: truncated);

    /// <summary>Converts the frame to the canonical AER value model using stable compact field names.</summary>
    public AerValue ToValue()
    {
        var fields = new Dictionary<string, AerValue>(StringComparer.Ordinal)
        {
            ["kind"] = AerValue.String(Kind switch
            {
                AerAgentFrameKind.ToolCall => "tool_call",
                AerAgentFrameKind.ToolResult => "tool_result",
                _ => Kind.ToString().ToLowerInvariant()
            }),
            ["seq"] = AerValue.Int(Sequence),
            ["id"] = AerValue.String(Id)
        };

        Add(fields, "data", Data);
        Add(fields, "name", Name);
        Add(fields, "status", Status);
        Add(fields, "summary", Summary);
        Add(fields, "parent", ParentId);
        if (Truncated) fields["truncated"] = AerValue.Bool(true);
        if (InputTokens is int input) fields["input_tokens"] = AerValue.Int(input);
        if (OutputTokens is int output) fields["output_tokens"] = AerValue.Int(output);
        return AerValue.Object(fields);
    }

    private static void Add(IDictionary<string, AerValue> fields, string key, string? value)
    {
        if (value is not null) fields[key] = AerValue.String(value);
    }

    private static void Add(IDictionary<string, AerValue> fields, string key, AerValue? value)
    {
        if (value is not null) fields[key] = value;
    }
}

/// <summary>Agent-oriented encoding helpers built on the canonical AER model.</summary>
public static class AerAgent
{
    /// <summary>Encode one frame using the token-aware AER-AI profile.</summary>
    public static string EncodeAi(AerAgentFrame frame) => AerAiAdapter.Encode(frame.ToValue(), options: new AerAiOptions(IncludeSchema: false)).Payload;

    /// <summary>Encode one frame as a deterministic AER-B stream frame.</summary>
    public static byte[] EncodeBinaryFrame(AerAgentFrame frame) => AerStream.EncodeFrame(frame.ToValue());

    /// <summary>Encode multiple frames as a sequence of length-prefixed AER-B frames.</summary>
    public static byte[] EncodeBinaryFrames(IEnumerable<AerAgentFrame> frames)
    {
        using var stream = new MemoryStream();
        foreach (var frame in frames)
        {
            var encoded = EncodeBinaryFrame(frame);
            stream.Write(encoded);
        }
        return stream.ToArray();
    }

    /// <summary>Encode a tool result as an AER-AI payload, preserving typed data and execution metadata.</summary>
    public static string EncodeToolResult(string tool, string callId, AerValue result, bool truncated = false, string? summary = null)
        => EncodeAi(AerAgentFrame.ToolResult(0, callId, tool, result, truncated, summary));
}

/// <summary>
/// Deterministic context projection for coding-agent transcripts.
/// It applies the same idea as agent-harness micro-compaction: keep recent frames intact while
/// replacing older tool payloads with metadata. No LLM call is made, so the operation is safe for
/// libraries, gateways and offline runtimes.
/// </summary>
public static class AerAgentContext
{
    /// <summary>
    /// Projects frames to a bounded recent window. Older tool results retain identity, tool name,
    /// summary and truncation metadata but their payload is removed. Frames are never reordered or
    /// split, so tool-call/result relationships remain intact.
    /// </summary>
    public static IReadOnlyList<AerAgentFrame> MicroCompact(
        IReadOnlyList<AerAgentFrame> frames,
        int keepRecentFrames,
        bool compactToolResults = true)
    {
        if (keepRecentFrames < 0) throw new ArgumentOutOfRangeException(nameof(keepRecentFrames));
        if (frames.Count <= keepRecentFrames || !compactToolResults) return frames.ToArray();

        var boundary = frames.Count - keepRecentFrames;
        var result = new AerAgentFrame[frames.Count];
        for (var i = 0; i < frames.Count; i++)
        {
            var frame = frames[i];
            if (i < boundary && frame.Kind == AerAgentFrameKind.ToolResult && frame.Data is not null)
            {
                result[i] = frame with { Data = null, Truncated = true, Summary = frame.Summary ?? "tool result elided by microcompaction" };
            }
            else
            {
                result[i] = frame;
            }
        }
        return result;
    }
}
