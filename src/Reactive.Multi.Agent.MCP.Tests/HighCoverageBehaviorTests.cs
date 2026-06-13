using Reactive.Multi.Agent.MCP.Core.Abstractions;
using Reactive.Multi.Agent.MCP.Core.Configuration;
using Reactive.Multi.Agent.MCP.Core.Models;
using Reactive.Multi.Agent.MCP.Core.Persistence;
using Reactive.Multi.Agent.MCP.Core.Services;
using Reactive.Multi.Agent.MCP.Knowledge.Services;
using Reactive.Multi.Agent.MCP.Server.Prompts;
using Reactive.Multi.Agent.MCP.Server.Tools;
using System.Text.Json;

namespace Reactive.Multi.Agent.MCP.Tests;

public class HighCoverageBehaviorTests
{
    [Test]
    public async Task SupervisorStatus_Reports_All_Action_And_Task_Alert_Shapes()
    {
        var fixture = CreateFixture("coverage-supervisor-status");
        using var store = fixture.Store;
        var session = fixture.Orchestration.CreateSession(OrchestrationRequest.FromStrings("Build a Blazor app, generate a CI pipeline, and write docs"));
        _ = fixture.Orchestration.GetSupervisorActionPlan(session.SessionId);

        var reloaded = fixture.Orchestration.GetSession(session.SessionId)!;
        var now = DateTimeOffset.UtcNow;
        reloaded.LastHeartbeatUtc = now.AddMinutes(-90);
        foreach (var task in reloaded.Plan.Tasks)
        {
            task.LastUpdatedUtc = now.AddMinutes(-90);
            task.LastHeartbeatUtc = now.AddMinutes(-90);
        }

        var readyTask = reloaded.Plan.Tasks.First(task => task.Dependencies.Count == 0);
        readyTask.RecoveryState.NeedsResume = true;
        readyTask.RecoveryState.PolicyState.AutoCheckpointRecommended = true;
        readyTask.RecoveryState.PolicyState.AutoRetryRecommended = true;

        var action = reloaded.SupervisorActions[0];
        action.Escalation = SupervisorActionEscalation.Warning;
        action.LastHeartbeatUtc = now.AddMinutes(-90);
        store.Save(reloaded);

        var status = fixture.Orchestration.GetSupervisorStatus(session.SessionId, stalledAfterMinutes: 30);

        await Assert.That(status.Alerts.Any(alert => alert.Kind == SupervisorAlertKind.StalledTask)).IsTrue();
        await Assert.That(status.Alerts.Any(alert => alert.Kind == SupervisorAlertKind.SilentHeartbeat)).IsTrue();
        await Assert.That(status.Alerts.Any(alert => alert.Kind == SupervisorAlertKind.ResumeRequired)).IsTrue();
        await Assert.That(status.Alerts.Any(alert => alert.Kind == SupervisorAlertKind.AutoCheckpointRecommended)).IsTrue();
        await Assert.That(status.Alerts.Any(alert => alert.Kind == SupervisorAlertKind.AutoRetryRecommended)).IsTrue();
        await Assert.That(status.Alerts.Any(alert => alert.Kind == SupervisorAlertKind.BlockedByDependency)).IsTrue();
        await Assert.That(status.Alerts.Any(alert => alert.Kind == SupervisorAlertKind.StaleSupervisorAction)).IsTrue();
        await Assert.That(status.HeartbeatIssues.Any(issue => issue.Scope == "session")).IsTrue();
        await Assert.That(status.HeartbeatIssues.Any(issue => issue.Scope == "action")).IsTrue();
        await Assert.That(status.Recommendations.Count).IsGreaterThanOrEqualTo(1);
    }

    [Test]
    public async Task SupervisorPlan_Returns_Noop_When_No_Action_Is_Required()
    {
        var fixture = CreateFixture("coverage-supervisor-noop");
        using var store = fixture.Store;
        var session = fixture.Orchestration.CreateSession(OrchestrationRequest.FromStrings("Build a Blazor app"));
        var task = session.Plan.Tasks.Single();
        _ = fixture.Orchestration.RecordAgentResult(session.SessionId, task.TaskId, task.AgentId, workSummary: "Complete.", markComplete: true);

        var plan = fixture.Orchestration.GetSupervisorActionPlan(session.SessionId);

        await Assert.That(plan.ActionIds).Contains("noop");
        await Assert.That(plan.OrderedActions.Single()).Contains("No immediate supervisor actions");
    }

    [Test]
    public async Task SupervisorPlan_Covers_Heartbeat_Retry_Checkpoint_And_Resume_Actions()
    {
        var fixture = CreateFixture("coverage-supervisor-plan-branches");
        using var store = fixture.Store;
        var session = fixture.Orchestration.CreateSession(OrchestrationRequest.FromStrings("Build a Blazor app, generate a CI pipeline, and write docs"));
        var reloaded = fixture.Orchestration.GetSession(session.SessionId)!;
        var now = DateTimeOffset.UtcNow;
        var tasks = reloaded.Plan.Tasks.ToArray();

        tasks[0].LastHeartbeatUtc = now.AddMinutes(-90);
        tasks[0].RecoveryState.PolicyState.AutoRetryRecommended = true;
        tasks[1].RecoveryState.PolicyState.AutoCheckpointRecommended = true;
        tasks[2].RecoveryState.NeedsResume = true;
        store.Save(reloaded);

        var plan = fixture.Orchestration.GetSupervisorActionPlan(session.SessionId, stalledAfterMinutes: 30, autoApplyPolicies: true, networkRecovered: true);

        await Assert.That(plan.ActionIds.Any(actionId => actionId.StartsWith("heartbeat:task:", StringComparison.Ordinal))).IsTrue();
        await Assert.That(plan.ActionIds).Contains($"retry:{tasks[0].TaskId}");
        await Assert.That(plan.ActionIds).Contains($"checkpoint:{tasks[1].TaskId}");
        await Assert.That(plan.ActionIds).Contains($"resume:{tasks[2].TaskId}");
        await Assert.That(plan.AutoAppliedActions.Count).IsGreaterThanOrEqualTo(2);
    }

    [Test]
    public async Task SupervisorActionEscalation_Covers_Warning_Path_And_Status_Target_Extraction()
    {
        var fixture = CreateFixture("coverage-supervisor-warning");
        using var store = fixture.Store;
        var session = fixture.Orchestration.CreateSession(OrchestrationRequest.FromStrings("Build a Blazor app"));
        _ = fixture.Orchestration.GetSupervisorActionPlan(session.SessionId);
        var reloaded = fixture.Orchestration.GetSession(session.SessionId)!;
        var action = reloaded.SupervisorActions.First(action => action.ActionId.StartsWith("run:", StringComparison.OrdinalIgnoreCase));
        action.ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(-45);
        store.Save(reloaded);

        var escalated = fixture.Orchestration.ApplySupervisorActionEscalation(session.SessionId, staleAfterMinutes: 30, criticalAfterMinutes: 90);
        var status = fixture.Orchestration.GetSupervisorStatus(session.SessionId);

        await Assert.That(escalated.SupervisorActions.Any(candidate => candidate.ActionId == action.ActionId && candidate.Escalation == SupervisorActionEscalation.Warning)).IsTrue();
        await Assert.That(escalated.SupervisorActions.Any(candidate => candidate.ActionId.StartsWith("followup:", StringComparison.OrdinalIgnoreCase))).IsFalse();
        await Assert.That(status.Alerts.Any(alert => alert.Kind == SupervisorAlertKind.StaleSupervisorAction && alert.TaskId == "task-1")).IsTrue();
    }

    [Test]
    public async Task RecordHeartbeat_Throws_For_Unknown_Task_And_Action()
    {
        var fixture = CreateFixture("coverage-heartbeat-errors");
        using var store = fixture.Store;
        var session = fixture.Orchestration.CreateSession(OrchestrationRequest.FromStrings("Build a Blazor app"));

        await Assert.That(() => fixture.Orchestration.RecordHeartbeat(session.SessionId, taskId: "missing", agentId: "blazor")).Throws<InvalidOperationException>();
        await Assert.That(() => fixture.Orchestration.RecordHeartbeat(session.SessionId, actionId: "missing-action")).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task AutomaticPolicy_Covers_Token_Subscription_And_Unknown_Failure_Reasons()
    {
        var fixture = CreateFixture("coverage-policy-reasons");
        using var store = fixture.Store;
        var session = fixture.Orchestration.CreateSession(OrchestrationRequest.FromStrings("Build a Blazor app, write tests, and publish docs"));
        var tasks = session.Plan.Tasks.ToArray();

        var tokenLow = fixture.Orchestration.ReportTaskFailure(session.SessionId, tasks[0].TaskId, tasks[0].AgentId, AgentFailureKind.TokenBudgetLow, "Token budget low.");
        var subscriptionExhausted = fixture.Orchestration.ReportTaskFailure(session.SessionId, tasks[1].TaskId, tasks[1].AgentId, AgentFailureKind.SubscriptionTokensExhausted, "Subscription exhausted.");
        var unknown = fixture.Orchestration.ReportTaskFailure(session.SessionId, tasks[2].TaskId, tasks[2].AgentId, AgentFailureKind.Unknown, "Unexpected condition.");

        await Assert.That(tokenLow.RecoveryState.PolicyState.PolicyReason).Contains("Token budget is low");
        await Assert.That(subscriptionExhausted.RecoveryState.PolicyState.PolicyReason).Contains("Subscription tokens are exhausted");
        await Assert.That(unknown.RecoveryState.PolicyState.PolicyReason).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task AutomaticPolicy_Covers_AutoResume_When_Network_Recovered_For_Context_Failure()
    {
        var fixture = CreateFixture("coverage-policy-autoresume");
        using var store = fixture.Store;
        var session = fixture.Orchestration.CreateSession(OrchestrationRequest.FromStrings("Build a Blazor app"));
        var task = session.Plan.Tasks.Single();

        _ = fixture.Orchestration.ReportTaskFailure(session.SessionId, task.TaskId, task.AgentId, AgentFailureKind.ContextWindowLimit, "Context full.");
        var packet = fixture.Orchestration.ApplyAutomaticPolicy(session.SessionId, task.TaskId, task.AgentId, networkRecovered: true);

        await Assert.That(packet.RecoveryState.NeedsResume).IsTrue();
        await Assert.That(packet.RecoveryState.PolicyState.AutoResumeRecommended).IsTrue();
        await Assert.That(packet.RecoveryState.ResumeInstructions).Contains("ContextWindowLimit");
        await Assert.That(packet.Checkpoints.Count).IsGreaterThanOrEqualTo(1);
    }

    [Test]
    public async Task AgentPackets_Cover_Blocked_Checkpoint_Resume_And_Shutdown_NextSteps()
    {
        var fixture = CreateFixture("coverage-packet-branches");
        using var store = fixture.Store;
        var session = fixture.Orchestration.CreateSession(OrchestrationRequest.FromStrings("Build a Blazor app, generate a CI pipeline, and write docs"));
        var implementationTask = session.Plan.Tasks.First(task => task.AgentId == "blazor");
        var blockedTask = session.Plan.Tasks.First(task => task.AgentId == "ci");

        var blockedPacket = fixture.Orchestration.GetAgentTaskPacket(session.SessionId, blockedTask.TaskId, blockedTask.AgentId);
        var checkpointPacket = fixture.Orchestration.RecordCheckpoint(session.SessionId, implementationTask.TaskId, implementationTask.AgentId, "Checkpoint for prompt coverage.", ["reload-a", "reload-b"]);
        var failedPacket = fixture.Orchestration.ReportTaskFailure(session.SessionId, implementationTask.TaskId, implementationTask.AgentId, AgentFailureKind.ContextWindowLimit, "Context full.");
        var completedPacket = fixture.Orchestration.RecordAgentResult(session.SessionId, implementationTask.TaskId, implementationTask.AgentId, workSummary: "Done.", markComplete: true);

        await Assert.That(blockedPacket.BlockingDependencies.Count).IsGreaterThanOrEqualTo(1);
        await Assert.That(blockedPacket.ExecutionPrompt).Contains("Blocking dependencies not yet complete");
        await Assert.That(blockedPacket.NextSteps.Any(step => step.Contains("Wait for blocking dependencies", StringComparison.Ordinal))).IsTrue();
        await Assert.That(checkpointPacket.ExecutionPrompt).Contains("Latest checkpoint");
        await Assert.That(checkpointPacket.ResumeMemoryReloadItems).Contains("reload-a");
        await Assert.That(failedPacket.NextSteps.Any(step => step.Contains("Resume the task", StringComparison.Ordinal))).IsTrue();
        await Assert.That(completedPacket.NextSteps.Any(step => step.Contains("Shut down", StringComparison.OrdinalIgnoreCase))).IsTrue();
    }

    [Test]
    public async Task FinalizeSession_Reports_Resume_AutoPolicy_Risks_And_Pending_Work()
    {
        var fixture = CreateFixture("coverage-finalize-branches");
        using var store = fixture.Store;
        var session = fixture.Orchestration.CreateSession(OrchestrationRequest.FromStrings("Build a Blazor app, generate a CI pipeline, and write docs"));
        var implementationTask = session.Plan.Tasks.First(task => task.AgentId == "blazor");
        var ciTask = session.Plan.Tasks.First(task => task.AgentId == "ci");

        _ = fixture.Orchestration.RecordAgentResult(
            session.SessionId,
            implementationTask.TaskId,
            implementationTask.AgentId,
            workSummary: "Blazor shell complete.",
            artifacts: [new AgentArtifact { ArtifactId = "artifact-ui", Kind = ArtifactKind.SourceFile, Title = "App.razor", Summary = "UI shell" }],
            handoffItems: [new HandoffItem { ItemId = "handoff-docs", Category = "docs", Title = "Document UI shell", Details = "Mention layout decisions.", IsBlocking = false }],
            risks: ["CSS polish pending"],
            markComplete: true);

        _ = fixture.Orchestration.ReportTaskFailure(session.SessionId, ciTask.TaskId, ciTask.AgentId, AgentFailureKind.ContextWindowLimit, "CI context full.");
        _ = fixture.Orchestration.ApplyAutomaticPolicy(session.SessionId, ciTask.TaskId, ciTask.AgentId, currentEstimatedTokens: 9500);

        var summary = fixture.Orchestration.FinalizeSession(session.SessionId);

        await Assert.That(summary.Status).IsEqualTo("InProgress");
        await Assert.That(summary.UnifiedResponse).Contains("Resume required for");
        await Assert.That(summary.UnifiedResponse).Contains("Automatic checkpoint recommended");
        await Assert.That(summary.UnifiedResponse).Contains("App.razor");
        await Assert.That(summary.UnifiedResponse).Contains("Document UI shell");
        await Assert.That(summary.UnifiedResponse).Contains("Pending specialist work");
        await Assert.That(summary.CompletedWork.Count).IsEqualTo(1);
        await Assert.That(summary.PendingWork.Count).IsGreaterThanOrEqualTo(1);
    }

    [Test]
    public async Task FinalizeSession_Reports_AutoRetry_And_Supervisor_Action_Lifecycle()
    {
        var fixture = CreateFixture("coverage-finalize-retry-actions");
        using var store = fixture.Store;
        var session = fixture.Orchestration.CreateSession(OrchestrationRequest.FromStrings("Build a Blazor app"));
        _ = fixture.Orchestration.GetSupervisorActionPlan(session.SessionId);
        var reloaded = fixture.Orchestration.GetSession(session.SessionId)!;
        reloaded.Plan.Tasks.Single().RecoveryState.PolicyState.AutoRetryRecommended = true;
        store.Save(reloaded);

        var summary = fixture.Orchestration.FinalizeSession(session.SessionId);

        await Assert.That(summary.UnifiedResponse).Contains("Automatic retry recommended");
        await Assert.That(summary.UnifiedResponse).Contains("Supervisor action lifecycle");
        await Assert.That(summary.AutoRetryTaskIds).Contains("task-1");
    }

    [Test]
    public async Task MaintenanceReports_Cover_Improving_And_Stable_Trends()
    {
        var fixture = CreateFixture("coverage-maintenance-trends");
        using var store = fixture.Store;
        var session = fixture.Orchestration.CreateSession(OrchestrationRequest.FromStrings("Build a Blazor app"));

        var reloaded = fixture.Orchestration.GetSession(session.SessionId)!;
        reloaded.LastHeartbeatUtc = DateTimeOffset.UtcNow.AddMinutes(-90);
        reloaded.Plan.Tasks.Single().LastHeartbeatUtc = DateTimeOffset.UtcNow.AddMinutes(-90);
        store.Save(reloaded);

        _ = fixture.Orchestration.GetMaintenanceReport(session.SessionId, silentHeartbeatMinutes: 15);
        var recovered = fixture.Orchestration.RecordHeartbeat(session.SessionId, reloaded.Plan.Tasks.Single().TaskId, reloaded.Plan.Tasks.Single().AgentId, source: "recovered");
        var improving = fixture.Orchestration.GetMaintenanceReport(recovered.SessionId, silentHeartbeatMinutes: 15);
        var stable = fixture.Orchestration.GetMaintenanceReport(recovered.SessionId, silentHeartbeatMinutes: 15);

        await Assert.That(improving.Trend).IsEqualTo(MaintenanceTrend.Improving);
        await Assert.That(improving.TrendSummary).Contains("improved");
        await Assert.That(stable.TrendSummary).Contains("stable");
    }

    [Test]
    public async Task MaintenanceReport_Returns_Healthy_For_Completed_Session()
    {
        var fixture = CreateFixture("coverage-maintenance-healthy");
        using var store = fixture.Store;
        var session = fixture.Orchestration.CreateSession(OrchestrationRequest.FromStrings("Build a Blazor app"));
        var task = session.Plan.Tasks.Single();
        _ = fixture.Orchestration.RecordAgentResult(session.SessionId, task.TaskId, task.AgentId, workSummary: "Complete.", markComplete: true);

        var report = fixture.Orchestration.GetMaintenanceReport(session.SessionId);

        await Assert.That(report.Verdict).IsEqualTo("healthy");
        await Assert.That(report.CronSummary).Contains("verdict=healthy");
    }

    [Test]
    public async Task LegacySessions_Backfill_Missing_Agent_Identity()
    {
        var fixture = CreateFixture("coverage-legacy-identity");
        using var store = fixture.Store;
        var session = fixture.Orchestration.CreateSession(OrchestrationRequest.FromStrings("Build a Blazor app"));
        var reloaded = fixture.Orchestration.GetSession(session.SessionId)!;
        var task = reloaded.Plan.Tasks.Single();
        task.AgentName = string.Empty;
        task.AgentSessionId = string.Empty;
        store.Save(reloaded);

        var packet = fixture.Orchestration.GetAgentTaskPacket(session.SessionId, task.TaskId, task.AgentId);

        await Assert.That(packet.AgentName).IsEqualTo("Blazor Agent - task-1");
        await Assert.That(packet.AgentSessionId).Contains("blazor-agent-task-1");
    }

    [Test]
    public async Task Decomposer_Covers_Fallbacks_And_Validation_Branches()
    {
        var noProfiles = new StaticAgentCatalog([]);
        var noProfilesDecomposer = new RequestDecomposer(noProfiles);

        await Assert.That(() => noProfilesDecomposer.CreatePlan(OrchestrationRequest.FromStrings("Do anything"))).Throws<InvalidOperationException>();

        var customProfile = new AgentProfile
        {
            Id = "custom",
            Domain = "custom",
            Category = "other",
            DisplayName = "Custom Agent",
            Role = "Fallback role",
            ToolName = "multiagent_custom_agent",
            RoutingKeywords = ["impossible-keyword"],
        };

        var customDecomposer = new RequestDecomposer(new StaticAgentCatalog([customProfile]));
        var fallbackPlan = customDecomposer.CreatePlan(OrchestrationRequest.FromStrings("Unmatched request text"));

        await Assert.That(fallbackPlan.Tasks.Single().AgentId).IsEqualTo("custom");
        await Assert.That(fallbackPlan.Tasks.Single().PhaseName).IsEqualTo("Implementation");

        var embeddedPlan = new RequestDecomposer(new EmbeddedAgentCatalog()).CreatePlan(OrchestrationRequest.FromStrings("..."));
        await Assert.That(embeddedPlan.Tasks.Single().Objective).IsEqualTo("...");
    }

    [Test]
    public async Task ProgramCreateOptions_Uses_Environment_Overrides()
    {
        var previousStateRoot = Environment.GetEnvironmentVariable("REACTIVE_MULTI_AGENT_MCP_STATE_ROOT");
        var previousPackageId = Environment.GetEnvironmentVariable("REACTIVE_MULTI_AGENT_MCP_PACKAGE_ID");
        var previousServerId = Environment.GetEnvironmentVariable("REACTIVE_MULTI_AGENT_MCP_SERVER_ID");

        try
        {
            Environment.SetEnvironmentVariable("REACTIVE_MULTI_AGENT_MCP_STATE_ROOT", "D:\\Temp\\ReactiveMultiAgentState");
            Environment.SetEnvironmentVariable("REACTIVE_MULTI_AGENT_MCP_PACKAGE_ID", "custom.package");
            Environment.SetEnvironmentVariable("REACTIVE_MULTI_AGENT_MCP_SERVER_ID", "custom/server");

            var options = Reactive.Multi.Agent.MCP.Server.Program.CreateOptions();

            await Assert.That(options.StateRootPath).IsEqualTo("D:\\Temp\\ReactiveMultiAgentState");
            await Assert.That(options.PackageId).IsEqualTo("custom.package");
            await Assert.That(options.ServerId).IsEqualTo("custom/server");
        }
        finally
        {
            Environment.SetEnvironmentVariable("REACTIVE_MULTI_AGENT_MCP_STATE_ROOT", previousStateRoot);
            Environment.SetEnvironmentVariable("REACTIVE_MULTI_AGENT_MCP_PACKAGE_ID", previousPackageId);
            Environment.SetEnvironmentVariable("REACTIVE_MULTI_AGENT_MCP_SERVER_ID", previousServerId);
        }
    }

    [Test]
    public async Task OrchestratorSessionStatus_And_SupervisorTool_Return_Alert_Payloads()
    {
        var fixture = CreateFixture("coverage-orchestrator-tools");
        using var store = fixture.Store;
        var session = fixture.Orchestration.CreateSession(OrchestrationRequest.FromStrings("Build a Blazor app"));
        var reloaded = fixture.Orchestration.GetSession(session.SessionId)!;
        reloaded.Plan.Tasks.Single().LastUpdatedUtc = DateTimeOffset.UtcNow.AddMinutes(-90);
        store.Save(reloaded);

        var statusJson = OrchestratorTools.SessionStatus(fixture.Orchestration, session.SessionId);
        var supervisorJson = OrchestratorTools.SupervisorStatus(fixture.Orchestration, session.SessionId, stalledAfterMinutes: 30);

        using var statusDocument = JsonDocument.Parse(statusJson);
        using var supervisorDocument = JsonDocument.Parse(supervisorJson);

        await Assert.That(statusDocument.RootElement.TryGetProperty("plan", out var plan)).IsTrue();
        await Assert.That(plan.TryGetProperty("tasks", out var tasks)).IsTrue();
        await Assert.That(tasks.GetArrayLength()).IsGreaterThanOrEqualTo(1);
        await Assert.That(supervisorDocument.RootElement.GetProperty("alerts").GetArrayLength()).IsGreaterThanOrEqualTo(1);
        await Assert.That(supervisorDocument.RootElement.GetProperty("alerts")[0].TryGetProperty("recommendedActions", out _)).IsTrue();
    }

    [Test]
    public async Task OrchestratorEscalationTool_Returns_Escalated_Action_Projection()
    {
        var fixture = CreateFixture("coverage-orchestrator-escalation-tool");
        using var store = fixture.Store;
        var session = fixture.Orchestration.CreateSession(OrchestrationRequest.FromStrings("Build a Blazor app"));
        _ = fixture.Orchestration.GetSupervisorActionPlan(session.SessionId);
        var reloaded = fixture.Orchestration.GetSession(session.SessionId)!;
        reloaded.SupervisorActions[0].ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(-120);
        store.Save(reloaded);

        var json = OrchestratorTools.ApplySupervisorActionEscalation(fixture.Orchestration, session.SessionId, staleAfterMinutes: 30, criticalAfterMinutes: 90);
        using var document = JsonDocument.Parse(json);

        await Assert.That(document.RootElement.GetProperty("escalatedActions").GetArrayLength()).IsGreaterThanOrEqualTo(1);
        await Assert.That(document.RootElement.GetProperty("escalatedActions")[0].TryGetProperty("followUpActionId", out _)).IsTrue();
    }

    [Test]
    public async Task PromptAndCatalogSearch_Cover_SpecialistPrompt_And_MultiResult_Search()
    {
        var fixture = CreateFixture("coverage-prompt-catalog");
        using var store = fixture.Store;
        var session = fixture.Orchestration.CreateSession(OrchestrationRequest.FromStrings("Build a Blazor app"));
        var task = session.Plan.Tasks.Single();

        var prompt = OrchestrationPrompts.CreateSpecialistAgentPrompt(fixture.Orchestration, session.SessionId, task.TaskId, task.AgentId);
        var catalog = new EmbeddedAgentCatalog();
        var results = catalog.Search("agent");

        await Assert.That(prompt).Contains("Top-level request");
        await Assert.That(results.Count).IsGreaterThanOrEqualTo(2);
        await Assert.That(results.Select(profile => profile.DisplayName).ToArray()).Contains("Blazor Agent");
    }

    private static TestFixture CreateFixture(string folderPrefix)
    {
        var options = new ReactiveMultiAgentOptions
        {
            StateRootPath = Path.Combine(Path.GetTempPath(), folderPrefix, Guid.NewGuid().ToString("N")),
        };

        var store = new SqliteOrchestrationSessionStore(options);
        IAgentCatalog catalog = new EmbeddedAgentCatalog();
        IRequestDecomposer decomposer = new RequestDecomposer(catalog);
        IOrchestrationService orchestration = new OrchestrationService(decomposer, catalog, store);
        return new TestFixture(store, orchestration);
    }

    private sealed record TestFixture(SqliteOrchestrationSessionStore Store, IOrchestrationService Orchestration);

    private sealed class StaticAgentCatalog(IReadOnlyList<AgentProfile> profiles) : IAgentCatalog
    {
        public IReadOnlyList<AgentProfile> GetAll() => profiles;

        public AgentProfile? GetById(string id)
            => profiles.FirstOrDefault(profile => profile.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

        public IReadOnlyList<AgentProfile> Search(string? query)
            => string.IsNullOrWhiteSpace(query)
                ? profiles
                : profiles.Where(profile => profile.Id.Contains(query, StringComparison.OrdinalIgnoreCase)).ToArray();
    }
}
