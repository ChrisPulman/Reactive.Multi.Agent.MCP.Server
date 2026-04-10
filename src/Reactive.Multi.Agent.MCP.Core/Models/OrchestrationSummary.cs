namespace Reactive.Multi.Agent.MCP.Core.Models;

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
