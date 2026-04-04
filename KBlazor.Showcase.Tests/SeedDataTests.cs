// KBlazor.Showcase.Tests/SeedDataTests.cs
using KBlazor.Showcase.Data;
using Xunit;

namespace KBlazor.Showcase.Tests;

public class SeedDataTests
{
    [Fact]
    public void Seed_Produces20Orders()
    {
        var store = SeedData.Create();
        Assert.Equal(20, store.Orders.Count);
    }

    [Fact]
    public void Seed_Produces5Customers()
    {
        var store = SeedData.Create();
        Assert.Equal(5, store.Customers.Count);
    }

    [Fact]
    public void Seed_AllOrdersHaveValidCustomerId()
    {
        var store = SeedData.Create();
        var customerIds = store.Customers.Select(c => c.Id).ToHashSet();
        Assert.All(store.Orders, o => Assert.Contains(o.CustomerId!.Value, customerIds));
    }

    [Fact]
    public void Seed_AllStatusesRepresented()
    {
        var store = SeedData.Create();
        var statuses = store.Orders.Select(o => o.Status).Distinct().ToList();
        Assert.Equal(5, statuses.Count);
    }
}
