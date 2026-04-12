namespace Reactive.Multi.Agent.MCP.Core.Models;

/// <summary>
/// Represents a record of an action performed or tracked by a supervisor, including its state, timing, and escalation
/// details.
/// </summary>
/// <remarks>This class is typically used to store and manage the lifecycle of supervisor-initiated actions, such
/// as approvals, escalations, or follow-up tasks. It includes metadata for tracking the action's progress and timing,
/// as well as optional escalation and follow-up information. Instances of this class are intended to be immutable once
/// created, except for state and timing updates as the action progresses.</remarks>
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
