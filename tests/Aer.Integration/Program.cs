using Aer.Mcp;
using Aer.Orchestration;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

var capabilities = AerMcpCapabilities.FromProfiles(new[] { "aer.ai.v1", "aer.text.v1" });
var payload = AerMcpNegotiator.Encode(
    new[]
    {
        new { id = 1, name = "Amit" },
        new { id = 2, name = "Priya" }
    },
    capabilities);

Assert(payload.Profile == AerMcpProfile.Ai, "AI profile should win when requested and supported.");
Assert(payload.Text is not null, "AI MCP payload must contain text.");
Assert(payload.ContentType == "application/aer; profile=ai", "Unexpected AI content type.");

var fallback = AerMcpNegotiator.Encode(new { status = "ok" }, AerMcpCapabilities.JsonOnly);
Assert(fallback.Profile == AerMcpProfile.Json, "Unsupported AER clients must receive JSON fallback.");
Assert(fallback.Text is not null && fallback.Text.Contains("status", StringComparison.Ordinal), "JSON fallback is empty.");

var orchestrator = new AerOrchestrator();
var plan = orchestrator.Plan(
    new AerOrchestrationContext("task-1"),
    new[]
    {
        new AerContextItem("optional", new { value = 1 }, Priority: 1),
        new AerContextItem("required", new { value = 2 }, Priority: 0, Required: true)
    },
    maxCharacters: 10_000);

Assert(plan.Items.Count == 2, "All context items should fit the budget.");
Assert(plan.Items[0].Name == "required", "Required context must be selected first.");
Assert(!string.IsNullOrWhiteSpace(orchestrator.EncodePlan(plan)), "Orchestrator plan encoding is empty.");

Console.WriteLine("AER integration contracts passed.");
