namespace Reactive.Multi.Agent.MCP.Core.Models;

public sealed class HeartbeatIssue
{
    public string Scope { get; set; } = string.Empty;

    public string TargetId { get; set; } = string.Empty;

    public DateTimeOffset LastHeartbeatUtc { get; set; }

    public string Severity { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}
