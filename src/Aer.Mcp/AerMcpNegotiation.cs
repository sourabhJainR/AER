namespace Aer.Mcp;

/// <summary>Client capabilities used to select an AER MCP representation.</summary>
public sealed record AerMcpCapabilities(
    bool Text = false,
    bool Ai = false,
    bool Binary = false)
{
    public static AerMcpCapabilities JsonOnly { get; } = new();

    public static AerMcpCapabilities FromProfiles(IEnumerable<string> profiles)
    {
        ArgumentNullException.ThrowIfNull(profiles);
        var set = profiles.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return new AerMcpCapabilities(
            set.Contains("aer.text.v1"),
            set.Contains("aer.ai.v1"),
            set.Contains("aer.binary.v1"));
    }
}

/// <summary>Transport-neutral AER payload selected for an MCP result.</summary>
public sealed record AerMcpPayload(
    AerMcpProfile Profile,
    string ContentType,
    string? Text,
    byte[]? Binary)
{
    public bool IsBinary => Binary is not null;
}

public static class AerMcpNegotiator
{
    public static AerMcpProfile SelectProfile(
        AerMcpCapabilities capabilities,
        AerMcpProfile preferred = AerMcpProfile.Ai)
    {
        ArgumentNullException.ThrowIfNull(capabilities);

        if (preferred == AerMcpProfile.Ai && capabilities.Ai) return AerMcpProfile.Ai;
        if (preferred == AerMcpProfile.Binary && capabilities.Binary) return AerMcpProfile.Binary;
        if (preferred == AerMcpProfile.Text && capabilities.Text) return AerMcpProfile.Text;
        if (capabilities.Ai) return AerMcpProfile.Ai;
        if (capabilities.Text) return AerMcpProfile.Text;
        if (capabilities.Binary) return AerMcpProfile.Binary;
        return AerMcpProfile.Json;
    }

    public static AerMcpPayload Encode(
        object? value,
        AerMcpCapabilities capabilities,
        AerMcpProfile preferred = AerMcpProfile.Ai,
        AerSchema? schema = null)
    {
        var profile = SelectProfile(capabilities, preferred);
        return profile switch
        {
            AerMcpProfile.Ai => new(profile, AerMcpProfileEncoder.ContentType(profile), AerMcpProfileEncoder.EncodeAi(value, schema), null),
            AerMcpProfile.Text => new(profile, AerMcpProfileEncoder.ContentType(profile), AerMcpProfileEncoder.EncodeText(value), null),
            AerMcpProfile.Binary => new(profile, AerMcpProfileEncoder.ContentType(profile), null, AerMcpProfileEncoder.EncodeBinary(value)),
            _ => new(profile, AerMcpProfileEncoder.ContentType(profile), System.Text.Json.JsonSerializer.Serialize(value), null)
        };
    }
}
