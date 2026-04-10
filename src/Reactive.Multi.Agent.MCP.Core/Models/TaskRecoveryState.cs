namespace Reactive.Multi.Agent.MCP.Core.Models;

public sealed class TaskRecoveryState
{
    public int RestartCount { get; set; }

    public bool NeedsResume { get; set; }

    public AgentFailureKind LastFailureKind { get; set; } = AgentFailureKind.None;

    public string? LastFailureReason { get; set; }

    public DateTimeOffset? LastFailureAtUtc { get; set; }

    public string ResumeInstructions { get; set; } = string.Empty;

    public AutomaticPolicyState PolicyState { get; set; } = new();
}
