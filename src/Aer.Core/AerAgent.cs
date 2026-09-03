namespace Aer;

/// <summary>Canonical event kinds used at an AI agent/tool boundary.</summary>
public enum AerAgentFrameKind
{
    Session,
    TurnStarted,
    User,
    Steering,
    Assistant,
    ToolCall,
    PermissionRequested,
    PermissionResult,
    ToolResult,
    Observation,
    UserQuestion,
    FollowUp,
    Checkpoint,
    ContextCompacted,
    ContextMicrocompacted,
    Error,
    Abort,
    TurnFinished,
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
    string? RelatedId = null,
    string? TurnId = null,
    string? Boundary = null,
    bool Truncated = false,
    bool Retryable = false,
    int? Attempt = null,
    int? InputTokens = null,
    int? OutputTokens = null)
{
    public static AerAgentFrame Text(AerAgentFrameKind kind, long sequence, string id, string text, string? summary = null, string? turnId = null)
        => new(kind, sequence, id, AerValue.String(text), Summary: summary, TurnId: turnId);

    public static AerAgentFrame ToolCall(long sequence, string id, string tool, AerValue arguments, string? parentId = null, string? turnId = null, int? attempt = null)
        => new(AerAgentFrameKind.ToolCall, sequence, id, arguments, Name: tool, ParentId: parentId, TurnId: turnId, Attempt: attempt);

    public static AerAgentFrame ToolResult(long sequence, string id, string tool, AerValue result, bool truncated = false, string? summary = null, string? parentId = null, string? relatedId = null, string? turnId = null, bool retryable = false, int? attempt = null)
        => new(AerAgentFrameKind.ToolResult, sequence, id, result, Name: tool, Summary: summary, ParentId: parentId, RelatedId: relatedId, TurnId: turnId, Truncated: truncated, Retryable: retryable, Attempt: attempt);

    /// <summary>Converts the frame to the canonical AER value model using stable compact field names.</summary>
    public AerValue ToValue()
    {
        var fields = new Dictionary<string, AerValue>(StringComparer.Ordinal)
        {
            ["kind"] = AerValue.String(ToKindText(Kind)),
            ["seq"] = AerValue.Int(Sequence),
            ["id"] = AerValue.String(Id)
        };

        Add(fields, "data", Data);
        Add(fields, "name", Name);
        Add(fields, "status", Status);
        Add(fields, "summary", Summary);
        Add(fields, "parent", ParentId);
        Add(fields, "related", RelatedId);
        Add(fields, "turn", TurnId);
        Add(fields, "boundary", Boundary);
        if (Truncated) fields["truncated"] = AerValue.Bool(true);
        if (Retryable) fields["retryable"] = AerValue.Bool(true);
        if (Attempt is int attempt) fields["attempt"] = AerValue.Int(attempt);
        if (InputTokens is int input) fields["input_tokens"] = AerValue.Int(input);
        if (OutputTokens is int output) fields["output_tokens"] = AerValue.Int(output);
        return AerValue.Object(fields);
    }

    /// <summary>Reconstructs a frame from a canonical AER object.</summary>
    public static AerAgentFrame FromValue(AerValue value)
    {
        if (value.Kind != AerKind.Object) throw new AerFormatException("AER005", "Agent frame must be an object.");
        var fields = (IReadOnlyDictionary<string, AerValue>)value.Data!;
        var kind = ReadRequiredString(fields, "kind");
        var sequence = ReadRequiredLong(fields, "seq");
        var id = ReadRequiredString(fields, "id");
        var frame = new AerAgentFrame(
            ParseKind(kind), sequence, id,
            ReadOptional(fields, "data"),
            ReadOptionalString(fields, "name"),
            ReadOptionalString(fields, "status"),
            ReadOptionalString(fields, "summary"),
            ReadOptionalString(fields, "parent"),
            ReadOptionalString(fields, "related"),
            ReadOptionalString(fields, "turn"),
            ReadOptionalString(fields, "boundary"),
            ReadOptionalBool(fields, "truncated"),
            ReadOptionalBool(fields, "retryable"),
            ReadOptionalInt(fields, "attempt"),
            ReadOptionalInt(fields, "input_tokens"),
            ReadOptionalInt(fields, "output_tokens"));
        frame.Validate();
        return frame;
    }

    /// <summary>Validates invariants that are useful to every agent implementation.</summary>
    public void Validate()
    {
        if (Sequence < 0) throw new AerFormatException("AER005", "Agent frame sequence cannot be negative.");
        if (string.IsNullOrWhiteSpace(Id)) throw new AerFormatException("AER005", "Agent frame id is required.");
        if (Attempt is <= 0) throw new AerFormatException("AER005", "Agent frame attempt must be positive.");
        if (InputTokens is < 0 || OutputTokens is < 0) throw new AerFormatException("AER005", "Agent token counts cannot be negative.");
        if (Kind == AerAgentFrameKind.ToolCall && string.IsNullOrWhiteSpace(Name))
            throw new AerFormatException("AER005", "Tool call name is required.");
        if (Kind == AerAgentFrameKind.ToolResult && string.IsNullOrWhiteSpace(Name))
            throw new AerFormatException("AER005", "Tool result name is required.");
    }

    private static string ToKindText(AerAgentFrameKind kind) => kind switch
    {
        AerAgentFrameKind.ToolCall => "tool_call",
        AerAgentFrameKind.ToolResult => "tool_result",
        AerAgentFrameKind.TurnStarted => "turn_started",
        AerAgentFrameKind.PermissionRequested => "permission_requested",
        AerAgentFrameKind.PermissionResult => "permission_result",
        AerAgentFrameKind.UserQuestion => "user_question",
        AerAgentFrameKind.ContextCompacted => "context_compacted",
        AerAgentFrameKind.ContextMicrocompacted => "context_microcompacted",
        AerAgentFrameKind.TurnFinished => "turn_finished",
        _ => kind.ToString().ToLowerInvariant()
    };

    private static AerAgentFrameKind ParseKind(string value) => value switch
    {
        "session" => AerAgentFrameKind.Session,
        "turn_started" => AerAgentFrameKind.TurnStarted,
        "user" => AerAgentFrameKind.User,
        "steering" => AerAgentFrameKind.Steering,
        "assistant" => AerAgentFrameKind.Assistant,
        "tool_call" => AerAgentFrameKind.ToolCall,
        "permission_requested" => AerAgentFrameKind.PermissionRequested,
        "permission_result" => AerAgentFrameKind.PermissionResult,
        "tool_result" => AerAgentFrameKind.ToolResult,
        "observation" => AerAgentFrameKind.Observation,
        "user_question" => AerAgentFrameKind.UserQuestion,
        "follow_up" => AerAgentFrameKind.FollowUp,
        "checkpoint" => AerAgentFrameKind.Checkpoint,
        "context_compacted" => AerAgentFrameKind.ContextCompacted,
        "context_microcompacted" => AerAgentFrameKind.ContextMicrocompacted,
        "error" => AerAgentFrameKind.Error,
        "abort" => AerAgentFrameKind.Abort,
        "turn_finished" => AerAgentFrameKind.TurnFinished,
        "done" => AerAgentFrameKind.Done,
        _ => throw new AerFormatException("AER005", $"Unknown agent frame kind '{value}'.")
    };

    private static string ReadRequiredString(IReadOnlyDictionary<string, AerValue> fields, string key)
        => ReadOptionalString(fields, key) ?? throw new AerFormatException("AER005", $"Agent frame field '{key}' is required.");

    private static string? ReadOptionalString(IReadOnlyDictionary<string, AerValue> fields, string key)
    {
        if (!fields.TryGetValue(key, out var value)) return null;
        return value.Kind == AerKind.String ? (string)value.Data! : throw new AerFormatException("AER005", $"Agent frame field '{key}' must be a string.");
    }

    private static long ReadRequiredLong(IReadOnlyDictionary<string, AerValue> fields, string key)
        => ReadOptionalLong(fields, key) ?? throw new AerFormatException("AER005", $"Agent frame field '{key}' is required.");

    private static long? ReadOptionalLong(IReadOnlyDictionary<string, AerValue> fields, string key)
    {
        if (!fields.TryGetValue(key, out var value)) return null;
        return value.Kind == AerKind.Int ? (long)value.Data! : throw new AerFormatException("AER005", $"Agent frame field '{key}' must be an integer.");
    }

    private static int? ReadOptionalInt(IReadOnlyDictionary<string, AerValue> fields, string key)
    {
        var value = ReadOptionalLong(fields, key);
        if (value is null) return null;
        if (value.Value is < int.MinValue or > int.MaxValue) throw new AerFormatException("AER005", $"Agent frame field '{key}' is outside the supported integer range.");
        return (int)value.Value;
    }

    private static bool ReadOptionalBool(IReadOnlyDictionary<string, AerValue> fields, string key)
    {
        if (!fields.TryGetValue(key, out var value)) return false;
        return value.Kind == AerKind.Bool ? (bool)value.Data! : throw new AerFormatException("AER005", $"Agent frame field '{key}' must be a boolean.");
    }

    private static AerValue? ReadOptional(IReadOnlyDictionary<string, AerValue> fields, string key)
        => fields.TryGetValue(key, out var value) ? value : null;

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
    public static string EncodeAi(AerAgentFrame frame) => AerAiAdapter.Encode(frame.ToValue(), options: new AerAiOptions(IncludeSchema: false)).Payload;

    public static byte[] EncodeBinaryFrame(AerAgentFrame frame) => AerStream.EncodeFrame(frame.ToValue());

    public static byte[] EncodeBinaryFrames(IEnumerable<AerAgentFrame> frames)
    {
        using var stream = new MemoryStream();
        foreach (var frame in frames) stream.Write(EncodeBinaryFrame(frame));
        return stream.ToArray();
    }

    public static string EncodeToolResult(string tool, string callId, AerValue result, bool truncated = false, string? summary = null)
        => EncodeAi(AerAgentFrame.ToolResult(0, callId, tool, result, truncated, summary));

    public static IReadOnlyList<AerAgentFrame> DecodeBinaryFrames(ReadOnlySpan<byte> data, int maxFrameBytes = 16 * 1024 * 1024)
        => AerStream.DecodeFrames(data, maxFrameBytes).Select(AerAgentFrame.FromValue).ToArray();
}

/// <summary>
/// Deterministic context projection for coding-agent transcripts.
/// It applies the representation-level part of harness compaction: old tool bodies are elided,
/// while the original tool-result identity remains intact for replay and correlation.
/// </summary>
public static class AerAgentContext
{
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
                result[i] = frame with
                {
                    Data = null,
                    Truncated = true,
                    Summary = frame.Summary ?? "tool result elided by microcompaction"
                };
            else
                result[i] = frame;
        }
        return result;
    }

    /// <summary>
    /// Adds an explicit semantic checkpoint to a transcript. The summary is supplied by the
    /// harness/model; AER only transports it and never performs an LLM summarization itself.
    /// </summary>
    public static AerAgentFrame Checkpoint(long sequence, string id, string summary, AerValue? state = null, string? turnId = null)
        => new(AerAgentFrameKind.Checkpoint, sequence, id, state, Summary: summary, TurnId: turnId, Boundary: "context_boundary");
}

/// <summary>Append-only transcript with ordering, identity and turn correlation invariants.</summary>
public sealed class AerAgentTranscript
{
    private readonly List<AerAgentFrame> _frames = [];

    public IReadOnlyList<AerAgentFrame> Frames => _frames;

    public void Append(AerAgentFrame frame)
    {
        frame.Validate();
        if (_frames.Count > 0 && frame.Sequence <= _frames[^1].Sequence)
            throw new AerFormatException("AER005", "Agent frame sequence must increase monotonically.");
        if (_frames.Any(x => x.Id == frame.Id))
            throw new AerFormatException("AER005", $"Duplicate agent frame id '{frame.Id}'.");
        _frames.Add(frame);
    }

    public IReadOnlyList<AerAgentFrame> MicroCompact(int keepRecentFrames)
        => AerAgentContext.MicroCompact(_frames, keepRecentFrames);
}
