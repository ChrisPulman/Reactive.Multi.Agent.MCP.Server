namespace Reactive.Multi.Agent.MCP.Core.Models;

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
