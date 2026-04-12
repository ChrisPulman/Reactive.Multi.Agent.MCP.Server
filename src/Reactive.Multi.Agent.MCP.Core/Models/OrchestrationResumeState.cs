namespace Reactive.Multi.Agent.MCP.Core.Models;

/// <summary>
/// Represents the state information required to resume an orchestration, including pending actions and recommended next
/// steps.
/// </summary>
/// <remarks>Use this class to inspect the current status of an orchestration that is paused or awaiting external
/// input. The properties provide details about which actions are incomplete, which actions are pending, and guidance
/// for resuming the orchestration.</remarks>
public sealed class OrchestrationResumeState
{
    public bool NeedsOrchestrationResume { get; set; }

    public string ResumeSummary { get; set; } = string.Empty;

    public IReadOnlyList<string> PendingActionIds { get; set; } = [];

    public IReadOnlyList<string> RecommendedNextSteps { get; set; } = [];

    public IReadOnlyList<string> IncompleteActionIds { get; set; } = [];
}
