using KBlazor.Models;
using KBlazor.Showcase.Data;
using KBlazor.Showcase.Domain;
using Xunit;

namespace KBlazor.Showcase.Tests;

public class StringFilterInMemoryTests
{
    private static PropertySetting NameFilter(string term)
    {
        var prop = new PropertySetting
        {
            PropertyInfo = typeof(PurchaseOrder).GetProperty(nameof(PurchaseOrder.Name))!,
            Name = nameof(PurchaseOrder.Name),
        };
        prop.Filter = term;
        return prop;
    }

    [Fact]
    public void GenerateWhere_StringColumn_InMemory_FiltersWithoutThrowing()
    {
        var store = SeedData.Create();
        var result = NameFilter("ORD-0041").GenerateWhere(store.Orders.AsQueryable()).ToList();
        Assert.Single(result);
        Assert.Equal("ORD-0041", result[0].Name);
    }

    [Fact]
    public void GenerateWhere_StringColumn_InMemory_IsCaseInsensitive()
    {
        var store = SeedData.Create();
        var result = NameFilter("ord-0041").GenerateWhere(store.Orders.AsQueryable()).ToList();
        Assert.Single(result);
        Assert.Equal("ORD-0041", result[0].Name);
    }
}
