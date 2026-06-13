using ModelContextProtocol.Server;
using Reactive.Multi.Agent.MCP.Core.Abstractions;
using Reactive.Multi.Agent.MCP.Core.Models;
using Reactive.Multi.Agent.MCP.Server.Infrastructure;
using System.ComponentModel;

namespace Reactive.Multi.Agent.MCP.Server.Tools;

/// <summary>
/// Provides static methods to activate or update various specialized agent task contexts within a multi-agent
/// orchestration environment.
/// </summary>
/// <remarks>Each method in this class corresponds to a specific agent type, such as Architect, CSharp, Reactive,
/// or CI, and facilitates the management of agent tasks by interacting with an orchestration service. These methods are
/// intended for use in scenarios where automated task delegation, progress tracking, or result recording is required
/// for different agent roles. The class is static and cannot be instantiated.</remarks>
[McpServerToolType]
public sealed class WorkerAgentTools
{
    [McpServerTool(Name = "multiagent_architect_agent"), Description("Activate or update the Architect Agent task context for planning, decomposition, and system-shaping work.")]
    public static string ArchitectAgent(IOrchestrationService orchestrationService, string sessionId, string taskId, string? additionalContext = null, string? workLog = null, string? workSummary = null, AgentArtifact[]? artifacts = null, HandoffItem[]? handoffItems = null, string[]? risks = null, bool markComplete = false, bool createCheckpoint = false, string? checkpointSummary = null, string[]? memoryReloadItems = null, AgentFailureKind failureKind = AgentFailureKind.None, string? failureReason = null, int? currentEstimatedTokens = null, int? remainingSubscriptionTokens = null)
        => DispatchAgent(orchestrationService, sessionId, taskId, "architect", additionalContext, workLog, workSummary, artifacts, handoffItems, risks, markComplete, createCheckpoint, checkpointSummary, memoryReloadItems, failureKind, failureReason, currentEstimatedTokens, remainingSubscriptionTokens);

    [McpServerTool(Name = "multiagent_csharp_agent"), Description("Activate or update the C# Agent task context for general .NET implementation and project generation.")]
    public static string CSharpAgent(IOrchestrationService orchestrationService, string sessionId, string taskId, string? additionalContext = null, string? workLog = null, string? workSummary = null, AgentArtifact[]? artifacts = null, HandoffItem[]? handoffItems = null, string[]? risks = null, bool markComplete = false, bool createCheckpoint = false, string? checkpointSummary = null, string[]? memoryReloadItems = null, AgentFailureKind failureKind = AgentFailureKind.None, string? failureReason = null, int? currentEstimatedTokens = null, int? remainingSubscriptionTokens = null)
        => DispatchAgent(orchestrationService, sessionId, taskId, "csharp", additionalContext, workLog, workSummary, artifacts, handoffItems, risks, markComplete, createCheckpoint, checkpointSummary, memoryReloadItems, failureKind, failureReason, currentEstimatedTokens, remainingSubscriptionTokens);

    [McpServerTool(Name = "multiagent_reactive_agent"), Description("Activate or update the Reactive Agent task context for Rx and stream-oriented orchestration work.")]
    public static string ReactiveAgent(IOrchestrationService orchestrationService, string sessionId, string taskId, string? additionalContext = null, string? workLog = null, string? workSummary = null, AgentArtifact[]? artifacts = null, HandoffItem[]? handoffItems = null, string[]? risks = null, bool markComplete = false, bool createCheckpoint = false, string? checkpointSummary = null, string[]? memoryReloadItems = null, AgentFailureKind failureKind = AgentFailureKind.None, string? failureReason = null, int? currentEstimatedTokens = null, int? remainingSubscriptionTokens = null)
        => DispatchAgent(orchestrationService, sessionId, taskId, "reactive", additionalContext, workLog, workSummary, artifacts, handoffItems, risks, markComplete, createCheckpoint, checkpointSummary, memoryReloadItems, failureKind, failureReason, currentEstimatedTokens, remainingSubscriptionTokens);

    [McpServerTool(Name = "multiagent_reactiveui_agent"), Description("Activate or update the ReactiveUI Agent task context for ReactiveUI-specific implementation work.")]
    public static string ReactiveUiAgent(IOrchestrationService orchestrationService, string sessionId, string taskId, string? additionalContext = null, string? workLog = null, string? workSummary = null, AgentArtifact[]? artifacts = null, HandoffItem[]? handoffItems = null, string[]? risks = null, bool markComplete = false, bool createCheckpoint = false, string? checkpointSummary = null, string[]? memoryReloadItems = null, AgentFailureKind failureKind = AgentFailureKind.None, string? failureReason = null, int? currentEstimatedTokens = null, int? remainingSubscriptionTokens = null)
        => DispatchAgent(orchestrationService, sessionId, taskId, "reactiveui", additionalContext, workLog, workSummary, artifacts, handoffItems, risks, markComplete, createCheckpoint, checkpointSummary, memoryReloadItems, failureKind, failureReason, currentEstimatedTokens, remainingSubscriptionTokens);

    [McpServerTool(Name = "multiagent_mcp_agent"), Description("Activate or update the MCP Agent task context for MCP tool/resource/prompt and protocol-specific work.")]
    public static string McpAgent(IOrchestrationService orchestrationService, string sessionId, string taskId, string? additionalContext = null, string? workLog = null, string? workSummary = null, AgentArtifact[]? artifacts = null, HandoffItem[]? handoffItems = null, string[]? risks = null, bool markComplete = false, bool createCheckpoint = false, string? checkpointSummary = null, string[]? memoryReloadItems = null, AgentFailureKind failureKind = AgentFailureKind.None, string? failureReason = null, int? currentEstimatedTokens = null, int? remainingSubscriptionTokens = null)
        => DispatchAgent(orchestrationService, sessionId, taskId, "mcp", additionalContext, workLog, workSummary, artifacts, handoffItems, risks, markComplete, createCheckpoint, checkpointSummary, memoryReloadItems, failureKind, failureReason, currentEstimatedTokens, remainingSubscriptionTokens);

    [McpServerTool(Name = "multiagent_ci_agent"), Description("Activate or update the CI Agent task context for pipeline, publishing, and automation work.")]
    public static string CiAgent(IOrchestrationService orchestrationService, string sessionId, string taskId, string? additionalContext = null, string? workLog = null, string? workSummary = null, AgentArtifact[]? artifacts = null, HandoffItem[]? handoffItems = null, string[]? risks = null, bool markComplete = false, bool createCheckpoint = false, string? checkpointSummary = null, string[]? memoryReloadItems = null, AgentFailureKind failureKind = AgentFailureKind.None, string? failureReason = null, int? currentEstimatedTokens = null, int? remainingSubscriptionTokens = null)
        => DispatchAgent(orchestrationService, sessionId, taskId, "ci", additionalContext, workLog, workSummary, artifacts, handoffItems, risks, markComplete, createCheckpoint, checkpointSummary, memoryReloadItems, failureKind, failureReason, currentEstimatedTokens, remainingSubscriptionTokens);

    [McpServerTool(Name = "multiagent_docs_agent"), Description("Activate or update the Docs Agent task context for README, onboarding, and usage documentation.")]
    public static string DocsAgent(IOrchestrationService orchestrationService, string sessionId, string taskId, string? additionalContext = null, string? workLog = null, string? workSummary = null, AgentArtifact[]? artifacts = null, HandoffItem[]? handoffItems = null, string[]? risks = null, bool markComplete = false, bool createCheckpoint = false, string? checkpointSummary = null, string[]? memoryReloadItems = null, AgentFailureKind failureKind = AgentFailureKind.None, string? failureReason = null, int? currentEstimatedTokens = null, int? remainingSubscriptionTokens = null)
        => DispatchAgent(orchestrationService, sessionId, taskId, "docs", additionalContext, workLog, workSummary, artifacts, handoffItems, risks, markComplete, createCheckpoint, checkpointSummary, memoryReloadItems, failureKind, failureReason, currentEstimatedTokens, remainingSubscriptionTokens);

    [McpServerTool(Name = "multiagent_migration_agent"), Description("Activate or update the Migration Agent task context for modernization and upgrade planning.")]
    public static string MigrationAgent(IOrchestrationService orchestrationService, string sessionId, string taskId, string? additionalContext = null, string? workLog = null, string? workSummary = null, AgentArtifact[]? artifacts = null, HandoffItem[]? handoffItems = null, string[]? risks = null, bool markComplete = false, bool createCheckpoint = false, string? checkpointSummary = null, string[]? memoryReloadItems = null, AgentFailureKind failureKind = AgentFailureKind.None, string? failureReason = null, int? currentEstimatedTokens = null, int? remainingSubscriptionTokens = null)
        => DispatchAgent(orchestrationService, sessionId, taskId, "migration", additionalContext, workLog, workSummary, artifacts, handoffItems, risks, markComplete, createCheckpoint, checkpointSummary, memoryReloadItems, failureKind, failureReason, currentEstimatedTokens, remainingSubscriptionTokens);

    [McpServerTool(Name = "multiagent_wpf_agent"), Description("Activate or update the WPF Agent task context for WPF/XAML-specific implementation work.")]
    public static string WpfAgent(IOrchestrationService orchestrationService, string sessionId, string taskId, string? additionalContext = null, string? workLog = null, string? workSummary = null, AgentArtifact[]? artifacts = null, HandoffItem[]? handoffItems = null, string[]? risks = null, bool markComplete = false, bool createCheckpoint = false, string? checkpointSummary = null, string[]? memoryReloadItems = null, AgentFailureKind failureKind = AgentFailureKind.None, string? failureReason = null, int? currentEstimatedTokens = null, int? remainingSubscriptionTokens = null)
        => DispatchAgent(orchestrationService, sessionId, taskId, "wpf", additionalContext, workLog, workSummary, artifacts, handoffItems, risks, markComplete, createCheckpoint, checkpointSummary, memoryReloadItems, failureKind, failureReason, currentEstimatedTokens, remainingSubscriptionTokens);

    [McpServerTool(Name = "multiagent_winforms_agent"), Description("Activate or update the WinForms Agent task context for Windows Forms-specific implementation work.")]
    public static string WinFormsAgent(IOrchestrationService orchestrationService, string sessionId, string taskId, string? additionalContext = null, string? workLog = null, string? workSummary = null, AgentArtifact[]? artifacts = null, HandoffItem[]? handoffItems = null, string[]? risks = null, bool markComplete = false, bool createCheckpoint = false, string? checkpointSummary = null, string[]? memoryReloadItems = null, AgentFailureKind failureKind = AgentFailureKind.None, string? failureReason = null, int? currentEstimatedTokens = null, int? remainingSubscriptionTokens = null)
        => DispatchAgent(orchestrationService, sessionId, taskId, "winforms", additionalContext, workLog, workSummary, artifacts, handoffItems, risks, markComplete, createCheckpoint, checkpointSummary, memoryReloadItems, failureKind, failureReason, currentEstimatedTokens, remainingSubscriptionTokens);

    [McpServerTool(Name = "multiagent_avalonia_agent"), Description("Activate or update the Avalonia Agent task context for Avalonia-specific implementation work.")]
    public static string AvaloniaAgent(IOrchestrationService orchestrationService, string sessionId, string taskId, string? additionalContext = null, string? workLog = null, string? workSummary = null, AgentArtifact[]? artifacts = null, HandoffItem[]? handoffItems = null, string[]? risks = null, bool markComplete = false, bool createCheckpoint = false, string? checkpointSummary = null, string[]? memoryReloadItems = null, AgentFailureKind failureKind = AgentFailureKind.None, string? failureReason = null, int? currentEstimatedTokens = null, int? remainingSubscriptionTokens = null)
        => DispatchAgent(orchestrationService, sessionId, taskId, "avalonia", additionalContext, workLog, workSummary, artifacts, handoffItems, risks, markComplete, createCheckpoint, checkpointSummary, memoryReloadItems, failureKind, failureReason, currentEstimatedTokens, remainingSubscriptionTokens);

    [McpServerTool(Name = "multiagent_maui_agent"), Description("Activate or update the MAUI Agent task context for MAUI-specific implementation work.")]
    public static string MauiAgent(IOrchestrationService orchestrationService, string sessionId, string taskId, string? additionalContext = null, string? workLog = null, string? workSummary = null, AgentArtifact[]? artifacts = null, HandoffItem[]? handoffItems = null, string[]? risks = null, bool markComplete = false, bool createCheckpoint = false, string? checkpointSummary = null, string[]? memoryReloadItems = null, AgentFailureKind failureKind = AgentFailureKind.None, string? failureReason = null, int? currentEstimatedTokens = null, int? remainingSubscriptionTokens = null)
        => DispatchAgent(orchestrationService, sessionId, taskId, "maui", additionalContext, workLog, workSummary, artifacts, handoffItems, risks, markComplete, createCheckpoint, checkpointSummary, memoryReloadItems, failureKind, failureReason, currentEstimatedTokens, remainingSubscriptionTokens);

    [McpServerTool(Name = "multiagent_blazor_agent"), Description("Activate or update the Blazor Agent task context for Blazor/Razor-specific implementation work.")]
    public static string BlazorAgent(IOrchestrationService orchestrationService, string sessionId, string taskId, string? additionalContext = null, string? workLog = null, string? workSummary = null, AgentArtifact[]? artifacts = null, HandoffItem[]? handoffItems = null, string[]? risks = null, bool markComplete = false, bool createCheckpoint = false, string? checkpointSummary = null, string[]? memoryReloadItems = null, AgentFailureKind failureKind = AgentFailureKind.None, string? failureReason = null, int? currentEstimatedTokens = null, int? remainingSubscriptionTokens = null)
        => DispatchAgent(orchestrationService, sessionId, taskId, "blazor", additionalContext, workLog, workSummary, artifacts, handoffItems, risks, markComplete, createCheckpoint, checkpointSummary, memoryReloadItems, failureKind, failureReason, currentEstimatedTokens, remainingSubscriptionTokens);

    [McpServerTool(Name = "multiagent_test_agent"), Description("Activate or update the Test Agent task context for verification, testing, and regression work.")]
    public static string TestAgent(IOrchestrationService orchestrationService, string sessionId, string taskId, string? additionalContext = null, string? workLog = null, string? workSummary = null, AgentArtifact[]? artifacts = null, HandoffItem[]? handoffItems = null, string[]? risks = null, bool markComplete = false, bool createCheckpoint = false, string? checkpointSummary = null, string[]? memoryReloadItems = null, AgentFailureKind failureKind = AgentFailureKind.None, string? failureReason = null, int? currentEstimatedTokens = null, int? remainingSubscriptionTokens = null)
        => DispatchAgent(orchestrationService, sessionId, taskId, "tester", additionalContext, workLog, workSummary, artifacts, handoffItems, risks, markComplete, createCheckpoint, checkpointSummary, memoryReloadItems, failureKind, failureReason, currentEstimatedTokens, remainingSubscriptionTokens, operationName: "multiagent_test_agent");

    [McpServerTool(Name = "multiagent_reviewer_agent"), Description("Activate or update the Reviewer Agent task context for critique, security, and readiness checks.")]
    public static string ReviewerAgent(IOrchestrationService orchestrationService, string sessionId, string taskId, string? additionalContext = null, string? workLog = null, string? workSummary = null, AgentArtifact[]? artifacts = null, HandoffItem[]? handoffItems = null, string[]? risks = null, bool markComplete = false, bool createCheckpoint = false, string? checkpointSummary = null, string[]? memoryReloadItems = null, AgentFailureKind failureKind = AgentFailureKind.None, string? failureReason = null, int? currentEstimatedTokens = null, int? remainingSubscriptionTokens = null)
        => DispatchAgent(orchestrationService, sessionId, taskId, "reviewer", additionalContext, workLog, workSummary, artifacts, handoffItems, risks, markComplete, createCheckpoint, checkpointSummary, memoryReloadItems, failureKind, failureReason, currentEstimatedTokens, remainingSubscriptionTokens);

    private static string DispatchAgent(
        IOrchestrationService orchestrationService,
        string sessionId,
        string taskId,
        string agentId,
        string? additionalContext,
        string? workLog,
        string? workSummary,
        AgentArtifact[]? artifacts,
        HandoffItem[]? handoffItems,
        string[]? risks,
        bool markComplete,
        bool createCheckpoint,
        string? checkpointSummary,
        string[]? memoryReloadItems,
        AgentFailureKind failureKind,
        string? failureReason,
        int? currentEstimatedTokens,
        int? remainingSubscriptionTokens,
        string? operationName = null)
        => McpSafeExecutor.ExecuteJson(operationName ?? $"multiagent_{agentId}_agent", () =>
        {
            ArgumentNullException.ThrowIfNull(orchestrationService);
            ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
            ArgumentException.ThrowIfNullOrWhiteSpace(taskId);

            if (failureKind != AgentFailureKind.None)
            {
                return orchestrationService.ReportTaskFailure(
                    sessionId,
                    taskId,
                    agentId,
                    failureKind,
                    string.IsNullOrWhiteSpace(failureReason) ? failureKind.ToString() : failureReason,
                    memoryReloadItems,
                    currentEstimatedTokens,
                    remainingSubscriptionTokens);
            }

            if (createCheckpoint)
            {
                return orchestrationService.RecordCheckpoint(
                    sessionId,
                    taskId,
                    agentId,
                    string.IsNullOrWhiteSpace(checkpointSummary) ? "Checkpoint recorded." : checkpointSummary,
                    memoryReloadItems,
                    currentEstimatedTokens,
                    remainingSubscriptionTokens);
            }

            return string.IsNullOrWhiteSpace(workSummary) && (artifacts is null || artifacts.Length == 0) && (handoffItems is null || handoffItems.Length == 0) && (risks is null || risks.Length == 0) && !markComplete
                ? orchestrationService.ActivateAgentTask(sessionId, taskId, agentId, additionalContext, workLog)
                : orchestrationService.RecordAgentResult(sessionId, taskId, agentId, workSummary, artifacts, handoffItems, risks, markComplete);
        });
}
