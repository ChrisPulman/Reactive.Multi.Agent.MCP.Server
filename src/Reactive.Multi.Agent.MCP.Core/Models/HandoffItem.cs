namespace Reactive.Multi.Agent.MCP.Core.Models;

public sealed class HandoffItem
{
    public string ItemId { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Details { get; set; } = string.Empty;

    public bool IsBlocking { get; set; }
}
