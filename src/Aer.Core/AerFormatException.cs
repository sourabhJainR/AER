namespace Aer;

/// <summary>Represents a deterministic AER parsing or wire-format error.</summary>
public sealed class AerFormatException : FormatException
{
    /// <summary>Creates an AER error with a stable code.</summary>
    public AerFormatException(string code, string message) : base($"{code}: {message}") => Code = code;

    /// <summary>Stable machine-readable AER error code.</summary>
    public string Code { get; }
}
