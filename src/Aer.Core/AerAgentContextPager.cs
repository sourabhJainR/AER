using System.Security.Cryptography;

namespace Aer;

/// <summary>
/// A page-addressable, immutable slice of an agent context. Pages are deliberately smaller than
/// a full transcript so runtimes can reuse unchanged prefixes instead of rebuilding the whole
/// model context. AER stores cache metadata, not model KV tensors.
/// </summary>
public sealed record AerAgentContextPage(
    string PageId,
    long StartSequence,
    long EndSequence,
    int TokenEstimate,
    string ContentHash,
    string PrefixHash,
    IReadOnlyList<AerAgentFrame> Frames,
    bool Pinned = false)
{
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(PageId)) throw new AerFormatException("AER005", "Context page id is required.");
        if (StartSequence < 0 || EndSequence < StartSequence)
            throw new AerFormatException("AER005", "Context page sequence range is invalid.");
        if (TokenEstimate < 0) throw new AerFormatException("AER005", "Context page token estimate cannot be negative.");
        if (string.IsNullOrWhiteSpace(ContentHash) || ContentHash.Length != 64)
            throw new AerFormatException("AER005", "Context page content hash must be a SHA-256 hex digest.");
        if (string.IsNullOrWhiteSpace(PrefixHash) || PrefixHash.Length != 64)
            throw new AerFormatException("AER005", "Context page prefix hash must be a SHA-256 hex digest.");
        if (Frames.Count == 0) throw new AerFormatException("AER005", "Context pages cannot be empty.");
        if (Frames[0].Sequence != StartSequence || Frames[^1].Sequence != EndSequence)
            throw new AerFormatException("AER005", "Context page sequence range does not match its frames.");
    }
}

/// <summary>Cache-reuse decision for a paged agent context.</summary>
public sealed record AerAgentContextCachePlan(
    IReadOnlyList<AerAgentContextPage> Pages,
    int ReusedPrefixPages,
    int ReusedTokens,
    int TotalTokens)
{
    public bool HasReusablePrefix => ReusedPrefixPages > 0;
}

/// <summary>
/// Deterministic paging and cache-key helpers inspired by paged KV-cache designs.
/// AER never stores or manipulates provider-specific KV tensors; it creates stable page boundaries
/// and hashes that an inference runtime can map to its own KV cache.
/// </summary>
public static class AerAgentContextPager
{
    public static IReadOnlyList<AerAgentContextPage> BuildPages(
        IReadOnlyList<AerAgentFrame> frames,
        int targetTokens,
        Func<AerAgentFrame, int> tokenEstimator,
        bool pinFirstPage = true)
    {
        if (targetTokens <= 0) throw new ArgumentOutOfRangeException(nameof(targetTokens));
        ArgumentNullException.ThrowIfNull(tokenEstimator);
        if (frames.Count == 0) return Array.Empty<AerAgentContextPage>();

        var pages = new List<AerAgentContextPage>();
        var current = new List<AerAgentFrame>();
        var currentTokens = 0;
        long? previousSequence = null;
        var ids = new HashSet<string>(StringComparer.Ordinal);

        foreach (var frame in frames)
        {
            frame.Validate();
            if (previousSequence is long previous && frame.Sequence <= previous)
                throw new AerFormatException("AER005", "Context frames must have strictly increasing sequences.");
            if (!ids.Add(frame.Id))
                throw new AerFormatException("AER005", $"Duplicate context frame id '{frame.Id}'.");
            previousSequence = frame.Sequence;

            var estimate = tokenEstimator(frame);
            if (estimate < 0) throw new ArgumentException("Token estimator cannot return a negative value.", nameof(tokenEstimator));

            // Never split a frame. If one frame exceeds the target, it gets a page by itself.
            if (current.Count > 0 && currentTokens + estimate > targetTokens)
            {
                pages.Add(CreatePage(current, currentTokens, pages.Count, pages.Count == 0 && pinFirstPage, pages.Count == 0 ? string.Empty : pages[^1].PrefixHash));
                current = new List<AerAgentFrame>();
                currentTokens = 0;
            }

            current.Add(frame);
            currentTokens = checked(currentTokens + estimate);
        }

        if (current.Count > 0)
            pages.Add(CreatePage(current, currentTokens, pages.Count, pages.Count == 0 && pinFirstPage, pages.Count == 0 ? string.Empty : pages[^1].PrefixHash));

        return pages;
    }

    public static AerAgentContextCachePlan PlanReuse(
        IReadOnlyList<AerAgentContextPage> previous,
        IReadOnlyList<AerAgentContextPage> current)
    {
        ArgumentNullException.ThrowIfNull(previous);
        ArgumentNullException.ThrowIfNull(current);

        var reusedPages = 0;
        var reusedTokens = 0;
        var limit = Math.Min(previous.Count, current.Count);
        while (reusedPages < limit)
        {
            previous[reusedPages].Validate();
            current[reusedPages].Validate();
            if (!string.Equals(previous[reusedPages].ContentHash, current[reusedPages].ContentHash, StringComparison.Ordinal))
                break;
            reusedTokens = checked(reusedTokens + current[reusedPages].TokenEstimate);
            reusedPages++;
        }

        var totalTokens = current.Sum(p => p.TokenEstimate);
        return new AerAgentContextCachePlan(current, reusedPages, reusedTokens, totalTokens);
    }

    /// <summary>Returns a scoped SHA-256 key that an inference runtime can map to its KV cache.</summary>
    public static string ComputeCacheKey(AerAgentContextPage page, string? modelScope = null)
    {
        ArgumentNullException.ThrowIfNull(page);
        page.Validate();
        return Sha256Hex($"aer-kv-page-v1\n{modelScope ?? string.Empty}\n{page.ContentHash}\n{page.PrefixHash}");
    }

    private static AerAgentContextPage CreatePage(
        IReadOnlyList<AerAgentFrame> frames,
        int tokenEstimate,
        int index,
        bool pinned,
        string previousPrefixHash)
    {
        var bytes = frames.SelectMany(f => AerStream.EncodeFrame(f.ToValue())).ToArray();
        var contentHash = Sha256Hex(bytes);
        var prefixHash = Sha256Hex($"aer-context-prefix-v1\n{previousPrefixHash}\n{contentHash}");
        var pageId = $"p{index:D6}-{contentHash[..12]}";
        var page = new AerAgentContextPage(pageId, frames[0].Sequence, frames[^1].Sequence, tokenEstimate, contentHash, prefixHash, frames.ToArray(), pinned);
        page.Validate();
        return page;
    }

    private static string Sha256Hex(string text) => Sha256Hex(System.Text.Encoding.UTF8.GetBytes(text));

    private static string Sha256Hex(byte[] bytes)
        => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
}
