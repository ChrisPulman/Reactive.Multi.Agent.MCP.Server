namespace Reactive.Multi.Agent.MCP.Core.Models;

/// <summary>
/// Represents a unit of work assigned to an agent, including task details, status, dependencies, and resource budgets.
/// </summary>
/// <remarks>An AgentWorkItem encapsulates all information required for an agent to execute a specific task,
/// including context, objectives, dependencies, and progress tracking. This type is typically used in agent
/// orchestration scenarios to coordinate and monitor the execution of complex workflows. All properties are mutable to
/// support dynamic updates as the task progresses. Thread safety is not guaranteed; synchronize access if used
/// concurrently.</remarks>
public sealed class AgentWorkItem
{
    public string TaskId { get; set; } = string.Empty;

    public string AgentId { get; set; } = string.Empty;

    public string AgentName { get; set; } = string.Empty;

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

    public bool ShutdownRequired { get; set; }

    public DateTimeOffset? CompletedAtUtc { get; set; }

    public DateTimeOffset LastUpdatedUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset LastHeartbeatUtc { get; set; } = DateTimeOffset.UtcNow;

    public ContextWindowBudget ContextWindowBudget { get; set; } = new();

    public SubscriptionTokenBudget SubscriptionTokenBudget { get; set; } = new();

    public IReadOnlyList<AgentCheckpoint> Checkpoints { get; set; } = [];

    public TaskRecoveryState RecoveryState { get; set; } = new();
}
