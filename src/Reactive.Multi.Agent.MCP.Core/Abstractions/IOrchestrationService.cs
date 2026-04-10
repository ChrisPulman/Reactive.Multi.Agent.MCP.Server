using Reactive.Multi.Agent.MCP.Core.Models;

namespace Reactive.Multi.Agent.MCP.Core.Abstractions;

public interface IOrchestrationService
{
    OrchestrationSession CreateSession(OrchestrationRequest request);

    OrchestrationSession? GetSession(string sessionId);

    IReadOnlyList<SessionHistoryEntry> SearchSessions(string? query = null, int limit = 20);

    SupervisorStatus GetSupervisorStatus(string sessionId, int stalledAfterMinutes = 30);

    SupervisorActionPlan GetSupervisorActionPlan(string sessionId, int stalledAfterMinutes = 30, bool autoApplyPolicies = false, bool networkRecovered = false);

    OrchestrationSession ResumeOrchestration(string sessionId);

    OrchestrationSession UpdateSupervisorAction(string sessionId, string actionId, SupervisorActionState state);

    OrchestrationSession ApplySupervisorActionEscalation(string sessionId, int staleAfterMinutes = 30, int criticalAfterMinutes = 90);

    OrchestrationSession RecordHeartbeat(string sessionId, string? taskId = null, string? agentId = null, string? actionId = null, string source = "external");

    OrchestrationSession RunMaintenanceSweep(string sessionId, int silentHeartbeatMinutes = 15, int staleTaskMinutes = 30, int staleActionMinutes = 30, int criticalActionMinutes = 90);

    MaintenanceReport GetMaintenanceReport(string sessionId, int silentHeartbeatMinutes = 15, int staleTaskMinutes = 30, int staleActionMinutes = 30, int criticalActionMinutes = 90, bool autoApplyPolicies = false, bool networkRecovered = false);

    IReadOnlyList<MaintenanceSnapshot> GetMaintenanceHistory(string sessionId, int limit = 10);

    AgentTaskPacket GetAgentTaskPacket(string sessionId, string taskId, string agentId);

    AgentTaskPacket ActivateAgentTask(string sessionId, string taskId, string agentId, string? additionalContext = null, string? workLog = null);

    AgentTaskPacket RecordAgentResult(
        string sessionId,
        string taskId,
        string agentId,
        string? workSummary = null,
        IReadOnlyList<AgentArtifact>? artifacts = null,
        IReadOnlyList<HandoffItem>? handoffItems = null,
        IReadOnlyList<string>? risks = null,
        bool markComplete = false);

    AgentTaskPacket RecordCheckpoint(
        string sessionId,
        string taskId,
        string agentId,
        string checkpointSummary,
        IReadOnlyList<string>? memoryReloadItems = null,
        int? currentEstimatedTokens = null,
        int? remainingSubscriptionTokens = null);

    AgentTaskPacket ReportTaskFailure(
        string sessionId,
        string taskId,
        string agentId,
        AgentFailureKind failureKind,
        string reason,
        IReadOnlyList<string>? memoryReloadItems = null,
        int? currentEstimatedTokens = null,
        int? remainingSubscriptionTokens = null);

    AgentTaskPacket ApplyAutomaticPolicy(
        string sessionId,
        string taskId,
        string agentId,
        int? currentEstimatedTokens = null,
        int? remainingSubscriptionTokens = null,
        bool networkRecovered = false);

    AgentTaskPacket ResumeTask(string sessionId, string taskId, string agentId);

    OrchestrationSummary FinalizeSession(string sessionId);
}
