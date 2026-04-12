namespace Reactive.Multi.Agent.MCP.Core.Abstractions;

/// <summary>
/// Represents a catalog that provides access to agent profiles and supports retrieval and search operations.
/// </summary>
/// <remarks>Implementations of this interface are expected to provide read-only access to agent profile data.
/// Thread safety and performance characteristics may vary depending on the implementation.</remarks>
public interface IAgentCatalog
{
    /// <summary>
    /// Retrieves a read-only list of all agent profiles.
    /// </summary>
    /// <returns>A read-only list containing all available agent profiles. The list will be empty if no agent profiles exist.</returns>
    IReadOnlyList<AgentProfile> GetAll();

    /// <summary>
    /// Retrieves the agent profile associated with the specified identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the agent profile to retrieve. Cannot be null or empty.</param>
    /// <returns>An instance of AgentProfile if a profile with the specified identifier exists; otherwise, null.</returns>
    AgentProfile? GetById(string id);

    /// <summary>
    /// Searches for agent profiles that match the specified query string.
    /// </summary>
    /// <param name="query">The search query used to filter agent profiles. If null or empty, all agent profiles are returned.</param>
    /// <returns>A read-only list of agent profiles that match the search criteria. The list is empty if no profiles match.</returns>
    IReadOnlyList<AgentProfile> Search(string? query);
}
