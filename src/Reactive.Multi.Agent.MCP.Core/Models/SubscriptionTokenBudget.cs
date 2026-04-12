namespace Reactive.Multi.Agent.MCP.Core.Models;

/// <summary>
/// Represents the current token budget and status for a subscription, including remaining tokens and budget thresholds.
/// </summary>
/// <remarks>Use this class to monitor and manage token consumption for a subscription. It provides information
/// about the number of tokens left, configurable low-budget thresholds, and status flags indicating when the budget is
/// low or exhausted.</remarks>
public sealed class SubscriptionTokenBudget
{
    public int? RemainingTokens { get; set; }

    public int LowWatermark { get; set; } = 2000;

    public bool LowBudgetWarning { get; set; }

    public bool Exhausted { get; set; }
}
