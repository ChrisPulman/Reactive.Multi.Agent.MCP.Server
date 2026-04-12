namespace Reactive.Multi.Agent.MCP.Core.Models;

/// <summary>
/// Represents the state and metadata of a single orchestration session, including its lifecycle, execution history, and
/// recovery information.
/// </summary>
/// <remarks>An orchestration session tracks the progress and status of a workflow execution, including its
/// request details, execution plan, ledger of actions, and any supervisor or maintenance activities. This type is
/// typically used to monitor, resume, or recover orchestrated workflows in distributed systems.</remarks>
public sealed class OrchestrationSession
{
    public string SessionId { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }

    public DateTimeOffset LastHeartbeatUtc { get; set; }

    public OrchestrationRequest Request { get; set; } = new();

    public OrchestrationPlan Plan { get; set; } = new();

    public string RecoveryGuidance { get; set; } = string.Empty;

    public IReadOnlyList<ExecutionLedgerEntry> ExecutionLedger { get; set; } = [];

    public OrchestrationResumeState ResumeState { get; set; } = new();

    public IReadOnlyList<SupervisorActionRecord> SupervisorActions { get; set; } = [];

    public IReadOnlyList<MaintenanceSnapshot> MaintenanceHistory { get; set; } = [];
}
