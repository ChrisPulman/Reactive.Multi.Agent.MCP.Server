namespace Reactive.Multi.Agent.MCP.Core.Models;

/// <summary>
/// Represents the current state and recommendations for automatic policy actions, such as checkpointing, resuming, and
/// retrying operations.
/// </summary>
/// <remarks>This class is typically used to communicate policy-driven recommendations and state information
/// between components that manage automatic recovery or retry logic. It provides guidance on whether certain actions
/// are advisable based on the current context and tracks retry attempt usage.</remarks>
public sealed class AutomaticPolicyState
{
    public bool AutoCheckpointRecommended { get; set; }

    public bool AutoResumeRecommended { get; set; }

    public bool AutoRetryRecommended { get; set; }

    public int RetryAttemptsUsed { get; set; }

    public int MaxRetryAttempts { get; set; } = 2;

    public string PolicyReason { get; set; } = string.Empty;
}
