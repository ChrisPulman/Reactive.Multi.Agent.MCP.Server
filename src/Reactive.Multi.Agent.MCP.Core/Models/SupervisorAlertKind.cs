namespace Reactive.Multi.Agent.MCP.Core.Models;

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
