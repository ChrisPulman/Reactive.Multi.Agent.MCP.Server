namespace Reactive.Multi.Agent.MCP.Core.Models;

public sealed class NextTaskCandidate
{
    public string TaskId { get; set; } = string.Empty;

    public string AgentId { get; set; } = string.Empty;

    public string AgentToolName { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;

    public int Priority { get; set; }
}
