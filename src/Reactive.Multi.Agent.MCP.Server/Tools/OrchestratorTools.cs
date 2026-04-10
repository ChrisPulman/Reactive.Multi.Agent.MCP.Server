using ModelContextProtocol.Server;
using Reactive.Multi.Agent.MCP.Core.Abstractions;
using Reactive.Multi.Agent.MCP.Core.Models;
using Reactive.Multi.Agent.MCP.Server.Serialization;

namespace Reactive.Multi.Agent.MCP.Server.Tools;

[McpServerToolType]
public sealed class OrchestratorTools
{
    [McpServerTool(Name = "multiagent_orchestrate_request")]
    public static string OrchestrateRequest(IOrchestrationService orchestrationService, string userRequest, string? constraints = null, string? desiredArtifacts = null, string? preferredAgents = null, int maxParallelAgents = 4)
    {
        ArgumentNullException.ThrowIfNull(orchestrationService);
        var session = orchestrationService.CreateSession(OrchestrationRequest.FromStrings(userRequest, constraints, desiredArtifacts, preferredAgents, maxParallelAgents));
        var summary = orchestrationService.FinalizeSession(session.SessionId);
        return JsonOutput.Serialize(new { session, summary });
    }

    [McpServerTool(Name = "multiagent_session_status")]
    public static string SessionStatus(IOrchestrationService orchestrationService, string sessionId)
    {
        ArgumentNullException.ThrowIfNull(orchestrationService);
        var session = orchestrationService.GetSession(sessionId)
            ?? throw new InvalidOperationException($"Unknown orchestration session '{sessionId}'.");
        var summary = orchestrationService.FinalizeSession(sessionId);
        var supervisor = orchestrationService.GetSupervisorStatus(sessionId);
        return JsonOutput.Serialize(new { session, summary, supervisor });
    }

    [McpServerTool(Name = "multiagent_finalize_session")]
    public static string FinalizeSession(IOrchestrationService orchestrationService, string sessionId)
    {
        ArgumentNullException.ThrowIfNull(orchestrationService);
        return JsonOutput.Serialize(orchestrationService.FinalizeSession(sessionId));
    }

    [McpServerTool(Name = "multiagent_resume_task")]
    public static string ResumeTask(IOrchestrationService orchestrationService, string sessionId, string taskId, string agentId)
    {
        ArgumentNullException.ThrowIfNull(orchestrationService);
        return JsonOutput.Serialize(orchestrationService.ResumeTask(sessionId, taskId, agentId));
    }

    [McpServerTool(Name = "multiagent_resume_orchestration")]
    public static string ResumeOrchestration(IOrchestrationService orchestrationService, string sessionId)
    {
        ArgumentNullException.ThrowIfNull(orchestrationService);
        return JsonOutput.Serialize(orchestrationService.ResumeOrchestration(sessionId));
    }

    [McpServerTool(Name = "multiagent_update_supervisor_action")]
    public static string UpdateSupervisorAction(IOrchestrationService orchestrationService, string sessionId, string actionId, SupervisorActionState state)
    {
        ArgumentNullException.ThrowIfNull(orchestrationService);
        return JsonOutput.Serialize(orchestrationService.UpdateSupervisorAction(sessionId, actionId, state));
    }

    [McpServerTool(Name = "multiagent_apply_supervisor_action_escalation")]
    public static string ApplySupervisorActionEscalation(IOrchestrationService orchestrationService, string sessionId, int staleAfterMinutes = 30, int criticalAfterMinutes = 90)
    {
        ArgumentNullException.ThrowIfNull(orchestrationService);
        return JsonOutput.Serialize(orchestrationService.ApplySupervisorActionEscalation(sessionId, staleAfterMinutes, criticalAfterMinutes));
    }

    [McpServerTool(Name = "multiagent_record_heartbeat")]
    public static string RecordHeartbeat(IOrchestrationService orchestrationService, string sessionId, string? taskId = null, string? agentId = null, string? actionId = null, string source = "external")
    {
        ArgumentNullException.ThrowIfNull(orchestrationService);
        return JsonOutput.Serialize(orchestrationService.RecordHeartbeat(sessionId, taskId, agentId, actionId, source));
    }

    [McpServerTool(Name = "multiagent_run_maintenance_sweep")]
    public static string RunMaintenanceSweep(IOrchestrationService orchestrationService, string sessionId, int silentHeartbeatMinutes = 15, int staleTaskMinutes = 30, int staleActionMinutes = 30, int criticalActionMinutes = 90)
    {
        ArgumentNullException.ThrowIfNull(orchestrationService);
        return JsonOutput.Serialize(orchestrationService.RunMaintenanceSweep(sessionId, silentHeartbeatMinutes, staleTaskMinutes, staleActionMinutes, criticalActionMinutes));
    }

    [McpServerTool(Name = "multiagent_get_maintenance_report")]
    public static string GetMaintenanceReport(IOrchestrationService orchestrationService, string sessionId, int silentHeartbeatMinutes = 15, int staleTaskMinutes = 30, int staleActionMinutes = 30, int criticalActionMinutes = 90, bool autoApplyPolicies = false, bool networkRecovered = false)
    {
        ArgumentNullException.ThrowIfNull(orchestrationService);
        return JsonOutput.Serialize(orchestrationService.GetMaintenanceReport(sessionId, silentHeartbeatMinutes, staleTaskMinutes, staleActionMinutes, criticalActionMinutes, autoApplyPolicies, networkRecovered));
    }

    [McpServerTool(Name = "multiagent_get_maintenance_history")]
    public static string GetMaintenanceHistory(IOrchestrationService orchestrationService, string sessionId, int limit = 10)
    {
        ArgumentNullException.ThrowIfNull(orchestrationService);
        return JsonOutput.Serialize(orchestrationService.GetMaintenanceHistory(sessionId, limit));
    }

    [McpServerTool(Name = "multiagent_apply_automatic_policy")]
    public static string ApplyAutomaticPolicy(IOrchestrationService orchestrationService, string sessionId, string taskId, string agentId, int? currentEstimatedTokens = null, int? remainingSubscriptionTokens = null, bool networkRecovered = false)
    {
        ArgumentNullException.ThrowIfNull(orchestrationService);
        return JsonOutput.Serialize(orchestrationService.ApplyAutomaticPolicy(sessionId, taskId, agentId, currentEstimatedTokens, remainingSubscriptionTokens, networkRecovered));
    }

    [McpServerTool(Name = "multiagent_search_sessions")]
    public static string SearchSessions(IOrchestrationService orchestrationService, string? query = null, int limit = 20)
    {
        ArgumentNullException.ThrowIfNull(orchestrationService);
        return JsonOutput.Serialize(new { query, results = orchestrationService.SearchSessions(query, limit) });
    }

    [McpServerTool(Name = "multiagent_supervisor_status")]
    public static string SupervisorStatus(IOrchestrationService orchestrationService, string sessionId, int stalledAfterMinutes = 30)
    {
        ArgumentNullException.ThrowIfNull(orchestrationService);
        return JsonOutput.Serialize(orchestrationService.GetSupervisorStatus(sessionId, stalledAfterMinutes));
    }

    [McpServerTool(Name = "multiagent_supervisor_plan")]
    public static string SupervisorPlan(IOrchestrationService orchestrationService, string sessionId, int stalledAfterMinutes = 30, bool autoApplyPolicies = false, bool networkRecovered = false)
    {
        ArgumentNullException.ThrowIfNull(orchestrationService);
        return JsonOutput.Serialize(orchestrationService.GetSupervisorActionPlan(sessionId, stalledAfterMinutes, autoApplyPolicies, networkRecovered));
    }
}
