namespace Reactive.Multi.Agent.MCP.Core.Models;

public enum AgentFailureKind
{
    None,
    ContextWindowLimit,
    NetworkLoss,
    TokenBudgetLow,
    SubscriptionTokensExhausted,
    Unknown,
}
