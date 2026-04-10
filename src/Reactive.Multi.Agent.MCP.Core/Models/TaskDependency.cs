namespace Reactive.Multi.Agent.MCP.Core.Models;

public sealed class TaskDependency
{
    public string TaskId { get; set; } = string.Empty;

    public DependencyKind Kind { get; set; } = DependencyKind.Blocking;

    public string Reason { get; set; } = string.Empty;
}
