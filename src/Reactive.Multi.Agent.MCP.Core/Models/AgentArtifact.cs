namespace Reactive.Multi.Agent.MCP.Core.Models;

/// <summary>
/// Represents an artifact produced or used by an agent, including its identity, type, and associated metadata.
/// </summary>
/// <remarks>Use this class to describe files, links, or other resources generated or referenced by an agent. The
/// properties provide information such as the artifact's unique identifier, kind, display title, summary, file path,
/// URI, media type, and optional content. This type is intended for scenarios where agents need to communicate or
/// persist information about artifacts in a structured way.</remarks>
public sealed class AgentArtifact
{
    /// <summary>
    /// Gets or sets the unique identifier for the artifact.
    /// </summary>
    public string ArtifactId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the kind of artifact represented by this instance.
    /// </summary>
    public ArtifactKind Kind { get; set; } = ArtifactKind.Other;

    /// <summary>
    /// Gets or sets the title associated with the object.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the summary description.
    /// </summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the file system path associated with this instance.
    /// </summary>
    public string? FilePath { get; set; }
    
    /// <summary>
    /// Gets or sets the URI associated with this instance.
    /// </summary>
    public string? Uri { get; set; }

    /// <summary>
    /// Gets or sets the media type associated with the content.
    /// </summary>
    public string? MediaType { get; set; }

    /// <summary>
    /// Gets or sets the content associated with this instance.
    /// </summary>
    public string? Content { get; set; }
}
