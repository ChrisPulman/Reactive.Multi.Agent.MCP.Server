namespace Reactive.Multi.Agent.MCP.Core.Models;

/// <summary>
/// Specifies the types of alerts that can be raised by a supervisor to indicate various operational states or required
/// actions.
/// </summary>
/// <remarks>Use this enumeration to identify and handle specific alert conditions reported by a supervisor
/// component, such as stalled tasks, required user intervention, or recommended automated actions. The values can be
/// used to trigger appropriate responses or logging within monitoring and orchestration systems.</remarks>
public enum SupervisorAlertKind
{
    None,
    StalledTask,
    ResumeRequired,
    AutoCheckpointRecommended,
    AutoRetryRecommended,
    BlockedByDependency,
    StaleSupervisorAction,
    SilentHeartbeat,
}
