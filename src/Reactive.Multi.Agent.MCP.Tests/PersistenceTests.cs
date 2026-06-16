using Reactive.Multi.Agent.MCP.Core.Abstractions;
using Reactive.Multi.Agent.MCP.Core.Configuration;
using Reactive.Multi.Agent.MCP.Core.Models;
using Reactive.Multi.Agent.MCP.Core.Persistence;
using Reactive.Multi.Agent.MCP.Core.Services;
using Reactive.Multi.Agent.MCP.Knowledge.Services;

namespace Reactive.Multi.Agent.MCP.Tests;

public class PersistenceTests
{
    [Test]
    public async Task Session_Survives_Service_Recreation_Across_Process_Like_Restarts()
    {
        var options = new ReactiveMultiAgentOptions
        {
            StateRootPath = Path.Combine(Path.GetTempPath(), "reactive-multi-agent-mcp-tests", Guid.NewGuid().ToString("N")),
        };

        EmbeddedAgentCatalog catalog = new();
        RequestDecomposer decomposer = new(catalog);
        string sessionId;

        using (var store = new SqliteOrchestrationSessionStore(options))
        {
            OrchestrationService orchestration = new(decomposer, catalog, store);
            var session = orchestration.CreateSession(OrchestrationRequest.FromStrings("Build a Blazor app"));
            var task = session.Plan.Tasks.Single();

            orchestration.ActivateAgentTask(session.SessionId, task.TaskId, task.AgentId);
            orchestration.RecordAgentResult(
                session.SessionId,
                task.TaskId,
                task.AgentId,
                workSummary: "Created the Blazor starter shell.",
                artifacts:
                [
                    new AgentArtifact
                    {
                        ArtifactId = "artifact-shell",
                        Kind = ArtifactKind.SourceFile,
                        Title = "App.razor",
                        Summary = "Blazor shell entry point",
                        FilePath = "src/App.razor",
                    },
                ],
                markComplete: true);

            sessionId = session.SessionId;
        }

        using (var reopenedStore = new SqliteOrchestrationSessionStore(options))
        {
            OrchestrationService reopened = new(decomposer, catalog, reopenedStore);
            var reloaded = reopened.GetSession(sessionId);

            await Assert.That(reloaded).IsNotNull();
            await Assert.That(reloaded!.Plan.Tasks.Single().LatestResult).IsNotNull();
            await Assert.That(reloaded.Plan.Tasks.Single().LatestResult!.Artifacts.Single().Title).IsEqualTo("App.razor");
        }

        _ = options;
    }
}
