using Reactive.Multi.Agent.MCP.Core.Abstractions;
using Reactive.Multi.Agent.MCP.Knowledge.Services;

namespace Reactive.Multi.Agent.MCP.Tests;

public class CatalogLoadingTests
{
    [Test]
    public async Task EmbeddedCatalog_Loads_All_Domain_Specialist_Agents()
    {
        EmbeddedAgentCatalog catalog = new();

        await Assert.That(catalog.GetAll().Count).IsGreaterThanOrEqualTo(15);
        await Assert.That(catalog.GetById("reactiveui")).IsNotNull();
        await Assert.That(catalog.Search("avalonia").Any(profile => profile.Id == "avalonia")).IsTrue();
    }

    [Test]
    public async Task EmbeddedCatalog_Search_Returns_Category_Then_DisplayName_Sorted_Projection()
    {
        EmbeddedAgentCatalog catalog = new();

        var results = catalog.Search("agent");

        await Assert.That(results.Count).IsGreaterThan(1);
        for (var index = 1; index < results.Count; index++)
        {
            var previous = results[index - 1];
            var current = results[index];
            var categoryComparison = string.Compare(previous.Category, current.Category, StringComparison.OrdinalIgnoreCase);

            await Assert.That(categoryComparison <= 0).IsTrue();
            if (categoryComparison == 0)
            {
                await Assert.That(string.Compare(previous.DisplayName, current.DisplayName, StringComparison.OrdinalIgnoreCase) <= 0).IsTrue();
            }
        }
    }
}
