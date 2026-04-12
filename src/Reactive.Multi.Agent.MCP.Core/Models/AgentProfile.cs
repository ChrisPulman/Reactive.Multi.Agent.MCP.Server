namespace Reactive.Multi.Agent.MCP.Core.Models;

/// <summary>
/// Represents the profile information for an agent, including identification, categorization, and configuration details
/// used for agent management and routing.
/// </summary>
/// <remarks>The AgentProfile class encapsulates metadata and configuration settings that describe an agent's
/// capabilities, domain, and routing preferences. It is typically used to register, configure, or query agent
/// characteristics within a system that manages multiple agents. All properties are required to be set to valid,
/// non-null values for correct operation.</remarks>
public sealed class AgentProfile
{
    public string Id { get; set; } = string.Empty;

    public string Domain { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public string ToolName { get; set; } = string.Empty;

    public IReadOnlyList<string> DefaultSkills { get; set; } = [];

    public IReadOnlyList<string> DefaultTools { get; set; } = [];

    public IReadOnlyList<string> RoutingKeywords { get; set; } = [];

    public string CompletionContract { get; set; } = string.Empty;
}
