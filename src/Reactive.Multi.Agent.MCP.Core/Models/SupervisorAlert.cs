namespace Reactive.Multi.Agent.MCP.Core.Models;

/// <summary>
/// Represents an alert generated for a supervisor, containing information about a specific task, agent, alert kind,
/// severity, message, and recommended actions.
/// </summary>
/// <remarks>This class is typically used to notify supervisors of important events or issues that require
/// attention. The properties provide context and suggested next steps for addressing the alert. Instances of this class
/// are immutable once created, except for property setters.</remarks>
public sealed class SupervisorAlert
{
    public string TaskId { get; set; } = string.Empty;

    public string AgentId { get; set; } = string.Empty;

    public SupervisorAlertKind Kind { get; set; } = SupervisorAlertKind.None;

    public string Severity { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public IReadOnlyList<string> RecommendedActions { get; set; } = [];
}
