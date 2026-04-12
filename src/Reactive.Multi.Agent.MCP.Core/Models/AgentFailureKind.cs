namespace Reactive.Multi.Agent.MCP.Core.Models;

/// <summary>
/// Specifies the reason for an agent operation failure.
/// </summary>
/// <remarks>Use this enumeration to determine the cause of a failed agent operation, such as exceeding resource
/// limits or encountering network issues. The values can be used for error handling, logging, or displaying
/// user-friendly error messages.</remarks>
public enum AgentFailureKind
{
    None,
    ContextWindowLimit,
    NetworkLoss,
    TokenBudgetLow,
    SubscriptionTokensExhausted,
    Unknown,
}
