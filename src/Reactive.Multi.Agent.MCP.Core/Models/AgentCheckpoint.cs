namespace Reactive.Multi.Agent.MCP.Core.Models;

public sealed class AgentCheckpoint
{
    public string CheckpointId { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public string Summary { get; set; } = string.Empty;

    public string ScratchpadSnapshot { get; set; } = string.Empty;

    public IReadOnlyList<string> MemoryReloadItems { get; set; } = [];

    public IReadOnlyList<AgentArtifact> Artifacts { get; set; } = [];

    public IReadOnlyList<HandoffItem> HandoffItems { get; set; } = [];
}
