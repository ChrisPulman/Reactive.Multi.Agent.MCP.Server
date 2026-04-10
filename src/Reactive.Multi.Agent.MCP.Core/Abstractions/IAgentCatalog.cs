using Reactive.Multi.Agent.MCP.Core.Models;

namespace Reactive.Multi.Agent.MCP.Core.Abstractions;

public interface IAgentCatalog
{
    IReadOnlyList<AgentProfile> GetAll();

    AgentProfile? GetById(string id);

    IReadOnlyList<AgentProfile> Search(string? query);
}
