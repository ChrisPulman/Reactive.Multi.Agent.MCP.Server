using System.Text;

namespace Reactive.Multi.Agent.MCP.Core.Services;

/// <summary>
/// Provides orchestration and supervision services for multi-agent workflows, including session management, task
/// coordination, maintenance, and recovery operations.
/// </summary>
/// <remarks>This service coordinates the lifecycle of orchestration sessions, including task assignment, progress
/// tracking, supervisor action planning, maintenance sweeps, and recovery from failures. It is designed for use in
/// environments where multiple agents collaborate on complex tasks and require robust supervision, checkpointing, and
/// automated policy enforcement. Thread safety and persistence are determined by the provided session store
/// implementation.</remarks>
/// <param name="requestDecomposer">The request decomposer used to break down orchestration requests into executable agent plans. Cannot be null.</param>
/// <param name="agentCatalog">The agent catalog that supplies available agent profiles and capabilities. Cannot be null.</param>
/// <param name="sessionStore">The session store used to persist orchestration sessions and their state. Cannot be null.</param>
public sealed class OrchestrationService(
    IRequestDecomposer requestDecomposer,
    IAgentCatalog agentCatalog,
    IOrchestrationSessionStore sessionStore) : IOrchestrationService
{
    private const string OrchestratorModelRequirement = "GPT-5.5 or an equivalent highest-capacity model must own orchestration/control-plane context for this session.";

    private readonly Dictionary<string, AgentProfile> _profiles = agentCatalog.GetAll().ToDictionary(profile => profile.Id, StringComparer.OrdinalIgnoreCase);

    public OrchestrationSession CreateSession(OrchestrationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var sessionId = Guid.NewGuid().ToString("N");
        var now = DateTimeOffset.UtcNow;
        var plan = requestDecomposer.CreatePlan(request);
        foreach (var task in plan.Tasks)
        {
            if (this._profiles.TryGetValue(task.AgentId, out var profile))
            {
                task.AgentName = string.IsNullOrWhiteSpace(task.AgentName)
                    ? BuildAgentName(profile, task.TaskId)
                    : task.AgentName.Trim();
            }

            task.AgentSessionId = BuildAgentSessionId(sessionId, task);
            task.ShutdownRequired = false;
            task.CompletedAtUtc = null;
            task.LastUpdatedUtc = now;
            task.LastHeartbeatUtc = now;
        }

        var session = new OrchestrationSession
        {
            SessionId = sessionId,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            LastHeartbeatUtc = now,
            Request = request,
            Plan = plan,
            RecoveryGuidance = "If a task hits a context-window limit, token-budget limit, network loss, or subscription token issue, checkpoint state and use automatic policy guidance so the client can resume in a fresh context window without user intervention.",
            ExecutionLedger = [CreateLedgerEntry("session", "Session created", $"Created orchestration session for request: {request.UserRequest}", "completed")],
            ResumeState = new OrchestrationResumeState(),
            SupervisorActions = [],
            MaintenanceHistory = [],
        };

        sessionStore.Save(session);
        return session;
    }

    public OrchestrationSession? GetSession(string sessionId)
        => sessionStore.Load(sessionId);

    public IReadOnlyList<SessionHistoryEntry> SearchSessions(string? query = null, int limit = 20)
        => sessionStore.Search(query, limit);

    public IReadOnlyList<MaintenanceSnapshot> GetMaintenanceHistory(string sessionId, int limit = 10)
    {
        var session = sessionStore.Load(sessionId)
            ?? throw new InvalidOperationException($"Unknown orchestration session '{sessionId}'.");
        return [.. session.MaintenanceHistory.TakeLast(Math.Max(1, limit))];
    }

    public SupervisorStatus GetSupervisorStatus(string sessionId, int stalledAfterMinutes = 30)
    {
        var session = sessionStore.Load(sessionId)
            ?? throw new InvalidOperationException($"Unknown orchestration session '{sessionId}'.");
        this.EnsureTaskIdentities(session);

        var now = DateTimeOffset.UtcNow;
        var alerts = new List<SupervisorAlert>();
        var heartbeatIssues = new List<HeartbeatIssue>();
        var stalledTaskIds = new List<string>();
        var threshold = TimeSpan.FromMinutes(Math.Max(1, stalledAfterMinutes));

        foreach (var task in session.Plan.Tasks)
        {
            if (task.Status != AgentTaskStatus.Completed && now - task.LastUpdatedUtc >= threshold)
            {
                alerts.Add(new SupervisorAlert
                {
                    TaskId = task.TaskId,
                    AgentId = task.AgentId,
                    Kind = SupervisorAlertKind.StalledTask,
                    Severity = "warning",
                    Message = $"Task '{task.TaskId}' appears stalled. Last update: {task.LastUpdatedUtc:O}.",
                    RecommendedActions = ["Inspect task packet", "Apply automatic policy", "Checkpoint or resume task"],
                });
                stalledTaskIds.Add(task.TaskId);
            }

            if (task.Status != AgentTaskStatus.Completed && now - task.LastHeartbeatUtc >= threshold)
            {
                heartbeatIssues.Add(new HeartbeatIssue
                {
                    Scope = "task",
                    TargetId = task.TaskId,
                    LastHeartbeatUtc = task.LastHeartbeatUtc,
                    Severity = "warning",
                    Message = $"Task '{task.TaskId}' heartbeat is silent since {task.LastHeartbeatUtc:O}.",
                });

                alerts.Add(new SupervisorAlert
                {
                    TaskId = task.TaskId,
                    AgentId = task.AgentId,
                    Kind = SupervisorAlertKind.SilentHeartbeat,
                    Severity = "warning",
                    Message = $"Task '{task.TaskId}' heartbeat is silent.",
                    RecommendedActions = ["Record heartbeat", "Run maintenance sweep", "Inspect task packet for silent agent"],
                });
            }

            if (task.RecoveryState.NeedsResume)
            {
                alerts.Add(new SupervisorAlert
                {
                    TaskId = task.TaskId,
                    AgentId = task.AgentId,
                    Kind = SupervisorAlertKind.ResumeRequired,
                    Severity = "critical",
                    Message = $"Task '{task.TaskId}' requires resume.",
                    RecommendedActions = ["Open latest checkpoint", "Reload memory items", "Call multiagent_resume_task"],
                });
            }

            if (task.RecoveryState.PolicyState.AutoCheckpointRecommended)
            {
                alerts.Add(new SupervisorAlert
                {
                    TaskId = task.TaskId,
                    AgentId = task.AgentId,
                    Kind = SupervisorAlertKind.AutoCheckpointRecommended,
                    Severity = "warning",
                    Message = $"Task '{task.TaskId}' should checkpoint due to policy thresholds.",
                    RecommendedActions = ["Call specialist tool with createCheckpoint=true", "Persist memory reload items"],
                });
            }

            if (task.RecoveryState.PolicyState.AutoRetryRecommended)
            {
                alerts.Add(new SupervisorAlert
                {
                    TaskId = task.TaskId,
                    AgentId = task.AgentId,
                    Kind = SupervisorAlertKind.AutoRetryRecommended,
                    Severity = "info",
                    Message = $"Task '{task.TaskId}' can retry automatically.",
                    RecommendedActions = ["Retry the task in the same agent session", "Record updated work log"],
                });
            }

            if (task.Dependencies.Count > 0 && task.Dependencies.Any(dependency => dependency.Kind == DependencyKind.Blocking)
                && !task.RecoveryState.NeedsResume
                && task.Status != AgentTaskStatus.Completed
                && !IsTaskReady(session, task))
            {
                alerts.Add(new SupervisorAlert
                {
                    TaskId = task.TaskId,
                    AgentId = task.AgentId,
                    Kind = SupervisorAlertKind.BlockedByDependency,
                    Severity = "info",
                    Message = $"Task '{task.TaskId}' is blocked by unfinished dependencies.",
                    RecommendedActions = ["Complete upstream task", "Recheck session status after dependency completion"],
                });
            }
        }

        if (now - session.LastHeartbeatUtc >= threshold)
        {
            heartbeatIssues.Add(new HeartbeatIssue
            {
                Scope = "session",
                TargetId = session.SessionId,
                LastHeartbeatUtc = session.LastHeartbeatUtc,
                Severity = "warning",
                Message = $"Session heartbeat is silent since {session.LastHeartbeatUtc:O}.",
            });
        }

        foreach (var action in session.SupervisorActions.Where(action => action.State is SupervisorActionState.Pending or SupervisorActionState.Acknowledged))
        {
            if (now - action.LastHeartbeatUtc >= threshold)
            {
                heartbeatIssues.Add(new HeartbeatIssue
                {
                    Scope = "action",
                    TargetId = action.ActionId,
                    LastHeartbeatUtc = action.LastHeartbeatUtc,
                    Severity = action.Escalation == SupervisorActionEscalation.Critical ? "critical" : "warning",
                    Message = $"Supervisor action '{action.ActionId}' heartbeat is silent since {action.LastHeartbeatUtc:O}.",
                });
            }

            if (action.Escalation != SupervisorActionEscalation.None)
            {
                alerts.Add(new SupervisorAlert
                {
                    TaskId = ExtractActionTargetId(action.ActionId),
                    AgentId = string.Empty,
                    Kind = SupervisorAlertKind.StaleSupervisorAction,
                    Severity = action.Escalation == SupervisorActionEscalation.Critical ? "critical" : "warning",
                    Message = $"Supervisor action '{action.ActionId}' is stale and escalated to {action.Escalation}.",
                    RecommendedActions = ["Acknowledge or complete the action", "Review follow-up action if present", "Apply supervisor plan again if needed"],
                });
            }
        }

        var recommendations = alerts.SelectMany(alert => alert.RecommendedActions)
            .Concat(heartbeatIssues.Select(issue => issue.Message))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new SupervisorStatus
        {
            SessionId = sessionId,
            EvaluatedAtUtc = now,
            Alerts = alerts,
            Recommendations = recommendations,
            StalledTaskIds = stalledTaskIds,
            NextRunnableTasks = BuildNextRunnableTasks(session),
            HeartbeatIssues = heartbeatIssues,
        };
    }

    public SupervisorActionPlan GetSupervisorActionPlan(string sessionId, int stalledAfterMinutes = 30, bool autoApplyPolicies = false, bool networkRecovered = false)
    {
        var session = sessionStore.Load(sessionId)
            ?? throw new InvalidOperationException($"Unknown orchestration session '{sessionId}'.");
        this.EnsureTaskIdentities(session);
        var supervisor = this.GetSupervisorStatus(sessionId, stalledAfterMinutes);
        var orderedActions = new List<string>();
        var actionIds = new List<string>();
        var autoAppliedActions = new List<string>();
        var actionRecords = new List<SupervisorActionRecord>();

        foreach (var issue in supervisor.HeartbeatIssues)
        {
            orderedActions.Add($"Inspect silent heartbeat for {issue.Scope} '{issue.TargetId}': {issue.Message}");
            actionIds.Add($"heartbeat:{issue.Scope}:{issue.TargetId}");
            actionRecords.Add(CreateActionRecord($"heartbeat:{issue.Scope}:{issue.TargetId}", orderedActions[^1], expiresAfterMinutes: stalledAfterMinutes));
        }

        foreach (var task in session.Plan.Tasks.OrderBy(task => task.PhaseOrder).ThenBy(task => task.SequenceOrder))
        {
            if (task.RecoveryState.PolicyState.AutoRetryRecommended)
            {
                var actionId = $"retry:{task.TaskId}";
                orderedActions.Add($"Retry task {task.TaskId} ({task.AgentToolName}) after recovered network.");
                actionIds.Add(actionId);
                actionRecords.Add(CreateActionRecord(actionId, orderedActions[^1]));
                if (autoApplyPolicies)
                {
                    _ = this.ApplyAutomaticPolicy(sessionId, task.TaskId, task.AgentId, networkRecovered: networkRecovered);
                    autoAppliedActions.Add($"Applied retry policy to {task.TaskId}.");
                }
            }

            if (task.RecoveryState.PolicyState.AutoCheckpointRecommended)
            {
                var actionId = $"checkpoint:{task.TaskId}";
                orderedActions.Add($"Checkpoint task {task.TaskId} ({task.AgentToolName}) to preserve context.");
                actionIds.Add(actionId);
                actionRecords.Add(CreateActionRecord(actionId, orderedActions[^1]));
                if (autoApplyPolicies)
                {
                    _ = this.ApplyAutomaticPolicy(sessionId, task.TaskId, task.AgentId);
                    autoAppliedActions.Add($"Applied checkpoint policy to {task.TaskId}.");
                }
            }

            if (task.RecoveryState.NeedsResume)
            {
                var actionId = $"resume:{task.TaskId}";
                orderedActions.Add($"Resume task {task.TaskId} ({task.AgentToolName}) from latest checkpoint.");
                actionIds.Add(actionId);
                actionRecords.Add(CreateActionRecord(actionId, orderedActions[^1]));
            }
        }

        foreach (var candidate in supervisor.NextRunnableTasks)
        {
            var actionId = $"run:{candidate.TaskId}";
            orderedActions.Add($"Run next ready task {candidate.TaskId} via {candidate.AgentToolName}: {candidate.Reason}");
            actionIds.Add(actionId);
            actionRecords.Add(CreateActionRecord(actionId, orderedActions[^1]));
        }

        if (orderedActions.Count == 0)
        {
            orderedActions.Add("No immediate supervisor actions are required.");
            actionIds.Add("noop");
            actionRecords.Add(CreateActionRecord("noop", orderedActions[^1]));
        }

        var refreshed = sessionStore.Load(sessionId) ?? session;
        this.EnsureTaskIdentities(refreshed);
        refreshed.SupervisorActions = MergeSupervisorActions(refreshed.SupervisorActions, actionRecords);
        refreshed.ExecutionLedger =
        [
            .. refreshed.ExecutionLedger,
            CreateLedgerEntry("supervisor", "Supervisor plan created", string.Join(" | ", orderedActions), "completed"),
            .. actionRecords.Select(record => CreateLedgerEntry("supervisor-action", "Supervisor action tracked", record.Description, record.State.ToString().ToLowerInvariant(), record.ActionId)),
            .. autoAppliedActions.Select(action => CreateLedgerEntry("supervisor-auto-apply", "Auto-applied supervisor action", action, "completed")),
        ];
        refreshed.ResumeState = BuildResumeState(refreshed);
        refreshed.UpdatedAtUtc = DateTimeOffset.UtcNow;
        sessionStore.Save(refreshed);

        return new SupervisorActionPlan
        {
            SessionId = sessionId,
            EvaluatedAtUtc = DateTimeOffset.UtcNow,
            OrderedActions = orderedActions,
            AutoAppliedActions = autoAppliedActions,
            NextRunnableTasks = BuildNextRunnableTasks(refreshed),
            ActionIds = actionIds,
        };
    }

    public OrchestrationSession ApplySupervisorActionEscalation(string sessionId, int staleAfterMinutes = 30, int criticalAfterMinutes = 90)
    {
        var session = sessionStore.Load(sessionId)
            ?? throw new InvalidOperationException($"Unknown orchestration session '{sessionId}'.");
        var now = DateTimeOffset.UtcNow;
        var warningThreshold = TimeSpan.FromMinutes(Math.Max(1, staleAfterMinutes));
        var criticalThreshold = TimeSpan.FromMinutes(Math.Max(staleAfterMinutes, criticalAfterMinutes));
        var followUps = new List<SupervisorActionRecord>();

        foreach (var action in session.SupervisorActions.Where(action => action.State is SupervisorActionState.Pending or SupervisorActionState.Acknowledged))
        {
            var age = now - (action.ExpiresAtUtc ?? action.UpdatedAtUtc);
            if (age >= criticalThreshold)
            {
                action.Escalation = SupervisorActionEscalation.Critical;
                action.State = SupervisorActionState.Abandoned;
                action.UpdatedAtUtc = now;
                action.LastHeartbeatUtc = now;
                if (string.IsNullOrWhiteSpace(action.FollowUpActionId))
                {
                    var followUpId = $"followup:{action.ActionId}";
                    action.FollowUpActionId = followUpId;
                    followUps.Add(CreateActionRecord(followUpId, $"Follow up escalated action {action.ActionId}", expiresAfterMinutes: staleAfterMinutes));
                }

                session.ExecutionLedger = [.. session.ExecutionLedger, CreateLedgerEntry("supervisor-action-escalation", $"Escalated {action.ActionId}", "Action became critically stale and was abandoned.", "critical", action.ActionId)];
            }
            else if (age >= warningThreshold)
            {
                action.Escalation = SupervisorActionEscalation.Warning;
                session.ExecutionLedger = [.. session.ExecutionLedger, CreateLedgerEntry("supervisor-action-escalation", $"Warning for {action.ActionId}", "Action became stale and was escalated to warning.", "warning", action.ActionId)];
            }
        }

        if (followUps.Count > 0)
        {
            session.SupervisorActions = MergeSupervisorActions(session.SupervisorActions, followUps);
            session.ExecutionLedger = [.. session.ExecutionLedger, .. followUps.Select(f => CreateLedgerEntry("supervisor-followup", "Generated follow-up supervisor action", f.Description, "pending", f.ActionId))];
        }

        session.ResumeState = BuildResumeState(session);
        session.UpdatedAtUtc = now;
        sessionStore.Save(session);
        return session;
    }

    public OrchestrationSession RecordHeartbeat(string sessionId, string? taskId = null, string? agentId = null, string? actionId = null, string source = "external")
    {
        var session = sessionStore.Load(sessionId)
            ?? throw new InvalidOperationException($"Unknown orchestration session '{sessionId}'.");
        var now = DateTimeOffset.UtcNow;
        session.LastHeartbeatUtc = now;

        if (!string.IsNullOrWhiteSpace(taskId))
        {
            var task = session.Plan.Tasks.FirstOrDefault(candidate =>
                candidate.TaskId.Equals(taskId, StringComparison.OrdinalIgnoreCase)
                && (string.IsNullOrWhiteSpace(agentId) || candidate.AgentId.Equals(agentId, StringComparison.OrdinalIgnoreCase)))
                ?? throw new InvalidOperationException($"Task '{taskId}' was not found in session '{sessionId}'.");
            task.LastHeartbeatUtc = now;
            task.LastUpdatedUtc = now;
        }

        if (!string.IsNullOrWhiteSpace(actionId))
        {
            var action = session.SupervisorActions.FirstOrDefault(candidate => candidate.ActionId.Equals(actionId, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException($"Supervisor action '{actionId}' was not found in session '{sessionId}'.");
            action.LastHeartbeatUtc = now;
            action.UpdatedAtUtc = now;
        }

        session.ExecutionLedger = [.. session.ExecutionLedger, CreateLedgerEntry("heartbeat", "Heartbeat recorded", $"Heartbeat source '{source}' recorded.", "completed", actionId)];
        session.UpdatedAtUtc = now;
        session.ResumeState = BuildResumeState(session);
        sessionStore.Save(session);
        return session;
    }

    public OrchestrationSession RunMaintenanceSweep(string sessionId, int silentHeartbeatMinutes = 15, int staleTaskMinutes = 30, int staleActionMinutes = 30, int criticalActionMinutes = 90)
    {
        var session = this.RecordHeartbeat(sessionId, source: "maintenance-sweep");
        session = this.ApplySupervisorActionEscalation(sessionId, staleActionMinutes, criticalActionMinutes);
        var now = DateTimeOffset.UtcNow;
        var silentThreshold = TimeSpan.FromMinutes(Math.Max(1, silentHeartbeatMinutes));

        foreach (var task in session.Plan.Tasks.Where(task => task.Status != AgentTaskStatus.Completed && now - task.LastHeartbeatUtc >= silentThreshold))
        {
            session.ExecutionLedger = [.. session.ExecutionLedger, CreateLedgerEntry("maintenance-silent-task", $"Silent task heartbeat {task.TaskId}", $"No heartbeat seen since {task.LastHeartbeatUtc:O}.", "warning", $"heartbeat:task:{task.TaskId}")];
        }

        session.ExecutionLedger = [.. session.ExecutionLedger, CreateLedgerEntry("maintenance-sweep", "Maintenance sweep completed", $"Heartbeat threshold={silentHeartbeatMinutes}m, stale task threshold={staleTaskMinutes}m, stale action threshold={staleActionMinutes}m.", "completed")];
        session.ResumeState = BuildResumeState(session);
        session.UpdatedAtUtc = now;
        sessionStore.Save(session);
        return sessionStore.Load(sessionId)!;
    }

    public MaintenanceReport GetMaintenanceReport(string sessionId, int silentHeartbeatMinutes = 15, int staleTaskMinutes = 30, int staleActionMinutes = 30, int criticalActionMinutes = 90, bool autoApplyPolicies = false, bool networkRecovered = false)
    {
        var session = this.RunMaintenanceSweep(sessionId, silentHeartbeatMinutes, staleTaskMinutes, staleActionMinutes, criticalActionMinutes);
        var status = this.GetSupervisorStatus(sessionId, staleTaskMinutes);
        var autoApplied = new List<string>();

        if (autoApplyPolicies)
        {
            foreach (var task in session.Plan.Tasks.Where(ShouldAutoApplyDuringMaintenance))
            {
                _ = this.ApplyAutomaticPolicy(sessionId, task.TaskId, task.AgentId, networkRecovered: networkRecovered);
                autoApplied.Add($"Applied maintenance auto-policy to {task.TaskId}.");
            }

            session = sessionStore.Load(sessionId) ?? session;
            session.ExecutionLedger = [.. session.ExecutionLedger, .. autoApplied.Select(action => CreateLedgerEntry("maintenance-auto-apply", "Maintenance auto-apply", action, "completed"))];
            sessionStore.Save(session);
        }

        var summary = this.FinalizeSession(sessionId);
        var findings = BuildMaintenanceFindings(status, summary, session);
        var verdict = DetermineMaintenanceVerdict(status, summary);
        var recommended = status.Recommendations
            .Concat(summary.ResumeRequiredTaskIds.Select(taskId => $"Resume task {taskId}."))
            .Concat(session.ResumeState.IncompleteActionIds.Select(actionId => $"Resolve supervisor action {actionId}."))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var cronSummary = BuildCronSummary(sessionId, verdict, status, summary, autoApplied);
        var previous = session.MaintenanceHistory.Count > 0 ? session.MaintenanceHistory[^1] : null;
        var currentSnapshot = CreateMaintenanceSnapshot(verdict, status, summary, session, cronSummary);
        var trend = DetermineMaintenanceTrend(previous, currentSnapshot);
        var trendSummary = BuildTrendSummary(previous, currentSnapshot, trend);
        session.MaintenanceHistory = [.. session.MaintenanceHistory, currentSnapshot];
        session.ExecutionLedger = [.. session.ExecutionLedger, CreateLedgerEntry("maintenance-report", "Maintenance report recorded", trendSummary, verdict)];
        sessionStore.Save(session);

        return new MaintenanceReport
        {
            SessionId = sessionId,
            GeneratedAtUtc = DateTimeOffset.UtcNow,
            Verdict = verdict,
            AutoAppliedPolicies = autoApplyPolicies,
            AutoAppliedActions = autoApplied,
            Findings = findings,
            RecommendedActions = recommended,
            HeartbeatIssues = status.HeartbeatIssues,
            ResumeRequiredTaskIds = summary.ResumeRequiredTaskIds,
            IncompleteSupervisorActionIds = session.ResumeState.IncompleteActionIds,
            CronSummary = cronSummary,
            Trend = trend,
            TrendSummary = trendSummary,
            RecentHistory = [.. session.MaintenanceHistory.TakeLast(5)],
        };
    }

    public OrchestrationSession ResumeOrchestration(string sessionId)
    {
        var session = sessionStore.Load(sessionId)
            ?? throw new InvalidOperationException($"Unknown orchestration session '{sessionId}'.");

        var incomplete = session.SupervisorActions
            .Where(action => action.State is SupervisorActionState.Pending or SupervisorActionState.Acknowledged)
            .Select(action => action.ActionId)
            .ToArray();

        session.ResumeState = BuildResumeState(session);
        session.ResumeState.NeedsOrchestrationResume = incomplete.Length > 0 || session.Plan.Tasks.Any(task => task.RecoveryState.NeedsResume);
        session.ResumeState.ResumeSummary = incomplete.Length == 0
            ? "Orchestration resumed with no unfinished supervisor actions."
            : $"Resume unfinished supervisor actions: {string.Join(", ", incomplete)}";
        session.ExecutionLedger =
        [
            .. session.ExecutionLedger,
            CreateLedgerEntry("orchestration-resume", "Orchestration resumed", session.ResumeState.ResumeSummary, "completed"),
        ];
        session.LastHeartbeatUtc = DateTimeOffset.UtcNow;
        session.UpdatedAtUtc = DateTimeOffset.UtcNow;
        sessionStore.Save(session);
        return session;
    }

    public OrchestrationSession UpdateSupervisorAction(string sessionId, string actionId, SupervisorActionState state)
    {
        var session = sessionStore.Load(sessionId)
            ?? throw new InvalidOperationException($"Unknown orchestration session '{sessionId}'.");
        var existing = session.SupervisorActions.FirstOrDefault(action => action.ActionId.Equals(actionId, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Unknown supervisor action '{actionId}'.");

        existing.State = state;
        existing.UpdatedAtUtc = DateTimeOffset.UtcNow;
        existing.LastHeartbeatUtc = DateTimeOffset.UtcNow;
        session.ExecutionLedger =
        [
            .. session.ExecutionLedger,
            CreateLedgerEntry("supervisor-action-update", $"Supervisor action {actionId} updated", $"State changed to {state}.", state.ToString().ToLowerInvariant(), actionId),
        ];
        session.ResumeState = BuildResumeState(session);
        session.UpdatedAtUtc = DateTimeOffset.UtcNow;
        sessionStore.Save(session);
        return session;
    }

    public AgentTaskPacket GetAgentTaskPacket(string sessionId, string taskId, string agentId)
    {
        var (session, task, profile) = this.ResolveTask(sessionId, taskId, agentId);
        return BuildPacket(session, task, profile);
    }

    public AgentTaskPacket ActivateAgentTask(string sessionId, string taskId, string agentId, string? additionalContext = null, string? workLog = null)
    {
        var (session, task, profile) = this.ResolveTask(sessionId, taskId, agentId);
        if (!string.IsNullOrWhiteSpace(additionalContext))
        {
            task.Scratchpad = AppendScratchpad(task.Scratchpad, "Additional context", additionalContext);
        }

        if (!string.IsNullOrWhiteSpace(workLog))
        {
            task.Scratchpad = AppendScratchpad(task.Scratchpad, "Work log", workLog);
        }

        if (IsTaskReady(session, task))
        {
            task.Status = task.Status == AgentTaskStatus.Completed ? AgentTaskStatus.Completed : AgentTaskStatus.InProgress;
        }

        var now = DateTimeOffset.UtcNow;
        session.ExecutionLedger = [.. session.ExecutionLedger, CreateLedgerEntry("task-activation", $"Activated {task.TaskId}", task.Title, "completed")];
        task.LastUpdatedUtc = now;
        task.LastHeartbeatUtc = now;
        session.LastHeartbeatUtc = now;
        session.UpdatedAtUtc = now;
        sessionStore.Save(session);
        return BuildPacket(session, task, profile);
    }

    public AgentTaskPacket RecordAgentResult(string sessionId, string taskId, string agentId, string? workSummary = null, IReadOnlyList<AgentArtifact>? artifacts = null, IReadOnlyList<HandoffItem>? handoffItems = null, IReadOnlyList<string>? risks = null, bool markComplete = false)
    {
        var (session, task, profile) = this.ResolveTask(sessionId, taskId, agentId);
        var now = DateTimeOffset.UtcNow;
        task.LatestResult = new AgentTaskResult
        {
            AgentId = profile.Id,
            AgentName = task.AgentName,
            AgentToolName = profile.ToolName,
            Summary = string.IsNullOrWhiteSpace(workSummary) ? "Progress update recorded." : workSummary.Trim(),
            Artifacts = artifacts ?? [],
            HandoffItems = handoffItems ?? [],
            Risks = risks ?? [],
            Completed = markComplete,
            ShutdownRequired = markComplete,
            ReportedAtUtc = now,
        };

        task.Scratchpad = AppendScratchpad(task.Scratchpad, "Result summary", task.LatestResult.Summary);
        if (task.LatestResult.Artifacts.Count > 0)
        {
            task.Scratchpad = AppendScratchpad(task.Scratchpad, "Artifacts", string.Join("; ", task.LatestResult.Artifacts.Select(static artifact => artifact.Title)));
        }

        if (task.LatestResult.HandoffItems.Count > 0)
        {
            task.Scratchpad = AppendScratchpad(task.Scratchpad, "Handoff items", string.Join("; ", task.LatestResult.HandoffItems.Select(static item => item.Title)));
        }

        if (task.LatestResult.Risks.Count > 0)
        {
            task.Scratchpad = AppendScratchpad(task.Scratchpad, "Risks", string.Join("; ", task.LatestResult.Risks));
        }

        task.Status = markComplete ? AgentTaskStatus.Completed : AgentTaskStatus.InProgress;
        task.ShutdownRequired = markComplete;
        task.CompletedAtUtc = markComplete ? now : null;
        task.RecoveryState.NeedsResume = false;
        task.RecoveryState.LastFailureKind = AgentFailureKind.None;
        task.RecoveryState.LastFailureReason = null;
        task.RecoveryState.ResumeInstructions = string.Empty;
        task.RecoveryState.PolicyState.AutoCheckpointRecommended = false;
        task.RecoveryState.PolicyState.AutoResumeRecommended = false;
        task.RecoveryState.PolicyState.AutoRetryRecommended = false;
        task.RecoveryState.PolicyState.PolicyReason = markComplete
            ? "Task completed; named sub-agent should be shut down."
            : task.RecoveryState.PolicyState.PolicyReason;
        session.ExecutionLedger = [.. session.ExecutionLedger, CreateLedgerEntry("task-result", $"Recorded result for {task.TaskId}", task.LatestResult.Summary, markComplete ? "completed" : "in-progress")];
        if (markComplete)
        {
            session.ExecutionLedger =
            [
                .. session.ExecutionLedger,
                CreateLedgerEntry("task-shutdown", $"Shutdown requested for {task.AgentName}", $"Task '{task.TaskId}' is complete; close agent session '{task.AgentSessionId}'.", "required"),
            ];
        }

        session = AutoCompleteRunActionIfTaskCompleted(session, task.TaskId, markComplete);
        task.LastUpdatedUtc = now;
        task.LastHeartbeatUtc = now;
        session.LastHeartbeatUtc = now;
        session.UpdatedAtUtc = now;
        sessionStore.Save(session);
        return BuildPacket(session, task, profile);
    }

    public AgentTaskPacket RecordCheckpoint(string sessionId, string taskId, string agentId, string checkpointSummary, IReadOnlyList<string>? memoryReloadItems = null, int? currentEstimatedTokens = null, int? remainingSubscriptionTokens = null)
    {
        var (session, task, profile) = this.ResolveTask(sessionId, taskId, agentId);
        UpdateBudgets(task, currentEstimatedTokens, remainingSubscriptionTokens);
        task.Checkpoints = [.. task.Checkpoints, CreateCheckpoint(task, checkpointSummary, memoryReloadItems)];
        EvaluateAutomaticPolicies(task, null, false);
        session.ExecutionLedger = [.. session.ExecutionLedger, CreateLedgerEntry("checkpoint", $"Checkpoint for {task.TaskId}", checkpointSummary, "completed")];
        session = AutoCompleteMatchingAction(session, "checkpoint", task.TaskId, "Checkpoint creation satisfied the supervisor checkpoint action.");
        var now = DateTimeOffset.UtcNow;
        task.LastUpdatedUtc = now;
        task.LastHeartbeatUtc = now;
        session.LastHeartbeatUtc = now;
        session.UpdatedAtUtc = now;
        sessionStore.Save(session);
        return BuildPacket(session, task, profile);
    }

    public AgentTaskPacket ReportTaskFailure(string sessionId, string taskId, string agentId, AgentFailureKind failureKind, string reason, IReadOnlyList<string>? memoryReloadItems = null, int? currentEstimatedTokens = null, int? remainingSubscriptionTokens = null)
    {
        var (session, task, profile) = this.ResolveTask(sessionId, taskId, agentId);
        UpdateBudgets(task, currentEstimatedTokens, remainingSubscriptionTokens);
        task.Checkpoints = [.. task.Checkpoints, CreateCheckpoint(task, $"Failure checkpoint: {reason}", memoryReloadItems)];
        task.RecoveryState.NeedsResume = true;
        task.RecoveryState.RestartCount += 1;
        task.RecoveryState.LastFailureKind = failureKind;
        task.RecoveryState.LastFailureReason = reason;
        task.RecoveryState.LastFailureAtUtc = DateTimeOffset.UtcNow;
        task.RecoveryState.ResumeInstructions = BuildResumeInstructions(task, failureKind);
        EvaluateAutomaticPolicies(task, failureKind, false);
        session.ExecutionLedger = [.. session.ExecutionLedger, CreateLedgerEntry("task-failure", $"Failure for {task.TaskId}", reason, "failed")];
        var now = DateTimeOffset.UtcNow;
        task.LastUpdatedUtc = now;
        task.LastHeartbeatUtc = now;
        session.LastHeartbeatUtc = now;
        session.UpdatedAtUtc = now;
        session.ResumeState = BuildResumeState(session);
        sessionStore.Save(session);
        return BuildPacket(session, task, profile);
    }

    public AgentTaskPacket ApplyAutomaticPolicy(string sessionId, string taskId, string agentId, int? currentEstimatedTokens = null, int? remainingSubscriptionTokens = null, bool networkRecovered = false)
    {
        var (session, task, profile) = this.ResolveTask(sessionId, taskId, agentId);
        UpdateBudgets(task, currentEstimatedTokens, remainingSubscriptionTokens);
        EvaluateAutomaticPolicies(task, task.RecoveryState.LastFailureKind, networkRecovered);

        if (task.RecoveryState.PolicyState.AutoCheckpointRecommended)
        {
            task.Checkpoints = [.. task.Checkpoints, CreateCheckpoint(task, "Automatic checkpoint triggered by policy.", null)];
            session.ExecutionLedger = [.. session.ExecutionLedger, CreateLedgerEntry("policy-checkpoint", $"Auto-checkpoint for {task.TaskId}", task.RecoveryState.PolicyState.PolicyReason, "completed")];
        }

        if (task.RecoveryState.PolicyState.AutoResumeRecommended && networkRecovered)
        {
            task.RecoveryState.NeedsResume = true;
            task.RecoveryState.ResumeInstructions = BuildResumeInstructions(task, task.RecoveryState.LastFailureKind);
            session.ExecutionLedger = [.. session.ExecutionLedger, CreateLedgerEntry("policy-resume", $"Auto-resume recommendation for {task.TaskId}", task.RecoveryState.PolicyState.PolicyReason, "pending")];
        }

        if (task.RecoveryState.PolicyState.AutoRetryRecommended && networkRecovered)
        {
            task.RecoveryState.PolicyState.RetryAttemptsUsed += 1;
            task.RecoveryState.NeedsResume = false;
            task.Status = task.Status == AgentTaskStatus.Completed ? AgentTaskStatus.Completed : AgentTaskStatus.InProgress;
            session.ExecutionLedger = [.. session.ExecutionLedger, CreateLedgerEntry("policy-retry", $"Auto-retry for {task.TaskId}", task.RecoveryState.PolicyState.PolicyReason, "completed")];
            session = AutoCompleteMatchingAction(session, "retry", task.TaskId, "Automatic retry satisfied the supervisor retry action.");
        }

        var now = DateTimeOffset.UtcNow;
        task.LastUpdatedUtc = now;
        task.LastHeartbeatUtc = now;
        session.LastHeartbeatUtc = now;
        session.UpdatedAtUtc = now;
        sessionStore.Save(session);
        return BuildPacket(session, task, profile);
    }

    public AgentTaskPacket ResumeTask(string sessionId, string taskId, string agentId)
    {
        var (session, task, profile) = this.ResolveTask(sessionId, taskId, agentId);
        task.RecoveryState.NeedsResume = false;
        task.RecoveryState.ResumeInstructions = string.Empty;
        task.RecoveryState.PolicyState.AutoResumeRecommended = false;
        task.Status = task.Status == AgentTaskStatus.Completed ? AgentTaskStatus.Completed : AgentTaskStatus.InProgress;
        session.ExecutionLedger = [.. session.ExecutionLedger, CreateLedgerEntry("task-resume", $"Resumed {task.TaskId}", task.Title, "completed")];
        session = AutoCompleteMatchingAction(session, "resume", task.TaskId, "Task resume satisfied the supervisor resume action.");
        var now = DateTimeOffset.UtcNow;
        task.LastUpdatedUtc = now;
        task.LastHeartbeatUtc = now;
        session.LastHeartbeatUtc = now;
        session.UpdatedAtUtc = now;
        session.ResumeState = BuildResumeState(session);
        sessionStore.Save(session);
        return BuildPacket(session, task, profile);
    }

    public OrchestrationSummary FinalizeSession(string sessionId)
    {
        var session = sessionStore.Load(sessionId)
            ?? throw new InvalidOperationException($"Unknown orchestration session '{sessionId}'.");
        this.EnsureTaskIdentities(session);

        var tasks = session.Plan.Tasks.OrderBy(task => task.PhaseOrder).ThenBy(task => task.SequenceOrder).ToList();
        var completed = tasks.Where(static task => task.Status == AgentTaskStatus.Completed).ToList();
        var pending = tasks.Where(static task => task.Status != AgentTaskStatus.Completed).ToList();
        var readyTaskIds = pending.Where(task => IsTaskReady(session, task)).Select(task => task.TaskId).ToArray();
        var blockedTaskIds = pending.Where(task => !IsTaskReady(session, task)).Select(task => task.TaskId).ToArray();
        var resumeRequiredTaskIds = pending.Where(task => task.RecoveryState.NeedsResume).Select(task => task.TaskId).ToArray();
        var autoCheckpointTaskIds = pending.Where(task => task.RecoveryState.PolicyState.AutoCheckpointRecommended).Select(task => task.TaskId).ToArray();
        var autoRetryTaskIds = pending.Where(task => task.RecoveryState.PolicyState.AutoRetryRecommended).Select(task => task.TaskId).ToArray();
        var summary = $"{completed.Count} of {tasks.Count} task(s) completed for '{session.Request.UserRequest}'.";

        var unified = new StringBuilder();
        unified.AppendLine(summary);
        unified.AppendLine();
        unified.AppendLine("Continuity guidance:");
        unified.AppendLine($"- {session.RecoveryGuidance}");
        unified.AppendLine($"- Session heartbeat: {session.LastHeartbeatUtc:O}");
        if (session.ResumeState.NeedsOrchestrationResume)
        {
            unified.AppendLine($"- Orchestration resume required: {session.ResumeState.ResumeSummary}");
        }
        if (resumeRequiredTaskIds.Length > 0)
        {
            unified.AppendLine($"- Resume required for: {string.Join(", ", resumeRequiredTaskIds)}");
        }
        if (autoCheckpointTaskIds.Length > 0)
        {
            unified.AppendLine($"- Automatic checkpoint recommended for: {string.Join(", ", autoCheckpointTaskIds)}");
        }
        if (autoRetryTaskIds.Length > 0)
        {
            unified.AppendLine($"- Automatic retry recommended for: {string.Join(", ", autoRetryTaskIds)}");
        }

        unified.AppendLine();
        unified.AppendLine("Supervisor action lifecycle:");
        foreach (var action in session.SupervisorActions.TakeLast(10))
        {
            unified.AppendLine($"- {action.ActionId}: {action.State} — {action.Description} (heartbeat {action.LastHeartbeatUtc:O})");
        }

        unified.AppendLine();
        unified.AppendLine("Execution ledger:");
        foreach (var entry in session.ExecutionLedger.TakeLast(10))
        {
            unified.AppendLine($"- [{entry.RecordedAtUtc:O}] {entry.Category}: {entry.Title} ({entry.Status})");
        }

        unified.AppendLine();
        unified.AppendLine("Completed specialist outputs:");
        if (completed.Count == 0)
        {
            unified.AppendLine("- No completed specialist outputs are recorded yet.");
        }
        else
        {
            foreach (var task in completed)
            {
                unified.AppendLine($"- {task.Title}");
                unified.AppendLine($"  Agent: {task.AgentName} ({task.AgentSessionId})");
                unified.AppendLine($"  Summary: {task.LatestResult?.Summary ?? "No summary recorded."}");
                unified.AppendLine("  Lifecycle: task complete; sub-agent should be shut down.");
                foreach (var artifact in task.LatestResult?.Artifacts ?? [])
                {
                    unified.AppendLine($"  Artifact [{artifact.Kind}]: {artifact.Title} — {artifact.Summary}");
                }

                foreach (var handoff in task.LatestResult?.HandoffItems ?? [])
                {
                    unified.AppendLine($"  Handoff [{handoff.Category}]: {handoff.Title} — {handoff.Details}");
                }
            }
        }

        if (pending.Count > 0)
        {
            unified.AppendLine();
            unified.AppendLine("Pending specialist work:");
            foreach (var task in pending)
            {
                var state = IsTaskReady(session, task) ? "ready" : "blocked";
                var resume = task.RecoveryState.NeedsResume ? ", resume-required" : string.Empty;
                unified.AppendLine($"- {task.Title} via {task.AgentToolName} as {task.AgentName} ({state}{resume}, heartbeat {task.LastHeartbeatUtc:O})");
            }
        }

        return new OrchestrationSummary
        {
            SessionId = session.SessionId,
            Status = pending.Count == 0 ? "Completed" : "InProgress",
            TotalTasks = tasks.Count,
            CompletedTasks = completed.Count,
            PendingTasks = pending.Count,
            ReadyTaskIds = readyTaskIds,
            BlockedTaskIds = blockedTaskIds,
            ResumeRequiredTaskIds = resumeRequiredTaskIds,
            AutoCheckpointTaskIds = autoCheckpointTaskIds,
            AutoRetryTaskIds = autoRetryTaskIds,
            Summary = summary,
            CoordinationNotes = session.Plan.CoordinationNotes,
            CompletedWork =
            [
                .. completed.Select(task => (object)new
                {
                    task.TaskId,
                    task.AgentId,
                    task.AgentName,
                    task.AgentToolName,
                    task.AgentSessionId,
                    task.Title,
                    task.PhaseName,
                    task.Status,
                    ShutdownRequired = task.Status == AgentTaskStatus.Completed || task.ShutdownRequired,
                    task.CompletedAtUtc,
                    task.LastHeartbeatUtc,
                    task.ContextWindowBudget,
                    task.SubscriptionTokenBudget,
                    task.Checkpoints,
                    task.RecoveryState,
                    task.LatestResult,
                }),
            ],
            PendingWork =
            [
                .. pending.Select(task => (object)new
                {
                    task.TaskId,
                    task.AgentId,
                    task.AgentName,
                    task.AgentToolName,
                    task.AgentSessionId,
                    task.Title,
                    task.PhaseName,
                    task.Status,
                    ShutdownRequired = task.Status == AgentTaskStatus.Completed || task.ShutdownRequired,
                    task.LastHeartbeatUtc,
                    task.Dependencies,
                    Ready = IsTaskReady(session, task),
                    task.ContextWindowBudget,
                    task.SubscriptionTokenBudget,
                    task.RecoveryState,
                    task.Checkpoints,
                }),
            ],
            UnifiedResponse = unified.ToString().Trim(),
            LastHeartbeatUtc = session.LastHeartbeatUtc,
        };
    }

    private (OrchestrationSession Session, AgentWorkItem Task, AgentProfile Profile) ResolveTask(string sessionId, string taskId, string agentId)
    {
        var session = sessionStore.Load(sessionId)
            ?? throw new InvalidOperationException($"Unknown orchestration session '{sessionId}'.");
        this.EnsureTaskIdentities(session);

        var task = session.Plan.Tasks.FirstOrDefault(candidate =>
            candidate.TaskId.Equals(taskId, StringComparison.OrdinalIgnoreCase)
            && candidate.AgentId.Equals(agentId, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Task '{taskId}' for agent '{agentId}' was not found in session '{sessionId}'.");

        if (!this._profiles.TryGetValue(agentId, out var profile))
        {
            throw new InvalidOperationException($"Unknown agent profile '{agentId}'.");
        }

        return (session, task, profile);
    }

    private void EnsureTaskIdentities(OrchestrationSession session)
    {
        foreach (var task in session.Plan.Tasks)
        {
            if (!this._profiles.TryGetValue(task.AgentId, out var profile))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(task.AgentName))
            {
                task.AgentName = BuildAgentName(profile, task.TaskId);
            }

            if (string.IsNullOrWhiteSpace(task.AgentSessionId))
            {
                task.AgentSessionId = BuildAgentSessionId(session.SessionId, task);
            }
        }
    }

    private static OrchestrationSession AutoCompleteMatchingAction(OrchestrationSession session, string actionIdPrefix, string taskId, string completionReason)
    {
        var match = session.SupervisorActions.FirstOrDefault(action =>
            action.ActionId.Equals($"{actionIdPrefix}:{taskId}", StringComparison.OrdinalIgnoreCase)
            && action.State is SupervisorActionState.Pending or SupervisorActionState.Acknowledged);

        if (match is null)
        {
            return session;
        }

        match.State = SupervisorActionState.Completed;
        match.UpdatedAtUtc = DateTimeOffset.UtcNow;
        match.LastHeartbeatUtc = DateTimeOffset.UtcNow;
        session.ExecutionLedger =
        [
            .. session.ExecutionLedger,
            CreateLedgerEntry("supervisor-action-auto-complete", $"Auto-completed {match.ActionId}", completionReason, "completed", match.ActionId),
        ];
        session.ResumeState = BuildResumeState(session);
        return session;
    }

    private static OrchestrationSession AutoCompleteRunActionIfTaskCompleted(OrchestrationSession session, string taskId, bool markComplete)
        => markComplete
            ? AutoCompleteMatchingAction(session, "run", taskId, "Task completion satisfied the supervisor run action.")
            : session;

    private static NextTaskCandidate[] BuildNextRunnableTasks(OrchestrationSession session)
        =>
        [
            .. session.Plan.Tasks
                .Where(task => task.Status != AgentTaskStatus.Completed)
                .Where(task => !task.RecoveryState.NeedsResume)
                .Where(task => IsTaskReady(session, task))
                .OrderBy(task => task.PhaseOrder)
                .ThenBy(task => task.SequenceOrder)
                .Select(task => new NextTaskCandidate
                {
                    TaskId = task.TaskId,
                    AgentId = task.AgentId,
                    AgentName = task.AgentName,
                    AgentToolName = task.AgentToolName,
                    Title = task.Title,
                    Reason = task.RecoveryState.PolicyState.AutoCheckpointRecommended
                        ? "Task is ready but should checkpoint promptly due to policy thresholds."
                        : "Task is ready to run with dependencies satisfied.",
                    Priority = task.PhaseOrder * 100 + task.SequenceOrder,
                }),
        ];

    private static AgentTaskPacket BuildPacket(OrchestrationSession session, AgentWorkItem task, AgentProfile profile)
    {
        var blockingDependencies = task.Dependencies
            .Where(dependency => dependency.Kind == DependencyKind.Blocking)
            .Where(dependency => !IsDependencyCompleted(session, dependency.TaskId))
            .Select(dependency => dependency.TaskId)
            .ToArray();

        AgentCheckpoint? latestCheckpoint = task.Checkpoints.Count > 0 ? task.Checkpoints[task.Checkpoints.Count - 1] : null;
        var shutdownRequired = task.Status == AgentTaskStatus.Completed || task.ShutdownRequired;

        return new AgentTaskPacket
        {
            SessionId = session.SessionId,
            TaskId = task.TaskId,
            AgentId = task.AgentId,
            AgentName = task.AgentName,
            AgentToolName = task.AgentToolName,
            AgentSessionId = task.AgentSessionId,
            Status = task.Status,
            IsReady = blockingDependencies.Length == 0,
            BlockingDependencies = blockingDependencies,
            NeedsResume = task.RecoveryState.NeedsResume,
            Objective = task.Objective,
            ContextSnapshot = task.ContextSnapshot,
            PhaseName = task.PhaseName,
            Dependencies = task.Dependencies,
            AcceptanceCriteria = task.AcceptanceCriteria,
            SuggestedSkills = task.SuggestedSkills,
            SuggestedTools = task.SuggestedTools,
            CompletionContract = profile.CompletionContract,
            Scratchpad = task.Scratchpad,
            ExecutionPrompt = shutdownRequired
                ? BuildShutdownPrompt(task)
                : BuildExecutionPrompt(session, task, profile, blockingDependencies, latestCheckpoint),
            NextSteps = BuildNextSteps(profile, task, blockingDependencies, shutdownRequired),
            ShutdownRequired = shutdownRequired,
            LifecycleInstruction = BuildLifecycleInstruction(task, shutdownRequired),
            ArtifactSchemaHint = "Artifacts must be structured objects with artifactId, kind, title, summary, optional filePath, uri, mediaType, and content. Handoff items must be structured objects with itemId, category, title, details, and isBlocking.",
            ContextWindowBudget = task.ContextWindowBudget,
            SubscriptionTokenBudget = task.SubscriptionTokenBudget,
            Checkpoints = task.Checkpoints,
            RecoveryState = task.RecoveryState,
            ResumeMemoryReloadItems = latestCheckpoint?.MemoryReloadItems ?? BuildDefaultMemoryReloadItems(task),
            LatestResult = task.LatestResult,
            CompletedAtUtc = task.CompletedAtUtc,
            LastHeartbeatUtc = task.LastHeartbeatUtc,
        };
    }

    private static string BuildShutdownPrompt(AgentWorkItem task)
        => $"Task '{task.TaskId}' is completed. Sub-agent '{task.AgentName}' with session '{task.AgentSessionId}' must be shut down now; do not continue work in this agent context.";

    private static string BuildExecutionPrompt(OrchestrationSession session, AgentWorkItem task, AgentProfile profile, string[] blockingDependencies, AgentCheckpoint? latestCheckpoint)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"You are the {profile.DisplayName} inside the Reactive Multi Agent MCP orchestration server.");
        builder.AppendLine($"Top-level request: {session.Request.UserRequest}");
        builder.AppendLine($"Orchestration session id: {session.SessionId}");
        builder.AppendLine($"Orchestrator model requirement: {OrchestratorModelRequirement}");
        builder.AppendLine($"Session heartbeat: {session.LastHeartbeatUtc:O}");
        builder.AppendLine($"Agent-scoped session id: {task.AgentSessionId}");
        builder.AppendLine($"Assigned objective: {task.Objective}");
        builder.AppendLine($"Role boundary: {profile.Role}");
        builder.AppendLine($"Execution phase: {task.PhaseName} ({task.PhaseOrder})");
        builder.AppendLine($"Task heartbeat: {task.LastHeartbeatUtc:O}");
        builder.AppendLine($"Orchestration resume required: {session.ResumeState.NeedsOrchestrationResume}");
        builder.AppendLine();
        builder.AppendLine("Incomplete supervisor actions:");
        builder.AppendLine($"- {string.Join("\n- ", session.ResumeState.IncompleteActionIds.DefaultIfEmpty("none"))}");
        builder.AppendLine();
        builder.AppendLine("Continuity constraints:");
        builder.AppendLine($"- Estimated tokens: {task.ContextWindowBudget.CurrentEstimatedTokens}/{task.ContextWindowBudget.MaxContextTokens}");
        builder.AppendLine($"- Warning threshold reached: {task.ContextWindowBudget.WarningReached}");
        builder.AppendLine($"- Hard limit reached: {task.ContextWindowBudget.HardLimitReached}");
        builder.AppendLine($"- Subscription low budget warning: {task.SubscriptionTokenBudget.LowBudgetWarning}");
        builder.AppendLine($"- Subscription exhausted: {task.SubscriptionTokenBudget.Exhausted}");
        builder.AppendLine($"- Automatic checkpoint recommended: {task.RecoveryState.PolicyState.AutoCheckpointRecommended}");
        builder.AppendLine($"- Automatic resume recommended: {task.RecoveryState.PolicyState.AutoResumeRecommended}");
        builder.AppendLine($"- Automatic retry recommended: {task.RecoveryState.PolicyState.AutoRetryRecommended}");
        builder.AppendLine();
        builder.AppendLine("Context:");
        builder.AppendLine(task.ContextSnapshot);

        if (blockingDependencies.Length > 0)
        {
            builder.AppendLine();
            builder.AppendLine($"Blocking dependencies not yet complete: {string.Join(", ", blockingDependencies)}");
        }

        if (latestCheckpoint is not null)
        {
            builder.AppendLine();
            builder.AppendLine($"Latest checkpoint: {latestCheckpoint.Summary}");
            builder.AppendLine("Reload memory into the fresh context window from:");
            builder.AppendLine($"- {string.Join("\n- ", latestCheckpoint.MemoryReloadItems)}");
        }

        if (session.ResumeState.RecommendedNextSteps.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Orchestration-level next steps:");
            builder.AppendLine($"- {string.Join("\n- ", session.ResumeState.RecommendedNextSteps)}");
        }

        builder.AppendLine();
        builder.AppendLine("If you are still alive and making progress, record heartbeat updates during long-running work so maintenance sweeps do not treat the task as silent.");

        return builder.ToString().Trim();
    }

    private static List<string> BuildNextSteps(AgentProfile profile, AgentWorkItem task, string[] blockingDependencies, bool shutdownRequired)
    {
        var nextSteps = new List<string>();
        if (shutdownRequired)
        {
            nextSteps.Add($"Shut down or close the named sub-agent '{task.AgentName}' for session '{task.AgentSessionId}'.");
            nextSteps.Add("Start a fresh named sub-agent only if additional work is assigned in a new task packet.");
        }
        else if (task.RecoveryState.NeedsResume)
        {
            nextSteps.Add($"Resume the task using persisted checkpoint memory before continuing: {task.RecoveryState.ResumeInstructions}");
        }
        else if (blockingDependencies.Length > 0)
        {
            nextSteps.Add($"Wait for blocking dependencies to complete: {string.Join(", ", blockingDependencies)}.");
        }
        else
        {
            nextSteps.Add($"Proceed with the assigned {profile.Domain} objective in the {task.PhaseName} wave.");
        }

        if (task.RecoveryState.PolicyState.AutoCheckpointRecommended)
        {
            nextSteps.Add("Automatic policy recommends checkpointing immediately.");
        }

        if (task.RecoveryState.PolicyState.AutoRetryRecommended)
        {
            nextSteps.Add("Automatic policy recommends retrying once connectivity is restored.");
        }

        if (!shutdownRequired)
        {
            nextSteps.Add("Record heartbeat updates during long-running work.");
            nextSteps.Add($"Record structured artifacts and handoff items with {profile.ToolName}.");
            nextSteps.Add("Keep orchestration/control-plane decisions with GPT-5.5 or an equivalent highest-capacity orchestrator context.");
        }

        return nextSteps;
    }

    private static string BuildLifecycleInstruction(AgentWorkItem task, bool shutdownRequired)
        => shutdownRequired
            ? $"Task complete. Close sub-agent '{task.AgentName}' and release session '{task.AgentSessionId}'."
            : $"Spawn or continue sub-agent '{task.AgentName}' with session '{task.AgentSessionId}'.";

    private static string BuildAgentName(AgentProfile profile, string taskId)
        => $"{profile.DisplayName} - {taskId}";

    private static string BuildAgentSessionId(string sessionId, AgentWorkItem task)
    {
        var sessionPrefix = sessionId.Length <= 8 ? sessionId : sessionId[..8];
        return $"{ToIdentifierSlug(task.AgentName)}-{sessionPrefix}";
    }

    private static string ToIdentifierSlug(string value)
    {
        var builder = new StringBuilder(value.Length);
        var previousWasSeparator = false;

        foreach (var character in value)
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
                previousWasSeparator = false;
                continue;
            }

            if (!previousWasSeparator && builder.Length > 0)
            {
                builder.Append('-');
                previousWasSeparator = true;
            }
        }

        return builder.ToString().Trim('-') is { Length: > 0 } slug ? slug : "agent";
    }

    private static string ExtractActionTargetId(string actionId)
    {
        var separatorIndex = actionId.LastIndexOf(':');
        return separatorIndex >= 0 && separatorIndex < actionId.Length - 1
            ? actionId[(separatorIndex + 1)..]
            : string.Empty;
    }

    private static bool ShouldAutoApplyDuringMaintenance(AgentWorkItem task)
        => task.RecoveryState.PolicyState.AutoCheckpointRecommended || task.RecoveryState.PolicyState.AutoRetryRecommended;

    private static string[] BuildMaintenanceFindings(SupervisorStatus status, OrchestrationSummary summary, OrchestrationSession session)
    {
        var findings = new List<string>();
        findings.AddRange(status.HeartbeatIssues.Select(issue => issue.Message));
        findings.AddRange(status.Alerts.Select(alert => alert.Message));
        if (summary.ResumeRequiredTaskIds.Count > 0)
        {
            findings.Add($"Resume required for tasks: {string.Join(", ", summary.ResumeRequiredTaskIds)}");
        }
        if (session.ResumeState.IncompleteActionIds.Count > 0)
        {
            findings.Add($"Incomplete supervisor actions: {string.Join(", ", session.ResumeState.IncompleteActionIds)}");
        }
        if (findings.Count == 0)
        {
            findings.Add("No maintenance findings.");
        }

        return [.. findings.Distinct(StringComparer.OrdinalIgnoreCase)];
    }

    private static string DetermineMaintenanceVerdict(SupervisorStatus status, OrchestrationSummary summary)
    {
        if (status.Alerts.Any(alert => string.Equals(alert.Severity, "critical", StringComparison.OrdinalIgnoreCase))
            || summary.ResumeRequiredTaskIds.Count > 0)
        {
            return "critical";
        }

        if (status.Alerts.Count > 0 || status.HeartbeatIssues.Count > 0 || summary.PendingTasks > 0)
        {
            return "warning";
        }

        return "healthy";
    }

    private static string BuildCronSummary(string sessionId, string verdict, SupervisorStatus status, OrchestrationSummary summary, List<string> autoApplied)
    {
        var parts = new List<string>
        {
            $"session={sessionId}",
            $"verdict={verdict}",
            $"heartbeat_issues={status.HeartbeatIssues.Count}",
            $"alerts={status.Alerts.Count}",
            $"resume_required={summary.ResumeRequiredTaskIds.Count}",
            $"pending_tasks={summary.PendingTasks}",
            $"auto_applied={autoApplied.Count}",
        };

        return string.Join("; ", parts);
    }

    private static MaintenanceSnapshot CreateMaintenanceSnapshot(string verdict, SupervisorStatus status, OrchestrationSummary summary, OrchestrationSession session, string cronSummary)
        => new()
        {
            SnapshotId = Guid.NewGuid().ToString("N"),
            RecordedAtUtc = DateTimeOffset.UtcNow,
            Verdict = verdict,
            HeartbeatIssueCount = status.HeartbeatIssues.Count,
            AlertCount = status.Alerts.Count,
            ResumeRequiredCount = summary.ResumeRequiredTaskIds.Count,
            IncompleteSupervisorActionCount = session.ResumeState.IncompleteActionIds.Count,
            CronSummary = cronSummary,
        };

    private static MaintenanceTrend DetermineMaintenanceTrend(MaintenanceSnapshot? previous, MaintenanceSnapshot current)
    {
        if (previous is null)
        {
            return MaintenanceTrend.Stable;
        }

        var previousScore = ComputeMaintenanceScore(previous);
        var currentScore = ComputeMaintenanceScore(current);
        if (currentScore > previousScore)
        {
            return MaintenanceTrend.Worsening;
        }

        if (currentScore < previousScore)
        {
            return MaintenanceTrend.Improving;
        }

        return MaintenanceTrend.Stable;
    }

    private static int ComputeMaintenanceScore(MaintenanceSnapshot snapshot)
    {
        var verdictWeight = snapshot.Verdict switch
        {
            "critical" => 100,
            "warning" => 50,
            _ => 0,
        };

        return verdictWeight
            + (snapshot.HeartbeatIssueCount * 5)
            + (snapshot.AlertCount * 4)
            + (snapshot.ResumeRequiredCount * 10)
            + (snapshot.IncompleteSupervisorActionCount * 3);
    }

    private static string BuildTrendSummary(MaintenanceSnapshot? previous, MaintenanceSnapshot current, MaintenanceTrend trend)
    {
        if (previous is null)
        {
            return "No previous maintenance snapshot exists yet; trend is stable by default.";
        }

        return trend switch
        {
            MaintenanceTrend.Improving => $"Maintenance health improved from {previous.Verdict} to {current.Verdict}.",
            MaintenanceTrend.Worsening => $"Maintenance health worsened from {previous.Verdict} to {current.Verdict}.",
            _ => $"Maintenance health is stable at {current.Verdict}.",
        };
    }

    private static bool IsTaskReady(OrchestrationSession session, AgentWorkItem task)
        => task.Dependencies.Where(dependency => dependency.Kind == DependencyKind.Blocking)
            .All(dependency => IsDependencyCompleted(session, dependency.TaskId));

    private static bool IsDependencyCompleted(OrchestrationSession session, string taskId)
        => session.Plan.Tasks.Any(task => task.TaskId.Equals(taskId, StringComparison.OrdinalIgnoreCase) && task.Status == AgentTaskStatus.Completed);

    private static void UpdateBudgets(AgentWorkItem task, int? currentEstimatedTokens, int? remainingSubscriptionTokens)
    {
        if (currentEstimatedTokens.HasValue)
        {
            task.ContextWindowBudget.CurrentEstimatedTokens = currentEstimatedTokens.Value;
            task.ContextWindowBudget.WarningReached = currentEstimatedTokens.Value >= task.ContextWindowBudget.WarningThresholdTokens;
            task.ContextWindowBudget.HardLimitReached = currentEstimatedTokens.Value >= task.ContextWindowBudget.HardLimitTokens;
        }

        if (remainingSubscriptionTokens.HasValue)
        {
            task.SubscriptionTokenBudget.RemainingTokens = remainingSubscriptionTokens.Value;
            task.SubscriptionTokenBudget.LowBudgetWarning = remainingSubscriptionTokens.Value <= task.SubscriptionTokenBudget.LowWatermark;
            task.SubscriptionTokenBudget.Exhausted = remainingSubscriptionTokens.Value <= 0;
        }
    }

    private static AgentCheckpoint CreateCheckpoint(AgentWorkItem task, string summary, IReadOnlyList<string>? memoryReloadItems)
        => new()
        {
            CheckpointId = Guid.NewGuid().ToString("N"),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            Summary = summary,
            ScratchpadSnapshot = task.Scratchpad,
            MemoryReloadItems = memoryReloadItems ?? BuildDefaultMemoryReloadItems(task),
            Artifacts = task.LatestResult?.Artifacts ?? [],
            HandoffItems = task.LatestResult?.HandoffItems ?? [],
        };

    private static ExecutionLedgerEntry CreateLedgerEntry(string category, string title, string details, string status, string? actionId = null)
        => new()
        {
            EntryId = Guid.NewGuid().ToString("N"),
            RecordedAtUtc = DateTimeOffset.UtcNow,
            Category = category,
            Title = title,
            Details = details,
            Status = status,
            ActionId = actionId,
        };

    private static SupervisorActionRecord CreateActionRecord(string actionId, string description, int expiresAfterMinutes = 30)
        => new()
        {
            ActionId = actionId,
            Description = description,
            State = SupervisorActionState.Pending,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            LastHeartbeatUtc = DateTimeOffset.UtcNow,
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(expiresAfterMinutes),
            Escalation = SupervisorActionEscalation.None,
        };

    private static SupervisorActionRecord[] MergeSupervisorActions(IReadOnlyList<SupervisorActionRecord> existing, IReadOnlyList<SupervisorActionRecord> proposed)
    {
        var merged = existing.ToDictionary(action => action.ActionId, StringComparer.OrdinalIgnoreCase);
        foreach (var record in proposed)
        {
            if (!merged.ContainsKey(record.ActionId))
            {
                merged[record.ActionId] = record;
            }
        }

        return [.. merged.Values.OrderBy(action => action.ActionId, StringComparer.OrdinalIgnoreCase)];
    }

    private static OrchestrationResumeState BuildResumeState(OrchestrationSession session)
    {
        var incompleteActions = session.SupervisorActions
            .Where(action => action.State is SupervisorActionState.Pending or SupervisorActionState.Acknowledged)
            .Select(action => action.ActionId)
            .ToArray();
        var nextSteps = session.SupervisorActions
            .Where(action => action.State is SupervisorActionState.Pending or SupervisorActionState.Acknowledged)
            .Select(action => action.Description)
            .ToArray();

        return new OrchestrationResumeState
        {
            NeedsOrchestrationResume = incompleteActions.Length > 0 || session.Plan.Tasks.Any(task => task.RecoveryState.NeedsResume),
            ResumeSummary = incompleteActions.Length == 0
                ? "No orchestration-level resume is currently required."
                : $"Pending supervisor actions: {string.Join(", ", incompleteActions)}",
            PendingActionIds = incompleteActions,
            RecommendedNextSteps = nextSteps,
            IncompleteActionIds = incompleteActions,
        };
    }

    private static void EvaluateAutomaticPolicies(AgentWorkItem task, AgentFailureKind? failureKind, bool networkRecovered)
    {
        var policy = task.RecoveryState.PolicyState;
        policy.AutoCheckpointRecommended = task.ContextWindowBudget.WarningReached || task.SubscriptionTokenBudget.LowBudgetWarning;
        policy.AutoResumeRecommended = failureKind is AgentFailureKind.ContextWindowLimit or AgentFailureKind.TokenBudgetLow or AgentFailureKind.SubscriptionTokensExhausted;
        policy.AutoRetryRecommended = failureKind == AgentFailureKind.NetworkLoss && networkRecovered && policy.RetryAttemptsUsed < policy.MaxRetryAttempts;

        policy.PolicyReason = failureKind switch
        {
            AgentFailureKind.ContextWindowLimit => "Context window exceeded safe budget; checkpoint and resume in a fresh context.",
            AgentFailureKind.TokenBudgetLow => "Token budget is low; checkpoint before the task is lost.",
            AgentFailureKind.SubscriptionTokensExhausted => "Subscription tokens are exhausted; preserve reload memory for later continuation.",
            AgentFailureKind.NetworkLoss => networkRecovered && policy.AutoRetryRecommended
                ? "Network recovered; one automatic retry is recommended."
                : "Network loss recorded; persist state and wait for recovery.",
            _ when task.ContextWindowBudget.WarningReached => "Context window warning threshold reached; proactive checkpoint recommended.",
            _ when task.SubscriptionTokenBudget.LowBudgetWarning => "Subscription tokens are low; proactive checkpoint recommended.",
            _ => string.Empty,
        };
    }

    private static IReadOnlyList<string> BuildDefaultMemoryReloadItems(AgentWorkItem task)
        =>
        [
            $"Objective: {task.Objective}",
            $"Phase: {task.PhaseName}",
            $"Acceptance criteria: {string.Join(" | ", task.AcceptanceCriteria)}",
            $"Scratchpad: {task.Scratchpad}",
            $"Latest result summary: {task.LatestResult?.Summary ?? "none"}",
        ];

    private static string BuildResumeInstructions(AgentWorkItem task, AgentFailureKind failureKind)
    {
        AgentCheckpoint? checkpoint = task.Checkpoints.Count > 0 ? task.Checkpoints[task.Checkpoints.Count - 1] : null;
        var memoryReload = checkpoint?.MemoryReloadItems ?? BuildDefaultMemoryReloadItems(task);
        return $"Start a fresh context window for {task.AgentToolName}, reload: {string.Join(" ; ", memoryReload)}, then continue the task from the latest checkpoint. Failure kind: {failureKind}.";
    }

    private static string AppendScratchpad(string scratchpad, string label, string value)
    {
        var entry = $"[{DateTimeOffset.UtcNow:O}] {label}: {value.Trim()}";
        return string.IsNullOrWhiteSpace(scratchpad) ? entry : $"{scratchpad}{Environment.NewLine}{entry}";
    }
}
