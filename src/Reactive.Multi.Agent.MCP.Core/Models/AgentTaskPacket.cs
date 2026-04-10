namespace Reactive.Multi.Agent.MCP.Core.Models;

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
