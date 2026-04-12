namespace Reactive.Multi.Agent.MCP.Core.Models;

/// <summary>
/// Represents a budget for tracking and managing token usage within a context window, including configurable limits and
/// thresholds.
/// </summary>
/// <remarks>Use this class to monitor and enforce token consumption constraints in scenarios where exceeding
/// context window limits may impact performance or cause errors. The properties allow configuration of soft and hard
/// token limits, as well as tracking the current estimated usage and whether thresholds have been reached.</remarks>
public sealed class ContextWindowBudget
{
    public int MaxContextTokens { get; set; } = 12000;

    public int WarningThresholdTokens { get; set; } = 9000;

    public int HardLimitTokens { get; set; } = 11000;

    public int CurrentEstimatedTokens { get; set; }

    public bool WarningReached { get; set; }

    public bool HardLimitReached { get; set; }
}
