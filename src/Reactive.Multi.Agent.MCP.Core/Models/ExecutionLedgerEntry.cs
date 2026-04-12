namespace Reactive.Multi.Agent.MCP.Core.Models;

/// <summary>
/// Represents a single entry in the execution ledger, capturing details about a recorded event or action.
/// </summary>
/// <remarks>Use this class to store and retrieve information about individual execution events, such as their
/// category, status, and associated metadata. This type is intended for scenarios where tracking and auditing of
/// execution steps or actions is required.</remarks>
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
