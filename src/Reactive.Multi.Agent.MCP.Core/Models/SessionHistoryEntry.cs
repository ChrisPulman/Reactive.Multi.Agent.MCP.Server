namespace Reactive.Multi.Agent.MCP.Core.Models;

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
