namespace Reactive.Multi.Agent.MCP.Core.Models;

/// <summary>
/// Specifies the possible states of a supervisor action within a workflow or process.
/// </summary>
/// <remarks>Use this enumeration to track and manage the lifecycle of actions that require supervisor
/// intervention. The values represent distinct stages, such as when an action is awaiting attention, has been
/// acknowledged, completed, or abandoned. This can be used to implement logic based on the current state of a
/// supervisor action.</remarks>
public enum SupervisorActionState
{
    Pending,
    Acknowledged,
    Completed,
    Abandoned,
}
