namespace Reactive.Multi.Agent.MCP.Core.Models;

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
