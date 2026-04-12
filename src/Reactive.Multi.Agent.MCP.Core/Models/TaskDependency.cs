namespace Reactive.Multi.Agent.MCP.Core.Models;

/// <summary>
/// Represents a dependency relationship between tasks, including the type of dependency and an optional reason.
/// </summary>
/// <remarks>Use this class to describe how one task depends on another within a scheduling or workflow system.
/// The dependency kind indicates whether the dependency is blocking or of another type, and the reason provides
/// additional context for the dependency.</remarks>
public sealed class TaskDependency
{
    public string TaskId { get; set; } = string.Empty;

    public DependencyKind Kind { get; set; } = DependencyKind.Blocking;

    public string Reason { get; set; } = string.Empty;
}
