using ModelContextProtocol.Server;
using Reactive.Multi.Agent.MCP.Core.Abstractions;
using Reactive.Multi.Agent.MCP.Server.Serialization;
using System.ComponentModel;

namespace Reactive.Multi.Agent.MCP.Server.Tools;

[McpServerToolType]
public sealed class AgentCatalogTools
{
    [McpServerTool(Name = "multiagent_agent_catalog_list"), Description("List all available specialist agents, including domain-specific C#, Reactive, UI-platform, MCP, CI, docs, and migration workers.")]
    public static string ListCatalog(IAgentCatalog agentCatalog)
    {
        ArgumentNullException.ThrowIfNull(agentCatalog);
        return JsonOutput.Serialize(new
        {
            count = agentCatalog.GetAll().Count,
            agents = agentCatalog.GetAll(),
        });
    }

    [McpServerTool(Name = "multiagent_agent_catalog_search"), Description("Search specialist agents by domain, category, skills, keywords, or tool names.")]
    public static string SearchCatalog(
        IAgentCatalog agentCatalog,
        [Description("Optional free-form search text such as 'reactiveui', 'ci pipeline', 'avalonia', or 'migration'.")] string? query = null)
    {
        ArgumentNullException.ThrowIfNull(agentCatalog);

        var results = agentCatalog.Search(query);
        return JsonOutput.Serialize(new
        {
            query,
            count = results.Count,
            agents = results,
        });
    }

    [McpServerTool(Name = "multiagent_agent_catalog_get"), Description("Get the full manifest for one specialist agent by id.")]
    public static string GetAgent(
        IAgentCatalog agentCatalog,
        [Description("The agent id such as csharp, reactiveui, mcp, ci, docs, migration, wpf, winforms, avalonia, maui, blazor, tester, or reviewer.")] string id)
    {
        ArgumentNullException.ThrowIfNull(agentCatalog);

        var profile = agentCatalog.GetById(id) ?? throw new InvalidOperationException($"Unknown agent id '{id}'.");
        return JsonOutput.Serialize(profile);
    }
}
