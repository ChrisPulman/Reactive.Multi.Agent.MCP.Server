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
        IAgentCatalog catalog = new EmbeddedAgentCatalog();
        IRequestDecomposer decomposer = new RequestDecomposer(catalog);
        IOrchestrationService orchestration = new OrchestrationService(decomposer, catalog, store);
        var session = orchestration.CreateSession(OrchestrationRequest.FromStrings("Build a Blazor app"));
        var task = session.Plan.Tasks.Single();

        var json = WorkerAgentTools.BlazorAgent(orchestration, session.SessionId, task.TaskId);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        await Assert.That(root.TryGetProperty("taskId", out var taskId)).IsTrue();
        await Assert.That(taskId.GetString()).IsEqualTo(task.TaskId);
        await Assert.That(root.TryGetProperty("status", out var status)).IsTrue();
        await Assert.That(status.GetString()).IsEqualTo("InProgress");
        await Assert.That(root.TryGetProperty("executionPrompt", out var prompt)).IsTrue();
        await Assert.That(string.IsNullOrWhiteSpace(prompt.GetString())).IsFalse();
    }

    [Test]
    public async Task BlazorAgent_Submit_Results_Marks_Task_Completed()
    {
        var options = CreateOptions("worker-submit");
        using var store = new SqliteOrchestrationSessionStore(options);
        IAgentCatalog catalog = new EmbeddedAgentCatalog();
        IRequestDecomposer decomposer = new RequestDecomposer(catalog);
        IOrchestrationService orchestration = new OrchestrationService(decomposer, catalog, store);
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
    }

    [Test]
    public async Task BlazorAgent_Create_Checkpoint_Returns_Checkpoints_In_Packet()
    {
        var options = CreateOptions("worker-checkpoint");
        using var store = new SqliteOrchestrationSessionStore(options);
        IAgentCatalog catalog = new EmbeddedAgentCatalog();
        IRequestDecomposer decomposer = new RequestDecomposer(catalog);
        IOrchestrationService orchestration = new OrchestrationService(decomposer, catalog, store);
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
        IAgentCatalog catalog = new EmbeddedAgentCatalog();
        IRequestDecomposer decomposer = new RequestDecomposer(catalog);
        IOrchestrationService orchestration = new OrchestrationService(decomposer, catalog, store);
        var session = orchestration.CreateSession(OrchestrationRequest.FromStrings("Build a Blazor app"));
        var task = session.Plan.Tasks.Single();

        WorkerAgentTools.BlazorAgent(orchestration, session.SessionId, task.TaskId);
        var json = WorkerAgentTools.BlazorAgent(
            orchestration, session.SessionId, task.TaskId,
            failureKind: AgentFailureKind.ContextWindowLimit, failureReason: "Context too large.");

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        await Assert.That(root.TryGetProperty("ok", out _)).IsFalse();
        await Assert.That(root.TryGetProperty("needsResume", out var needsResume)).IsTrue();
        await Assert.That(needsResume.GetBoolean()).IsTrue();
    }

    [Test]
    public async Task WorkerAgent_Invalid_Session_Returns_Safe_Error_Payload()
    {
        var options = CreateOptions("worker-invalid-session");
        using var store = new SqliteOrchestrationSessionStore(options);
        IAgentCatalog catalog = new EmbeddedAgentCatalog();
        IRequestDecomposer decomposer = new RequestDecomposer(catalog);
        IOrchestrationService orchestration = new OrchestrationService(decomposer, catalog, store);

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
        IAgentCatalog catalog = new EmbeddedAgentCatalog();
        IRequestDecomposer decomposer = new RequestDecomposer(catalog);
        IOrchestrationService orchestration = new OrchestrationService(decomposer, catalog, store);

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
            ("multiagent_tester_agent",      () => WorkerAgentTools.TestAgent(orchestration, "x", "x")),
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
        IAgentCatalog catalog = new EmbeddedAgentCatalog();

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
        IAgentCatalog catalog = new EmbeddedAgentCatalog();

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
        IAgentCatalog catalog = new EmbeddedAgentCatalog();

        var json = AgentCatalogTools.SearchCatalog(catalog, query: null);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        await Assert.That(root.GetProperty("count").GetInt32()).IsGreaterThanOrEqualTo(15);
    }

    [Test]
    public async Task GetAgent_Tool_Returns_Profile_For_Valid_Id()
    {
        IAgentCatalog catalog = new EmbeddedAgentCatalog();

        var json = AgentCatalogTools.GetAgent(catalog, "reactiveui");

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        await Assert.That(root.TryGetProperty("id", out var id)).IsTrue();
        await Assert.That(id.GetString()).IsEqualTo("reactiveui");
    }

    [Test]
    public async Task GetAgent_Tool_Returns_Safe_Error_For_Unknown_Id()
    {
        IAgentCatalog catalog = new EmbeddedAgentCatalog();

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
