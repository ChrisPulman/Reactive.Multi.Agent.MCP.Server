using Reactive.Multi.Agent.MCP.Core.Abstractions;
using Reactive.Multi.Agent.MCP.Core.Configuration;
using Reactive.Multi.Agent.MCP.Core.Models;
using Reactive.Multi.Agent.MCP.Core.Persistence;
using Reactive.Multi.Agent.MCP.Core.Services;
using Reactive.Multi.Agent.MCP.Knowledge.Services;

namespace Reactive.Multi.Agent.MCP.Tests;

public class ContinuityTests
{
    [Test]
    public async Task ReportTaskFailure_Marks_Task_As_ResumeRequired_And_Persists_Reload_Items()
    {
        var options = new ReactiveMultiAgentOptions
        {
            StateRootPath = Path.Combine(Path.GetTempPath(), "reactive-multi-agent-mcp-tests", Guid.NewGuid().ToString("N")),
        };

        using var store = new SqliteOrchestrationSessionStore(options);
        EmbeddedAgentCatalog catalog = new();
        RequestDecomposer decomposer = new(catalog);
        OrchestrationService orchestration = new(decomposer, catalog, store);

        var session = orchestration.CreateSession(OrchestrationRequest.FromStrings("Build a Blazor app"));
        var task = session.Plan.Tasks.Single();

        var packet = orchestration.ReportTaskFailure(
            session.SessionId,
            task.TaskId,
            task.AgentId,
            AgentFailureKind.ContextWindowLimit,
            "Context window nearly full.",
            ["Reload objective", "Reload acceptance criteria"],
            currentEstimatedTokens: 12000,
            remainingSubscriptionTokens: 1500);

        await Assert.That(packet.NeedsResume).IsTrue();
        await Assert.That(packet.RecoveryState.LastFailureKind).IsEqualTo(AgentFailureKind.ContextWindowLimit);
        await Assert.That(packet.ResumeMemoryReloadItems.Count).IsGreaterThanOrEqualTo(2);
        await Assert.That(packet.ContextWindowBudget.CurrentEstimatedTokens).IsEqualTo(12000);
        await Assert.That(packet.ContextWindowBudget.HardLimitReached).IsTrue();
        await Assert.That(packet.SubscriptionTokenBudget.LowBudgetWarning).IsTrue();
    }

    [Test]
    public async Task ResumeTask_Clears_ResumeRequired_State_While_Keeping_Checkpoints()
    {
        var options = new ReactiveMultiAgentOptions
        {
            StateRootPath = Path.Combine(Path.GetTempPath(), "reactive-multi-agent-mcp-tests", Guid.NewGuid().ToString("N")),
        };

        using var store = new SqliteOrchestrationSessionStore(options);
        EmbeddedAgentCatalog catalog = new();
        RequestDecomposer decomposer = new(catalog);
        OrchestrationService orchestration = new(decomposer, catalog, store);

        var session = orchestration.CreateSession(OrchestrationRequest.FromStrings("Build a Blazor app"));
        var task = session.Plan.Tasks.Single();

        _ = orchestration.ReportTaskFailure(
            session.SessionId,
            task.TaskId,
            task.AgentId,
            AgentFailureKind.NetworkLoss,
            "Network connection dropped.",
            ["Reload network recovery context"]);

        var afterFailure = orchestration.GetAgentTaskPacket(session.SessionId, task.TaskId, task.AgentId);
        var resumed = orchestration.ResumeTask(session.SessionId, task.TaskId, task.AgentId);

        await Assert.That(afterFailure.Checkpoints.Count).IsGreaterThanOrEqualTo(1);
        await Assert.That(resumed.NeedsResume).IsFalse();
        await Assert.That(resumed.ResumeMemoryReloadItems.Count).IsGreaterThanOrEqualTo(1);
        await Assert.That(resumed.Status).IsEqualTo(AgentTaskStatus.InProgress);
    }

    [Test]
    public async Task AutomaticPolicy_Recommends_Checkpoint_When_Context_Window_Is_Near_Limit()
    {
        var options = new ReactiveMultiAgentOptions
        {
            StateRootPath = Path.Combine(Path.GetTempPath(), "reactive-multi-agent-mcp-tests", Guid.NewGuid().ToString("N")),
        };

        using var store = new SqliteOrchestrationSessionStore(options);
        EmbeddedAgentCatalog catalog = new();
        RequestDecomposer decomposer = new(catalog);
        OrchestrationService orchestration = new(decomposer, catalog, store);

        var session = orchestration.CreateSession(OrchestrationRequest.FromStrings("Build a Blazor app"));
        var task = session.Plan.Tasks.Single();

        var packet = orchestration.ApplyAutomaticPolicy(session.SessionId, task.TaskId, task.AgentId, currentEstimatedTokens: 9500);

        await Assert.That(packet.RecoveryState.PolicyState.AutoCheckpointRecommended).IsTrue();
        await Assert.That(packet.Checkpoints.Count).IsGreaterThanOrEqualTo(1);
    }

    [Test]
    public async Task AutomaticPolicy_Recommends_Retry_After_Network_Recovery_When_Retry_Budget_Remains()
    {
        var options = new ReactiveMultiAgentOptions
        {
            StateRootPath = Path.Combine(Path.GetTempPath(), "reactive-multi-agent-mcp-tests", Guid.NewGuid().ToString("N")),
        };

        using var store = new SqliteOrchestrationSessionStore(options);
        EmbeddedAgentCatalog catalog = new();
        RequestDecomposer decomposer = new(catalog);
        OrchestrationService orchestration = new(decomposer, catalog, store);

        var session = orchestration.CreateSession(OrchestrationRequest.FromStrings("Build a Blazor app"));
        var task = session.Plan.Tasks.Single();

        _ = orchestration.ReportTaskFailure(session.SessionId, task.TaskId, task.AgentId, AgentFailureKind.NetworkLoss, "Network dropped.");
        var packet = orchestration.ApplyAutomaticPolicy(session.SessionId, task.TaskId, task.AgentId, networkRecovered: true);

        await Assert.That(packet.RecoveryState.PolicyState.AutoRetryRecommended).IsTrue();
        await Assert.That(packet.RecoveryState.PolicyState.RetryAttemptsUsed).IsEqualTo(1);
    }
}
