namespace Reactive.Multi.Agent.MCP.Core.Configuration;

/// <summary>
/// Configuration options for the reactive multi-agent MCP server.
/// </summary>
public sealed class ReactiveMultiAgentOptions
{
    /// <summary>
    /// Gets or sets the root folder for persisted orchestration state.
    /// </summary>
    public string StateRootPath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".reactive-multi-agent-mcp");

    /// <summary>
    /// Gets the SQLite database path used for orchestration persistence.
    /// </summary>
    public string SessionDatabasePath => Path.Combine(this.StateRootPath, "orchestration.sqlite3");

    /// <summary>
    /// Gets or sets the package identifier used in install badges and packaging metadata.
    /// </summary>
    public string PackageId { get; set; } = "CP.Reactive.Multi.Agent.MCP.Server";

    /// <summary>
    /// Gets or sets the MCP server identifier.
    /// </summary>
    public string ServerId { get; set; } = "io.github.chrispulman/reactive-multi-agent-mcp-server";
}
