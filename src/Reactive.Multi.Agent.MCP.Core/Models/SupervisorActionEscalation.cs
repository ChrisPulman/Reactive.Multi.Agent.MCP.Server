namespace Reactive.Multi.Agent.MCP.Core.Models;

/// <summary>
/// Specifies the escalation level for a supervisor action.
/// </summary>
/// <remarks>Use this enumeration to indicate the severity or urgency of a supervisor's response. The values
/// represent increasing levels of escalation, from no action to a critical response.</remarks>
public enum SupervisorActionEscalation
{
    None,
    Warning,
    Critical,
}
