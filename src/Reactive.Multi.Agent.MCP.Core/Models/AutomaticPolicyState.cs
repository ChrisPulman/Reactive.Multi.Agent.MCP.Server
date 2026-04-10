namespace Reactive.Multi.Agent.MCP.Core.Models;

public sealed class AutomaticPolicyState
{
    public bool AutoCheckpointRecommended { get; set; }

    public bool AutoResumeRecommended { get; set; }

    public bool AutoRetryRecommended { get; set; }

    public int RetryAttemptsUsed { get; set; }

    public int MaxRetryAttempts { get; set; } = 2;

    public string PolicyReason { get; set; } = string.Empty;
}
