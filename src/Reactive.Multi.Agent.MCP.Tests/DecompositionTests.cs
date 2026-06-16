using Reactive.Multi.Agent.MCP.Core.Abstractions;
using Reactive.Multi.Agent.MCP.Core.Models;
using Reactive.Multi.Agent.MCP.Core.Services;
using Reactive.Multi.Agent.MCP.Knowledge.Services;

namespace Reactive.Multi.Agent.MCP.Tests;

public class DecompositionTests
{
    [Test]
    public async Task Decomposer_Assigns_Blazor_Ci_And_Migration_For_Mixed_Request()
    {
        EmbeddedAgentCatalog catalog = new();
        RequestDecomposer decomposer = new(catalog);

        var plan = decomposer.CreatePlan(OrchestrationRequest.FromStrings(
            "Build a Blazor app, generate a CI pipeline, and produce a migration plan."));

        await Assert.That(plan.Tasks.Count).IsEqualTo(3);
        await Assert.That(plan.Tasks.Any(task => task.AgentId == "blazor")).IsTrue();
        await Assert.That(plan.Tasks.Any(task => task.AgentId == "ci")).IsTrue();
        await Assert.That(plan.Tasks.Any(task => task.AgentId == "migration")).IsTrue();
    }
}
