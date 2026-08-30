namespace Aer;

/// <summary>Limits used to protect AER parsing from untrusted input.</summary>
public sealed record AerParseOptions(
    int MaxDocumentBytes = 4 * 1024 * 1024,
    int MaxLines = 100_000,
    int MaxDepth = 128,
    int MaxCollectionItems = 1_000_000,
    int MaxScalarLength = 1_000_000);
