using Reactive.Multi.Agent.MCP.Core.Abstractions;
using Reactive.Multi.Agent.MCP.Core.Configuration;
using Reactive.Multi.Agent.MCP.Core.Models;
using Reactive.Multi.Agent.MCP.Core.Persistence;
using Reactive.Multi.Agent.MCP.Core.Services;
using Reactive.Multi.Agent.MCP.Knowledge.Services;
using Reactive.Multi.Agent.MCP.Server.Tools;
using System.Text.Json;

namespace Reactive.Multi.Agent.MCP.Tests;

public class WorkerAgentAndCatalogToolsTests
{
    [Test]
    public async Task BlazorAgent_Activate_Returns_Packet_With_ExecutionPrompt_And_InProgress_Status()
    {
        var options = CreateOptions("worker-activate");
        using var store = new SqliteOrchestrationSessionStore(options);
        EmbeddedAgentCatalog catalog = new();
        RequestDecomposer decomposer = new(catalog);
        OrchestrationService orchestration = new(decomposer, catalog, store);
        var session = orchestration.CreateSession(OrchestrationRequest.FromStrings("Build a Blazor app"));
        var task = session.Plan.Tasks.Single();

        var json = WorkerAgentTools.BlazorAgent(orchestration, session.SessionId, task.TaskId);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        await Assert.That(root.TryGetProperty("taskId", out var taskId)).IsTrue();
        await Assert.That(taskId.GetString()).IsEqualTo(task.TaskId);
        await Assert.That(root.TryGetProperty("status", out var status)).IsTrue();
        await Assert.That(status.GetString()).IsEqualTo("InProgress");
        await Assert.That(root.TryGetProperty("agentName", out var agentName)).IsTrue();
        await Assert.That(agentName.GetString()).IsEqualTo("Blazor Agent - task-1");
        await Assert.That(root.TryGetProperty("shutdownRequired", out var shutdownRequired)).IsTrue();
        await Assert.That(shutdownRequired.GetBoolean()).IsFalse();
        await Assert.That(root.TryGetProperty("lifecycleInstruction", out var lifecycleInstruction)).IsTrue();
        await Assert.That(lifecycleInstruction.GetString()).Contains("Spawn or continue sub-agent");
        await Assert.That(root.TryGetProperty("executionPrompt", out var prompt)).IsTrue();
        await Assert.That(string.IsNullOrWhiteSpace(prompt.GetString())).IsFalse();
        await Assert.That(prompt.GetString()).Contains("GPT-5.5");
        await Assert.That(root.TryGetProperty("nextSteps", out var nextSteps)).IsTrue();
        await Assert.That(nextSteps.EnumerateArray().Select(step => step.GetString()).ToArray()).Contains("Keep orchestration/control-plane decisions with GPT-5.5 or an equivalent highest-capacity orchestrator context.");
    }

    [Test]
    public async Task BlazorAgent_Submit_Results_Marks_Task_Completed()
    {
        var options = CreateOptions("worker-submit");
        using var store = new SqliteOrchestrationSessionStore(options);
        EmbeddedAgentCatalog catalog = new();
        RequestDecomposer decomposer = new(catalog);
        OrchestrationService orchestration = new(decomposer, catalog, store);
        var session = orchestration.CreateSession(OrchestrationRequest.FromStrings("Build a Blazor app"));
        var task = session.Plan.Tasks.Single();

        WorkerAgentTools.BlazorAgent(orchestration, session.SessionId, task.TaskId);
        var json = WorkerAgentTools.BlazorAgent(
            orchestration, session.SessionId, task.TaskId,
            workSummary: "Blazor app shell complete.", markComplete: true);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        await Assert.That(root.TryGetProperty("ok", out _)).IsFalse();
        await Assert.That(root.TryGetProperty("status", out var status)).IsTrue();
        await Assert.That(status.GetString()).IsEqualTo("Completed");
        await Assert.That(root.TryGetProperty("shutdownRequired", out var shutdownRequired)).IsTrue();
        await Assert.That(shutdownRequired.GetBoolean()).IsTrue();
        await Assert.That(root.TryGetProperty("lifecycleInstruction", out var lifecycleInstruction)).IsTrue();
        await Assert.That(lifecycleInstruction.GetString()).Contains("Close sub-agent");
    }

    [Test]
    public async Task BlazorAgent_Create_Checkpoint_Returns_Checkpoints_In_Packet()
    {
        var options = CreateOptions("worker-checkpoint");
        using var store = new SqliteOrchestrationSessionStore(options);
        EmbeddedAgentCatalog catalog = new();
        RequestDecomposer decomposer = new(catalog);
        OrchestrationService orchestration = new(decomposer, catalog, store);
        var session = orchestration.CreateSession(OrchestrationRequest.FromStrings("Build a Blazor app"));
        var task = session.Plan.Tasks.Single();

        WorkerAgentTools.BlazorAgent(orchestration, session.SessionId, task.TaskId);
        var json = WorkerAgentTools.BlazorAgent(
            orchestration, session.SessionId, task.TaskId,
            createCheckpoint: true, checkpointSummary: "Mid-point reached.");

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        await Assert.That(root.TryGetProperty("ok", out _)).IsFalse();
        await Assert.That(root.TryGetProperty("checkpoints", out var checkpoints)).IsTrue();
        await Assert.That(checkpoints.GetArrayLength()).IsGreaterThanOrEqualTo(1);
    }

    [Test]
    public async Task BlazorAgent_Report_Failure_Returns_NeedsResume_True()
    {
        var options = CreateOptions("worker-failure");
        using var store = new SqliteOrchestrationSessionStore(options);
        EmbeddedAgentCatalog catalog = new();
        RequestDecomposer decomposer = new(catalog);
        OrchestrationService orchestration = new(decomposer, catalog, store);
        var session = orchestration.CreateSession(OrchestrationRequest.FromStrings("Build a Blazor app"));
        var task = session.Plan.Tasks.Single();

        WorkerAgentTools.BlazorAgent(orchestration, session.SessionId, task.TaskId);
        var json = WorkerAgentTools.BlazorAgent(
            orchestration, session.SessionId, task.TaskId,
            failureKind: AgentFailureKind.ContextWindowLimit);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        await Assert.That(root.TryGetProperty("ok", out _)).IsFalse();
        await Assert.That(root.TryGetProperty("needsResume", out var needsResume)).IsTrue();
        await Assert.That(needsResume.GetBoolean()).IsTrue();

        var reloaded = orchestration.GetSession(session.SessionId)!;
        await Assert.That(reloaded.Plan.Tasks.Single().RecoveryState.LastFailureReason).IsEqualTo(nameof(AgentFailureKind.ContextWindowLimit));
    }

    [Test]
    public async Task BlazorAgent_Uses_Explicit_Failure_Reason_When_Provided()
    {
        var options = CreateOptions("worker-explicit-failure-reason");
        using var store = new SqliteOrchestrationSessionStore(options);
        EmbeddedAgentCatalog catalog = new();
        RequestDecomposer decomposer = new(catalog);
        OrchestrationService orchestration = new(decomposer, catalog, store);
        var session = orchestration.CreateSession(OrchestrationRequest.FromStrings("Build a Blazor app"));
        var task = session.Plan.Tasks.Single();

        _ = WorkerAgentTools.BlazorAgent(
            orchestration,
            session.SessionId,
            task.TaskId,
            failureKind: AgentFailureKind.ContextWindowLimit,
            failureReason: "Context burst boundary reached");

        var reloaded = orchestration.GetSession(session.SessionId)!;
        var updatedTask = reloaded.Plan.Tasks.Single();

        await Assert.That(updatedTask.RecoveryState.LastFailureKind).IsEqualTo(AgentFailureKind.ContextWindowLimit);
        await Assert.That(updatedTask.RecoveryState.LastFailureReason).IsEqualTo("Context burst boundary reached");
    }

    [Test]
    public async Task BlazorAgent_Checkpoint_Summary_Defaults_When_Whitespace_Summary()
    {
        var options = CreateOptions("worker-default-checkpoint-summary");
        using var store = new SqliteOrchestrationSessionStore(options);
        EmbeddedAgentCatalog catalog = new();
        RequestDecomposer decomposer = new(catalog);
        OrchestrationService orchestration = new(decomposer, catalog, store);
        var session = orchestration.CreateSession(OrchestrationRequest.FromStrings("Build a Blazor app"));
        var task = session.Plan.Tasks.Single();

        var json = WorkerAgentTools.BlazorAgent(orchestration, session.SessionId, task.TaskId, createCheckpoint: true, checkpointSummary: "   ");

        using var document = JsonDocument.Parse(json);
        var checkpoint = document.RootElement.GetProperty("checkpoints")[0];
        await Assert.That(checkpoint.GetProperty("summary").GetString()).IsEqualTo("Checkpoint recorded.");
    }

    [Test]
    public async Task BlazorAgent_Activates_With_NonNull_Empty_Collections_And_Whitespace_Summary()
    {
        var options = CreateOptions("worker-empty-collections-result");
        using var store = new SqliteOrchestrationSessionStore(options);
        EmbeddedAgentCatalog catalog = new();
        RequestDecomposer decomposer = new(catalog);
        OrchestrationService orchestration = new(decomposer, catalog, store);
        var session = orchestration.CreateSession(OrchestrationRequest.FromStrings("Build a Blazor app"));
        var task = session.Plan.Tasks.Single();

        var json = WorkerAgentTools.BlazorAgent(
            orchestration,
            session.SessionId,
            task.TaskId,
            workSummary: "   ",
            artifacts: [],
            handoffItems: [],
            risks: [],
            markComplete: false);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        await Assert.That(root.GetProperty("status").GetString()).IsEqualTo("InProgress");
        await Assert.That(root.GetProperty("shutdownRequired").GetBoolean()).IsFalse();
        await Assert.That(root.TryGetProperty("executionPrompt", out var executionPrompt)).IsTrue();
        await Assert.That(executionPrompt.GetString()).Contains("Assigned objective");
        await Assert.That(root.TryGetProperty("latestResult", out _)).IsFalse();
    }

    [Test]
    public async Task WorkerAgent_Invalid_Session_Returns_Safe_Error_Payload()
    {
        var options = CreateOptions("worker-invalid-session");
        using var store = new SqliteOrchestrationSessionStore(options);
        EmbeddedAgentCatalog catalog = new();
        RequestDecomposer decomposer = new(catalog);
        OrchestrationService orchestration = new(decomposer, catalog, store);

        var json = WorkerAgentTools.CSharpAgent(orchestration, "missing-session", "missing-task");

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        await Assert.That(root.GetProperty("ok").GetBoolean()).IsFalse();
        await Assert.That(root.GetProperty("operation").GetString()).IsEqualTo("multiagent_csharp_agent");
        await Assert.That(root.GetProperty("error").GetProperty("message").GetString()).Contains("Unknown orchestration session");
    }

    [Test]
    public async Task Each_Specialist_Agent_Tool_Routes_With_Correct_Operation_Name()
    {
        var options = CreateOptions("worker-operation-names");
        using var store = new SqliteOrchestrationSessionStore(options);
        EmbeddedAgentCatalog catalog = new();
        RequestDecomposer decomposer = new(catalog);
        OrchestrationService orchestration = new(decomposer, catalog, store);

        var agentToolCalls = new (string ExpectedOperation, Func<string> Call)[]
        {
            ("multiagent_architect_agent",   () => WorkerAgentTools.ArchitectAgent(orchestration, "x", "x")),
            ("multiagent_csharp_agent",      () => WorkerAgentTools.CSharpAgent(orchestration, "x", "x")),
            ("multiagent_reactive_agent",    () => WorkerAgentTools.ReactiveAgent(orchestration, "x", "x")),
            ("multiagent_reactiveui_agent",  () => WorkerAgentTools.ReactiveUiAgent(orchestration, "x", "x")),
            ("multiagent_mcp_agent",         () => WorkerAgentTools.McpAgent(orchestration, "x", "x")),
            ("multiagent_ci_agent",          () => WorkerAgentTools.CiAgent(orchestration, "x", "x")),
            ("multiagent_docs_agent",        () => WorkerAgentTools.DocsAgent(orchestration, "x", "x")),
            ("multiagent_migration_agent",   () => WorkerAgentTools.MigrationAgent(orchestration, "x", "x")),
            ("multiagent_wpf_agent",         () => WorkerAgentTools.WpfAgent(orchestration, "x", "x")),
            ("multiagent_winforms_agent",    () => WorkerAgentTools.WinFormsAgent(orchestration, "x", "x")),
            ("multiagent_avalonia_agent",    () => WorkerAgentTools.AvaloniaAgent(orchestration, "x", "x")),
            ("multiagent_maui_agent",        () => WorkerAgentTools.MauiAgent(orchestration, "x", "x")),
            ("multiagent_blazor_agent",      () => WorkerAgentTools.BlazorAgent(orchestration, "x", "x")),
            ("multiagent_test_agent",        () => WorkerAgentTools.TestAgent(orchestration, "x", "x")),
            ("multiagent_reviewer_agent",    () => WorkerAgentTools.ReviewerAgent(orchestration, "x", "x")),
        };

        foreach (var (expectedOperation, call) in agentToolCalls)
        {
            var json = call();
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            await Assert.That(root.GetProperty("ok").GetBoolean()).IsFalse();
            await Assert.That(root.GetProperty("operation").GetString()).IsEqualTo(expectedOperation);
        }
    }

    [Test]
    public async Task ListCatalog_Tool_Returns_All_Agents()
    {
        EmbeddedAgentCatalog catalog = new();

        var json = AgentCatalogTools.ListCatalog(catalog);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        await Assert.That(root.TryGetProperty("count", out var count)).IsTrue();
        await Assert.That(count.GetInt32()).IsGreaterThanOrEqualTo(15);
        await Assert.That(root.TryGetProperty("agents", out _)).IsTrue();
    }

    [Test]
    public async Task SearchCatalog_Tool_With_Query_Returns_Filtered_Results_Containing_Match()
    {
        EmbeddedAgentCatalog catalog = new();

        var json = AgentCatalogTools.SearchCatalog(catalog, query: "avalonia");

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        await Assert.That(root.GetProperty("count").GetInt32()).IsGreaterThanOrEqualTo(1);
        var agents = root.GetProperty("agents");
        var hasAvalonia = false;
        foreach (var agent in agents.EnumerateArray())
        {
            if (agent.TryGetProperty("id", out var id) && id.GetString()?.Contains("avalonia", StringComparison.OrdinalIgnoreCase) == true)
            {
                hasAvalonia = true;
                break;
            }
        }
        await Assert.That(hasAvalonia).IsTrue();
    }

    [Test]
    public async Task SearchCatalog_Tool_Without_Query_Returns_All_Agents()
    {
        EmbeddedAgentCatalog catalog = new();

        var json = AgentCatalogTools.SearchCatalog(catalog, query: null);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        await Assert.That(root.GetProperty("count").GetInt32()).IsGreaterThanOrEqualTo(15);
    }

    [Test]
    public async Task GetAgent_Tool_Returns_Profile_For_Valid_Id()
    {
        EmbeddedAgentCatalog catalog = new();

        var json = AgentCatalogTools.GetAgent(catalog, "reactiveui");

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        await Assert.That(root.TryGetProperty("id", out var id)).IsTrue();
        await Assert.That(id.GetString()).IsEqualTo("reactiveui");
    }

    [Test]
    public async Task GetAgent_Tool_Returns_Safe_Error_For_Unknown_Id()
    {
        EmbeddedAgentCatalog catalog = new();

        var json = AgentCatalogTools.GetAgent(catalog, "nonexistent-agent-xyz");

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        await Assert.That(root.GetProperty("ok").GetBoolean()).IsFalse();
        await Assert.That(root.GetProperty("operation").GetString()).IsEqualTo("multiagent_agent_catalog_get");
        await Assert.That(root.GetProperty("error").GetProperty("message").GetString()).Contains("nonexistent-agent-xyz");
    }

    private static ReactiveMultiAgentOptions CreateOptions(string folderPrefix)
        => new()
        {
            StateRootPath = Path.Combine(Path.GetTempPath(), folderPrefix, Guid.NewGuid().ToString("N")),
        };
}
