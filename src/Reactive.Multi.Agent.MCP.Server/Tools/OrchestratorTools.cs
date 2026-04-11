using ModelContextProtocol.Server;
using Reactive.Multi.Agent.MCP.Core.Abstractions;
using Reactive.Multi.Agent.MCP.Core.Models;
using Reactive.Multi.Agent.MCP.Server.Infrastructure;

namespace Reactive.Multi.Agent.MCP.Server.Tools;

[McpServerToolType]
public sealed class OrchestratorTools
{
    [McpServerTool(Name = "multiagent_orchestrate_request")]
    public static string OrchestrateRequest(IOrchestrationService orchestrationService, string userRequest, string? constraints = null, string? desiredArtifacts = null, string? preferredAgents = null, int maxParallelAgents = 4)
        => McpSafeExecutor.ExecuteJson("multiagent_orchestrate_request", () =>
        {
            ArgumentNullException.ThrowIfNull(orchestrationService);
            ArgumentException.ThrowIfNullOrWhiteSpace(userRequest);
            var session = orchestrationService.CreateSession(OrchestrationRequest.FromStrings(userRequest, constraints, desiredArtifacts, preferredAgents, maxParallelAgents));
            var summary = orchestrationService.FinalizeSession(session.SessionId);
            return BuildStartupPayload(session, summary);
        });

    [McpServerTool(Name = "multiagent_session_status")]
    public static string SessionStatus(IOrchestrationService orchestrationService, string sessionId)
        => McpSafeExecutor.ExecuteJson("multiagent_session_status", () =>
        {
            ArgumentNullException.ThrowIfNull(orchestrationService);
            ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
            var session = orchestrationService.GetSession(sessionId)
                ?? throw new InvalidOperationException($"Unknown orchestration session '{sessionId}'.");
            var summary = orchestrationService.FinalizeSession(sessionId);
            var supervisor = orchestrationService.GetSupervisorStatus(sessionId);
            return new
            {
                sessionId = session.SessionId,
                request = session.Request.UserRequest,
                lastHeartbeatUtc = session.LastHeartbeatUtc,
                progress = BuildProgress(summary),
                plan = BuildPlanOverview(session.Plan),
                resume = session.ResumeState,
                supervisor = BuildSupervisorOverview(supervisor),
                recentMaintenance = session.MaintenanceHistory.TakeLast(3).ToArray(),
            };
        });

    [McpServerTool(Name = "multiagent_finalize_session")]
    public static string FinalizeSession(IOrchestrationService orchestrationService, string sessionId)
        => McpSafeExecutor.ExecuteJson("multiagent_finalize_session", () =>
        {
            ArgumentNullException.ThrowIfNull(orchestrationService);
            ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
            var summary = orchestrationService.FinalizeSession(sessionId);
            return new
            {
                summary.SessionId,
                summary.Status,
                summary.TotalTasks,
                summary.CompletedTasks,
                summary.PendingTasks,
                summary.ReadyTaskIds,
                summary.BlockedTaskIds,
                summary.ResumeRequiredTaskIds,
                summary.AutoCheckpointTaskIds,
                summary.AutoRetryTaskIds,
                summary.Summary,
                summary.CoordinationNotes,
                summary.UnifiedResponse,
                summary.LastHeartbeatUtc,
            };
        });

    [McpServerTool(Name = "multiagent_resume_task")]
    public static string ResumeTask(IOrchestrationService orchestrationService, string sessionId, string taskId, string agentId)
        => McpSafeExecutor.ExecuteJson("multiagent_resume_task", () =>
        {
            ArgumentNullException.ThrowIfNull(orchestrationService);
            ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
            ArgumentException.ThrowIfNullOrWhiteSpace(taskId);
            ArgumentException.ThrowIfNullOrWhiteSpace(agentId);
            return orchestrationService.ResumeTask(sessionId, taskId, agentId);
        });

    [McpServerTool(Name = "multiagent_resume_orchestration")]
    public static string ResumeOrchestration(IOrchestrationService orchestrationService, string sessionId)
        => McpSafeExecutor.ExecuteJson("multiagent_resume_orchestration", () =>
        {
            ArgumentNullException.ThrowIfNull(orchestrationService);
            ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
            var session = orchestrationService.ResumeOrchestration(sessionId);
            return new
            {
                sessionId = session.SessionId,
                lastHeartbeatUtc = session.LastHeartbeatUtc,
                resumeState = session.ResumeState,
            };
        });

    [McpServerTool(Name = "multiagent_update_supervisor_action")]
    public static string UpdateSupervisorAction(IOrchestrationService orchestrationService, string sessionId, string actionId, SupervisorActionState state)
        => McpSafeExecutor.ExecuteJson("multiagent_update_supervisor_action", () =>
        {
            ArgumentNullException.ThrowIfNull(orchestrationService);
            ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
            ArgumentException.ThrowIfNullOrWhiteSpace(actionId);
            var session = orchestrationService.UpdateSupervisorAction(sessionId, actionId, state);
            var action = session.SupervisorActions.First(candidate => candidate.ActionId.Equals(actionId, StringComparison.OrdinalIgnoreCase));
            return new
            {
                sessionId = session.SessionId,
                action.ActionId,
                action.State,
                action.Escalation,
                action.FollowUpActionId,
                incompleteActionIds = session.ResumeState.IncompleteActionIds,
            };
        });

    [McpServerTool(Name = "multiagent_apply_supervisor_action_escalation")]
    public static string ApplySupervisorActionEscalation(IOrchestrationService orchestrationService, string sessionId, int staleAfterMinutes = 30, int criticalAfterMinutes = 90)
        => McpSafeExecutor.ExecuteJson("multiagent_apply_supervisor_action_escalation", () =>
        {
            ArgumentNullException.ThrowIfNull(orchestrationService);
            ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
            var session = orchestrationService.ApplySupervisorActionEscalation(sessionId, staleAfterMinutes, criticalAfterMinutes);
            return new
            {
                sessionId = session.SessionId,
                staleAfterMinutes,
                criticalAfterMinutes,
                escalatedActions = session.SupervisorActions
                    .Where(action => action.Escalation != SupervisorActionEscalation.None)
                    .Select(action => new { action.ActionId, action.State, action.Escalation, action.FollowUpActionId })
                    .ToArray(),
                incompleteActionIds = session.ResumeState.IncompleteActionIds,
            };
        });

    [McpServerTool(Name = "multiagent_record_heartbeat")]
    public static string RecordHeartbeat(IOrchestrationService orchestrationService, string sessionId, string? taskId = null, string? agentId = null, string? actionId = null, string source = "external")
        => McpSafeExecutor.ExecuteJson("multiagent_record_heartbeat", () =>
        {
            ArgumentNullException.ThrowIfNull(orchestrationService);
            ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
            var session = orchestrationService.RecordHeartbeat(sessionId, taskId, agentId, actionId, source);
            return new
            {
                sessionId = session.SessionId,
                source,
                taskId,
                agentId,
                actionId,
                session.LastHeartbeatUtc,
            };
        });

    [McpServerTool(Name = "multiagent_run_maintenance_sweep")]
    public static string RunMaintenanceSweep(IOrchestrationService orchestrationService, string sessionId, int silentHeartbeatMinutes = 15, int staleTaskMinutes = 30, int staleActionMinutes = 30, int criticalActionMinutes = 90)
        => McpSafeExecutor.ExecuteJson("multiagent_run_maintenance_sweep", () =>
        {
            ArgumentNullException.ThrowIfNull(orchestrationService);
            ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
            var session = orchestrationService.RunMaintenanceSweep(sessionId, silentHeartbeatMinutes, staleTaskMinutes, staleActionMinutes, criticalActionMinutes);
            var supervisor = orchestrationService.GetSupervisorStatus(sessionId, staleTaskMinutes);
            return new
            {
                sessionId = session.SessionId,
                session.LastHeartbeatUtc,
                heartbeatIssueCount = supervisor.HeartbeatIssues.Count,
                alertCount = supervisor.Alerts.Count,
                incompleteActionIds = session.ResumeState.IncompleteActionIds,
                recentMaintenanceEvents = session.ExecutionLedger.TakeLast(5).ToArray(),
            };
        });

    [McpServerTool(Name = "multiagent_get_maintenance_report")]
    public static string GetMaintenanceReport(IOrchestrationService orchestrationService, string sessionId, int silentHeartbeatMinutes = 15, int staleTaskMinutes = 30, int staleActionMinutes = 30, int criticalActionMinutes = 90, bool autoApplyPolicies = false, bool networkRecovered = false)
        => McpSafeExecutor.ExecuteJson("multiagent_get_maintenance_report", () =>
        {
            ArgumentNullException.ThrowIfNull(orchestrationService);
            ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
            return orchestrationService.GetMaintenanceReport(sessionId, silentHeartbeatMinutes, staleTaskMinutes, staleActionMinutes, criticalActionMinutes, autoApplyPolicies, networkRecovered);
        });

    [McpServerTool(Name = "multiagent_get_maintenance_history")]
    public static string GetMaintenanceHistory(IOrchestrationService orchestrationService, string sessionId, int limit = 10)
        => McpSafeExecutor.ExecuteJson("multiagent_get_maintenance_history", () =>
        {
            ArgumentNullException.ThrowIfNull(orchestrationService);
            ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
            return orchestrationService.GetMaintenanceHistory(sessionId, limit);
        });

    [McpServerTool(Name = "multiagent_apply_automatic_policy")]
    public static string ApplyAutomaticPolicy(IOrchestrationService orchestrationService, string sessionId, string taskId, string agentId, int? currentEstimatedTokens = null, int? remainingSubscriptionTokens = null, bool networkRecovered = false)
        => McpSafeExecutor.ExecuteJson("multiagent_apply_automatic_policy", () =>
        {
            ArgumentNullException.ThrowIfNull(orchestrationService);
            ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
            ArgumentException.ThrowIfNullOrWhiteSpace(taskId);
            ArgumentException.ThrowIfNullOrWhiteSpace(agentId);
            return orchestrationService.ApplyAutomaticPolicy(sessionId, taskId, agentId, currentEstimatedTokens, remainingSubscriptionTokens, networkRecovered);
        });

    [McpServerTool(Name = "multiagent_search_sessions")]
    public static string SearchSessions(IOrchestrationService orchestrationService, string? query = null, int limit = 20)
        => McpSafeExecutor.ExecuteJson("multiagent_search_sessions", () =>
        {
            ArgumentNullException.ThrowIfNull(orchestrationService);
            return new { query, results = orchestrationService.SearchSessions(query, limit) };
        });

    [McpServerTool(Name = "multiagent_supervisor_status")]
    public static string SupervisorStatus(IOrchestrationService orchestrationService, string sessionId, int stalledAfterMinutes = 30)
        => McpSafeExecutor.ExecuteJson("multiagent_supervisor_status", () =>
        {
            ArgumentNullException.ThrowIfNull(orchestrationService);
            ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
            return BuildSupervisorOverview(orchestrationService.GetSupervisorStatus(sessionId, stalledAfterMinutes));
        });

    [McpServerTool(Name = "multiagent_supervisor_plan")]
    public static string SupervisorPlan(IOrchestrationService orchestrationService, string sessionId, int stalledAfterMinutes = 30, bool autoApplyPolicies = false, bool networkRecovered = false)
        => McpSafeExecutor.ExecuteJson("multiagent_supervisor_plan", () =>
        {
            ArgumentNullException.ThrowIfNull(orchestrationService);
            ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
            var plan = orchestrationService.GetSupervisorActionPlan(sessionId, stalledAfterMinutes, autoApplyPolicies, networkRecovered);
            return new
            {
                plan.SessionId,
                plan.EvaluatedAtUtc,
                plan.OrderedActions,
                plan.AutoAppliedActions,
                plan.ActionIds,
                nextRunnableTasks = plan.NextRunnableTasks.Select(task => new
                {
                    task.TaskId,
                    task.AgentId,
                    task.Title,
                    task.Reason,
                }).ToArray(),
            };
        });

    private static object BuildStartupPayload(OrchestrationSession session, OrchestrationSummary summary)
        => new
        {
            sessionId = session.SessionId,
            session.CreatedAtUtc,
            session.UpdatedAtUtc,
            request = new
            {
                session.Request.UserRequest,
                session.Request.Constraints,
                session.Request.DesiredArtifacts,
                session.Request.PreferredAgents,
                session.Request.MaxParallelAgents,
            },
            session.RecoveryGuidance,
            progress = BuildProgress(summary),
            plan = BuildPlanOverview(session.Plan),
            nextStep = "Use multiagent_session_status for progress, then activate specialist agents for ready tasks.",
        };

    private static object BuildProgress(OrchestrationSummary summary)
        => new
        {
            summary.Status,
            summary.TotalTasks,
            summary.CompletedTasks,
            summary.PendingTasks,
            summary.ReadyTaskIds,
            summary.BlockedTaskIds,
            summary.ResumeRequiredTaskIds,
            summary.AutoCheckpointTaskIds,
            summary.AutoRetryTaskIds,
            summary.LastHeartbeatUtc,
        };

    private static object BuildPlanOverview(OrchestrationPlan plan)
        => new
        {
            plan.Summary,
            plan.ParallelizationWindow,
            plan.CoordinationNotes,
            plan.ExecutionWaves,
            taskCount = plan.Tasks.Count,
            tasks = plan.Tasks.Select(task => new
            {
                task.TaskId,
                task.AgentId,
                task.AgentToolName,
                task.Title,
                task.Objective,
                task.PhaseName,
                task.PhaseOrder,
                task.SequenceOrder,
                dependencyTaskIds = task.Dependencies.Where(dependency => dependency.Kind == DependencyKind.Blocking).Select(dependency => dependency.TaskId).ToArray(),
            }).ToArray(),
        };

    private static object BuildSupervisorOverview(SupervisorStatus supervisor)
        => new
        {
            supervisor.SessionId,
            supervisor.EvaluatedAtUtc,
            alerts = supervisor.Alerts.Select(alert => new
            {
                alert.TaskId,
                alert.AgentId,
                alert.Kind,
                alert.Severity,
                alert.Message,
                alert.RecommendedActions,
            }).ToArray(),
            supervisor.Recommendations,
            supervisor.StalledTaskIds,
            supervisor.NextRunnableTasks,
            supervisor.HeartbeatIssues,
        };
}
