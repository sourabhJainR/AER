using Aer;

namespace Aer.Mcp;

public enum AerMcpProfile
{
    Json,
    Text,
    Ai,
    Agent,
    Binary
}

public static class AerMcpProfileEncoder
{
    public static string EncodeText(object? value) => AER.Serialize(value);

    public static string EncodeAi(object? value, AerSchema? schema = null) => AER.ToAi(value, schema);

    public static string EncodeAgent(AerAgentFrame frame) => AerAgent.EncodeAi(frame);

    public static byte[] EncodeBinary(object? value) => AER.ToBinary(value);

    public static byte[] EncodeAgentBinary(AerAgentFrame frame) => AerAgent.EncodeBinaryFrame(frame);

    public static string ContentType(AerMcpProfile profile) => profile switch
    {
        AerMcpProfile.Json => "application/json",
        AerMcpProfile.Text => "application/aer; profile=text",
        AerMcpProfile.Ai => "application/aer; profile=ai",
        AerMcpProfile.Agent => "application/aer; profile=agent",
        AerMcpProfile.Binary => "application/aer; profile=binary",
        _ => throw new ArgumentOutOfRangeException(nameof(profile), profile, null)
    };
}
