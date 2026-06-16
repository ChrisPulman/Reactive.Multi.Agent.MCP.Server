using Reactive.Multi.Agent.MCP.Core.Abstractions;
using Reactive.Multi.Agent.MCP.Core.Models;
using Reactive.Multi.Agent.MCP.Core.Services;
using Reactive.Multi.Agent.MCP.Knowledge.Services;

namespace Reactive.Multi.Agent.MCP.Tests;

public class DependencyGraphTests
{
    [Test]
    public async Task Plan_Produces_Dependency_Aware_Execution_Waves()
    {
        EmbeddedAgentCatalog catalog = new();
        RequestDecomposer decomposer = new(catalog);

        var plan = decomposer.CreatePlan(OrchestrationRequest.FromStrings(
            "Create a migration plan, build a Blazor app, generate a CI pipeline, write docs, and run a review."));

        var migration = plan.Tasks.Single(task => task.AgentId == "migration");
        var blazor = plan.Tasks.Single(task => task.AgentId == "blazor");
        var ci = plan.Tasks.Single(task => task.AgentId == "ci");
        var docs = plan.Tasks.Single(task => task.AgentId == "docs");
        var reviewer = plan.Tasks.Single(task => task.AgentId == "reviewer");

        await Assert.That(blazor.PhaseOrder).IsGreaterThan(migration.PhaseOrder);
        await Assert.That(blazor.Dependencies.Any(dependency => dependency.TaskId == migration.TaskId)).IsTrue();
        await Assert.That(ci.Dependencies.Any(dependency => dependency.TaskId == blazor.TaskId)).IsTrue();
        await Assert.That(docs.Dependencies.Any(dependency => dependency.TaskId == ci.TaskId)).IsTrue();
        await Assert.That(reviewer.Dependencies.Any(dependency => dependency.TaskId == docs.TaskId)).IsTrue();
    }
}
