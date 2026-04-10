using Reactive.Multi.Agent.MCP.Core.Abstractions;
using Reactive.Multi.Agent.MCP.Knowledge.Services;

namespace Reactive.Multi.Agent.MCP.Tests;

public class CatalogLoadingTests
{
    [Test]
    public async Task EmbeddedCatalog_Loads_All_Domain_Specialist_Agents()
    {
        IAgentCatalog catalog = new EmbeddedAgentCatalog();

        await Assert.That(catalog.GetAll().Count).IsGreaterThanOrEqualTo(15);
        await Assert.That(catalog.GetById("reactiveui")).IsNotNull();
        await Assert.That(catalog.Search("avalonia").Any(profile => profile.Id == "avalonia")).IsTrue();
    }
}
