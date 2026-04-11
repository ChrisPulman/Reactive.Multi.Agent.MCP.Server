using Reactive.Multi.Agent.MCP.Core.Abstractions;
using Reactive.Multi.Agent.MCP.Core.Configuration;
using Reactive.Multi.Agent.MCP.Core.Models;
using Reactive.Multi.Agent.MCP.Core.Persistence;
using Reactive.Multi.Agent.MCP.Core.Services;
using Reactive.Multi.Agent.MCP.Knowledge.Services;
using Reactive.Multi.Agent.MCP.Server;
using Reactive.Multi.Agent.MCP.Server.Prompts;
using Reactive.Multi.Agent.MCP.Server.Resources;
using Reactive.Multi.Agent.MCP.Server.Tools;
using System.Text.Json;

namespace Reactive.Multi.Agent.MCP.Tests;

public class CopilotCrashHardeningTests
{
    [Test]
    public async Task CreateOptions_Uses_LocalApplicationData_Default_And_Allows_Environment_Overrides()
    {
        var originalStateRoot = Environment.GetEnvironmentVariable("REACTIVE_MULTI_AGENT_MCP_STATE_ROOT");
        var originalPackageId = Environment.GetEnvironmentVariable("REACTIVE_MULTI_AGENT_MCP_PACKAGE_ID");
        var originalServerId = Environment.GetEnvironmentVariable("REACTIVE_MULTI_AGENT_MCP_SERVER_ID");

        try
        {
            Environment.SetEnvironmentVariable("REACTIVE_MULTI_AGENT_MCP_STATE_ROOT", null);
            Environment.SetEnvironmentVariable("REACTIVE_MULTI_AGENT_MCP_PACKAGE_ID", null);
            Environment.SetEnvironmentVariable("REACTIVE_MULTI_AGENT_MCP_SERVER_ID", null);

            var defaults = Program.CreateOptions();
            var expectedDefaultRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ReactiveMultiAgentMcp");

            await Assert.That(defaults.StateRootPath).IsEqualTo(expectedDefaultRoot);
            await Assert.That(defaults.PackageId).IsEqualTo("CP.Reactive.Multi.Agent.MCP.Server");
            await Assert.That(defaults.ServerId).IsEqualTo("io.github.chrispulman/reactive-multi-agent-mcp-server");

            var overrideRoot = Path.Combine(Path.GetTempPath(), "reactive-multi-agent-mcp-env", Guid.NewGuid().ToString("N"));
            Environment.SetEnvironmentVariable("REACTIVE_MULTI_AGENT_MCP_STATE_ROOT", overrideRoot);
            Environment.SetEnvironmentVariable("REACTIVE_MULTI_AGENT_MCP_PACKAGE_ID", "Custom.Package");
            Environment.SetEnvironmentVariable("REACTIVE_MULTI_AGENT_MCP_SERVER_ID", "custom/server");

            var overridden = Program.CreateOptions();
            await Assert.That(overridden.StateRootPath).IsEqualTo(overrideRoot);
            await Assert.That(overridden.PackageId).IsEqualTo("Custom.Package");
            await Assert.That(overridden.ServerId).IsEqualTo("custom/server");
        }
        finally
        {
            Environment.SetEnvironmentVariable("REACTIVE_MULTI_AGENT_MCP_STATE_ROOT", originalStateRoot);
            Environment.SetEnvironmentVariable("REACTIVE_MULTI_AGENT_MCP_PACKAGE_ID", originalPackageId);
            Environment.SetEnvironmentVariable("REACTIVE_MULTI_AGENT_MCP_SERVER_ID", originalServerId);
        }
    }

    [Test]
    public async Task Invalid_Tool_And_Resource_Inputs_Return_Safe_Error_Payloads_Instead_Of_Throwing()
    {
        var options = CreateOptions("reactive-multi-agent-mcp-safe-error-tests");
        using var store = new SqliteOrchestrationSessionStore(options);
        IAgentCatalog catalog = new EmbeddedAgentCatalog();
        IRequestDecomposer decomposer = new RequestDecomposer(catalog);
        IOrchestrationService orchestration = new OrchestrationService(decomposer, catalog, store);

        var toolJson = OrchestratorTools.SessionStatus(orchestration, "missing-session");
        using var toolDocument = JsonDocument.Parse(toolJson);
        var toolRoot = toolDocument.RootElement;
        await Assert.That(toolRoot.GetProperty("ok").GetBoolean()).IsFalse();
        await Assert.That(toolRoot.GetProperty("operation").GetString()).IsEqualTo("multiagent_session_status");
        await Assert.That(toolRoot.GetProperty("error").GetProperty("message").GetString()).Contains("Unknown orchestration session");

        var resourceJson = OrchestrationResources.GetSession(orchestration, "missing-session");
        using var resourceDocument = JsonDocument.Parse(resourceJson);
        var resourceRoot = resourceDocument.RootElement;
        await Assert.That(resourceRoot.GetProperty("ok").GetBoolean()).IsFalse();
        await Assert.That(resourceRoot.GetProperty("operation").GetString()).IsEqualTo("resource:multiagent://session/{sessionId}");
        await Assert.That(resourceRoot.GetProperty("error").GetProperty("message").GetString()).Contains("Unknown orchestration session");
    }

    [Test]
    public async Task Prompt_Failures_Return_Fallback_Text_Instead_Of_Throwing()
    {
        var options = CreateOptions("reactive-multi-agent-mcp-safe-prompt-tests");
        using var store = new SqliteOrchestrationSessionStore(options);
        IAgentCatalog catalog = new EmbeddedAgentCatalog();
        IRequestDecomposer decomposer = new RequestDecomposer(catalog);
        IOrchestrationService orchestration = new OrchestrationService(decomposer, catalog, store);

        var prompt = OrchestrationPrompts.CreateSpecialistAgentPrompt(orchestration, "missing-session", "missing-task", "csharp");

        await Assert.That(prompt).Contains("safe prompt fallback");
        await Assert.That(prompt).Contains("create_specialist_agent_prompt");
        await Assert.That(prompt).Contains("Unknown orchestration session");
    }

    [Test]
    public async Task Session_Resource_Returns_Compacted_Payload_For_Copilot_Clients()
    {
        var options = CreateOptions("reactive-multi-agent-mcp-compacted-resource-tests");
        using var store = new SqliteOrchestrationSessionStore(options);
        IAgentCatalog catalog = new EmbeddedAgentCatalog();
        IRequestDecomposer decomposer = new RequestDecomposer(catalog);
        IOrchestrationService orchestration = new OrchestrationService(decomposer, catalog, store);

        var session = orchestration.CreateSession(OrchestrationRequest.FromStrings(
            "Build a Blazor app with CI, docs, tests, and an MCP integration layer",
            constraints: "net10 only,no external services",
            desiredArtifacts: "source files,tests,docs",
            preferredAgents: "csharp,ci,docs,mcp",
            maxParallelAgents: 4));

        var json = OrchestrationResources.GetSession(orchestration, session.SessionId);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        await Assert.That(root.GetProperty("sessionId").GetString()).IsEqualTo(session.SessionId);
        await Assert.That(root.TryGetProperty("session", out _)).IsFalse();
        await Assert.That(root.TryGetProperty("summary", out var summary)).IsTrue();
        await Assert.That(summary.TryGetProperty("status", out _)).IsTrue();
        await Assert.That(root.TryGetProperty("executionLedgerTail", out _)).IsTrue();
        await Assert.That(json.Length).IsLessThan(20000);
    }

    private static ReactiveMultiAgentOptions CreateOptions(string folderPrefix)
        => new()
        {
            StateRootPath = Path.Combine(Path.GetTempPath(), folderPrefix, Guid.NewGuid().ToString("N")),
        };
}
