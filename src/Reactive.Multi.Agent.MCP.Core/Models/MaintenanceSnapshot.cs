namespace Reactive.Multi.Agent.MCP.Core.Models;

/// <summary>
/// Represents a snapshot of maintenance-related metrics and status information at a specific point in time.
/// </summary>
/// <remarks>This class is typically used to capture and convey the state of system maintenance, including issues,
/// alerts, and required actions, as recorded during a maintenance cycle. All properties are mutable to support
/// serialization and deserialization scenarios.</remarks>
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
