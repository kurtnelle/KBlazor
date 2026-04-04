// KBlazor.Showcase.Tests/SeedDataTests.cs
using KBlazor.Showcase.Data;
using KBlazor.Showcase.Domain;
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
        var seededStatuses = store.Orders.Select(o => o.Status).ToHashSet();
        var allStatuses = Enum.GetValues<OrderStatus>().ToHashSet();
        Assert.True(allStatuses.SetEquals(seededStatuses));
    }

    [Fact]
    public void Seed_AllOrdersHaveNavigationPropertyWired()
    {
        var store = SeedData.Create();
        Assert.All(store.Orders, o => Assert.NotNull(o.Customer));
    }
}
