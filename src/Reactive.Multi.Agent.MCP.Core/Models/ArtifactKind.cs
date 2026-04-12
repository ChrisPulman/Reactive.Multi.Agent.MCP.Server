namespace Reactive.Multi.Agent.MCP.Core.Models;

/// <summary>
/// Specifies the kinds of artifacts that can be managed or processed by the system.
/// </summary>
/// <remarks>Use this enumeration to categorize artifacts such as source files, configuration files, workflows,
/// documentation, and other related items. The values in this enumeration help distinguish between different artifact
/// types for processing, display, or organizational purposes.</remarks>
public enum ArtifactKind
{
    SourceFile,
    ConfigFile,
    Workflow,
    Documentation,
    Blueprint,
    MigrationPlan,
    Review,
    TestPlan,
    Prompt,
    Resource,
    PackageMetadata,
    Other,
}
