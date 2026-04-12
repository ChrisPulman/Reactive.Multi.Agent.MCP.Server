namespace Reactive.Multi.Agent.MCP.Core.Models;

/// <summary>
/// Specifies the trend of maintenance activity or condition over time.
/// </summary>
/// <remarks>Use this enumeration to indicate whether maintenance is stable, improving, or worsening. This can
/// help in tracking and reporting the overall direction of maintenance efforts or system health.</remarks>
public enum MaintenanceTrend
{
    Stable,
    Improving,
    Worsening,
}
