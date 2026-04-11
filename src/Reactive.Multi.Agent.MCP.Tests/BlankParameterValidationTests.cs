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

        AssertSafeErrorContainsParamName(json, "userRequest");
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task SessionStatus_With_Blank_SessionId_Returns_Safe_Error_With_Parameter_Name()
    {
        var (orchestration, _) = CreateSession("blank-session-status");

        var json = OrchestratorTools.SessionStatus(orchestration, "");

        AssertSafeErrorContainsParamName(json, "sessionId");
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task FinalizeSession_With_Blank_SessionId_Returns_Safe_Error_With_Parameter_Name()
    {
        var (orchestration, _) = CreateSession("blank-finalize");

        var json = OrchestratorTools.FinalizeSession(orchestration, "");

        AssertSafeErrorContainsParamName(json, "sessionId");
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task ResumeTask_With_Blank_SessionId_Returns_Safe_Error_With_Parameter_Name()
    {
        var (orchestration, session) = CreateSession("blank-resume-task-session");

        var json = OrchestratorTools.ResumeTask(orchestration, "", "task-1", "csharp");

        AssertSafeErrorContainsParamName(json, "sessionId");
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task ResumeTask_With_Blank_TaskId_Returns_Safe_Error_With_Parameter_Name()
    {
        var (orchestration, session) = CreateSession("blank-resume-task-taskid");

        var json = OrchestratorTools.ResumeTask(orchestration, session.SessionId, "", "csharp");

        AssertSafeErrorContainsParamName(json, "taskId");
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task ResumeTask_With_Blank_AgentId_Returns_Safe_Error_With_Parameter_Name()
    {
        var (orchestration, session) = CreateSession("blank-resume-task-agentid");
        var task = session.Plan.Tasks.Single();

        var json = OrchestratorTools.ResumeTask(orchestration, session.SessionId, task.TaskId, "");

        AssertSafeErrorContainsParamName(json, "agentId");
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task ResumeOrchestration_With_Blank_SessionId_Returns_Safe_Error_With_Parameter_Name()
    {
        var (orchestration, _) = CreateSession("blank-resume-orch");

        var json = OrchestratorTools.ResumeOrchestration(orchestration, "");

        AssertSafeErrorContainsParamName(json, "sessionId");
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task UpdateSupervisorAction_With_Blank_SessionId_Returns_Safe_Error_With_Parameter_Name()
    {
        var (orchestration, _) = CreateSession("blank-update-sup-session");

        var json = OrchestratorTools.UpdateSupervisorAction(orchestration, "", "action-1", SupervisorActionState.Completed);

        AssertSafeErrorContainsParamName(json, "sessionId");
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task UpdateSupervisorAction_With_Blank_ActionId_Returns_Safe_Error_With_Parameter_Name()
    {
        var (orchestration, session) = CreateSession("blank-update-sup-action");

        var json = OrchestratorTools.UpdateSupervisorAction(orchestration, session.SessionId, "", SupervisorActionState.Completed);

        AssertSafeErrorContainsParamName(json, "actionId");
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task ApplySupervisorActionEscalation_With_Blank_SessionId_Returns_Safe_Error_With_Parameter_Name()
    {
        var (orchestration, _) = CreateSession("blank-escalation");

        var json = OrchestratorTools.ApplySupervisorActionEscalation(orchestration, "");

        AssertSafeErrorContainsParamName(json, "sessionId");
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task RecordHeartbeat_With_Blank_SessionId_Returns_Safe_Error_With_Parameter_Name()
    {
        var (orchestration, _) = CreateSession("blank-heartbeat");

        var json = OrchestratorTools.RecordHeartbeat(orchestration, "");

        AssertSafeErrorContainsParamName(json, "sessionId");
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task RunMaintenanceSweep_With_Blank_SessionId_Returns_Safe_Error_With_Parameter_Name()
    {
        var (orchestration, _) = CreateSession("blank-sweep");

        var json = OrchestratorTools.RunMaintenanceSweep(orchestration, "");

        AssertSafeErrorContainsParamName(json, "sessionId");
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task GetMaintenanceReport_With_Blank_SessionId_Returns_Safe_Error_With_Parameter_Name()
    {
        var (orchestration, _) = CreateSession("blank-report");

        var json = OrchestratorTools.GetMaintenanceReport(orchestration, "");

        AssertSafeErrorContainsParamName(json, "sessionId");
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task GetMaintenanceHistory_With_Blank_SessionId_Returns_Safe_Error_With_Parameter_Name()
    {
        var (orchestration, _) = CreateSession("blank-history");

        var json = OrchestratorTools.GetMaintenanceHistory(orchestration, "");

        AssertSafeErrorContainsParamName(json, "sessionId");
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task ApplyAutomaticPolicy_With_Blank_SessionId_Returns_Safe_Error_With_Parameter_Name()
    {
        var (orchestration, _) = CreateSession("blank-policy-session");

        var json = OrchestratorTools.ApplyAutomaticPolicy(orchestration, "", "task-1", "csharp");

        AssertSafeErrorContainsParamName(json, "sessionId");
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task ApplyAutomaticPolicy_With_Blank_TaskId_Returns_Safe_Error_With_Parameter_Name()
    {
        var (orchestration, session) = CreateSession("blank-policy-task");

        var json = OrchestratorTools.ApplyAutomaticPolicy(orchestration, session.SessionId, "", "csharp");

        AssertSafeErrorContainsParamName(json, "taskId");
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task ApplyAutomaticPolicy_With_Blank_AgentId_Returns_Safe_Error_With_Parameter_Name()
    {
        var (orchestration, session) = CreateSession("blank-policy-agent");
        var task = session.Plan.Tasks.Single();

        var json = OrchestratorTools.ApplyAutomaticPolicy(orchestration, session.SessionId, task.TaskId, "");

        AssertSafeErrorContainsParamName(json, "agentId");
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task SupervisorStatus_With_Blank_SessionId_Returns_Safe_Error_With_Parameter_Name()
    {
        var (orchestration, _) = CreateSession("blank-supervisor-status");

        var json = OrchestratorTools.SupervisorStatus(orchestration, "");

        AssertSafeErrorContainsParamName(json, "sessionId");
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task SupervisorPlan_With_Blank_SessionId_Returns_Safe_Error_With_Parameter_Name()
    {
        var (orchestration, _) = CreateSession("blank-supervisor-plan");

        var json = OrchestratorTools.SupervisorPlan(orchestration, "");

        AssertSafeErrorContainsParamName(json, "sessionId");
        await Assert.That(true).IsTrue();
    }

    // ── Worker agent tools (all route through DispatchAgent) ────────────────

    [Test]
    public async Task CSharpAgent_With_Blank_SessionId_Returns_Safe_Error_With_Parameter_Name()
    {
        var (orchestration, _) = CreateSession("blank-csharp-session");

        var json = WorkerAgentTools.CSharpAgent(orchestration, "", "task-1");

        AssertSafeErrorContainsParamName(json, "sessionId");
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task CSharpAgent_With_Blank_TaskId_Returns_Safe_Error_With_Parameter_Name()
    {
        var (orchestration, session) = CreateSession("blank-csharp-task");

        var json = WorkerAgentTools.CSharpAgent(orchestration, session.SessionId, "");

        AssertSafeErrorContainsParamName(json, "taskId");
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task BlazorAgent_With_Blank_SessionId_Returns_Safe_Error_With_Parameter_Name()
    {
        var (orchestration, _) = CreateSession("blank-blazor-session");

        var json = WorkerAgentTools.BlazorAgent(orchestration, "", "task-1");

        AssertSafeErrorContainsParamName(json, "sessionId");
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task BlazorAgent_With_Blank_TaskId_Returns_Safe_Error_With_Parameter_Name()
    {
        var (orchestration, session) = CreateSession("blank-blazor-task");

        var json = WorkerAgentTools.BlazorAgent(orchestration, session.SessionId, "");

        AssertSafeErrorContainsParamName(json, "taskId");
        await Assert.That(true).IsTrue();
    }

    // ── Catalog tools ────────────────────────────────────────────────────────

    [Test]
    public async Task GetAgent_With_Blank_Id_Returns_Safe_Error_With_Parameter_Name()
    {
        IAgentCatalog catalog = new EmbeddedAgentCatalog();

        var json = AgentCatalogTools.GetAgent(catalog, "");

        AssertSafeErrorContainsParamName(json, "id");
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task GetAgent_With_Whitespace_Id_Returns_Safe_Error_With_Parameter_Name()
    {
        IAgentCatalog catalog = new EmbeddedAgentCatalog();

        var json = AgentCatalogTools.GetAgent(catalog, "   ");

        AssertSafeErrorContainsParamName(json, "id");
        await Assert.That(true).IsTrue();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Asserts that the JSON response is a safe error envelope with ok=false
    /// and that the error message identifies the blank parameter by name.
    /// </summary>
    private static void AssertSafeErrorContainsParamName(string json, string paramName)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var ok = root.GetProperty("ok").GetBoolean();
        var message = root.GetProperty("error").GetProperty("message").GetString() ?? string.Empty;

        if (ok)
            throw new Exception($"Expected ok=false for blank '{paramName}' but got ok=true. Response: {json}");

        if (!message.Contains(paramName, StringComparison.OrdinalIgnoreCase))
            throw new Exception($"Expected error message to contain '{paramName}' but got: {message}");
    }

    private static (IOrchestrationService orchestration, OrchestrationSession session) CreateSession(string folderPrefix)
    {
        var options = CreateOptions(folderPrefix);
        var store = new SqliteOrchestrationSessionStore(options);
        IAgentCatalog catalog = new EmbeddedAgentCatalog();
        IRequestDecomposer decomposer = new RequestDecomposer(catalog);
        IOrchestrationService orchestration = new OrchestrationService(decomposer, catalog, store);
        var session = orchestration.CreateSession(OrchestrationRequest.FromStrings("Build a Blazor app"));
        return (orchestration, session);
    }

    private static ReactiveMultiAgentOptions CreateOptions(string folderPrefix)
        => new()
        {
            StateRootPath = Path.Combine(Path.GetTempPath(), folderPrefix, Guid.NewGuid().ToString("N")),
        };
}
