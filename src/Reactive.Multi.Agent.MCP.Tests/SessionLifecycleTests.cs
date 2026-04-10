using Reactive.Multi.Agent.MCP.Core.Abstractions;
using Reactive.Multi.Agent.MCP.Core.Models;
using Reactive.Multi.Agent.MCP.Core.Persistence;
using Reactive.Multi.Agent.MCP.Core.Services;
using Reactive.Multi.Agent.MCP.Core.Configuration;
using Reactive.Multi.Agent.MCP.Knowledge.Services;

namespace Reactive.Multi.Agent.MCP.Tests;

public class SessionLifecycleTests
{
    [Test]
    public async Task ActivateAgentTask_Returns_Isolated_Prompt_And_Sets_InProgress_When_Ready()
    {
        var options = CreateOptions();
        AgentTaskPacket packet;

        using (var store = new SqliteOrchestrationSessionStore(options))
        {
            IAgentCatalog catalog = new EmbeddedAgentCatalog();
            IRequestDecomposer decomposer = new RequestDecomposer(catalog);
            IOrchestrationService orchestration = new OrchestrationService(decomposer, catalog, store);

            var session = orchestration.CreateSession(OrchestrationRequest.FromStrings("Build a Blazor app"));
            var task = session.Plan.Tasks.Single();
            packet = orchestration.ActivateAgentTask(session.SessionId, task.TaskId, task.AgentId, "Target .NET 10", "Started scaffolding");
        }

        await Assert.That(packet.Status).IsEqualTo(AgentTaskStatus.InProgress);
        await Assert.That(packet.ExecutionPrompt).Contains("Top-level request: Build a Blazor app");
        await Assert.That(packet.Scratchpad).Contains("Target .NET 10");
        Cleanup(options);
    }

    [Test]
    public async Task FinalizeSession_Merges_Structured_Agent_Output()
    {
        var options = CreateOptions();
        OrchestrationSummary summary;

        using (var store = new SqliteOrchestrationSessionStore(options))
        {
            IAgentCatalog catalog = new EmbeddedAgentCatalog();
            IRequestDecomposer decomposer = new RequestDecomposer(catalog);
            IOrchestrationService orchestration = new OrchestrationService(decomposer, catalog, store);

            var session = orchestration.CreateSession(OrchestrationRequest.FromStrings("Write documentation guide"));
            var task = session.Plan.Tasks.Single();

            orchestration.ActivateAgentTask(session.SessionId, task.TaskId, task.AgentId);
            orchestration.RecordAgentResult(
                session.SessionId,
                task.TaskId,
                task.AgentId,
                workSummary: "Created an onboarding guide.",
                artifacts:
                [
                    new AgentArtifact
                    {
                        ArtifactId = "artifact-readme",
                        Kind = ArtifactKind.Documentation,
                        Title = "README.md",
                        Summary = "Top-level onboarding guide",
                        FilePath = "README.md",
                    },
                ],
                handoffItems:
                [
                    new HandoffItem
                    {
                        ItemId = "handoff-screenshots",
                        Category = "follow-up",
                        Title = "Add screenshots later",
                        Details = "Screenshots were deferred until UI stabilizes.",
                        IsBlocking = false,
                    },
                ],
                risks: ["Screenshots pending"],
                markComplete: true);

            summary = orchestration.FinalizeSession(session.SessionId);
        }

        await Assert.That(summary.Status).IsEqualTo("Completed");
        await Assert.That(summary.CompletedTasks).IsEqualTo(1);
        await Assert.That(summary.UnifiedResponse).Contains("README.md");
        await Assert.That(summary.UnifiedResponse).Contains("Add screenshots later");
        Cleanup(options);
    }

    private static ReactiveMultiAgentOptions CreateOptions()
        => new()
        {
            StateRootPath = Path.Combine(Path.GetTempPath(), "reactive-multi-agent-mcp-tests", Guid.NewGuid().ToString("N")),
        };

    private static void Cleanup(ReactiveMultiAgentOptions options)
    {
        _ = options;
    }
}
