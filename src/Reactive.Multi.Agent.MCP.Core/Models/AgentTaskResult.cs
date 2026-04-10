namespace Reactive.Multi.Agent.MCP.Core.Models;

public sealed class AgentTaskResult
{
    public string AgentId { get; set; } = string.Empty;

    public string AgentToolName { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public IReadOnlyList<AgentArtifact> Artifacts { get; set; } = [];

    public IReadOnlyList<HandoffItem> HandoffItems { get; set; } = [];

    public IReadOnlyList<string> Risks { get; set; } = [];

    public bool Completed { get; set; }

    public DateTimeOffset ReportedAtUtc { get; set; }
}
