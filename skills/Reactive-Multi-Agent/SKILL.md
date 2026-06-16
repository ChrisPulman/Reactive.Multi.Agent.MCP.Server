---
name: reactive-multi-agent
description: Use when orchestrating work with CP.Reactive.Multi.Agent.MCP.Server, including creating durable multi-agent sessions, selecting specialist agents, spawning meaningful named sub-agents, recording checkpoints/results/heartbeats, applying recovery policies, finalizing sessions, and closing completed sub-agents.
---

# Reactive Multi Agent MCP Server

Use this skill when the Reactive Multi Agent MCP server is available and the task benefits from durable, dependency-aware specialist orchestration.

## Core Workflow

1. Discover specialists when needed with `multiagent_agent_catalog_list`, `multiagent_agent_catalog_search`, or `multiagent_agent_catalog_get`.
2. Keep the orchestration/control-plane role on GPT-5.5 or an equivalent highest-capacity model. Use smaller agents only for bounded specialist worker tasks after the session exists.
3. Create a session with `multiagent_orchestrate_request`. If the host surfaces creation tools more reliably than orchestration tools, call `multiagent_create_session`; it is an explicit alias with the same parameters and payload. This is the required first write tool before any worker tool. Pass the user request, constraints, desired artifacts, preferred agents, and a sensible `maxParallelAgents`.
4. Capture the returned `sessionId`, `plan.tasks[*].taskId`, `plan.tasks[*].agentId`, and `plan.tasks[*].agentToolName`; every worker tool call requires the relevant `sessionId` and `taskId`.
5. Read `session.plan.executionWaves` and work through waves by ascending `phaseOrder`.
6. Within one wave, dispatch ready independent tasks in parallel when the host supports parallel tool calls.
7. For each task, call the assigned worker tool to get an `AgentTaskPacket`.
8. Spawn or continue the sub-agent using `agentName` as the visible name and `agentSessionId` as the stable correlation id.
9. Give the sub-agent the packet `executionPrompt`, `acceptanceCriteria`, `suggestedSkills`, `suggestedTools`, artifact schema hint, and current `nextSteps`.
10. During long work, record progress with `multiagent_record_heartbeat`.
11. When the sub-agent has output, call the same worker tool with `workSummary`, `artifacts`, `handoffItems`, `risks`, and `markComplete: true`.
12. If the returned packet has `shutdownRequired: true`, close that named sub-agent immediately. Do not continue work in that agent context.
13. Periodically call `multiagent_supervisor_plan` and `multiagent_get_maintenance_report`.
14. When all required work is complete, call `multiagent_finalize_session` and use the unified response.

If a worker tool is available but no orchestration session exists yet, do not invent a `sessionId`. Call `multiagent_orchestrate_request` or `multiagent_create_session` first, then use the returned task metadata to call the worker tool.

## Agent Lifecycle Rules

- Always use the server-provided `agentName` when spawning or displaying a sub-agent.
- Keep `agentSessionId` with all notes, checkpoints, handoffs, and close operations for that specialist task.
- Treat `lifecycleInstruction` as authoritative.
- If `shutdownRequired` is false, spawn or continue the named sub-agent.
- If `shutdownRequired` is true, close the named sub-agent and release the session. Start a fresh sub-agent only after a new task packet asks for it.
- Never reuse a completed sub-agent for unrelated work.
- Preserve task boundaries. Each specialist should produce artifacts and handoffs only for its assigned objective.

## Worker Tools

Use the worker tool matching the task `agentId`:

- `architect`: `multiagent_architect_agent`
- `csharp`: `multiagent_csharp_agent`
- `reactive`: `multiagent_reactive_agent`
- `reactiveui`: `multiagent_reactiveui_agent`
- `mcp`: `multiagent_mcp_agent`
- `ci`: `multiagent_ci_agent`
- `docs`: `multiagent_docs_agent`
- `migration`: `multiagent_migration_agent`
- `wpf`: `multiagent_wpf_agent`
- `winforms`: `multiagent_winforms_agent`
- `avalonia`: `multiagent_avalonia_agent`
- `maui`: `multiagent_maui_agent`
- `blazor`: `multiagent_blazor_agent`
- `tester`: `multiagent_test_agent`
- `reviewer`: `multiagent_reviewer_agent`

Every worker tool supports the same operational pattern: activate task context, submit progress, record artifacts and handoffs, create checkpoints, report failures, apply completion, and return an updated `AgentTaskPacket`.

## Recovery

Use the worker tool with `createCheckpoint: true` and `checkpointSummary` when a sub-agent needs to pause or preserve state. Include `memoryReloadItems`, `currentEstimatedTokens`, and `remainingSubscriptionTokens` when known.

Report failures through the worker tool with:

- `failureKind: ContextWindowLimit`
- `failureKind: NetworkLoss`
- `failureKind: TokenBudgetLow`
- `failureKind: SubscriptionTokensExhausted`
- `failureKind: Unknown`

Then call `multiagent_apply_automatic_policy` or use `multiagent_supervisor_plan` with `autoApplyPolicies: true` when the host should apply checkpoint, retry, or resume guidance. Use `multiagent_resume_task` to continue a checkpointed task and `multiagent_resume_orchestration` when the orchestration-level resume state requires attention.

## Supervision And Maintenance

Use:

- `multiagent_session_status` to inspect session state, ready tasks, blocked tasks, and supervisor status.
- `multiagent_record_heartbeat` to keep long-running tasks and supervisor actions alive.
- `multiagent_update_supervisor_action` to acknowledge or complete supervisor actions.
- `multiagent_apply_supervisor_action_escalation` to escalate stale actions.
- `multiagent_run_maintenance_sweep` for an explicit diagnostic pass.
- `multiagent_get_maintenance_report` for trend-aware health output and optional automatic policy application.
- `multiagent_get_maintenance_history` to review prior reports.
- `multiagent_search_sessions` to find earlier sessions.

## Resources And Prompts

Use resources for read-only context:

- `multiagent://catalog`
- `multiagent://session/{sessionId}`
- `multiagent://history/recent`
- `multiagent://architecture/hub-and-spoke`
- `multiagent://schemas/artifacts`

Use prompts for guided orchestration:

- `create_multi_agent_plan`
- `create_specialist_agent_prompt`
- `merge_multi_agent_results`

## Result Shape

Return structured `AgentArtifact` objects with `artifactId`, `kind`, `title`, `summary`, and optional `filePath`, `uri`, `mediaType`, and `content`.

Return `HandoffItem` objects with `itemId`, `category`, `title`, `details`, and `isBlocking`.

For tests in this repository, use TUnit and TUnit assertions. When the `Mtpunittestmcp` MCP server is available, use it to run or inspect TUnit coverage.

## Safe Error Handling

The server returns safe JSON error envelopes for tool failures with `ok: false`, `operation`, `error.type`, `error.message`, and `guidance`. Treat those as actionable responses rather than transport failures. Ask the user for missing required values when the safe error reports blank input.

## Packaging

The MCP server package ships this skill at `skills/Reactive-Multi-Agent/SKILL.md` along with `README.md` and `.mcp/server.json`.
