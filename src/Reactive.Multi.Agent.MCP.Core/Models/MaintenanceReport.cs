namespace Reactive.Multi.Agent.MCP.Core.Models;

public sealed class MaintenanceReport
{
    public string SessionId { get; set; } = string.Empty;

    public DateTimeOffset GeneratedAtUtc { get; set; }

    public string Verdict { get; set; } = string.Empty;

    public bool AutoAppliedPolicies { get; set; }

    public IReadOnlyList<string> AutoAppliedActions { get; set; } = [];

    public IReadOnlyList<string> Findings { get; set; } = [];

    public IReadOnlyList<string> RecommendedActions { get; set; } = [];

    public IReadOnlyList<HeartbeatIssue> HeartbeatIssues { get; set; } = [];

    public IReadOnlyList<string> ResumeRequiredTaskIds { get; set; } = [];

    public IReadOnlyList<string> IncompleteSupervisorActionIds { get; set; } = [];

    public string CronSummary { get; set; } = string.Empty;

    public MaintenanceTrend Trend { get; set; } = MaintenanceTrend.Stable;

    public string TrendSummary { get; set; } = string.Empty;

    public IReadOnlyList<MaintenanceSnapshot> RecentHistory { get; set; } = [];
}
