namespace Reactive.Multi.Agent.MCP.Core.Models;

public sealed class SupervisorStatus
{
    public string SessionId { get; set; } = string.Empty;

    public DateTimeOffset EvaluatedAtUtc { get; set; }

    public IReadOnlyList<SupervisorAlert> Alerts { get; set; } = [];

    public IReadOnlyList<string> Recommendations { get; set; } = [];

    public IReadOnlyList<string> StalledTaskIds { get; set; } = [];

    public IReadOnlyList<NextTaskCandidate> NextRunnableTasks { get; set; } = [];

    public IReadOnlyList<HeartbeatIssue> HeartbeatIssues { get; set; } = [];
}
