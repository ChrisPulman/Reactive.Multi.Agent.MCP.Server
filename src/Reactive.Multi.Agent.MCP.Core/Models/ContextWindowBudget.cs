namespace Reactive.Multi.Agent.MCP.Core.Models;

public sealed class ContextWindowBudget
{
    public int MaxContextTokens { get; set; } = 12000;

    public int WarningThresholdTokens { get; set; } = 9000;

    public int HardLimitTokens { get; set; } = 11000;

    public int CurrentEstimatedTokens { get; set; }

    public bool WarningReached { get; set; }

    public bool HardLimitReached { get; set; }
}
