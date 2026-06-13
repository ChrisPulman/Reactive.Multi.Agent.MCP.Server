# Reactive Multi Agent MCP Server

<!-- mcp-name: io.github.chrispulman/reactive-multi-agent-mcp-server -->

Reactive Multi Agent MCP Server is a .NET 10 Model Context Protocol server for hub-and-spoke multi-agent orchestration inside GitHub Copilot Chat and any MCP-capable client.

It gives AI assistants a durable, structured orchestration layer for decomposing complex requests into typed tasks, routing them to domain-specialist agents, tracking execution waves with dependency awareness, recording structured artifacts, and persisting session state across restarts — all backed by a local SQLite store with no external dependencies.

It is implemented in C# on `net10.0` using `ModelContextProtocol` `1.4.0`.

## Quick Install

Click to install in your preferred environment:

[![VS Code - Install Reactive Multi Agent MCP](https://img.shields.io/badge/VS_Code-Install_Reactive_Multi_Agent_MCP-0098FF?style=flat-square&logo=visualstudiocode&logoColor=white)](https://vscode.dev/redirect/mcp/install?name=reactive-multi-agent-mcp-server&config=%7B%22type%22%3A%22stdio%22%2C%22command%22%3A%22dnx%22%2C%22args%22%3A%5B%22CP.Reactive.Multi.Agent.MCP.Server%401.*%22%2C%22--yes%22%5D%7D)
[![VS Code Insiders - Install Reactive Multi Agent MCP](https://img.shields.io/badge/VS_Code_Insiders-Install_Reactive_Multi_Agent_MCP-24bfa5?style=flat-square&logo=visualstudiocode&logoColor=white)](https://insiders.vscode.dev/redirect/mcp/install?name=reactive-multi-agent-mcp-server&config=%7B%22type%22%3A%22stdio%22%2C%22command%22%3A%22dnx%22%2C%22args%22%3A%5B%22CP.Reactive.Multi.Agent.MCP.Server%401.*%22%2C%22--yes%22%5D%7D&quality=insiders)
[![Visual Studio - Install Reactive Multi Agent MCP](https://img.shields.io/badge/Visual_Studio-Install_Reactive_Multi_Agent_MCP-5C2D91?style=flat-square&logo=visualstudio&logoColor=white)](https://vs-open.link/mcp-install?%7B%22name%22%3A%22CP.Reactive.Multi.Agent.MCP.Server%22%2C%22type%22%3A%22stdio%22%2C%22command%22%3A%22dnx%22%2C%22args%22%3A%5B%22CP.Reactive.Multi.Agent.MCP.Server%401.*%22%2C%22--yes%22%5D%7D)

> **Note:** These install links are prepared for the intended NuGet package identity `CP.Reactive.Multi.Agent.MCP.Server`.
> If the latest package has not been published yet, use the manual source-build configuration below.

## What Reactive Multi Agent helps with

Without structured orchestration, AI agents tackle complex requests sequentially, have no shared state between turns, and cannot hand work off between specialists. Reactive Multi Agent solves this by giving the AI host:

- **Decompose** any user request into typed tasks with agent assignments, execution phases, and dependency graphs — automatically
- **Route** each task to the right domain-specialist: C#, ReactiveUI, WPF, WinForms, Avalonia, MAUI, Blazor, MCP, CI, docs, migration, testing, or review
- **Parallelise** independent tasks in the same execution wave without extra orchestration code
- **Persist** session state durably in SQLite so sessions survive restarts and can be resumed mid-execution
- **Track** structured artifacts (source files, config, blueprints, test plans, reviews) and handoff items across agent boundaries
- **Recover** from context-window limits, network loss, and token budget exhaustion with automated checkpoint, retry, and resume policies
- **Supervise** in-flight sessions: detect stalled tasks, silent heartbeats, stale supervisor actions, and escalate automatically
- **Manage** specialist sub-agent lifecycles with meaningful `AgentName` values, stable `AgentSessionId` correlation, and explicit shutdown instructions when work completes
- **Audit** every maintenance sweep with trend-aware health reports (stable / improving / worsening) persisted per session

This server is intended for:
- Copilot Chat workflows that require multi-step, multi-specialist code generation across a full application
- AI agents building .NET solutions from scratch that span multiple UI frameworks or technology stacks
- Long-running orchestration sessions that need checkpoint/resume continuity across context resets
- Orchestration pipelines where structured artifacts and handoff items need to flow between agents

## Core concepts

Reactive Multi Agent organises work using a hub-and-spoke model with a central orchestration control plane:

| Term | Meaning |
|------|---------|
| **Session** | A single orchestration run identified by a stable `sessionId`. Persisted in SQLite. |
| **Plan** | The decomposed task graph produced from the user request: tasks, dependencies, execution waves, and parallel windows |
| **Execution wave** | A named phase grouping tasks that can run concurrently — earlier waves must complete before later ones begin |
| **Task / work item** | A single unit of work assigned to one specialist agent with acceptance criteria, skills, tools, and dependency links |
| **Specialist agent** | A named domain worker (e.g. `csharp`, `reactiveui`, `tester`) that executes a task and returns structured artifacts |
| **Agent-scoped session** | A stable `AgentSessionId` paired with a human-readable `AgentName` so MCP clients can spawn and close the correct specialist sub-agent |
| **Artifact** | A typed output from an agent: source file, config file, workflow, blueprint, migration plan, review, test plan, prompt, resource, or package metadata |
| **Handoff item** | A non-artifact communication item passed from one agent to downstream consumers — may be blocking or advisory |
| **Checkpoint** | A named snapshot of an agent's in-progress work that allows resumption after a context reset or failure |
| **Supervisor action** | A management action generated by the supervisor (retry, checkpoint, escalate) that must be acknowledged and completed |
| **Maintenance report** | A diagnostic snapshot of a session's health: heartbeat issues, stale tasks, incomplete supervisor actions, and trend classification |

### Execution wave model

Tasks within an execution wave are independent and can run in parallel. The orchestrator determines the wave assignment from the dependency graph it constructs during request decomposition.

```
Wave 1 ──► [architect]                  (blocking: all others depend on this)
Wave 2 ──► [csharp] [reactiveui] [mcp]  (independent — run in parallel)
Wave 3 ──► [tester] [docs]              (depend on wave 2 outputs)
Wave 4 ──► [reviewer]                   (depends on tester + docs)
```

### Failure and recovery model

Each task tracks a `RecoveryState` with automatic policy recommendations:

| Failure kind | Automatic policy |
|---|---|
| `ContextWindowLimit` | Checkpoint + reload from memory items |
| `NetworkLoss` | Retry when network recovered |
| `TokenBudgetLow` | Auto-checkpoint recommended |
| `SubscriptionTokensExhausted` | Pause and resume later |
| `Unknown` | Supervisor action required |

### Persistence

All session state is stored locally in SQLite — no external services or API keys are required.

| Path | Purpose |
|------|---------|
| `<LocalApplicationData>/ReactiveMultiAgentMcp/orchestration.sqlite3` | Full session state, tasks, supervisor actions, maintenance history when launched through the hosted MCP server |

---

## Agent protocol

When this server is active, agents should follow the **Reactive Multi Agent Protocol**:

1. Call `multiagent_orchestrate_request` with the user's top-level request to create the session and get the full execution plan.
2. Inspect `session.plan.executionWaves` — work through waves in `phaseOrder` order.
3. Within each wave, dispatch all tasks to their assigned specialist agent tools in parallel when the client supports parallel tool calls.
4. Each specialist agent tool call returns an `AgentTaskPacket` with `agentName`, `agentSessionId`, `executionPrompt`, `lifecycleInstruction`, and `nextSteps`. Use `agentName` as the visible sub-agent name and keep `agentSessionId` as the correlation id.
5. When a specialist agent produces output, call its tool again with `workSummary`, `artifacts`, `handoffItems`, `risks`, and `markComplete: true`.
6. If the returned packet has `shutdownRequired: true`, close the named sub-agent immediately and do not continue work in that agent context.
7. If an agent hits a context limit, call its tool with `createCheckpoint: true` and `checkpointSummary`, then later resume with `multiagent_resume_task`.
8. Periodically call `multiagent_supervisor_plan` to check for stalled tasks, silent heartbeats, and supervisor actions that need acknowledgement.
9. Call `multiagent_get_maintenance_report` on a schedule to receive trend-aware health diagnostics and auto-applied policy actions.
10. Call `multiagent_finalize_session` to merge all completed specialist outputs into a unified response.
11. Use the `create_multi_agent_plan`, `create_specialist_agent_prompt`, and `merge_multi_agent_results` prompts as guided shortcuts for the above workflow.

### Agent task packet lifecycle

Every worker tool returns an `AgentTaskPacket`. MCP clients should treat these fields as lifecycle instructions:

| Field | Meaning |
|-------|---------|
| `agentName` | Human-readable sub-agent name such as `Blazor Agent - task-1`; use this when spawning or displaying the specialist agent |
| `agentSessionId` | Stable agent-scoped id for correlating later updates, checkpoints, resumes, and shutdown |
| `lifecycleInstruction` | Short instruction telling the client whether to spawn/continue or close the sub-agent |
| `shutdownRequired` | `true` once the task is complete or the server has already marked the agent session for release |
| `completedAtUtc` | Completion timestamp set when `markComplete: true` records the final specialist result |
| `executionPrompt` | Either the work prompt for active tasks or a shutdown prompt for completed tasks |
| `nextSteps` | Ordered operational guidance for progressing, waiting, checkpointing, retrying, or closing the sub-agent |

When `shutdownRequired` is `true`, the correct client behavior is to close the named sub-agent and release the agent-scoped session. Start a fresh named sub-agent only after a later task packet asks for additional work.

### MCP safety contract

All tools and resources execute through a safe JSON wrapper. If a tool fails validation or throws, the server returns an error envelope instead of terminating the MCP host:

```json
{
  "ok": false,
  "operation": "multiagent_session_status",
  "error": {
    "type": "ArgumentException",
    "message": "sessionId is required."
  },
  "guidance": "The MCP server handled the failure and returned a safe error payload instead of terminating the host process."
}
```

Prompt failures return a safe fallback text response naming the failed operation and exception. Blank required parameters are reported in the same safe shape so clients can ask the user for the missing value.

---

## Available MCP tools

### Orchestrator tools

#### `multiagent_orchestrate_request`
Decomposes a user request into a full orchestration plan and creates a persisted session.

**Parameters:**
- `userRequest` — the top-level user request to decompose
- `constraints` *(optional)* — comma-separated constraints to apply during decomposition (e.g. `"no external packages,net10 only"`)
- `desiredArtifacts` *(optional)* — comma-separated list of desired output artifact types (e.g. `"source files,tests,docs"`)
- `preferredAgents` *(optional)* — comma-separated agent ids to prioritise during routing
- `maxParallelAgents` *(default: 4)* — maximum number of agents that may run concurrently within a wave

Returns: the created `OrchestrationSession` and an `OrchestrationSummary` with task status, ready tasks, and blocked tasks.

**When to use:** Call this once per user request at the start of every multi-agent workflow. The returned `sessionId` is required by all subsequent tool calls.

---

#### `multiagent_session_status`
Returns the current state of an orchestration session including supervisor status.

**Parameters:**
- `sessionId` — the session to inspect

Returns: the full `OrchestrationSession`, its `OrchestrationSummary`, and a `SupervisorStatus` snapshot with alerts and next-runnable tasks.

**When to use:** Call after completing one or more tasks to assess overall progress, surface newly unblocked tasks, and check for supervisor alerts before continuing.

---

#### `multiagent_finalize_session`
Evaluates the session, merges all completed specialist outputs, and produces a unified response.

**Parameters:**
- `sessionId` — the session to finalize

Returns: an `OrchestrationSummary` with `unifiedResponse`, completion counts, pending work, and coordination notes.

**When to use:** Call when all tasks in the final execution wave are complete. Can also be called at any intermediate point to get a partial merged view.

---

#### `multiagent_resume_task`
Resumes a task that was interrupted by a context limit, failure, or checkpoint.

**Parameters:**
- `sessionId` — the session containing the task
- `taskId` — the task to resume
- `agentId` — the agent resuming the task

Returns: an `AgentTaskPacket` containing the resumed execution prompt, checkpointed artifacts, and memory reload items.

**When to use:** Call after a task has been checkpointed or reported as failed with `ResumeRequired`. The returned execution prompt includes the prior work summary so the agent can continue from the checkpoint without full context reload.

---

#### `multiagent_resume_orchestration`
Evaluates and resumes an orchestration session that has stalled or been partially interrupted.

**Parameters:**
- `sessionId` — the session to resume

Returns: an `OrchestrationResumeState` with pending action IDs, recommended next steps, and incomplete action IDs.

**When to use:** Call when returning to a session after a connection drop or host restart to identify what needs to be resumed and in what order.

---

#### `multiagent_update_supervisor_action`
Updates the lifecycle state of a supervisor action.

**Parameters:**
- `sessionId` — the session containing the action
- `actionId` — the supervisor action ID to update
- `state` — the new state: `Pending`, `Acknowledged`, `Completed`, or `Abandoned`

**When to use:** Call when the client has received, acted on, or abandoned a supervisor action generated by `multiagent_supervisor_plan` or a maintenance sweep. Keeping action states current prevents duplicate alerts on subsequent supervisor evaluations.

---

#### `multiagent_apply_supervisor_action_escalation`
Applies time-based escalation to all pending supervisor actions in a session.

**Parameters:**
- `sessionId` — the session to evaluate
- `staleAfterMinutes` *(default: 30)* — minutes after which a pending action becomes stale
- `criticalAfterMinutes` *(default: 90)* — minutes after which a stale action becomes critical

Returns: updated escalation counts per action.

**When to use:** Call on a periodic schedule (e.g. alongside maintenance sweeps) to surface actions that have been pending too long and require immediate attention.

---

#### `multiagent_record_heartbeat`
Records a liveness heartbeat for a session, task, agent, or supervisor action.

**Parameters:**
- `sessionId` — the session to heartbeat
- `taskId` *(optional)* — specific task to heartbeat
- `agentId` *(optional)* — specific agent to heartbeat
- `actionId` *(optional)* — specific supervisor action to heartbeat
- `source` *(default: `"external"`)* — the heartbeat source identifier

**When to use:** Call regularly while an agent is actively working a task to prevent the supervisor from raising `SilentHeartbeat` alerts. Also call after reconnecting to mark the session as live.

---

#### `multiagent_run_maintenance_sweep`
Runs a full maintenance sweep over a session: detects silent heartbeats, stale tasks, and stale supervisor actions, and applies automated recovery policies.

**Parameters:**
- `sessionId` — the session to sweep
- `silentHeartbeatMinutes` *(default: 15)* — threshold for silent heartbeat detection
- `staleTaskMinutes` *(default: 30)* — threshold for stale task detection
- `staleActionMinutes` *(default: 30)* — threshold for stale supervisor action detection
- `criticalActionMinutes` *(default: 90)* — threshold for critical supervisor action escalation

Returns: a `MaintenanceReport` listing findings, recommended actions, auto-applied actions, heartbeat issues, resume-required task IDs, and incomplete supervisor action IDs.

**When to use:** Call on a periodic schedule to keep the session healthy. Suitable for a background cron-style polling loop.

---

#### `multiagent_get_maintenance_report`
Generates a diagnostic maintenance report for a session, optionally auto-applying recovery policies.

**Parameters:**
- `sessionId` — the session to diagnose
- `silentHeartbeatMinutes` *(default: 15)* — threshold for silent heartbeat detection
- `staleTaskMinutes` *(default: 30)* — threshold for stale tasks
- `staleActionMinutes` *(default: 30)* — threshold for stale supervisor actions
- `criticalActionMinutes` *(default: 90)* — threshold for critical escalation
- `autoApplyPolicies` *(default: false)* — automatically apply recommended recovery policies
- `networkRecovered` *(default: false)* — signal that network connectivity has been restored (triggers network-loss recovery policies)

Returns: a `MaintenanceReport` with verdict, findings, recommended actions, auto-applied actions, heartbeat issues, trend classification (`Stable` / `Improving` / `Worsening`), trend summary, and recent maintenance history snapshots.

**When to use:** Use this in preference to `multiagent_run_maintenance_sweep` when you want to inspect the report before committing to automated actions. Set `autoApplyPolicies: true` for fully autonomous maintenance.

---

#### `multiagent_get_maintenance_history`
Returns the persisted maintenance report history for a session.

**Parameters:**
- `sessionId` — the session to query
- `limit` *(default: 10)* — maximum number of recent snapshots to return

Returns: a list of `MaintenanceSnapshot` records, each containing verdict, heartbeat issue count, alert count, resume-required count, incomplete supervisor action count, and the cron summary.

**When to use:** Use to review the maintenance health trend of a session over time, or to present a diagnostic timeline to the user.

---

#### `multiagent_apply_automatic_policy`
Evaluates and applies the automatic recovery policy for a specific task and agent.

**Parameters:**
- `sessionId` — the session containing the task
- `taskId` — the task to evaluate
- `agentId` — the agent assigned to the task
- `currentEstimatedTokens` *(optional)* — current estimated token usage (for context-window budget evaluation)
- `remainingSubscriptionTokens` *(optional)* — remaining subscription tokens (for subscription budget evaluation)
- `networkRecovered` *(default: false)* — whether network connectivity has been restored

Returns: an `AutomaticPolicyState` indicating which automated actions were recommended or applied: `AutoCheckpointRecommended`, `AutoResumeRecommended`, `AutoRetryRecommended`, along with retry attempt counts and a `PolicyReason`.

**When to use:** Call when an agent encounters a potential failure condition (token pressure, network loss) and you want the server to compute the appropriate policy response before deciding how to proceed.

---

#### `multiagent_search_sessions`
Searches persisted orchestration sessions by query text.

**Parameters:**
- `query` *(optional)* — free-form text to filter sessions by; omit to return all recent sessions
- `limit` *(default: 20)* — maximum results to return

Returns: a list of matching `SessionHistoryEntry` records with session IDs, request summaries, status, task counts, and timestamps.

**When to use:** Use to locate a previous session to resume, or to present a history list to the user.

---

#### `multiagent_supervisor_status`
Returns the current supervisor evaluation for a session: alerts, stalled tasks, next runnable tasks, and heartbeat issues.

**Parameters:**
- `sessionId` — the session to evaluate
- `stalledAfterMinutes` *(default: 30)* — threshold for stalled task detection

Returns: a `SupervisorStatus` with `Alerts` (each with a `SupervisorAlertKind`), `StalledTaskIds`, `NextRunnableTasks`, `HeartbeatIssues`, and `Recommendations`.

Alert kinds include: `StalledTask`, `ResumeRequired`, `AutoCheckpointRecommended`, `AutoRetryRecommended`, `BlockedByDependency`, `StaleSupervisorAction`, `SilentHeartbeat`.

**When to use:** Call between execution waves to check for problems before dispatching the next set of tasks.

---

#### `multiagent_supervisor_plan`
Produces a prioritised action plan from the supervisor evaluation, optionally auto-applying recovery policies.

**Parameters:**
- `sessionId` — the session to plan for
- `stalledAfterMinutes` *(default: 30)* — stalled task threshold
- `autoApplyPolicies` *(default: false)* — automatically apply recommended policies
- `networkRecovered` *(default: false)* — signal network recovery

Returns: a `SupervisorActionPlan` with `OrderedActions`, `AutoAppliedActions`, `NextRunnableTasks`, and `ActionIds`.

**When to use:** Use instead of `multiagent_supervisor_status` when you want a prioritised to-do list rather than a raw status snapshot, especially when `autoApplyPolicies: true` is appropriate.

---

### Specialist agent tools

All specialist agent tools share the same parameter signature. Each tool activates or updates the task context for its domain. The agent executes its work, then calls the same tool again to submit results.

**Common parameters (all agent tools):**
- `sessionId` — the orchestration session ID
- `taskId` — the task ID assigned to this agent
- `additionalContext` *(optional)* — extra context to inject into the execution prompt
- `workLog` *(optional)* — running notes from the agent's work in progress
- `workSummary` *(optional)* — final work summary to record against the task
- `artifacts` *(optional)* — array of `AgentArtifact` objects produced by the agent
- `handoffItems` *(optional)* — array of `HandoffItem` objects for downstream agents
- `risks` *(optional)* — array of risk strings identified during the work
- `markComplete` *(default: false)* — mark the task as completed and submit all results
- `createCheckpoint` *(default: false)* — create a named checkpoint for later resumption
- `checkpointSummary` *(optional)* — description to attach to the checkpoint
- `memoryReloadItems` *(optional)* — key context strings to include in the resume prompt after a context reset
- `failureKind` *(default: `None`)* — report a failure: `ContextWindowLimit`, `NetworkLoss`, `TokenBudgetLow`, `SubscriptionTokensExhausted`, or `Unknown`
- `failureReason` *(optional)* — human-readable explanation for the failure
- `currentEstimatedTokens` *(optional)* — current estimated token count for budget tracking
- `remainingSubscriptionTokens` *(optional)* — remaining subscription tokens for budget tracking

**First call (activate):** Call with `sessionId` + `taskId` only (plus optional `additionalContext` / `workLog`) to receive the `AgentTaskPacket` with the `executionPrompt`.

**Subsequent call (submit results):** Call with `workSummary`, `artifacts`, `handoffItems`, `risks`, and `markComplete: true` to record the completed output.

**Checkpoint call:** Call with `createCheckpoint: true` and `checkpointSummary` to save progress. Follow with `multiagent_resume_task` after a context reset.

**Failure call:** Call with `failureKind` set to a non-`None` value to record the failure and trigger the automatic policy evaluator.

---

#### `multiagent_architect_agent`
Activate or update the **Architect Agent** — owns decomposition, system design, boundaries, and cross-agent coordination framing.

Skills: system design, dependency mapping, planning, trade-off analysis.

---

#### `multiagent_csharp_agent`
Activate or update the **C# Agent** — owns general C# and .NET implementation work when no narrower UI or domain agent is a better fit.

Skills: C#, .NET 10, project scaffolding, implementation.

---

#### `multiagent_reactive_agent`
Activate or update the **Reactive Agent** — owns Rx and stream-oriented orchestration work.

Skills: Reactive Extensions, observable composition, IObservable, stream-oriented architecture.

---

#### `multiagent_reactiveui_agent`
Activate or update the **ReactiveUI Agent** — owns ReactiveUI-specific implementation work.

Skills: ReactiveUI, MVVM, ReactiveCommand, WhenAnyValue, activation, routing, DynamicData.

---

#### `multiagent_mcp_agent`
Activate or update the **MCP Agent** — owns MCP tool, resource, prompt, and protocol-specific work.

Skills: ModelContextProtocol, tool authoring, resource definitions, prompt templates.

---

#### `multiagent_ci_agent`
Activate or update the **CI Agent** — owns pipeline, publishing, and automation work.

Skills: GitHub Actions, NuGet publishing, automated workflows, release pipelines.

---

#### `multiagent_docs_agent`
Activate or update the **Docs Agent** — owns README, onboarding, and usage documentation.

Skills: Markdown, README authoring, API documentation, installation guides.

---

#### `multiagent_migration_agent`
Activate or update the **Migration Agent** — owns modernization and upgrade planning.

Skills: .NET upgrade, framework migration, legacy modernization, migration plans.

---

#### `multiagent_wpf_agent`
Activate or update the **WPF Agent** — owns WPF and XAML-specific implementation work.

Skills: WPF, XAML, data binding, styles, control templates, WPF-specific ReactiveUI.

---

#### `multiagent_winforms_agent`
Activate or update the **WinForms Agent** — owns Windows Forms-specific implementation work.

Skills: WinForms, designer-generated code, data binding, control layout.

---

#### `multiagent_avalonia_agent`
Activate or update the **Avalonia Agent** — owns Avalonia-specific implementation work.

Skills: Avalonia UI, AXAML, cross-platform desktop, Avalonia ReactiveUI integration.

---

#### `multiagent_maui_agent`
Activate or update the **MAUI Agent** — owns MAUI-specific implementation work.

Skills: .NET MAUI, cross-platform mobile/desktop, XAML, Shell navigation, MAUI ReactiveUI.

---

#### `multiagent_blazor_agent`
Activate or update the **Blazor Agent** — owns Blazor and Razor-specific implementation work.

Skills: Blazor WebAssembly, Blazor Server, Razor components, interop, Blazor ReactiveUI.

---

#### `multiagent_test_agent`
Activate or update the **Test Agent** — owns verification, testing, and regression work.

Skills: TUnit, TUnit assertions, test design, coverage, regression verification.

---

#### `multiagent_reviewer_agent`
Activate or update the **Reviewer Agent** — owns critique, security, and readiness checks.

Skills: code review, security analysis, SOLID, API readiness, performance concerns.

---

### Agent catalog tools

#### `multiagent_agent_catalog_list`
Returns all specialist agent profiles registered in the embedded catalog.

Returns: `count` and `agents` array of `AgentProfile` records, each with `id`, `domain`, `category`, `displayName`, `summary`, `role`, `toolName`, `defaultSkills`, `defaultTools`, `routingKeywords`, and `completionContract`.

**When to use:** Use at the start of a session to understand what agents are available, or to present an agent selection menu to the user.

---

#### `multiagent_agent_catalog_search`
Searches the agent catalog by domain, category, skills, keywords, or tool names.

**Parameters:**
- `query` *(optional)* — free-form search text such as `"reactiveui"`, `"ci pipeline"`, `"avalonia"`, or `"migration"`; omit to return all

**When to use:** Use when you need to find the right agent for a specific technology or task domain before assigning work, or to let the user browse agents by topic.

---

#### `multiagent_agent_catalog_get`
Returns the full manifest for one specialist agent by ID.

**Parameters:**
- `id` — the agent ID: `architect`, `csharp`, `reactive`, `reactiveui`, `mcp`, `ci`, `docs`, `migration`, `wpf`, `winforms`, `avalonia`, `maui`, `blazor`, `tester`, or `reviewer`

**When to use:** Use before dispatching a task to a specific agent to verify its skills, tools, routing keywords, and completion contract.

---

## MCP resources

Resources are read-only snapshots exposed at stable URIs. Use them to inspect session and catalog state without modifying anything.

### `multiagent://catalog`
The complete embedded agent catalog as JSON. Returns all agent profiles with IDs, domains, skills, tools, and routing keywords.

### `multiagent://session/{sessionId}`
Full snapshot of one orchestration session: the `OrchestrationSession`, its `OrchestrationSummary`, `SupervisorStatus`, `SupervisorActionPlan`, `ExecutionLedger`, `ResumeState`, and `SupervisorActions`.

### `multiagent://history/recent`
The 20 most recent orchestration sessions as a list of `SessionHistoryEntry` records. Useful for presenting a session history picker.

### `multiagent://architecture/hub-and-spoke`
The architecture description for the hub-and-spoke orchestration model, including the control plane components: execution ledger, supervisor action lifecycle, orchestration-level resume state, and task-level checkpoint/retry/resume continuity.

### `multiagent://schemas/artifacts`
An example schema for structured `AgentArtifact` and `HandoffItem` objects. Use to understand the expected structure before having agents emit artifacts.

---

## MCP prompts

Prompts provide guided shortcuts for the most common orchestration workflows.

### `create_multi_agent_plan`
Creates a prompt that tells the MCP client how to orchestrate a single top-level request through the orchestrator tool and dependency-aware specialist agents.

**Parameters:**
- `userRequest` — the user's top-level request
- `constraints` *(optional)* — comma-separated constraints
- `desiredArtifacts` *(optional)* — comma-separated desired artifacts
- `preferredAgents` *(optional)* — comma-separated preferred agent IDs

Returns a step-by-step execution guide listing which tasks belong to each phase and can run in parallel.

---

### `create_specialist_agent_prompt`
Creates the isolated execution prompt for a specific specialist agent task inside an orchestration session.

**Parameters:**
- `sessionId` — the orchestration session ID
- `taskId` — the task ID
- `agentId` — the assigned agent ID

Returns the `executionPrompt` from the agent's `AgentTaskPacket` — ready to use as the context for the agent's work turn.

---

### `merge_multi_agent_results`
Creates a synthesis prompt that merges all currently recorded specialist outputs into one coherent answer.

**Parameters:**
- `sessionId` — the orchestration session ID

Returns a formatted synthesis prompt showing the session status, completed/total task counts, ready task IDs, and the current unified response.

---

## Artifact and handoff schemas

### AgentArtifact

| Field | Type | Description |
|-------|------|-------------|
| `artifactId` | string | Unique artifact identifier |
| `kind` | `ArtifactKind` | `SourceFile`, `ConfigFile`, `Workflow`, `Documentation`, `Blueprint`, `MigrationPlan`, `Review`, `TestPlan`, `Prompt`, `Resource`, `PackageMetadata`, `Other` |
| `title` | string | Human-readable title (e.g. `"Program.cs"`) |
| `summary` | string | One-sentence description of the artifact |
| `filePath` | string? | Optional file path for traceability |
| `uri` | string? | Optional URI reference |
| `mediaType` | string? | MIME type (e.g. `"text/plain"`, `"application/json"`) |
| `content` | string? | Optional inline content |

### HandoffItem

| Field | Type | Description |
|-------|------|-------------|
| `itemId` | string | Unique item identifier |
| `category` | string | Free-form category tag (e.g. `"review"`, `"dependency"`, `"config"`) |
| `title` | string | Short title |
| `details` | string | Full detail text |
| `isBlocking` | bool | Whether downstream agents must resolve this item before proceeding |

---

## Solution layout

```
README.md                                # Package README and feature guide
.mcp/server.json                         # MCP package manifest for dnx-based clients
skills/Reactive-Multi-Agent/SKILL.md     # Codex skill shipped with the MCP server package
src/
├── Reactive.Multi.Agent.MCP.Core/        # Models, abstractions, services, persistence
├── Reactive.Multi.Agent.MCP.Knowledge/   # Embedded agent catalog (JSON profiles)
├── Reactive.Multi.Agent.MCP.Server/      # MCP host, tool/resource/prompt registration
├── Reactive.Multi.Agent.MCP.Tests/       # Unit and integration tests
└── Reactive.Multi.Agent.MCP.Server.slnx  # Solution file
```

---

## Configuration

The hosted MCP server stores all data under the .NET `LocalApplicationData` folder by default, in a `ReactiveMultiAgentMcp` child directory. On Windows this is typically `%LocalAppData%\ReactiveMultiAgentMcp`. No external database, API key, or connection string is required.

| Path | Purpose |
|------|---------|
| `<LocalApplicationData>/ReactiveMultiAgentMcp/orchestration.sqlite3` | Full session state: tasks, supervisor actions, execution ledger, maintenance history |

The executable reads these environment variables at startup:

| Environment variable | Default | Purpose |
|----------------------|---------|---------|
| `REACTIVE_MULTI_AGENT_MCP_STATE_ROOT` | `<LocalApplicationData>/ReactiveMultiAgentMcp` | Root folder for persisted orchestration state |
| `REACTIVE_MULTI_AGENT_MCP_PACKAGE_ID` | `CP.Reactive.Multi.Agent.MCP.Server` | Package id reported by server metadata and install guidance |
| `REACTIVE_MULTI_AGENT_MCP_SERVER_ID` | `io.github.chrispulman/reactive-multi-agent-mcp-server` | MCP server identifier |

Configuration can also be overridden by providing a `ReactiveMultiAgentOptions` instance when hosting the server programmatically:

| Property | Default | Description |
|----------|---------|-------------|
| `StateRootPath` | Hosted default: `<LocalApplicationData>/ReactiveMultiAgentMcp`; direct options default: `~/.reactive-multi-agent-mcp` | Root folder for persisted orchestration state |
| `SessionDatabasePath` | `<StateRootPath>/orchestration.sqlite3` | SQLite database path |
| `PackageId` | `CP.Reactive.Multi.Agent.MCP.Server` | NuGet package identifier |
| `ServerId` | `io.github.chrispulman/reactive-multi-agent-mcp-server` | MCP server identifier |

The packaged `.mcp/server.json` manifest uses `runtimeHint: "dnx"` and sets `DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1` and `DOTNET_NOLOGO=1` for quieter client startup.

---

## Packaged Codex skill

The NuGet package includes `skills/Reactive-Multi-Agent/SKILL.md` alongside the MCP manifest. Use that skill when an agent needs operational instructions for this server: creating sessions, selecting specialists, spawning named sub-agents, recording heartbeats/checkpoints/results, applying recovery policies, finalizing sessions, and closing sub-agents once `shutdownRequired` is true.

---

## Build

```bash
dotnet build src/Reactive.Multi.Agent.MCP.Server.slnx
```

---

## Test

Build first, then run the TUnit test project:

```bash
dotnet test src/Reactive.Multi.Agent.MCP.Tests/Reactive.Multi.Agent.MCP.Tests.csproj
```

To collect coverage:

```bash
dotnet test src/Reactive.Multi.Agent.MCP.Tests/Reactive.Multi.Agent.MCP.Tests.csproj --coverage --coverage-output coverage.cobertura.xml --coverage-output-format cobertura
```

The test suite covers MCP host registration, tool/resource/prompt payload contracts, safe error handling, blank-parameter validation, orchestration decomposition, lifecycle and shutdown packets, supervisor actions, automatic policies, persistence, maintenance reports, session history, and package-facing metadata.

The latest verified local coverage for this suite is 98.08% line coverage and 90.64% branch coverage.

---

## Installation

### Requirements

- .NET 10 SDK
- An MCP-capable client (VS Code, Visual Studio, Claude Desktop, or any MCP 1.x host)

### Install as a .NET tool (recommended)

Once the NuGet package is published:

```bash
dotnet tool install -g CP.Reactive.Multi.Agent.MCP.Server
```

Then configure your MCP client:

```json
{
  "type": "stdio",
  "command": "reactive-multi-agent-mcp-server"
}
```

### Install via `dnx` (VS Code / Visual Studio quick install)

Use the badge links at the top of this file, or configure manually:

```json
{
  "type": "stdio",
  "command": "dnx",
  "args": ["CP.Reactive.Multi.Agent.MCP.Server@1.*", "--yes"]
}
```

### Manual configuration from source

Clone the repository and configure your MCP client to launch the server from the built output:

```json
{
  "name": "reactive-multi-agent-mcp-server",
  "type": "stdio",
  "command": "dotnet",
  "args": [
    "run",
    "--project",
    "/path/to/Reactive.Multi.Agent.MCP.Server/src/Reactive.Multi.Agent.MCP.Server/CP.Reactive.Multi.Agent.MCP.Server.csproj"
  ]
}
```

### VS Code (`settings.json`)

```json
{
  "mcp": {
    "servers": {
      "reactive-multi-agent-mcp-server": {
        "type": "stdio",
        "command": "dotnet",
        "args": [
          "run",
          "--project",
          "/path/to/Reactive.Multi.Agent.MCP.Server/src/Reactive.Multi.Agent.MCP.Server/CP.Reactive.Multi.Agent.MCP.Server.csproj"
        ]
      }
    }
  }
}
```

### Visual Studio (`mcp.json` or user settings)

Navigate to **Tools → Options → GitHub → Copilot → MCP Servers** and add:

```json
{
  "name": "CP.Reactive.Multi.Agent.MCP.Server",
  "type": "stdio",
  "command": "dotnet",
  "args": [
    "run",
    "--project",
    "/path/to/Reactive.Multi.Agent.MCP.Server/src/Reactive.Multi.Agent.MCP.Server/CP.Reactive.Multi.Agent.MCP.Server.csproj"
  ]
}
```

### Claude Desktop (`claude_desktop_config.json`)

```json
{
  "mcpServers": {
    "reactive-multi-agent-mcp-server": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "/path/to/Reactive.Multi.Agent.MCP.Server/src/Reactive.Multi.Agent.MCP.Server/CP.Reactive.Multi.Agent.MCP.Server.csproj"
      ]
    }
  }
}
```

---

## License

MIT
