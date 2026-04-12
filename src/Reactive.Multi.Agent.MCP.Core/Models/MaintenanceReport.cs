namespace Reactive.Multi.Agent.MCP.Core.Models;

/// <summary>
/// Represents a comprehensive report detailing the results of a maintenance session, including findings, actions taken,
/// recommendations, and trend analysis.
/// </summary>
/// <remarks>A maintenance report aggregates information about a maintenance session, such as automatically
/// applied policies, detected issues, recommended actions, and historical trends. This type is typically used to
/// provide a summary for review, auditing, or further action by supervisors or automated systems. All properties are
/// populated at the time the report is generated and are intended to be read-only after creation.</remarks>
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
