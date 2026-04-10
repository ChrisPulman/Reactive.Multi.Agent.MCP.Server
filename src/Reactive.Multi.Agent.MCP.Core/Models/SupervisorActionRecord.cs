namespace Reactive.Multi.Agent.MCP.Core.Models;

public sealed class SupervisorActionRecord
{
    public string ActionId { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public SupervisorActionState State { get; set; } = SupervisorActionState.Pending;

    public DateTimeOffset UpdatedAtUtc { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset LastHeartbeatUtc { get; set; }

    public DateTimeOffset? ExpiresAtUtc { get; set; }

    public SupervisorActionEscalation Escalation { get; set; } = SupervisorActionEscalation.None;

    public string? FollowUpActionId { get; set; }
}
