namespace Reactive.Multi.Agent.MCP.Core.Models;

/// <summary>
/// Represents an item to be handed off between processes or components, containing identifying and descriptive
/// information.
/// </summary>
/// <remarks>Use this class to encapsulate the details of a handoff operation, such as transferring work or data
/// between systems. The properties provide metadata and control information relevant to the handoff scenario.</remarks>
public sealed class HandoffItem
{
    public string ItemId { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Details { get; set; } = string.Empty;

    public bool IsBlocking { get; set; }
}
