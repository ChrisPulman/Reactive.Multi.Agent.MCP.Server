using Reactive.Multi.Agent.MCP.Core.Models;

namespace Reactive.Multi.Agent.MCP.Core.Abstractions;

public interface IOrchestrationSessionStore
{
    OrchestrationSession? Load(string sessionId);

    void Save(OrchestrationSession session);

    IReadOnlyList<SessionHistoryEntry> Search(string? query = null, int limit = 20);
}
