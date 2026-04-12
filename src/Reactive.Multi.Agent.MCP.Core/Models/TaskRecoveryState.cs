namespace Reactive.Multi.Agent.MCP.Core.Models;

/// <summary>
/// Represents the recovery state of a task, including information about restarts, failures, and resume instructions.
/// </summary>
/// <remarks>This class is used to track the status and recovery details of a task that may require automatic or
/// manual intervention after a failure. It provides information such as the number of restart attempts, the nature and
/// reason of the last failure, and any instructions needed to resume the task. This type is typically used in scenarios
/// where robust task recovery and monitoring are required.</remarks>
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
