namespace Reactive.Multi.Agent.MCP.Core.Models;

/// <summary>
/// Specifies the status of a task assigned to an agent.
/// </summary>
/// <remarks>Use this enumeration to represent the current progress state of an agent's task. The values indicate
/// whether the task is awaiting execution, currently being processed, or has been completed.</remarks>
public enum AgentTaskStatus
{
    Pending,
    InProgress,
    Completed,
}
