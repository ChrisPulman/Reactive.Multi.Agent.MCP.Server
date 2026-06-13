namespace Reactive.Multi.Agent.MCP.Core.Models;

/// <summary>
/// Represents the result of a task performed by an agent, including output artifacts, handoff items, risks, and
/// completion status.
/// </summary>
/// <remarks>Use this class to capture and communicate the outcome of an agent's operation, such as in automation
/// or workflow scenarios. The properties provide details about the agent, the tools used, a summary of the task, any
/// generated artifacts, items requiring handoff, identified risks, completion status, and the time the result was
/// reported.</remarks>
public sealed class AgentTaskResult
{
    public string AgentId { get; set; } = string.Empty;

    public string AgentName { get; set; } = string.Empty;

    public string AgentToolName { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public IReadOnlyList<AgentArtifact> Artifacts { get; set; } = [];

    public IReadOnlyList<HandoffItem> HandoffItems { get; set; } = [];

    public IReadOnlyList<string> Risks { get; set; } = [];

    public bool Completed { get; set; }

    public bool ShutdownRequired { get; set; }

    public DateTimeOffset ReportedAtUtc { get; set; }
}
