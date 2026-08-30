namespace Aer;

/// <summary>Limits used to protect AER parsing and binary decoding from untrusted input.</summary>
public sealed record AerParseOptions(
    int MaxDocumentBytes = 4 * 1024 * 1024,
    int MaxLines = 100_000,
    int MaxDepth = 128,
    int MaxCollectionItems = 1_000_000,
    int MaxScalarLength = 1_000_000)
{
    public void Validate()
    {
        if (MaxDocumentBytes <= 0) throw new ArgumentOutOfRangeException(nameof(MaxDocumentBytes));
        if (MaxLines <= 0) throw new ArgumentOutOfRangeException(nameof(MaxLines));
        if (MaxDepth < 0) throw new ArgumentOutOfRangeException(nameof(MaxDepth));
        if (MaxCollectionItems < 0) throw new ArgumentOutOfRangeException(nameof(MaxCollectionItems));
        if (MaxScalarLength <= 0) throw new ArgumentOutOfRangeException(nameof(MaxScalarLength));
    }
}
