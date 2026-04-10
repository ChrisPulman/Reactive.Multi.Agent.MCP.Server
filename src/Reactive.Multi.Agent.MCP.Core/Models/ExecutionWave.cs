namespace Reactive.Multi.Agent.MCP.Core.Models;

public sealed class ExecutionWave
{
    public int PhaseOrder { get; set; }

    public string PhaseName { get; set; } = string.Empty;

    public IReadOnlyList<string> TaskIds { get; set; } = [];
}
