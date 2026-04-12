namespace Reactive.Multi.Agent.MCP.Core.Models;

/// <summary>
/// Specifies the type of dependency relationship between two entities.
/// </summary>
/// <remarks>Use this enumeration to indicate whether a dependency is blocking, meaning it must be resolved before
/// proceeding, or advisory, meaning it is informational and does not prevent progress.</remarks>
public enum DependencyKind
{
    Blocking,
    Advisory,
}
