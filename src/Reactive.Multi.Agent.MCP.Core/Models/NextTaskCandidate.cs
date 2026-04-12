namespace Reactive.Multi.Agent.MCP.Core.Models;

/// <summary>
/// Represents a candidate task that is eligible to be scheduled or executed next, including associated agent and
/// prioritization details.
/// </summary>
/// <remarks>This class encapsulates information about a potential next task in a scheduling or workflow system,
/// including identifiers for the task and agent, the tool to be used, a descriptive title, a reason for selection, and
/// a priority value. Instances of this class are typically used to communicate or evaluate which task should be
/// processed next based on priority and context.</remarks>
public sealed class NextTaskCandidate
{
    public string TaskId { get; set; } = string.Empty;

    public string AgentId { get; set; } = string.Empty;

    public string AgentToolName { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;

    public int Priority { get; set; }
}
