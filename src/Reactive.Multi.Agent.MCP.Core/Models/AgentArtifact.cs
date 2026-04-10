namespace Reactive.Multi.Agent.MCP.Core.Models;

public sealed class AgentArtifact
{
    public string ArtifactId { get; set; } = string.Empty;

    public ArtifactKind Kind { get; set; } = ArtifactKind.Other;

    public string Title { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public string? FilePath { get; set; }

    public string? Uri { get; set; }

    public string? MediaType { get; set; }

    public string? Content { get; set; }
}
