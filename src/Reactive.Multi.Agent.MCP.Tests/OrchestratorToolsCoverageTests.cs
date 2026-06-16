using Reactive.Multi.Agent.MCP.Core.Abstractions;
using Reactive.Multi.Agent.MCP.Core.Configuration;
using Reactive.Multi.Agent.MCP.Core.Models;
using Reactive.Multi.Agent.MCP.Core.Persistence;
using Reactive.Multi.Agent.MCP.Core.Services;
using Reactive.Multi.Agent.MCP.Knowledge.Services;
using Reactive.Multi.Agent.MCP.Server.Tools;
using System.Text.Json;

namespace Reactive.Multi.Agent.MCP.Tests;

public class OrchestratorToolsCoverageTests
{
    [Test]
    public async Task CreateSession_Alias_Tool_Returns_Startup_Payload_With_Model_Guidance()
    {
        var options = CreateOptions("orchestrator-create-session-alias");
        using var store = new SqliteOrchestrationSessionStore(options);
        EmbeddedAgentCatalog catalog = new();
        RequestDecomposer decomposer = new(catalog);
        OrchestrationService orchestration = new(decomposer, catalog, store);

        var json = OrchestratorTools.CreateSession(orchestration, "Build a Blazor app");

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        await Assert.That(root.TryGetProperty("ok", out _)).IsFalse();
        await Assert.That(root.TryGetProperty("sessionId", out _)).IsTrue();
        await Assert.That(root.TryGetProperty("sessionCreationTools", out var sessionCreationTools)).IsTrue();
        await Assert.That(sessionCreationTools.EnumerateArray().Select(tool => tool.GetString()).ToArray()).Contains("multiagent_create_session");
        await Assert.That(root.TryGetProperty("orchestratorModelRequirement", out var modelRequirement)).IsTrue();
        await Assert.That(modelRequirement.GetString()).Contains("GPT-5.5");
    }

    [Test]
    public async Task FinalizeSession_Tool_Returns_Summary_Fields()
    {
        var (orchestration, session) = CreateSession("orchestrator-finalize");

        var json = OrchestratorTools.FinalizeSession(orchestration, session.SessionId);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        await Assert.That(root.TryGetProperty("ok", out _)).IsFalse();
        await Assert.That(root.TryGetProperty("sessionId", out var sessionId)).IsTrue();
        await Assert.That(sessionId.GetString()).IsEqualTo(session.SessionId);
        await Assert.That(root.TryGetProperty("status", out _)).IsTrue();
        await Assert.That(root.TryGetProperty("totalTasks", out _)).IsTrue();
    }

    [Test]
    public async Task FinalizeSession_Tool_Returns_Safe_Error_For_Unknown_Session()
    {
        var (orchestration, _) = CreateSession("orchestrator-finalize-error");

        var json = OrchestratorTools.FinalizeSession(orchestration, "missing-session");

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        await Assert.That(root.GetProperty("ok").GetBoolean()).IsFalse();
        await Assert.That(root.GetProperty("operation").GetString()).IsEqualTo("multiagent_finalize_session");
    }

    [Test]
    public async Task ResumeTask_Tool_Returns_Task_Packet_With_NeedsResume_False()
    {
        var options = CreateOptions("orchestrator-resume-task");
        using var store = new SqliteOrchestrationSessionStore(options);
        EmbeddedAgentCatalog catalog = new();
        RequestDecomposer decomposer = new(catalog);
        OrchestrationService orchestration = new(decomposer, catalog, store);
        var session = orchestration.CreateSession(OrchestrationRequest.FromStrings("Build a Blazor app"));
        var task = session.Plan.Tasks.Single();

        orchestration.ActivateAgentTask(session.SessionId, task.TaskId, task.AgentId);
        orchestration.ReportTaskFailure(session.SessionId, task.TaskId, task.AgentId, AgentFailureKind.ContextWindowLimit, "Context too large.");

        var json = OrchestratorTools.ResumeTask(orchestration, session.SessionId, task.TaskId, task.AgentId);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        await Assert.That(root.TryGetProperty("ok", out _)).IsFalse();
        await Assert.That(root.TryGetProperty("taskId", out var taskId)).IsTrue();
        await Assert.That(taskId.GetString()).IsEqualTo(task.TaskId);
        await Assert.That(root.TryGetProperty("needsResume", out var needsResume)).IsTrue();
        await Assert.That(needsResume.GetBoolean()).IsFalse();
    }

    [Test]
    public async Task ResumeOrchestration_Tool_Returns_Resume_State_Fields()
    {
        var (orchestration, session) = CreateSession("orchestrator-resume-orch");

        var json = OrchestratorTools.ResumeOrchestration(orchestration, session.SessionId);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        await Assert.That(root.TryGetProperty("ok", out _)).IsFalse();
        await Assert.That(root.TryGetProperty("sessionId", out var sessionId)).IsTrue();
        await Assert.That(sessionId.GetString()).IsEqualTo(session.SessionId);
        await Assert.That(root.TryGetProperty("resumeState", out _)).IsTrue();
        await Assert.That(root.TryGetProperty("lastHeartbeatUtc", out _)).IsTrue();
    }

    [Test]
    public async Task UpdateSupervisorAction_Tool_Returns_Updated_Action_State()
    {
        var options = CreateOptions("orchestrator-update-action");
        using var store = new SqliteOrchestrationSessionStore(options);
        EmbeddedAgentCatalog catalog = new();
        RequestDecomposer decomposer = new(catalog);
        OrchestrationService orchestration = new(decomposer, catalog, store);
        var session = orchestration.CreateSession(OrchestrationRequest.FromStrings("Build a Blazor app"));
        var plan = orchestration.GetSupervisorActionPlan(session.SessionId);
        var actionId = plan.ActionIds[0];

        var json = OrchestratorTools.UpdateSupervisorAction(orchestration, session.SessionId, actionId, SupervisorActionState.Acknowledged);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        await Assert.That(root.TryGetProperty("ok", out _)).IsFalse();
        await Assert.That(root.TryGetProperty("sessionId", out var sessionId)).IsTrue();
        await Assert.That(sessionId.GetString()).IsEqualTo(session.SessionId);
        await Assert.That(root.TryGetProperty("actionId", out var returnedActionId)).IsTrue();
        await Assert.That(returnedActionId.GetString()).IsEqualTo(actionId);
        await Assert.That(root.TryGetProperty("state", out var state)).IsTrue();
        await Assert.That(state.GetString()).IsEqualTo("Acknowledged");
    }

    [Test]
    public async Task ApplySupervisorActionEscalation_Tool_Returns_Escalated_Actions_Field()
    {
        var (orchestration, session) = CreateSession("orchestrator-escalation");
        orchestration.GetSupervisorActionPlan(session.SessionId);

        var json = OrchestratorTools.ApplySupervisorActionEscalation(orchestration, session.SessionId, staleAfterMinutes: 30, criticalAfterMinutes: 90);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        await Assert.That(root.TryGetProperty("ok", out _)).IsFalse();
        await Assert.That(root.TryGetProperty("sessionId", out var sessionId)).IsTrue();
        await Assert.That(sessionId.GetString()).IsEqualTo(session.SessionId);
        await Assert.That(root.TryGetProperty("escalatedActions", out _)).IsTrue();
        await Assert.That(root.TryGetProperty("incompleteActionIds", out _)).IsTrue();
    }

    [Test]
    public async Task RecordHeartbeat_Tool_Returns_Updated_Heartbeat_Fields()
    {
        var (orchestration, session) = CreateSession("orchestrator-heartbeat");

        var json = OrchestratorTools.RecordHeartbeat(orchestration, session.SessionId, source: "test");

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        await Assert.That(root.TryGetProperty("ok", out _)).IsFalse();
        await Assert.That(root.TryGetProperty("sessionId", out var sessionId)).IsTrue();
        await Assert.That(sessionId.GetString()).IsEqualTo(session.SessionId);
        await Assert.That(root.TryGetProperty("lastHeartbeatUtc", out _)).IsTrue();
        await Assert.That(root.TryGetProperty("source", out var source)).IsTrue();
        await Assert.That(source.GetString()).IsEqualTo("test");
    }

    [Test]
    public async Task RunMaintenanceSweep_Tool_Returns_Sweep_Fields()
    {
        var (orchestration, session) = CreateSession("orchestrator-sweep");

        var json = OrchestratorTools.RunMaintenanceSweep(orchestration, session.SessionId);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        await Assert.That(root.TryGetProperty("ok", out _)).IsFalse();
        await Assert.That(root.TryGetProperty("sessionId", out var sessionId)).IsTrue();
        await Assert.That(sessionId.GetString()).IsEqualTo(session.SessionId);
        await Assert.That(root.TryGetProperty("heartbeatIssueCount", out _)).IsTrue();
        await Assert.That(root.TryGetProperty("alertCount", out _)).IsTrue();
        await Assert.That(root.TryGetProperty("incompleteActionIds", out _)).IsTrue();
    }

    [Test]
    public async Task GetMaintenanceReport_Tool_Returns_Report_Fields()
    {
        var (orchestration, session) = CreateSession("orchestrator-report");

        var json = OrchestratorTools.GetMaintenanceReport(orchestration, session.SessionId);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        await Assert.That(root.TryGetProperty("ok", out _)).IsFalse();
        await Assert.That(root.TryGetProperty("verdict", out _)).IsTrue();
        await Assert.That(root.TryGetProperty("cronSummary", out _)).IsTrue();
    }

    [Test]
    public async Task GetMaintenanceHistory_Tool_Returns_Snapshot_List_After_Two_Reports()
    {
        var (orchestration, session) = CreateSession("orchestrator-history");
        OrchestratorTools.GetMaintenanceReport(orchestration, session.SessionId);
        OrchestratorTools.GetMaintenanceReport(orchestration, session.SessionId);

        var json = OrchestratorTools.GetMaintenanceHistory(orchestration, session.SessionId, limit: 10);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        await Assert.That(root.ValueKind).IsEqualTo(JsonValueKind.Array);
        await Assert.That(root.GetArrayLength()).IsGreaterThanOrEqualTo(2);
    }

    [Test]
    public async Task ApplyAutomaticPolicy_Tool_Returns_Task_Packet_With_Recovery_State()
    {
        var options = CreateOptions("orchestrator-auto-policy");
        using var store = new SqliteOrchestrationSessionStore(options);
        EmbeddedAgentCatalog catalog = new();
        RequestDecomposer decomposer = new(catalog);
        OrchestrationService orchestration = new(decomposer, catalog, store);
        var session = orchestration.CreateSession(OrchestrationRequest.FromStrings("Build a Blazor app"));
        var task = session.Plan.Tasks.Single();

        var json = OrchestratorTools.ApplyAutomaticPolicy(orchestration, session.SessionId, task.TaskId, task.AgentId, currentEstimatedTokens: 9500);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        await Assert.That(root.TryGetProperty("ok", out _)).IsFalse();
        await Assert.That(root.TryGetProperty("taskId", out var taskId)).IsTrue();
        await Assert.That(taskId.GetString()).IsEqualTo(task.TaskId);
        await Assert.That(root.TryGetProperty("recoveryState", out _)).IsTrue();
    }

    [Test]
    public async Task SearchSessions_Tool_Returns_Created_Session_In_Results()
    {
        var (orchestration, session) = CreateSession("orchestrator-search");

        var json = OrchestratorTools.SearchSessions(orchestration, query: "Blazor");

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        await Assert.That(root.TryGetProperty("ok", out _)).IsFalse();
        await Assert.That(root.TryGetProperty("results", out var results)).IsTrue();
        var found = false;
        foreach (var entry in results.EnumerateArray())
        {
            if (entry.TryGetProperty("sessionId", out var id) && id.GetString() == session.SessionId)
            {
                found = true;
                break;
            }
        }
        await Assert.That(found).IsTrue();
    }

    [Test]
    public async Task SupervisorStatus_Tool_Returns_Overview_Fields()
    {
        var (orchestration, session) = CreateSession("orchestrator-supervisor-status");

        var json = OrchestratorTools.SupervisorStatus(orchestration, session.SessionId);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        await Assert.That(root.TryGetProperty("ok", out _)).IsFalse();
        await Assert.That(root.TryGetProperty("sessionId", out var sessionId)).IsTrue();
        await Assert.That(sessionId.GetString()).IsEqualTo(session.SessionId);
        await Assert.That(root.TryGetProperty("evaluatedAtUtc", out _)).IsTrue();
        await Assert.That(root.TryGetProperty("alerts", out _)).IsTrue();
    }

    [Test]
    public async Task SupervisorPlan_Tool_Returns_Action_Plan_Fields()
    {
        var (orchestration, session) = CreateSession("orchestrator-supervisor-plan");

        var json = OrchestratorTools.SupervisorPlan(orchestration, session.SessionId);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        await Assert.That(root.TryGetProperty("ok", out _)).IsFalse();
        await Assert.That(root.TryGetProperty("sessionId", out var sessionId)).IsTrue();
        await Assert.That(sessionId.GetString()).IsEqualTo(session.SessionId);
        await Assert.That(root.TryGetProperty("orderedActions", out _)).IsTrue();
        await Assert.That(root.TryGetProperty("actionIds", out _)).IsTrue();
        await Assert.That(root.TryGetProperty("nextRunnableTasks", out _)).IsTrue();
    }

    private static (OrchestrationService orchestration, OrchestrationSession session) CreateSession(string folderPrefix)
    {
        var options = CreateOptions(folderPrefix);
        var store = new SqliteOrchestrationSessionStore(options);
        EmbeddedAgentCatalog catalog = new();
        RequestDecomposer decomposer = new(catalog);
        OrchestrationService orchestration = new(decomposer, catalog, store);
        var session = orchestration.CreateSession(OrchestrationRequest.FromStrings("Build a Blazor app"));
        return (orchestration, session);
    }

    private static ReactiveMultiAgentOptions CreateOptions(string folderPrefix)
        => new()
        {
            StateRootPath = Path.Combine(Path.GetTempPath(), folderPrefix, Guid.NewGuid().ToString("N")),
        };
}
