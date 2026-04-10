namespace Reactive.Multi.Agent.MCP.Core.Models;

public sealed class MaintenanceSnapshot
{
    public string SnapshotId { get; set; } = string.Empty;

    public DateTimeOffset RecordedAtUtc { get; set; }

    public string Verdict { get; set; } = string.Empty;

    public int HeartbeatIssueCount { get; set; }

    public int AlertCount { get; set; }

    public int ResumeRequiredCount { get; set; }

    public int IncompleteSupervisorActionCount { get; set; }

    public string CronSummary { get; set; } = string.Empty;
}
