using Reactive.Multi.Agent.MCP.Core.Abstractions;
using Reactive.Multi.Agent.MCP.Core.Configuration;
using Reactive.Multi.Agent.MCP.Core.Models;
using Reactive.Multi.Agent.MCP.Core.Persistence;
using Reactive.Multi.Agent.MCP.Core.Services;
using Reactive.Multi.Agent.MCP.Knowledge.Services;
using Reactive.Multi.Agent.MCP.Server.Tools;
using System.Text.Json;

namespace Reactive.Multi.Agent.MCP.Tests;

public class SupervisorAndHistoryTests
{
    [Test]
    public async Task SearchSessions_Returns_Persisted_History_Entries()
    {
        var options = new ReactiveMultiAgentOptions { StateRootPath = Path.Combine(Path.GetTempPath(), "reactive-multi-agent-mcp-history-tests", Guid.NewGuid().ToString("N")) };
        using var store = new SqliteOrchestrationSessionStore(options);
        IAgentCatalog catalog = new EmbeddedAgentCatalog();
        IRequestDecomposer decomposer = new RequestDecomposer(catalog);
        IOrchestrationService orchestration = new OrchestrationService(decomposer, catalog, store);
        var session = orchestration.CreateSession(OrchestrationRequest.FromStrings("Build a Blazor app with CI pipeline"));
        var results = orchestration.SearchSessions("Blazor", 10);
        await Assert.That(results.Count).IsGreaterThanOrEqualTo(1);
        await Assert.That(results.Any(entry => entry.SessionId == session.SessionId)).IsTrue();
    }

    [Test]
    public async Task SupervisorStatus_Flags_Resume_And_Stalled_Tasks()
    {
        var options = new ReactiveMultiAgentOptions { StateRootPath = Path.Combine(Path.GetTempPath(), "reactive-multi-agent-mcp-supervisor-tests", Guid.NewGuid().ToString("N")) };
        using var store = new SqliteOrchestrationSessionStore(options);
        IAgentCatalog catalog = new EmbeddedAgentCatalog();
        IRequestDecomposer decomposer = new RequestDecomposer(catalog);
        IOrchestrationService orchestration = new OrchestrationService(decomposer, catalog, store);
        var session = orchestration.CreateSession(OrchestrationRequest.FromStrings("Build a Blazor app"));
        var task = session.Plan.Tasks.Single();
        _ = orchestration.ReportTaskFailure(session.SessionId, task.TaskId, task.AgentId, AgentFailureKind.ContextWindowLimit, "Context too large.");
        var reloaded = orchestration.GetSession(session.SessionId)!;
        reloaded.Plan.Tasks.Single().LastUpdatedUtc = DateTimeOffset.UtcNow.AddMinutes(-45);
        store.Save(reloaded);
        var supervisor = orchestration.GetSupervisorStatus(session.SessionId, stalledAfterMinutes: 30);
        await Assert.That(supervisor.Alerts.Any(alert => alert.Kind == SupervisorAlertKind.ResumeRequired)).IsTrue();
        await Assert.That(supervisor.Alerts.Any(alert => alert.Kind == SupervisorAlertKind.StalledTask)).IsTrue();
        await Assert.That(supervisor.StalledTaskIds).Contains(task.TaskId);
    }

    [Test]
    public async Task SupervisorPlan_Returns_Next_Runnable_Task_For_Ready_Work()
    {
        var options = new ReactiveMultiAgentOptions { StateRootPath = Path.Combine(Path.GetTempPath(), "reactive-multi-agent-mcp-next-task-tests", Guid.NewGuid().ToString("N")) };
        using var store = new SqliteOrchestrationSessionStore(options);
        IAgentCatalog catalog = new EmbeddedAgentCatalog();
        IRequestDecomposer decomposer = new RequestDecomposer(catalog);
        IOrchestrationService orchestration = new OrchestrationService(decomposer, catalog, store);
        var session = orchestration.CreateSession(OrchestrationRequest.FromStrings("Build a Blazor app"));
        var plan = orchestration.GetSupervisorActionPlan(session.SessionId);
        await Assert.That(plan.NextRunnableTasks.Count).IsGreaterThanOrEqualTo(1);
        await Assert.That(plan.OrderedActions.Any(action => action.Contains("Run next ready task", StringComparison.Ordinal))).IsTrue();
        await Assert.That(plan.ActionIds.Count).IsGreaterThanOrEqualTo(1);
    }

    [Test]
    public async Task SupervisorPlan_AutoApplyPolicies_Adds_AutoApplied_Action()
    {
        var options = new ReactiveMultiAgentOptions { StateRootPath = Path.Combine(Path.GetTempPath(), "reactive-multi-agent-mcp-auto-plan-tests", Guid.NewGuid().ToString("N")) };
        using var store = new SqliteOrchestrationSessionStore(options);
        IAgentCatalog catalog = new EmbeddedAgentCatalog();
        IRequestDecomposer decomposer = new RequestDecomposer(catalog);
        IOrchestrationService orchestration = new OrchestrationService(decomposer, catalog, store);
        var session = orchestration.CreateSession(OrchestrationRequest.FromStrings("Build a Blazor app"));
        var task = session.Plan.Tasks.Single();
        _ = orchestration.ApplyAutomaticPolicy(session.SessionId, task.TaskId, task.AgentId, currentEstimatedTokens: 9500);
        var plan = orchestration.GetSupervisorActionPlan(session.SessionId, autoApplyPolicies: true);
        await Assert.That(plan.AutoAppliedActions.Count).IsGreaterThanOrEqualTo(1);
        await Assert.That(plan.AutoAppliedActions.Any(action => action.Contains(task.TaskId, StringComparison.Ordinal))).IsTrue();
    }

    [Test]
    public async Task ExecutionLedger_And_ResumeState_Are_Persisted_For_Orchestration_Level_Recovery()
    {
        var options = new ReactiveMultiAgentOptions { StateRootPath = Path.Combine(Path.GetTempPath(), "reactive-multi-agent-mcp-ledger-tests", Guid.NewGuid().ToString("N")) };
        using var store = new SqliteOrchestrationSessionStore(options);
        IAgentCatalog catalog = new EmbeddedAgentCatalog();
        IRequestDecomposer decomposer = new RequestDecomposer(catalog);
        IOrchestrationService orchestration = new OrchestrationService(decomposer, catalog, store);
        var session = orchestration.CreateSession(OrchestrationRequest.FromStrings("Build a Blazor app"));
        _ = orchestration.GetSupervisorActionPlan(session.SessionId, autoApplyPolicies: false);
        var resumed = orchestration.ResumeOrchestration(session.SessionId);
        await Assert.That(resumed.ExecutionLedger.Count).IsGreaterThanOrEqualTo(2);
        await Assert.That(resumed.ExecutionLedger.Any(entry => entry.Category == "orchestration-resume")).IsTrue();
    }

    [Test]
    public async Task SupervisorActionLifecycle_Can_Be_Acknowledged_And_Completed()
    {
        var options = new ReactiveMultiAgentOptions { StateRootPath = Path.Combine(Path.GetTempPath(), "reactive-multi-agent-mcp-action-lifecycle-tests", Guid.NewGuid().ToString("N")) };
        using var store = new SqliteOrchestrationSessionStore(options);
        IAgentCatalog catalog = new EmbeddedAgentCatalog();
        IRequestDecomposer decomposer = new RequestDecomposer(catalog);
        IOrchestrationService orchestration = new OrchestrationService(decomposer, catalog, store);
        var session = orchestration.CreateSession(OrchestrationRequest.FromStrings("Build a Blazor app"));
        var plan = orchestration.GetSupervisorActionPlan(session.SessionId);
        var actionId = plan.ActionIds[0];
        var acknowledged = orchestration.UpdateSupervisorAction(session.SessionId, actionId, SupervisorActionState.Acknowledged);
        var completed = orchestration.UpdateSupervisorAction(session.SessionId, actionId, SupervisorActionState.Completed);
        await Assert.That(acknowledged.SupervisorActions.Any(action => action.ActionId == actionId && action.State == SupervisorActionState.Acknowledged)).IsTrue();
        await Assert.That(completed.SupervisorActions.Any(action => action.ActionId == actionId && action.State == SupervisorActionState.Completed)).IsTrue();
    }

    [Test]
    public async Task SupervisorActions_AutoComplete_From_Task_Events()
    {
        var options = new ReactiveMultiAgentOptions { StateRootPath = Path.Combine(Path.GetTempPath(), "reactive-multi-agent-mcp-auto-close-tests", Guid.NewGuid().ToString("N")) };
        using var store = new SqliteOrchestrationSessionStore(options);
        IAgentCatalog catalog = new EmbeddedAgentCatalog();
        IRequestDecomposer decomposer = new RequestDecomposer(catalog);
        IOrchestrationService orchestration = new OrchestrationService(decomposer, catalog, store);
        var session = orchestration.CreateSession(OrchestrationRequest.FromStrings("Build a Blazor app"));
        var task = session.Plan.Tasks.Single();
        _ = orchestration.GetSupervisorActionPlan(session.SessionId);
        _ = orchestration.RecordAgentResult(session.SessionId, task.TaskId, task.AgentId, workSummary: "Completed the Blazor app shell.", markComplete: true);
        var reloaded = orchestration.GetSession(session.SessionId)!;
        await Assert.That(reloaded.SupervisorActions.Any(action => action.ActionId == $"run:{task.TaskId}" && action.State == SupervisorActionState.Completed)).IsTrue();
        await Assert.That(reloaded.ExecutionLedger.Any(entry => entry.Category == "supervisor-action-auto-complete" && entry.ActionId == $"run:{task.TaskId}")).IsTrue();
    }

    [Test]
    public async Task Stale_Supervisor_Actions_Are_Escalated_And_FollowUp_Is_Generated()
    {
        var options = new ReactiveMultiAgentOptions { StateRootPath = Path.Combine(Path.GetTempPath(), "reactive-multi-agent-mcp-escalation-tests", Guid.NewGuid().ToString("N")) };
        using var store = new SqliteOrchestrationSessionStore(options);
        IAgentCatalog catalog = new EmbeddedAgentCatalog();
        IRequestDecomposer decomposer = new RequestDecomposer(catalog);
        IOrchestrationService orchestration = new OrchestrationService(decomposer, catalog, store);
        var session = orchestration.CreateSession(OrchestrationRequest.FromStrings("Build a Blazor app"));
        _ = orchestration.GetSupervisorActionPlan(session.SessionId);
        var reloaded = orchestration.GetSession(session.SessionId)!;
        var action = reloaded.SupervisorActions[0];
        action.ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(-120);
        store.Save(reloaded);
        var escalated = orchestration.ApplySupervisorActionEscalation(session.SessionId, staleAfterMinutes: 30, criticalAfterMinutes: 90);
        await Assert.That(escalated.SupervisorActions.Any(a => a.ActionId == action.ActionId && a.State == SupervisorActionState.Abandoned)).IsTrue();
        await Assert.That(escalated.SupervisorActions.Any(a => a.ActionId.StartsWith("followup:", StringComparison.OrdinalIgnoreCase))).IsTrue();
        await Assert.That(escalated.ExecutionLedger.Any(entry => entry.Category == "supervisor-followup")).IsTrue();
    }

    [Test]
    public async Task Heartbeat_Can_Be_Recorded_For_Session_Task_And_Action()
    {
        var options = new ReactiveMultiAgentOptions { StateRootPath = Path.Combine(Path.GetTempPath(), "reactive-multi-agent-mcp-heartbeat-tests", Guid.NewGuid().ToString("N")) };
        using var store = new SqliteOrchestrationSessionStore(options);
        IAgentCatalog catalog = new EmbeddedAgentCatalog();
        IRequestDecomposer decomposer = new RequestDecomposer(catalog);
        IOrchestrationService orchestration = new OrchestrationService(decomposer, catalog, store);
        var session = orchestration.CreateSession(OrchestrationRequest.FromStrings("Build a Blazor app"));
        var task = session.Plan.Tasks.Single();
        var plan = orchestration.GetSupervisorActionPlan(session.SessionId);
        var actionId = plan.ActionIds[0];
        var updated = orchestration.RecordHeartbeat(session.SessionId, task.TaskId, task.AgentId, actionId, source: "test");
        await Assert.That(updated.LastHeartbeatUtc).IsGreaterThan(session.LastHeartbeatUtc);
        await Assert.That(updated.Plan.Tasks.Any(t => t.TaskId == task.TaskId && t.LastHeartbeatUtc >= updated.LastHeartbeatUtc.AddSeconds(-1))).IsTrue();
        await Assert.That(updated.SupervisorActions.Any(a => a.ActionId == actionId && a.LastHeartbeatUtc >= updated.LastHeartbeatUtc.AddSeconds(-1))).IsTrue();
    }

    [Test]
    public async Task Maintenance_Sweep_Records_Silent_Heartbeat_Issues()
    {
        var options = new ReactiveMultiAgentOptions { StateRootPath = Path.Combine(Path.GetTempPath(), "reactive-multi-agent-mcp-maintenance-tests", Guid.NewGuid().ToString("N")) };
        using var store = new SqliteOrchestrationSessionStore(options);
        IAgentCatalog catalog = new EmbeddedAgentCatalog();
        IRequestDecomposer decomposer = new RequestDecomposer(catalog);
        IOrchestrationService orchestration = new OrchestrationService(decomposer, catalog, store);
        var session = orchestration.CreateSession(OrchestrationRequest.FromStrings("Build a Blazor app"));
        var reloaded = orchestration.GetSession(session.SessionId)!;
        reloaded.LastHeartbeatUtc = DateTimeOffset.UtcNow.AddMinutes(-60);
        reloaded.Plan.Tasks.Single().LastHeartbeatUtc = DateTimeOffset.UtcNow.AddMinutes(-60);
        store.Save(reloaded);
        var sweep = orchestration.RunMaintenanceSweep(session.SessionId, silentHeartbeatMinutes: 15, staleTaskMinutes: 30, staleActionMinutes: 30, criticalActionMinutes: 90);
        var status = orchestration.GetSupervisorStatus(session.SessionId, stalledAfterMinutes: 15);
        await Assert.That(sweep.ExecutionLedger.Any(entry => entry.Category == "maintenance-sweep")).IsTrue();
        await Assert.That(status.HeartbeatIssues.Any(issue => issue.Scope == "task")).IsTrue();
        await Assert.That(status.Alerts.Any(alert => alert.Kind == SupervisorAlertKind.SilentHeartbeat)).IsTrue();
    }

    [Test]
    public async Task Maintenance_Report_Returns_Cron_Style_Summary_And_Verdict()
    {
        var options = new ReactiveMultiAgentOptions { StateRootPath = Path.Combine(Path.GetTempPath(), "reactive-multi-agent-mcp-report-tests", Guid.NewGuid().ToString("N")) };
        using var store = new SqliteOrchestrationSessionStore(options);
        IAgentCatalog catalog = new EmbeddedAgentCatalog();
        IRequestDecomposer decomposer = new RequestDecomposer(catalog);
        IOrchestrationService orchestration = new OrchestrationService(decomposer, catalog, store);
        var session = orchestration.CreateSession(OrchestrationRequest.FromStrings("Build a Blazor app"));
        var reloaded = orchestration.GetSession(session.SessionId)!;
        reloaded.LastHeartbeatUtc = DateTimeOffset.UtcNow.AddMinutes(-60);
        reloaded.Plan.Tasks.Single().LastHeartbeatUtc = DateTimeOffset.UtcNow.AddMinutes(-60);
        store.Save(reloaded);
        var report = orchestration.GetMaintenanceReport(session.SessionId, silentHeartbeatMinutes: 15);
        await Assert.That(string.IsNullOrWhiteSpace(report.CronSummary)).IsFalse();
        await Assert.That(report.Verdict == "warning" || report.Verdict == "critical").IsTrue();
        await Assert.That(report.HeartbeatIssues.Count).IsGreaterThanOrEqualTo(1);
    }

    [Test]
    public async Task Maintenance_Report_Can_AutoApply_Policies()
    {
        var options = new ReactiveMultiAgentOptions { StateRootPath = Path.Combine(Path.GetTempPath(), "reactive-multi-agent-mcp-report-autoapply-tests", Guid.NewGuid().ToString("N")) };
        using var store = new SqliteOrchestrationSessionStore(options);
        IAgentCatalog catalog = new EmbeddedAgentCatalog();
        IRequestDecomposer decomposer = new RequestDecomposer(catalog);
        IOrchestrationService orchestration = new OrchestrationService(decomposer, catalog, store);
        var session = orchestration.CreateSession(OrchestrationRequest.FromStrings("Build a Blazor app"));
        var task = session.Plan.Tasks.Single();
        _ = orchestration.ApplyAutomaticPolicy(session.SessionId, task.TaskId, task.AgentId, currentEstimatedTokens: 9500);
        var report = orchestration.GetMaintenanceReport(session.SessionId, autoApplyPolicies: true);
        await Assert.That(report.AutoAppliedPolicies).IsTrue();
        await Assert.That(report.AutoAppliedActions.Count).IsGreaterThanOrEqualTo(1);
    }


    [Test]
    public async Task Maintenance_Report_Persists_History_And_Trend()
    {
        var options = new ReactiveMultiAgentOptions { StateRootPath = Path.Combine(Path.GetTempPath(), "reactive-multi-agent-mcp-history-trend-tests", Guid.NewGuid().ToString("N")) };
        using var store = new SqliteOrchestrationSessionStore(options);
        IAgentCatalog catalog = new EmbeddedAgentCatalog();
        IRequestDecomposer decomposer = new RequestDecomposer(catalog);
        IOrchestrationService orchestration = new OrchestrationService(decomposer, catalog, store);
        var session = orchestration.CreateSession(OrchestrationRequest.FromStrings("Build a Blazor app"));
        _ = orchestration.GetMaintenanceReport(session.SessionId);
        var reloaded = orchestration.GetSession(session.SessionId)!;
        reloaded.LastHeartbeatUtc = DateTimeOffset.UtcNow.AddMinutes(-60);
        reloaded.Plan.Tasks.Single().LastHeartbeatUtc = DateTimeOffset.UtcNow.AddMinutes(-60);
        store.Save(reloaded);
        var second = orchestration.GetMaintenanceReport(session.SessionId, silentHeartbeatMinutes: 15);
        var history = orchestration.GetMaintenanceHistory(session.SessionId, 10);
        await Assert.That(history.Count).IsGreaterThanOrEqualTo(2);
        await Assert.That(second.RecentHistory.Count).IsGreaterThanOrEqualTo(2);
        await Assert.That(second.Trend == MaintenanceTrend.Worsening || second.Trend == MaintenanceTrend.Stable).IsTrue();
        await Assert.That(string.IsNullOrWhiteSpace(second.TrendSummary)).IsFalse();
    }

    [Test]
    public async Task Maintenance_History_Returns_Recent_Snapshots_In_Order()
    {
        var options = new ReactiveMultiAgentOptions { StateRootPath = Path.Combine(Path.GetTempPath(), "reactive-multi-agent-mcp-history-order-tests", Guid.NewGuid().ToString("N")) };
        using var store = new SqliteOrchestrationSessionStore(options);
        IAgentCatalog catalog = new EmbeddedAgentCatalog();
        IRequestDecomposer decomposer = new RequestDecomposer(catalog);
        IOrchestrationService orchestration = new OrchestrationService(decomposer, catalog, store);
        var session = orchestration.CreateSession(OrchestrationRequest.FromStrings("Build a Blazor app"));
        _ = orchestration.GetMaintenanceReport(session.SessionId);
        _ = orchestration.GetMaintenanceReport(session.SessionId);
        var history = orchestration.GetMaintenanceHistory(session.SessionId, 2);
        await Assert.That(history.Count).IsEqualTo(2);
        await Assert.That(history[1].RecordedAtUtc).IsGreaterThanOrEqualTo(history[0].RecordedAtUtc);
    }


    [Test]
    public async Task OrchestrateRequest_Returns_Compact_Client_Safe_Payload()
    {
        var options = new ReactiveMultiAgentOptions { StateRootPath = Path.Combine(Path.GetTempPath(), "reactive-multi-agent-mcp-compact-payload-tests", Guid.NewGuid().ToString("N")) };
        using var store = new SqliteOrchestrationSessionStore(options);
        IAgentCatalog catalog = new EmbeddedAgentCatalog();
        IRequestDecomposer decomposer = new RequestDecomposer(catalog);
        IOrchestrationService orchestration = new OrchestrationService(decomposer, catalog, store);

        var json = OrchestratorTools.OrchestrateRequest(
            orchestration,
            "Build a Blazor app with CI, docs, tests, and an MCP integration layer",
            constraints: "net10 only,no external services",
            desiredArtifacts: "source files,tests,docs",
            preferredAgents: "csharp,ci,docs,mcp",
            maxParallelAgents: 4);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        await Assert.That(root.TryGetProperty("sessionId", out _)).IsTrue();
        await Assert.That(root.TryGetProperty("plan", out var plan)).IsTrue();
        await Assert.That(plan.TryGetProperty("tasks", out _)).IsTrue();
        await Assert.That(root.TryGetProperty("nextStep", out _)).IsTrue();
        await Assert.That(json.Length).IsLessThan(12000);
        await Assert.That(root.TryGetProperty("session", out _)).IsFalse();
    }

}
