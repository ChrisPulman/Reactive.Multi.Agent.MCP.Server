namespace Reactive.Multi.Agent.MCP.Core.Abstractions;

/// <summary>
/// Defines methods for loading, saving, and searching orchestration session data in a persistent store.
/// </summary>
/// <remarks>Implementations of this interface are responsible for managing the lifecycle and retrieval of
/// orchestration sessions. Thread safety and persistence guarantees depend on the specific implementation.</remarks>
public interface IOrchestrationSessionStore
{
    /// <summary>
    /// Retrieves the orchestration session associated with the specified session identifier.
    /// </summary>
    /// <param name="sessionId">The unique identifier of the orchestration session to load. Cannot be null or empty.</param>
    /// <returns>An instance of OrchestrationSession if a session with the specified identifier exists; otherwise, null.</returns>
    OrchestrationSession? Load(string sessionId);

    /// <summary>
    /// Persists the specified orchestration session to the underlying storage.
    /// </summary>
    /// <param name="session">The orchestration session to be saved. Cannot be null.</param>
    void Save(OrchestrationSession session);

    /// <summary>
    /// Searches the session history for entries that match the specified query.
    /// </summary>
    /// <param name="query">The search term to filter session history entries. If null or empty, all entries are considered.</param>
    /// <param name="limit">The maximum number of results to return. Must be greater than zero.</param>
    /// <returns>A read-only list of session history entries that match the query. The list contains at most the specified number
    /// of entries and may be empty if no matches are found.</returns>
    IReadOnlyList<SessionHistoryEntry> Search(string? query = null, int limit = 20);
}
