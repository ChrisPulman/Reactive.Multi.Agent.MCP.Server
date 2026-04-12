namespace Reactive.Multi.Agent.MCP.Core.Models;

/// <summary>
/// Represents the current status of a supervisor, including session information, evaluation time, alerts,
/// recommendations, stalled tasks, next runnable tasks, and heartbeat issues.
/// </summary>
/// <remarks>This class is typically used to convey the overall health and actionable state of a supervisor
/// instance in a distributed or monitored system. All collections are read-only and may be empty if there are no
/// corresponding items to report.</remarks>
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
