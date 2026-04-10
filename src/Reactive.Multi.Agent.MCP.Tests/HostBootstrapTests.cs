using Microsoft.Extensions.DependencyInjection;
using Reactive.Multi.Agent.MCP.Core.Abstractions;
using Reactive.Multi.Agent.MCP.Server;

namespace Reactive.Multi.Agent.MCP.Tests;

public class HostBootstrapTests
{
    [Test]
    public async Task CreateHost_Registers_Orchestration_Services()
    {
        using var host = Program.CreateHost([]);

        var agentCatalog = host.Services.GetRequiredService<IAgentCatalog>();
        var orchestration = host.Services.GetRequiredService<IOrchestrationService>();

        await Assert.That(agentCatalog.GetAll().Count).IsGreaterThanOrEqualTo(15);
        await Assert.That(orchestration).IsNotNull();
    }
}
