namespace Reactive.Multi.Agent.MCP.Core.Models;

/// <summary>
/// Represents a summary of the current state and progress of an orchestration session, including task counts, status,
/// and related metadata.
/// </summary>
/// <remarks>This class provides a snapshot of orchestration execution, including identifiers for tasks in various
/// states, coordination notes, and summary information. It is typically used to monitor or report on the progress of a
/// distributed or long-running orchestration process. All properties are mutable to support serialization and
/// deserialization scenarios.</remarks>
public sealed class OrchestrationSummary
{
    public string SessionId { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public int TotalTasks { get; set; }

    public int CompletedTasks { get; set; }

    public int PendingTasks { get; set; }

    public IReadOnlyList<string> ReadyTaskIds { get; set; } = [];

    public IReadOnlyList<string> BlockedTaskIds { get; set; } = [];

    public IReadOnlyList<string> ResumeRequiredTaskIds { get; set; } = [];

    public IReadOnlyList<string> AutoCheckpointTaskIds { get; set; } = [];

    public IReadOnlyList<string> AutoRetryTaskIds { get; set; } = [];

    public string Summary { get; set; } = string.Empty;

    public IReadOnlyList<string> CoordinationNotes { get; set; } = [];

    public IReadOnlyList<object> CompletedWork { get; set; } = [];

    public IReadOnlyList<object> PendingWork { get; set; } = [];

    public string UnifiedResponse { get; set; } = string.Empty;

    public DateTimeOffset LastHeartbeatUtc { get; set; }
}
