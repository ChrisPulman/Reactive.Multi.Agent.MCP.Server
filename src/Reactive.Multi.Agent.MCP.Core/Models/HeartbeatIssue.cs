namespace Reactive.Multi.Agent.MCP.Core.Models;

/// <summary>
/// Represents an issue detected with a monitored heartbeat, including details about its scope, target, severity, and
/// the time of the last received heartbeat.
/// </summary>
/// <remarks>Use this class to capture and convey information about heartbeat monitoring problems, such as missed
/// or delayed heartbeats, within distributed systems or health monitoring scenarios. Each instance provides context for
/// diagnosing and responding to heartbeat-related issues.</remarks>
public sealed class HeartbeatIssue
{
    public string Scope { get; set; } = string.Empty;

    public string TargetId { get; set; } = string.Empty;

    public DateTimeOffset LastHeartbeatUtc { get; set; }

    public string Severity { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}
