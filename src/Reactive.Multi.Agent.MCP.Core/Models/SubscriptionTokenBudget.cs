namespace Reactive.Multi.Agent.MCP.Core.Models;

public sealed class SubscriptionTokenBudget
{
    public int? RemainingTokens { get; set; }

    public int LowWatermark { get; set; } = 2000;

    public bool LowBudgetWarning { get; set; }

    public bool Exhausted { get; set; }
}
