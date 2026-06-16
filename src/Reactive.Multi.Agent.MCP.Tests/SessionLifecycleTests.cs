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
    public async Task CreateSession_Assigns_Meaningful_Agent_Name_And_Session_Id()
    {
        var options = CreateOptions();
        AgentWorkItem task;
        string sessionId;

        using (var store = new SqliteOrchestrationSessionStore(options))
        {
            EmbeddedAgentCatalog catalog = new();
            RequestDecomposer decomposer = new(catalog);
            OrchestrationService orchestration = new(decomposer, catalog, store);

            var session = orchestration.CreateSession(OrchestrationRequest.FromStrings("Build a Blazor app"));
            task = session.Plan.Tasks.Single();
            sessionId = session.SessionId;
        }

        await Assert.That(task.AgentName).IsEqualTo("Blazor Agent - task-1");
        await Assert.That(task.AgentSessionId.StartsWith("blazor-agent-task-1-", StringComparison.Ordinal)).IsTrue();
        await Assert.That(task.AgentSessionId.Contains(sessionId[..8], StringComparison.Ordinal)).IsTrue();
        await Assert.That(task.ShutdownRequired).IsFalse();
        Cleanup(options);
    }

    [Test]
    public async Task ActivateAgentTask_Returns_Isolated_Prompt_And_Sets_InProgress_When_Ready()
    {
        var options = CreateOptions();
        AgentTaskPacket packet;

        using (var store = new SqliteOrchestrationSessionStore(options))
        {
            EmbeddedAgentCatalog catalog = new();
            RequestDecomposer decomposer = new(catalog);
            OrchestrationService orchestration = new(decomposer, catalog, store);

            var session = orchestration.CreateSession(OrchestrationRequest.FromStrings("Build a Blazor app"));
            var task = session.Plan.Tasks.Single();
            packet = orchestration.ActivateAgentTask(session.SessionId, task.TaskId, task.AgentId, "Target .NET 10", "Started scaffolding");
        }

        await Assert.That(packet.Status).IsEqualTo(AgentTaskStatus.InProgress);
        await Assert.That(packet.AgentName).IsEqualTo("Blazor Agent - task-1");
        await Assert.That(packet.ExecutionPrompt).Contains("Top-level request: Build a Blazor app");
        await Assert.That(packet.Scratchpad).Contains("Target .NET 10");
        await Assert.That(packet.ShutdownRequired).IsFalse();
        Cleanup(options);
    }

    [Test]
    public async Task RecordAgentResult_When_Complete_Returns_Shutdown_Required_Packet()
    {
        var options = CreateOptions();
        AgentTaskPacket packet;

        using (var store = new SqliteOrchestrationSessionStore(options))
        {
            EmbeddedAgentCatalog catalog = new();
            RequestDecomposer decomposer = new(catalog);
            OrchestrationService orchestration = new(decomposer, catalog, store);

            var session = orchestration.CreateSession(OrchestrationRequest.FromStrings("Build a Blazor app"));
            var task = session.Plan.Tasks.Single();

            orchestration.ActivateAgentTask(session.SessionId, task.TaskId, task.AgentId);
            packet = orchestration.RecordAgentResult(
                session.SessionId,
                task.TaskId,
                task.AgentId,
                workSummary: "Blazor app shell complete.",
                markComplete: true);
        }

        await Assert.That(packet.Status).IsEqualTo(AgentTaskStatus.Completed);
        await Assert.That(packet.ShutdownRequired).IsTrue();
        await Assert.That(packet.CompletedAtUtc.HasValue).IsTrue();
        await Assert.That(packet.LifecycleInstruction).Contains("Close sub-agent");
        await Assert.That(packet.ExecutionPrompt).Contains("must be shut down now");
        await Assert.That(packet.NextSteps.Any(step => step.Contains("Shut down", StringComparison.OrdinalIgnoreCase))).IsTrue();
        await Assert.That(packet.LatestResult?.ShutdownRequired).IsTrue();
        Cleanup(options);
    }

    [Test]
    public async Task FinalizeSession_Merges_Structured_Agent_Output()
    {
        var options = CreateOptions();
        OrchestrationSummary summary;

        using (var store = new SqliteOrchestrationSessionStore(options))
        {
            EmbeddedAgentCatalog catalog = new();
            RequestDecomposer decomposer = new(catalog);
            OrchestrationService orchestration = new(decomposer, catalog, store);

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
