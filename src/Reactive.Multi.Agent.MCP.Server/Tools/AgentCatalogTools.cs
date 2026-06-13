using ModelContextProtocol.Server;
using Reactive.Multi.Agent.MCP.Core.Abstractions;
using Reactive.Multi.Agent.MCP.Server.Infrastructure;
using System.ComponentModel;

namespace Reactive.Multi.Agent.MCP.Server.Tools;

/// <summary>
/// Provides static methods for listing, searching, and retrieving specialist agent information from an agent catalog.
/// </summary>
/// <remarks>This class is intended for use with agent catalog management scenarios, such as discovering available
/// agents or obtaining detailed agent manifests. All methods are static and require an implementation of IAgentCatalog.
/// Thread safety depends on the underlying IAgentCatalog implementation.</remarks>
[McpServerToolType]
public sealed class AgentCatalogTools
{
    [McpServerTool(Name = "multiagent_agent_catalog_list"), Description("List all available specialist agents, including domain-specific C#, Reactive, UI-platform, MCP, CI, docs, and migration workers.")]
    public static string ListCatalog(IAgentCatalog agentCatalog)
        => McpSafeExecutor.ExecuteJson("multiagent_agent_catalog_list", () =>
        {
            ArgumentNullException.ThrowIfNull(agentCatalog);
            var agents = agentCatalog.GetAll();
            return new
            {
                count = agents.Count,
                agents,
            };
        });

    [McpServerTool(Name = "multiagent_agent_catalog_search"), Description("Search specialist agents by domain, category, skills, keywords, or tool names.")]
    public static string SearchCatalog(
        IAgentCatalog agentCatalog,
        [Description("Optional free-form search text such as 'reactiveui', 'ci pipeline', 'avalonia', or 'migration'.")] string? query = null)
        => McpSafeExecutor.ExecuteJson("multiagent_agent_catalog_search", () =>
        {
            ArgumentNullException.ThrowIfNull(agentCatalog);

            var results = agentCatalog.Search(query);
            return new
            {
                query,
                count = results.Count,
                agents = results,
            };
        });

    [McpServerTool(Name = "multiagent_agent_catalog_get"), Description("Get the full manifest for one specialist agent by id.")]
    public static string GetAgent(
        IAgentCatalog agentCatalog,
        [Description("The agent id such as csharp, reactiveui, mcp, ci, docs, migration, wpf, winforms, avalonia, maui, blazor, tester, or reviewer.")] string id)
        => McpSafeExecutor.ExecuteJson("multiagent_agent_catalog_get", () =>
        {
            ArgumentNullException.ThrowIfNull(agentCatalog);
            ArgumentException.ThrowIfNullOrWhiteSpace(id);

            var profile = agentCatalog.GetById(id) ?? throw new InvalidOperationException($"Unknown agent id '{id}'.");
            return profile;
        });
}
