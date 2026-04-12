namespace Reactive.Multi.Agent.MCP.Core.Abstractions;

/// <summary>
/// Defines the contract for an orchestration service that manages sessions, tasks, supervision, maintenance, and agent
/// interactions within an orchestration workflow.
/// </summary>
/// <remarks>This interface provides methods for creating and managing orchestration sessions, tracking task and
/// agent progress, handling supervisor actions, performing maintenance operations, and recording results or failures.
/// Implementations are expected to coordinate distributed workflows, support monitoring and recovery scenarios, and
/// facilitate agent-based task execution. Thread safety and concurrency guarantees depend on the specific
/// implementation.</remarks>
public interface IOrchestrationService
{
    /// <summary>
    /// Creates a new orchestration session based on the specified request.
    /// </summary>
    /// <param name="request">The request containing the parameters and configuration for the orchestration session. Cannot be null.</param>
    /// <returns>An instance of OrchestrationSession representing the newly created session.</returns>
    OrchestrationSession CreateSession(OrchestrationRequest request);

    /// <summary>
    /// Retrieves the orchestration session associated with the specified session identifier.
    /// </summary>
    /// <param name="sessionId">The unique identifier of the session to retrieve. Cannot be null or empty.</param>
    /// <returns>The <see cref="OrchestrationSession"/> corresponding to the specified session identifier, or <see
    /// langword="null"/> if no matching session is found.</returns>
    OrchestrationSession? GetSession(string sessionId);

    /// <summary>
    /// Searches session history entries that match the specified query string.
    /// </summary>
    /// <param name="query">An optional search string used to filter session history entries. If null or empty, all sessions are returned up
    /// to the specified limit.</param>
    /// <param name="limit">The maximum number of session history entries to return. Must be greater than zero.</param>
    /// <returns>A read-only list of session history entries that match the search criteria. The list may be empty if no sessions
    /// match.</returns>
    IReadOnlyList<SessionHistoryEntry> SearchSessions(string? query = null, int limit = 20);

    /// <summary>
    /// Gets the current status of the supervisor for the specified session.
    /// </summary>
    /// <param name="sessionId">The unique identifier of the session for which to retrieve the supervisor status. Cannot be null or empty.</param>
    /// <param name="stalledAfterMinutes">The number of minutes of inactivity after which the supervisor is considered stalled. Must be a positive
    /// integer. The default is 30 minutes.</param>
    /// <returns>A SupervisorStatus value representing the current state of the supervisor for the specified session.</returns>
    SupervisorStatus GetSupervisorStatus(string sessionId, int stalledAfterMinutes = 30);

    /// <summary>
    /// Retrieves the supervisor action plan for the specified session, optionally applying policies and considering
    /// network recovery status.
    /// </summary>
    /// <param name="sessionId">The unique identifier of the session for which to retrieve the supervisor action plan. Cannot be null or empty.</param>
    /// <param name="stalledAfterMinutes">The number of minutes after which a session is considered stalled. Must be a positive integer. The default is 30
    /// minutes.</param>
    /// <param name="autoApplyPolicies">A value indicating whether supervisor policies should be automatically applied when generating the action plan.
    /// If set to <see langword="true"/>, policies are applied automatically; otherwise, they are not.</param>
    /// <param name="networkRecovered">A value indicating whether the network has recently recovered. If <see langword="true"/>, the action plan may
    /// include steps relevant to network recovery.</param>
    /// <returns>A <see cref="SupervisorActionPlan"/> representing the recommended actions for the supervisor based on the
    /// session state and provided options.</returns>
    SupervisorActionPlan GetSupervisorActionPlan(string sessionId, int stalledAfterMinutes = 30, bool autoApplyPolicies = false, bool networkRecovered = false);

    /// <summary>
    /// Resumes a previously suspended orchestration session using the specified session identifier.
    /// </summary>
    /// <param name="sessionId">The unique identifier of the orchestration session to resume. Cannot be null or empty.</param>
    /// <returns>An instance of OrchestrationSession representing the resumed session. Returns null if the session does not exist
    /// or cannot be resumed.</returns>
    OrchestrationSession ResumeOrchestration(string sessionId);

    /// <summary>
    /// Updates the state of a supervisor action within the specified orchestration session.
    /// </summary>
    /// <param name="sessionId">The unique identifier of the orchestration session containing the supervisor action to update. Cannot be null or
    /// empty.</param>
    /// <param name="actionId">The unique identifier of the supervisor action to update. Cannot be null or empty.</param>
    /// <param name="state">The new state to assign to the supervisor action.</param>
    /// <returns>An updated OrchestrationSession instance reflecting the changes to the supervisor action.</returns>
    OrchestrationSession UpdateSupervisorAction(string sessionId, string actionId, SupervisorActionState state);

    /// <summary>
    /// Applies escalation thresholds to a supervisor action for the specified orchestration session.
    /// </summary>
    /// <param name="sessionId">The unique identifier of the orchestration session to which the escalation thresholds will be applied. Cannot be
    /// null or empty.</param>
    /// <param name="staleAfterMinutes">The number of minutes after which the supervisor action is considered stale. Must be a positive integer. The
    /// default is 30 minutes.</param>
    /// <param name="criticalAfterMinutes">The number of minutes after which the supervisor action is considered critical. Must be greater than or equal to
    /// the value of staleAfterMinutes. The default is 90 minutes.</param>
    /// <returns>An OrchestrationSession object representing the updated session with the applied escalation thresholds.</returns>
    OrchestrationSession ApplySupervisorActionEscalation(string sessionId, int staleAfterMinutes = 30, int criticalAfterMinutes = 90);

    /// <summary>
    /// Records a heartbeat for the specified orchestration session, optionally associating it with a task, agent, or
    /// action.
    /// </summary>
    /// <param name="sessionId">The unique identifier of the orchestration session for which to record the heartbeat. Cannot be null or empty.</param>
    /// <param name="taskId">The identifier of the task associated with the heartbeat, if applicable. May be null if the heartbeat is not
    /// task-specific.</param>
    /// <param name="agentId">The identifier of the agent reporting the heartbeat, if applicable. May be null if not associated with a
    /// specific agent.</param>
    /// <param name="actionId">The identifier of the action associated with the heartbeat, if applicable. May be null if not related to a
    /// specific action.</param>
    /// <param name="source">The source of the heartbeat. Defaults to "external" if not specified.</param>
    /// <returns>An OrchestrationSession object representing the updated state of the session after recording the heartbeat.</returns>
    OrchestrationSession RecordHeartbeat(string sessionId, string? taskId = null, string? agentId = null, string? actionId = null, string source = "external");

    /// <summary>
    /// Performs a maintenance sweep for the specified orchestration session, identifying and updating the state of
    /// tasks and actions based on their activity and configured time thresholds.
    /// </summary>
    /// <remarks>Use this method to periodically check and update the health of orchestration sessions, tasks,
    /// and actions. Adjust the time thresholds to match the expected activity patterns of your workflows.</remarks>
    /// <param name="sessionId">The unique identifier of the orchestration session to process. Cannot be null or empty.</param>
    /// <param name="silentHeartbeatMinutes">The number of minutes after which a session with no heartbeat is considered silent. Must be a positive integer.
    /// The default is 15 minutes.</param>
    /// <param name="staleTaskMinutes">The number of minutes after which a task is considered stale if not updated. Must be a positive integer. The
    /// default is 30 minutes.</param>
    /// <param name="staleActionMinutes">The number of minutes after which an action is considered stale if not updated. Must be a positive integer. The
    /// default is 30 minutes.</param>
    /// <param name="criticalActionMinutes">The number of minutes after which an action is considered critical if not updated. Must be a positive integer.
    /// The default is 90 minutes.</param>
    /// <returns>An OrchestrationSession object representing the updated state of the session after the maintenance sweep.</returns>
    OrchestrationSession RunMaintenanceSweep(string sessionId, int silentHeartbeatMinutes = 15, int staleTaskMinutes = 30, int staleActionMinutes = 30, int criticalActionMinutes = 90);

    /// <summary>
    /// Generates a maintenance report summarizing the current system state and outstanding maintenance tasks based on
    /// the specified thresholds and options.
    /// </summary>
    /// <param name="sessionId">The unique identifier for the session for which the maintenance report is generated. Cannot be null or empty.</param>
    /// <param name="silentHeartbeatMinutes">The number of minutes after which a missing heartbeat is considered silent. Must be a positive integer.</param>
    /// <param name="staleTaskMinutes">The number of minutes after which a task is considered stale. Must be a positive integer.</param>
    /// <param name="staleActionMinutes">The number of minutes after which an action is considered stale. Must be a positive integer.</param>
    /// <param name="criticalActionMinutes">The number of minutes after which an action is considered critical. Must be a positive integer.</param>
    /// <param name="autoApplyPolicies">A value indicating whether maintenance policies should be automatically applied during report generation. If
    /// <see langword="true"/>, policies are applied; otherwise, they are not.</param>
    /// <param name="networkRecovered">A value indicating whether the network has recently recovered. If <see langword="true"/>, the report may include
    /// additional recovery actions.</param>
    /// <returns>A <see cref="MaintenanceReport"/> object containing the current maintenance status, including any detected
    /// issues and recommended actions.</returns>
    MaintenanceReport GetMaintenanceReport(string sessionId, int silentHeartbeatMinutes = 15, int staleTaskMinutes = 30, int staleActionMinutes = 30, int criticalActionMinutes = 90, bool autoApplyPolicies = false, bool networkRecovered = false);

    /// <summary>
    /// Retrieves a list of maintenance snapshots for the specified session.
    /// </summary>
    /// <param name="sessionId">The unique identifier of the session for which to retrieve maintenance history. Cannot be null or empty.</param>
    /// <param name="limit">The maximum number of maintenance snapshots to return. Must be greater than zero. The default is 10.</param>
    /// <returns>A read-only list of maintenance snapshots associated with the specified session. The list contains up to the
    /// specified limit of the most recent snapshots, or is empty if no history is available.</returns>
    IReadOnlyList<MaintenanceSnapshot> GetMaintenanceHistory(string sessionId, int limit = 10);

    /// <summary>
    /// Retrieves the task packet assigned to a specific agent within a given session and task context.
    /// </summary>
    /// <param name="sessionId">The unique identifier for the session in which the agent is operating. Cannot be null or empty.</param>
    /// <param name="taskId">The unique identifier of the task to retrieve. Cannot be null or empty.</param>
    /// <param name="agentId">The unique identifier of the agent for whom the task packet is requested. Cannot be null or empty.</param>
    /// <returns>An instance of AgentTaskPacket containing the details of the assigned task for the specified agent. Returns null
    /// if no matching task packet is found.</returns>
    AgentTaskPacket GetAgentTaskPacket(string sessionId, string taskId, string agentId);

    /// <summary>
    /// Activates a specified agent task within the given session and assigns it to the specified agent.
    /// </summary>
    /// <param name="sessionId">The unique identifier of the session in which the task is to be activated. Cannot be null or empty.</param>
    /// <param name="taskId">The unique identifier of the task to activate. Cannot be null or empty.</param>
    /// <param name="agentId">The unique identifier of the agent to whom the task will be assigned. Cannot be null or empty.</param>
    /// <param name="additionalContext">Optional. Additional context information to associate with the task activation. May be null.</param>
    /// <param name="workLog">Optional. A work log entry to record with the task activation. May be null.</param>
    /// <returns>An AgentTaskPacket representing the activated task and its assignment details.</returns>
    AgentTaskPacket ActivateAgentTask(string sessionId, string taskId, string agentId, string? additionalContext = null, string? workLog = null);

    /// <summary>
    /// Records the result of an agent's task execution and updates the task state accordingly.
    /// </summary>
    /// <remarks>If markComplete is set to true, the task will be marked as completed and no further updates
    /// will be accepted. Artifacts, handoff items, and risks are optional and can be omitted if not
    /// applicable.</remarks>
    /// <param name="sessionId">The unique identifier for the session in which the task was executed. Cannot be null or empty.</param>
    /// <param name="taskId">The unique identifier of the task whose result is being recorded. Cannot be null or empty.</param>
    /// <param name="agentId">The unique identifier of the agent that performed the task. Cannot be null or empty.</param>
    /// <param name="workSummary">An optional summary describing the work performed by the agent. May be null if no summary is provided.</param>
    /// <param name="artifacts">An optional list of artifacts produced by the agent during task execution. May be null or empty if no artifacts
    /// were generated.</param>
    /// <param name="handoffItems">An optional list of items to be handed off to subsequent processes or agents. May be null or empty if no handoff
    /// is required.</param>
    /// <param name="risks">An optional list of risk descriptions identified during task execution. May be null or empty if no risks were
    /// found.</param>
    /// <param name="markComplete">true to mark the task as complete after recording the result; otherwise, false to leave the task in its current
    /// state.</param>
    /// <returns>An AgentTaskPacket representing the updated state of the task after recording the agent's result.</returns>
    AgentTaskPacket RecordAgentResult(
        string sessionId,
        string taskId,
        string agentId,
        string? workSummary = null,
        IReadOnlyList<AgentArtifact>? artifacts = null,
        IReadOnlyList<HandoffItem>? handoffItems = null,
        IReadOnlyList<string>? risks = null,
        bool markComplete = false);

    /// <summary>
    /// Records a checkpoint for the specified agent task session, capturing the current progress and relevant state
    /// information.
    /// </summary>
    /// <remarks>Use this method to persist the current state of an agent task, enabling recovery or analysis
    /// at a later point. This is typically called after significant progress or at logical task boundaries.</remarks>
    /// <param name="sessionId">The unique identifier of the session for which the checkpoint is being recorded. Cannot be null or empty.</param>
    /// <param name="taskId">The unique identifier of the agent task associated with the checkpoint. Cannot be null or empty.</param>
    /// <param name="agentId">The unique identifier of the agent performing the task. Cannot be null or empty.</param>
    /// <param name="checkpointSummary">A summary describing the state or progress at the checkpoint. Cannot be null or empty.</param>
    /// <param name="memoryReloadItems">An optional list of memory item identifiers that should be reloaded as part of the checkpoint. If null, no
    /// memory items are reloaded.</param>
    /// <param name="currentEstimatedTokens">An optional estimate of the current number of tokens used in the session. If null, the estimate is not updated.</param>
    /// <param name="remainingSubscriptionTokens">An optional value indicating the remaining number of subscription tokens available. If null, the value is not
    /// updated.</param>
    /// <returns>An AgentTaskPacket representing the newly recorded checkpoint, including updated state and progress information.</returns>
    AgentTaskPacket RecordCheckpoint(
        string sessionId,
        string taskId,
        string agentId,
        string checkpointSummary,
        IReadOnlyList<string>? memoryReloadItems = null,
        int? currentEstimatedTokens = null,
        int? remainingSubscriptionTokens = null);

    /// <summary>
    /// Reports a task failure to the agent system and returns a packet describing the failure and related context.
    /// </summary>
    /// <param name="sessionId">The unique identifier for the session in which the task was executed. Cannot be null or empty.</param>
    /// <param name="taskId">The unique identifier of the task that has failed. Cannot be null or empty.</param>
    /// <param name="agentId">The unique identifier of the agent reporting the failure. Cannot be null or empty.</param>
    /// <param name="failureKind">The kind of failure that occurred, indicating the reason or category of the failure.</param>
    /// <param name="reason">A human-readable explanation of the failure. Provides additional context for diagnosing the issue. Cannot be
    /// null or empty.</param>
    /// <param name="memoryReloadItems">An optional list of memory item identifiers that should be reloaded as a result of the failure. May be null if
    /// no items require reloading.</param>
    /// <param name="currentEstimatedTokens">The current estimated number of tokens used at the time of failure, if available. May be null if not applicable.</param>
    /// <param name="remainingSubscriptionTokens">The estimated number of subscription tokens remaining after the failure, if available. May be null if not
    /// applicable.</param>
    /// <returns>An AgentTaskPacket containing details of the reported failure, including identifiers, failure kind, reason, and
    /// any relevant context.</returns>
    AgentTaskPacket ReportTaskFailure(
        string sessionId,
        string taskId,
        string agentId,
        AgentFailureKind failureKind,
        string reason,
        IReadOnlyList<string>? memoryReloadItems = null,
        int? currentEstimatedTokens = null,
        int? remainingSubscriptionTokens = null);

    /// <summary>
    /// Applies the automatic policy to the specified agent task and returns an updated task packet reflecting any
    /// policy-driven changes.
    /// </summary>
    /// <param name="sessionId">The unique identifier for the session in which the agent task is running. Cannot be null or empty.</param>
    /// <param name="taskId">The unique identifier of the agent task to which the policy will be applied. Cannot be null or empty.</param>
    /// <param name="agentId">The unique identifier of the agent for which the policy is being evaluated. Cannot be null or empty.</param>
    /// <param name="currentEstimatedTokens">The current estimated number of tokens consumed by the agent task, if available. Used to inform policy
    /// decisions. If null, the policy will proceed without this information.</param>
    /// <param name="remainingSubscriptionTokens">The number of tokens remaining in the subscription, if available. Used to determine if policy actions are
    /// required due to quota limits. If null, the policy will proceed without this information.</param>
    /// <param name="networkRecovered">Indicates whether network connectivity has recently been restored. Set to <see langword="true"/> if the network
    /// was previously unavailable and is now recovered; otherwise, <see langword="false"/>.</param>
    /// <returns>An updated <see cref="AgentTaskPacket"/> reflecting the results of applying the automatic policy to the
    /// specified agent task.</returns>
    AgentTaskPacket ApplyAutomaticPolicy(
        string sessionId,
        string taskId,
        string agentId,
        int? currentEstimatedTokens = null,
        int? remainingSubscriptionTokens = null,
        bool networkRecovered = false);

    /// <summary>
    /// Resumes a previously paused task for a specified agent within the given session.
    /// </summary>
    /// <param name="sessionId">The unique identifier of the session containing the task to resume. Cannot be null or empty.</param>
    /// <param name="taskId">The unique identifier of the task to resume. Cannot be null or empty.</param>
    /// <param name="agentId">The unique identifier of the agent for whom the task is to be resumed. Cannot be null or empty.</param>
    /// <returns>An AgentTaskPacket representing the resumed task, including its current state and relevant metadata.</returns>
    AgentTaskPacket ResumeTask(string sessionId, string taskId, string agentId);

    /// <summary>
    /// Finalizes the specified orchestration session and returns a summary of its execution.
    /// </summary>
    /// <param name="sessionId">The unique identifier of the session to finalize. Cannot be null or empty.</param>
    /// <returns>An OrchestrationSummary object containing details about the completed session.</returns>
    OrchestrationSummary FinalizeSession(string sessionId);
}
