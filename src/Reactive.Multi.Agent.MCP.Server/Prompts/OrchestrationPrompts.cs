using ModelContextProtocol.Server;
using Reactive.Multi.Agent.MCP.Core.Abstractions;
using Reactive.Multi.Agent.MCP.Core.Models;
using Reactive.Multi.Agent.MCP.Server.Infrastructure;
using System.ComponentModel;
using System.Text;

namespace Reactive.Multi.Agent.MCP.Server.Prompts;

[McpServerPromptType]
public sealed class OrchestrationPrompts
{
    [McpServerPrompt(Name = "create_multi_agent_plan"), Description("Create a prompt that tells the MCP client how to orchestrate a single top-level request through the orchestrator tool and dependency-aware specialist agents.")]
    public static string CreateMultiAgentPlan(
        IRequestDecomposer requestDecomposer,
        [Description("The user's top-level request.")] string userRequest,
        [Description("Optional comma-separated constraints.")] string? constraints = null,
        [Description("Optional comma-separated desired artifacts.")] string? desiredArtifacts = null,
        [Description("Optional comma-separated preferred agents.")] string? preferredAgents = null)
        => McpSafeExecutor.ExecutePrompt("create_multi_agent_plan", () =>
        {
            ArgumentNullException.ThrowIfNull(requestDecomposer);

            var request = OrchestrationRequest.FromStrings(userRequest, constraints, desiredArtifacts, preferredAgents);
            var plan = requestDecomposer.CreatePlan(request);

            var builder = new StringBuilder();
            builder.AppendLine("Use the Reactive Multi Agent MCP Server as follows:");
            builder.AppendLine("1. Call multiagent_orchestrate_request with the full user request.");
            builder.AppendLine("2. Work through execution waves in order.");
            builder.AppendLine("3. Run independent tasks in the same wave in parallel when possible.");
            builder.AppendLine("4. Record structured artifacts, risks, and handoff items with the specialist agent tools.");
            builder.AppendLine("5. Call multiagent_finalize_session to merge the final answer.");
            builder.AppendLine();
            foreach (var wave in plan.ExecutionWaves.OrderBy(wave => wave.PhaseOrder))
            {
                builder.AppendLine($"Phase {wave.PhaseOrder} — {wave.PhaseName}: {string.Join(", ", wave.TaskIds)}");
            }

            return builder.ToString().Trim();
        });

    [McpServerPrompt(Name = "create_specialist_agent_prompt"), Description("Create the isolated execution prompt for a specific specialist agent task inside an orchestration session.")]
    public static string CreateSpecialistAgentPrompt(
        IOrchestrationService orchestrationService,
        [Description("The orchestration session id.")] string sessionId,
        [Description("The task id.")] string taskId,
        [Description("The assigned agent id.")] string agentId)
        => McpSafeExecutor.ExecutePrompt("create_specialist_agent_prompt", () =>
        {
            ArgumentNullException.ThrowIfNull(orchestrationService);
            return orchestrationService.GetAgentTaskPacket(sessionId, taskId, agentId).ExecutionPrompt;
        });

    [McpServerPrompt(Name = "merge_multi_agent_results"), Description("Create a synthesis prompt that merges all currently recorded specialist outputs into one answer.")]
    public static string MergeMultiAgentResults(
        IOrchestrationService orchestrationService,
        [Description("The orchestration session id.")] string sessionId)
        => McpSafeExecutor.ExecutePrompt("merge_multi_agent_results", () =>
        {
            ArgumentNullException.ThrowIfNull(orchestrationService);
            var summary = orchestrationService.FinalizeSession(sessionId);

            var builder = new StringBuilder();
            builder.AppendLine("Merge the specialist results into one coherent response.");
            builder.AppendLine($"Status: {summary.Status}");
            builder.AppendLine($"Completed tasks: {summary.CompletedTasks}/{summary.TotalTasks}");
            builder.AppendLine($"Ready tasks: {string.Join(", ", summary.ReadyTaskIds)}");
            builder.AppendLine();
            builder.AppendLine(summary.UnifiedResponse);
            return builder.ToString().Trim();
        });
}
