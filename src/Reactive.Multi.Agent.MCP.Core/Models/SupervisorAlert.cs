namespace Reactive.Multi.Agent.MCP.Core.Models;

public sealed class SupervisorAlert
{
    public string TaskId { get; set; } = string.Empty;

    public string AgentId { get; set; } = string.Empty;

    public SupervisorAlertKind Kind { get; set; } = SupervisorAlertKind.None;

    public string Severity { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public IReadOnlyList<string> RecommendedActions { get; set; } = [];
}
