using ModelContextProtocol.Server;
using Reactive.Multi.Agent.MCP.Core.Abstractions;
using Reactive.Multi.Agent.MCP.Core.Models;
using Reactive.Multi.Agent.MCP.Server.Serialization;
using System.ComponentModel;

namespace Reactive.Multi.Agent.MCP.Server.Resources;

[McpServerResourceType]
public sealed class OrchestrationResources
{
    [McpServerResource(UriTemplate = "multiagent://catalog", Name = "Multi-Agent Catalog", MimeType = "application/json")]
    [Description("Read-only catalog of specialist multi-agent worker profiles.")]
    public static string GetCatalog(IAgentCatalog agentCatalog)
    {
        ArgumentNullException.ThrowIfNull(agentCatalog);
        return JsonOutput.Serialize(new { count = agentCatalog.GetAll().Count, agents = agentCatalog.GetAll() });
    }

    [McpServerResource(UriTemplate = "multiagent://session/{sessionId}", Name = "Multi-Agent Session", MimeType = "application/json")]
    [Description("Read-only orchestration session snapshot, including execution ledger, action lifecycle state, and orchestration resume state.")]
    public static string GetSession(IOrchestrationService orchestrationService, string sessionId)
    {
        ArgumentNullException.ThrowIfNull(orchestrationService);
        var session = orchestrationService.GetSession(sessionId)
            ?? throw new InvalidOperationException($"Unknown orchestration session '{sessionId}'.");
        var summary = orchestrationService.FinalizeSession(sessionId);
        var supervisor = orchestrationService.GetSupervisorStatus(sessionId);
        var plan = orchestrationService.GetSupervisorActionPlan(sessionId);
        return JsonOutput.Serialize(new { session, summary, supervisor, plan, session.ExecutionLedger, session.ResumeState, session.SupervisorActions });
    }

    [McpServerResource(UriTemplate = "multiagent://history/recent", Name = "Recent Orchestration History", MimeType = "application/json")]
    [Description("Read-only list of recent orchestration sessions for history/search workflows.")]
    public static string GetRecentHistory(IOrchestrationService orchestrationService)
    {
        ArgumentNullException.ThrowIfNull(orchestrationService);
        return JsonOutput.Serialize(orchestrationService.SearchSessions(null, 20));
    }

    [McpServerResource(UriTemplate = "multiagent://architecture/hub-and-spoke", Name = "Hub and Spoke Orchestration", MimeType = "application/json")]
    [Description("Read-only architecture description for the server's hub-and-spoke multi-agent orchestration model.")]
    public static string GetArchitecture()
        => JsonOutput.Serialize(new
        {
            model = "hub-and-spoke",
            controlPlane = new[]
            {
                "execution ledger",
                "supervisor action lifecycle tracking",
                "orchestration-level resume state",
                "task-level checkpoint/retry/resume continuity",
            },
        });

    [McpServerResource(UriTemplate = "multiagent://schemas/artifacts", Name = "Structured Artifact Schema", MimeType = "application/json")]
    [Description("Read-only schema example for structured artifacts and handoff items returned by specialist agents.")]
    public static string GetArtifactSchema()
        => JsonOutput.Serialize(new
        {
            artifacts = new[]
            {
                new AgentArtifact
                {
                    ArtifactId = "artifact-1",
                    Kind = ArtifactKind.SourceFile,
                    Title = "Program.cs",
                    Summary = "Server bootstrap entry point",
                    FilePath = "src/Example/Program.cs",
                    MediaType = "text/plain",
                },
            },
            handoffItems = new[]
            {
                new HandoffItem
                {
                    ItemId = "handoff-1",
                    Category = "review",
                    Title = "Review pipeline secrets",
                    Details = "CI publishing requires NuGet API key secret configuration.",
                    IsBlocking = true,
                },
            },
        });
}
