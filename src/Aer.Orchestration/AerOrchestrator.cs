using Aer;

namespace Aer.Orchestration;

public sealed record AerOrchestrationContext(
    string TaskId,
    string? Repository = null,
    string? Snapshot = null,
    IReadOnlyDictionary<string, object?>? Metadata = null);

public sealed record AerContextItem(
    string Name,
    object? Value,
    int Priority = 0,
    bool Required = false);

public sealed record AerOrchestrationPlan(
    string TaskId,
    IReadOnlyList<AerContextItem> Items,
    int EstimatedCharacters,
    int EstimatedBytes);

/// <summary>
/// Deterministic context planner for agent/orchestrator integrations.
/// It keeps selection policy separate from any LLM, MCP SDK or tool implementation.
/// </summary>
public sealed class AerOrchestrator
{
    public AerOrchestrationPlan Plan(
        AerOrchestrationContext context,
        IEnumerable<AerContextItem> candidates,
        int maxCharacters = 0)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(candidates);
        if (maxCharacters < 0) throw new ArgumentOutOfRangeException(nameof(maxCharacters));

        var ordered = candidates
            .OrderByDescending(x => x.Required)
            .ThenByDescending(x => x.Priority)
            .ThenBy(x => x.Name, StringComparer.Ordinal)
            .ToList();

        var selected = new List<AerContextItem>(ordered.Count);
        var characters = 0;
        foreach (var item in ordered)
        {
            var encoded = AER.ToAi(item.Value);
            var size = encoded.Length;
            if (!item.Required && maxCharacters > 0 && characters + size > maxCharacters)
                continue;
            if (item.Required && maxCharacters > 0 && characters + size > maxCharacters)
                throw new InvalidOperationException($"Required context item '{item.Name}' exceeds the configured context budget.");
            selected.Add(item);
            characters += size;
        }

        return new AerOrchestrationPlan(
            context.TaskId,
            selected,
            characters,
            System.Text.Encoding.UTF8.GetByteCount(string.Join('\n', selected.Select(x => AER.ToAi(x.Value)))));
    }

    public string EncodePlan(AerOrchestrationPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var payload = plan.Items.ToDictionary(x => x.Name, x => x.Value, StringComparer.Ordinal);
        return AER.ToAi(payload);
    }
}
