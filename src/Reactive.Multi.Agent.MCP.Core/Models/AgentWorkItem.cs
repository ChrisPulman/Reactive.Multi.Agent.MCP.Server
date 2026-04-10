namespace Reactive.Multi.Agent.MCP.Core.Models;

public sealed class AgentWorkItem
{
    public string TaskId { get; set; } = string.Empty;

    public string AgentId { get; set; } = string.Empty;

    public string AgentToolName { get; set; } = string.Empty;

    public string AgentSessionId { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Objective { get; set; } = string.Empty;

    public string ContextSnapshot { get; set; } = string.Empty;

    public string PhaseName { get; set; } = string.Empty;

    public int PhaseOrder { get; set; }

    public int SequenceOrder { get; set; }

    public IReadOnlyList<string> AcceptanceCriteria { get; set; } = [];

    public IReadOnlyList<string> SuggestedSkills { get; set; } = [];

    public IReadOnlyList<string> SuggestedTools { get; set; } = [];

    public IReadOnlyList<TaskDependency> Dependencies { get; set; } = [];

    public AgentTaskStatus Status { get; set; } = AgentTaskStatus.Pending;

    public string Scratchpad { get; set; } = string.Empty;

    public AgentTaskResult? LatestResult { get; set; }

    public DateTimeOffset LastUpdatedUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset LastHeartbeatUtc { get; set; } = DateTimeOffset.UtcNow;

    public ContextWindowBudget ContextWindowBudget { get; set; } = new();

    public SubscriptionTokenBudget SubscriptionTokenBudget { get; set; } = new();

    public IReadOnlyList<AgentCheckpoint> Checkpoints { get; set; } = [];

    public TaskRecoveryState RecoveryState { get; set; } = new();
}
