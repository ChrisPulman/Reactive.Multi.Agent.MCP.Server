namespace Reactive.Multi.Agent.MCP.Core.Models;

public sealed class OrchestrationResumeState
{
    public bool NeedsOrchestrationResume { get; set; }

    public string ResumeSummary { get; set; } = string.Empty;

    public IReadOnlyList<string> PendingActionIds { get; set; } = [];

    public IReadOnlyList<string> RecommendedNextSteps { get; set; } = [];

    public IReadOnlyList<string> IncompleteActionIds { get; set; } = [];
}
