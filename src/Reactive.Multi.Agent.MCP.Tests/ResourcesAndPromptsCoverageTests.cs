using Reactive.Multi.Agent.MCP.Core.Abstractions;
using Reactive.Multi.Agent.MCP.Core.Configuration;
using Reactive.Multi.Agent.MCP.Core.Models;
using Reactive.Multi.Agent.MCP.Core.Persistence;
using Reactive.Multi.Agent.MCP.Core.Services;
using Reactive.Multi.Agent.MCP.Knowledge.Services;
using Reactive.Multi.Agent.MCP.Server.Prompts;
using Reactive.Multi.Agent.MCP.Server.Resources;
using System.Text.Json;

namespace Reactive.Multi.Agent.MCP.Tests;

public class ResourcesAndPromptsCoverageTests
{
    [Test]
    public async Task GetCatalog_Resource_Returns_Count_And_Agents_Array()
    {
        EmbeddedAgentCatalog catalog = new();

        var json = OrchestrationResources.GetCatalog(catalog);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        await Assert.That(root.TryGetProperty("ok", out _)).IsFalse();
        await Assert.That(root.TryGetProperty("count", out var count)).IsTrue();
        await Assert.That(count.GetInt32()).IsGreaterThanOrEqualTo(15);
        await Assert.That(root.TryGetProperty("agents", out _)).IsTrue();
    }

    [Test]
    public async Task GetRecentHistory_Resource_Returns_List_Containing_Created_Session()
    {
        var (orchestration, session) = CreateSession("resource-history");

        var json = OrchestrationResources.GetRecentHistory(orchestration);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        await Assert.That(root.ValueKind).IsEqualTo(JsonValueKind.Array);
        var found = false;
        foreach (var entry in root.EnumerateArray())
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
    public async Task GetArchitecture_Resource_Returns_Hub_And_Spoke_Model()
    {
        var json = OrchestrationResources.GetArchitecture();

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        await Assert.That(root.TryGetProperty("ok", out _)).IsFalse();
        await Assert.That(root.TryGetProperty("model", out var model)).IsTrue();
        await Assert.That(model.GetString()).IsEqualTo("hub-and-spoke");
        await Assert.That(root.TryGetProperty("controlPlane", out _)).IsTrue();
    }

    [Test]
    public async Task GetArtifactSchema_Resource_Returns_Example_Artifacts_And_Handoffs()
    {
        var json = OrchestrationResources.GetArtifactSchema();

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        await Assert.That(root.TryGetProperty("ok", out _)).IsFalse();
        await Assert.That(root.TryGetProperty("artifacts", out var artifacts)).IsTrue();
        await Assert.That(artifacts.GetArrayLength()).IsGreaterThanOrEqualTo(1);
        await Assert.That(root.TryGetProperty("handoffItems", out var handoffs)).IsTrue();
        await Assert.That(handoffs.GetArrayLength()).IsGreaterThanOrEqualTo(1);
    }

    [Test]
    public async Task CreateMultiAgentPlan_Prompt_Returns_Phase_Guide()
    {
        EmbeddedAgentCatalog catalog = new();
        RequestDecomposer decomposer = new(catalog);

        var prompt = OrchestrationPrompts.CreateMultiAgentPlan(
            decomposer,
            userRequest: "Build a Blazor app with CI and docs",
            constraints: "net10 only");

        await Assert.That(prompt).Contains("multiagent_orchestrate_request");
        await Assert.That(prompt).Contains("multiagent_create_session");
        await Assert.That(prompt).Contains("GPT-5.5");
        await Assert.That(prompt).Contains("Phase");
    }

    [Test]
    public async Task MergeMultiAgentResults_Prompt_Returns_Synthesis_Prompt()
    {
        var (orchestration, session) = CreateSession("prompt-merge");

        var prompt = OrchestrationPrompts.MergeMultiAgentResults(orchestration, session.SessionId);

        await Assert.That(prompt).Contains("Status:");
        await Assert.That(prompt).Contains("Merge");
    }

    [Test]
    public async Task CreateMultiAgentPlan_Prompt_Failure_Returns_Safe_Fallback_Text()
    {
        var prompt = OrchestrationPrompts.CreateMultiAgentPlan(null!, userRequest: "anything");

        await Assert.That(prompt).Contains("safe prompt fallback");
        await Assert.That(prompt).Contains("create_multi_agent_plan");
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
