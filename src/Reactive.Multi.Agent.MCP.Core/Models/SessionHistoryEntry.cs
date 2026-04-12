namespace Reactive.Multi.Agent.MCP.Core.Models;

/// <summary>
/// Represents a record of a session's history, including metadata, user request details, and task progress information.
/// </summary>
/// <remarks>This class is typically used to track the state and progress of a session over time, such as in
/// workflow or job processing systems. It provides information about when the session was created and last updated, the
/// original user request, the current status, and various task-related counts. All properties are mutable to allow
/// updates as the session progresses.</remarks>
public sealed class SessionHistoryEntry
{
    public string SessionId { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }

    public string UserRequest { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public int TotalTasks { get; set; }

    public int CompletedTasks { get; set; }

    public int ResumeRequiredTasks { get; set; }

    public int AutoCheckpointTasks { get; set; }

    public int AutoRetryTasks { get; set; }
}
