namespace Reactive.Multi.Agent.MCP.Core.Models;

/// <summary>
/// Represents a packet containing all relevant information for an agent task, including identifiers, status,
/// dependencies, objectives, and execution context.
/// </summary>
/// <remarks>This class is used to encapsulate the full state and metadata required to manage, track, and execute
/// a task assigned to an agent. It includes properties for task coordination, dependency management, execution prompts,
/// checkpoints, and recovery state. The packet is typically exchanged between components responsible for orchestrating
/// agent workflows and task execution. Thread safety is not guaranteed; synchronize access if instances are shared
/// across threads.</remarks>
public sealed class AgentTaskPacket
{
    public string SessionId { get; set; } = string.Empty;

    public string TaskId { get; set; } = string.Empty;

    public string AgentId { get; set; } = string.Empty;

    public string AgentToolName { get; set; } = string.Empty;

    public string AgentSessionId { get; set; } = string.Empty;

    public AgentTaskStatus Status { get; set; }

    public bool IsReady { get; set; }

    public IReadOnlyList<string> BlockingDependencies { get; set; } = [];

    public bool NeedsResume { get; set; }

    public string Objective { get; set; } = string.Empty;

    public string ContextSnapshot { get; set; } = string.Empty;

    public string PhaseName { get; set; } = string.Empty;

    public IReadOnlyList<TaskDependency> Dependencies { get; set; } = [];

    public IReadOnlyList<string> AcceptanceCriteria { get; set; } = [];

    public IReadOnlyList<string> SuggestedSkills { get; set; } = [];

    public IReadOnlyList<string> SuggestedTools { get; set; } = [];

    public string CompletionContract { get; set; } = string.Empty;

    public string Scratchpad { get; set; } = string.Empty;

    public string ExecutionPrompt { get; set; } = string.Empty;

    public IReadOnlyList<string> NextSteps { get; set; } = [];

    public string ArtifactSchemaHint { get; set; } = string.Empty;

    public ContextWindowBudget ContextWindowBudget { get; set; } = new();

    public SubscriptionTokenBudget SubscriptionTokenBudget { get; set; } = new();

    public IReadOnlyList<AgentCheckpoint> Checkpoints { get; set; } = [];

    public TaskRecoveryState RecoveryState { get; set; } = new();

    public IReadOnlyList<string> ResumeMemoryReloadItems { get; set; } = [];

    public AgentTaskResult? LatestResult { get; set; }

    public DateTimeOffset LastHeartbeatUtc { get; set; }
}
