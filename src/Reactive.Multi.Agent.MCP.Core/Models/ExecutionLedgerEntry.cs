namespace Reactive.Multi.Agent.MCP.Core.Models;

public sealed class ExecutionLedgerEntry
{
    public string EntryId { get; set; } = string.Empty;

    public DateTimeOffset RecordedAtUtc { get; set; }

    public string Category { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Details { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string? ActionId { get; set; }
}
