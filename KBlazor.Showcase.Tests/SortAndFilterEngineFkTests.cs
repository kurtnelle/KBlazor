using KBlazor.Attributes;
using KBlazor.Models;
using KBlazor.Showcase.Data;
using KBlazor.Showcase.Domain;
using Xunit;

namespace KBlazor.Showcase.Tests;

public class SortAndFilterEngineFkTests
{
    private static ListViewSetting SettingFilteringCustomer(Guid customerId)
    {
        var prop = new PropertySetting
        {
            PropertyInfo = typeof(PurchaseOrder).GetProperty(nameof(PurchaseOrder.Customer))!,
            Name = nameof(PurchaseOrder.Customer),
        };
        prop.Filter = customerId.ToString();

        var setting = new ListViewSetting();
        setting.DisplaySettings.Add(prop);
        return setting;
    }

    [Fact]
    public void ApplyFilter_NullableGuidFk_ReturnsOnlyMatchingRows_AndDoesNotThrow()
    {
        var store = SeedData.Create();
        var target = store.Orders.First().CustomerId!.Value;
        var expected = store.Orders.Count(o => o.CustomerId == target);

        var setting = SettingFilteringCustomer(target);

        var result = store.Orders.AsQueryable().ApplyFilter(setting).ToList();

        Assert.NotEmpty(result);
        Assert.Equal(expected, result.Count);
        Assert.All(result, o => Assert.Equal(target, o.CustomerId));
    }

    [Fact]
    public void ApplyFilter_NullableGuidFk_ExcludesRowsWithNullFk_WithoutThrowing()
    {
        var target = Guid.NewGuid();
        var rows = new List<PurchaseOrder>
        {
            new PurchaseOrder { Id = Guid.NewGuid(), Name = "match",   CustomerId = target },
            new PurchaseOrder { Id = Guid.NewGuid(), Name = "other",   CustomerId = Guid.NewGuid() },
            new PurchaseOrder { Id = Guid.NewGuid(), Name = "nullfk",  CustomerId = null },
        };

        var setting = SettingFilteringCustomer(target);

        var result = rows.AsQueryable().ApplyFilter(setting).ToList();

        Assert.Single(result);
        Assert.Equal("match", result[0].Name);
    }
}
