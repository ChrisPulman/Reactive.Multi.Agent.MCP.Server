namespace Reactive.Multi.Agent.MCP.Core.Models;

/// <summary>
/// Represents a snapshot of an agent's state at a specific point in time, including metadata, memory, and artifacts.
/// </summary>
/// <remarks>An agent checkpoint captures the essential information needed to restore or analyze the agent's
/// state. This includes a unique identifier, creation timestamp, a summary description, a snapshot of the agent's
/// scratchpad, memory reload items, artifacts, and any handoff items. Checkpoints are typically used for persistence,
/// recovery, or auditing purposes.</remarks>
public sealed class AgentCheckpoint
{
    /// <summary>
    /// Gets or sets the unique identifier for the checkpoint.
    /// </summary>
    public string CheckpointId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time when the entity was created, in Coordinated Universal Time (UTC).
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the summary text for the current object.
    /// </summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current snapshot of the scratchpad content.
    /// </summary>
    public string ScratchpadSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the collection of item names that are reloaded from memory.
    /// </summary>
    public IReadOnlyList<string> MemoryReloadItems { get; set; } = [];

    /// <summary>
    /// Gets or sets the collection of artifacts associated with the agent.
    /// </summary>
    public IReadOnlyList<AgentArtifact> Artifacts { get; set; } = [];

    /// <summary>
    /// Gets or sets the collection of handoff items associated with the current operation.
    /// </summary>
    /// <remarks>The collection represents the set of items to be transferred or processed as part of a
    /// handoff. Modifying this collection affects which items are included in the handoff process.</remarks>
    public IReadOnlyList<HandoffItem> HandoffItems { get; set; } = [];
}
