using Reactive.Multi.Agent.MCP.Core.Abstractions;
using Reactive.Multi.Agent.MCP.Core.Configuration;
using Reactive.Multi.Agent.MCP.Core.Models;
using Reactive.Multi.Agent.MCP.Core.Persistence;
using Reactive.Multi.Agent.MCP.Core.Services;
using Reactive.Multi.Agent.MCP.Knowledge.Services;
using Reactive.Multi.Agent.MCP.Server.Tools;
using System.Text.Json;

namespace Reactive.Multi.Agent.MCP.Tests;

/// <summary>
/// Verifies that every tool method returns a safe error payload (ok=false, error.message contains the parameter name)
/// when a required string parameter is passed as blank, rather than throwing or crashing.
/// This ensures AI clients receive actionable guidance to prompt users for missing inputs.
/// </summary>
public class BlankParameterValidationTests
{
    // ── Orchestrator tools ──────────────────────────────────────────────────

    [Test]
    public async Task OrchestrateRequest_With_Blank_UserRequest_Returns_Safe_Error_With_Parameter_Name()
    {
        var (orchestration, _) = CreateSession("blank-orchestrate");

        var json = OrchestratorTools.OrchestrateRequest(orchestration, "");

        await Assert.That(SafeErrorContainsParamName(json, "userRequest")).IsTrue();
    }

    [Test]
    public async Task SessionStatus_With_Blank_SessionId_Returns_Safe_Error_With_Parameter_Name()
    {
        var (orchestration, _) = CreateSession("blank-session-status");

        var json = OrchestratorTools.SessionStatus(orchestration, "");

        await Assert.That(SafeErrorContainsParamName(json, "sessionId")).IsTrue();
    }

    [Test]
    public async Task FinalizeSession_With_Blank_SessionId_Returns_Safe_Error_With_Parameter_Name()
    {
        var (orchestration, _) = CreateSession("blank-finalize");

        var json = OrchestratorTools.FinalizeSession(orchestration, "");

        await Assert.That(SafeErrorContainsParamName(json, "sessionId")).IsTrue();
    }

    [Test]
    public async Task ResumeTask_With_Blank_SessionId_Returns_Safe_Error_With_Parameter_Name()
    {
        var (orchestration, session) = CreateSession("blank-resume-task-session");

        var json = OrchestratorTools.ResumeTask(orchestration, "", "task-1", "csharp");

        await Assert.That(SafeErrorContainsParamName(json, "sessionId")).IsTrue();
    }

    [Test]
    public async Task ResumeTask_With_Blank_TaskId_Returns_Safe_Error_With_Parameter_Name()
    {
        var (orchestration, session) = CreateSession("blank-resume-task-taskid");

        var json = OrchestratorTools.ResumeTask(orchestration, session.SessionId, "", "csharp");

        await Assert.That(SafeErrorContainsParamName(json, "taskId")).IsTrue();
    }

    [Test]
    public async Task ResumeTask_With_Blank_AgentId_Returns_Safe_Error_With_Parameter_Name()
    {
        var (orchestration, session) = CreateSession("blank-resume-task-agentid");
        var task = session.Plan.Tasks.Single();

        var json = OrchestratorTools.ResumeTask(orchestration, session.SessionId, task.TaskId, "");

        await Assert.That(SafeErrorContainsParamName(json, "agentId")).IsTrue();
    }

    [Test]
    public async Task ResumeOrchestration_With_Blank_SessionId_Returns_Safe_Error_With_Parameter_Name()
    {
        var (orchestration, _) = CreateSession("blank-resume-orch");

        var json = OrchestratorTools.ResumeOrchestration(orchestration, "");

        await Assert.That(SafeErrorContainsParamName(json, "sessionId")).IsTrue();
    }

    [Test]
    public async Task UpdateSupervisorAction_With_Blank_SessionId_Returns_Safe_Error_With_Parameter_Name()
    {
        var (orchestration, _) = CreateSession("blank-update-sup-session");

        var json = OrchestratorTools.UpdateSupervisorAction(orchestration, "", "action-1", SupervisorActionState.Completed);

        await Assert.That(SafeErrorContainsParamName(json, "sessionId")).IsTrue();
    }

    [Test]
    public async Task UpdateSupervisorAction_With_Blank_ActionId_Returns_Safe_Error_With_Parameter_Name()
    {
        var (orchestration, session) = CreateSession("blank-update-sup-action");

        var json = OrchestratorTools.UpdateSupervisorAction(orchestration, session.SessionId, "", SupervisorActionState.Completed);

        await Assert.That(SafeErrorContainsParamName(json, "actionId")).IsTrue();
    }

    [Test]
    public async Task ApplySupervisorActionEscalation_With_Blank_SessionId_Returns_Safe_Error_With_Parameter_Name()
    {
        var (orchestration, _) = CreateSession("blank-escalation");

        var json = OrchestratorTools.ApplySupervisorActionEscalation(orchestration, "");

        await Assert.That(SafeErrorContainsParamName(json, "sessionId")).IsTrue();
    }

    [Test]
    public async Task RecordHeartbeat_With_Blank_SessionId_Returns_Safe_Error_With_Parameter_Name()
    {
        var (orchestration, _) = CreateSession("blank-heartbeat");

        var json = OrchestratorTools.RecordHeartbeat(orchestration, "");

        await Assert.That(SafeErrorContainsParamName(json, "sessionId")).IsTrue();
    }

    [Test]
    public async Task RunMaintenanceSweep_With_Blank_SessionId_Returns_Safe_Error_With_Parameter_Name()
    {
        var (orchestration, _) = CreateSession("blank-sweep");

        var json = OrchestratorTools.RunMaintenanceSweep(orchestration, "");

        await Assert.That(SafeErrorContainsParamName(json, "sessionId")).IsTrue();
    }

    [Test]
    public async Task GetMaintenanceReport_With_Blank_SessionId_Returns_Safe_Error_With_Parameter_Name()
    {
        var (orchestration, _) = CreateSession("blank-report");

        var json = OrchestratorTools.GetMaintenanceReport(orchestration, "");

        await Assert.That(SafeErrorContainsParamName(json, "sessionId")).IsTrue();
    }

    [Test]
    public async Task GetMaintenanceHistory_With_Blank_SessionId_Returns_Safe_Error_With_Parameter_Name()
    {
        var (orchestration, _) = CreateSession("blank-history");

        var json = OrchestratorTools.GetMaintenanceHistory(orchestration, "");

        await Assert.That(SafeErrorContainsParamName(json, "sessionId")).IsTrue();
    }

    [Test]
    public async Task ApplyAutomaticPolicy_With_Blank_SessionId_Returns_Safe_Error_With_Parameter_Name()
    {
        var (orchestration, _) = CreateSession("blank-policy-session");

        var json = OrchestratorTools.ApplyAutomaticPolicy(orchestration, "", "task-1", "csharp");

        await Assert.That(SafeErrorContainsParamName(json, "sessionId")).IsTrue();
    }

    [Test]
    public async Task ApplyAutomaticPolicy_With_Blank_TaskId_Returns_Safe_Error_With_Parameter_Name()
    {
        var (orchestration, session) = CreateSession("blank-policy-task");

        var json = OrchestratorTools.ApplyAutomaticPolicy(orchestration, session.SessionId, "", "csharp");

        await Assert.That(SafeErrorContainsParamName(json, "taskId")).IsTrue();
    }

    [Test]
    public async Task ApplyAutomaticPolicy_With_Blank_AgentId_Returns_Safe_Error_With_Parameter_Name()
    {
        var (orchestration, session) = CreateSession("blank-policy-agent");
        var task = session.Plan.Tasks.Single();

        var json = OrchestratorTools.ApplyAutomaticPolicy(orchestration, session.SessionId, task.TaskId, "");

        await Assert.That(SafeErrorContainsParamName(json, "agentId")).IsTrue();
    }

    [Test]
    public async Task SupervisorStatus_With_Blank_SessionId_Returns_Safe_Error_With_Parameter_Name()
    {
        var (orchestration, _) = CreateSession("blank-supervisor-status");

        var json = OrchestratorTools.SupervisorStatus(orchestration, "");

        await Assert.That(SafeErrorContainsParamName(json, "sessionId")).IsTrue();
    }

    [Test]
    public async Task SupervisorPlan_With_Blank_SessionId_Returns_Safe_Error_With_Parameter_Name()
    {
        var (orchestration, _) = CreateSession("blank-supervisor-plan");

        var json = OrchestratorTools.SupervisorPlan(orchestration, "");

        await Assert.That(SafeErrorContainsParamName(json, "sessionId")).IsTrue();
    }

    // ── Worker agent tools (all route through DispatchAgent) ────────────────

    [Test]
    public async Task CSharpAgent_With_Blank_SessionId_Returns_Safe_Error_With_Parameter_Name()
    {
        var (orchestration, _) = CreateSession("blank-csharp-session");

        var json = WorkerAgentTools.CSharpAgent(orchestration, "", "task-1");

        await Assert.That(SafeErrorContainsParamName(json, "sessionId")).IsTrue();
    }

    [Test]
    public async Task CSharpAgent_With_Blank_TaskId_Returns_Safe_Error_With_Parameter_Name()
    {
        var (orchestration, session) = CreateSession("blank-csharp-task");

        var json = WorkerAgentTools.CSharpAgent(orchestration, session.SessionId, "");

        await Assert.That(SafeErrorContainsParamName(json, "taskId")).IsTrue();
    }

    [Test]
    public async Task BlazorAgent_With_Blank_SessionId_Returns_Safe_Error_With_Parameter_Name()
    {
        var (orchestration, _) = CreateSession("blank-blazor-session");

        var json = WorkerAgentTools.BlazorAgent(orchestration, "", "task-1");

        await Assert.That(SafeErrorContainsParamName(json, "sessionId")).IsTrue();
    }

    [Test]
    public async Task BlazorAgent_With_Blank_TaskId_Returns_Safe_Error_With_Parameter_Name()
    {
        var (orchestration, session) = CreateSession("blank-blazor-task");

        var json = WorkerAgentTools.BlazorAgent(orchestration, session.SessionId, "");

        await Assert.That(SafeErrorContainsParamName(json, "taskId")).IsTrue();
    }

    // ── Catalog tools ────────────────────────────────────────────────────────

    [Test]
    public async Task GetAgent_With_Blank_Id_Returns_Safe_Error_With_Parameter_Name()
    {
        EmbeddedAgentCatalog catalog = new();

        var json = AgentCatalogTools.GetAgent(catalog, "");

        await Assert.That(SafeErrorContainsParamName(json, "id")).IsTrue();
    }

    [Test]
    public async Task GetAgent_With_Whitespace_Id_Returns_Safe_Error_With_Parameter_Name()
    {
        EmbeddedAgentCatalog catalog = new();

        var json = AgentCatalogTools.GetAgent(catalog, "   ");

        await Assert.That(SafeErrorContainsParamName(json, "id")).IsTrue();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static bool SafeErrorContainsParamName(string json, string paramName)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var ok = root.GetProperty("ok").GetBoolean();
        var message = root.GetProperty("error").GetProperty("message").GetString() ?? string.Empty;

        return !ok && message.Contains(paramName, StringComparison.OrdinalIgnoreCase);
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