using KBlazor.Showcase.Data;
using KBlazor.Showcase.Domain;
using KBlazor.Showcase.Services;
using Xunit;

namespace KBlazor.Showcase.Tests;

public class InMemoryEntityLookupProviderTests
{
    private static InMemoryEntityLookupProvider MakeProvider()
    {
        var store = SeedData.Create();
        return new InMemoryEntityLookupProvider(store);
    }

    [Fact]
    public void IsKnownEntityType_Customer_ReturnsTrue()
    {
        var provider = MakeProvider();
        Assert.True(provider.IsKnownEntityType(typeof(Customer)));
    }

    [Fact]
    public void IsKnownEntityType_Unknown_ReturnsFalse()
    {
        var provider = MakeProvider();
        Assert.False(provider.IsKnownEntityType(typeof(string)));
    }

    [Fact]
    public void GetEntities_Customer_Returns150()
    {
        var provider = MakeProvider();
        var result = provider.GetEntities(typeof(Customer)).ToList();
        Assert.Equal(150, result.Count);
    }

    [Fact]
    public void GetEntitiesWithInclude_Customer_Returns150()
    {
        var provider = MakeProvider();
        // Include is a no-op for in-memory; just verify it doesn't throw
        var result = provider.GetEntitiesWithInclude(typeof(Customer), "Orders").ToList();
        Assert.Equal(150, result.Count);
    }
}
