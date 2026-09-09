using KBlazor.Models;
using KBlazor.Services;
using KBlazor.Showcase.Domain;
using Xunit;

namespace KBlazor.Showcase.Tests;

public class ListViewSettingWidthTests
{
    private static ListViewSetting ForPurchaseOrder() => new()
    {
        Name = "Default",
        ForEntity = typeof(PurchaseOrder).FullName!
    };

    [Fact]
    public void GetDefaultProperties_UsesEstimator_AndDoesNotLoadSystemDrawingCommon()
    {
        var props = ForPurchaseOrder().GetDefaultProperties("Helvetica Neue", 24.4f);

        Assert.NotEmpty(props);
        Assert.All(props, p => Assert.True(p.DisplayWidth > 0, $"{p.Name} has width {p.DisplayWidth}"));
        Assert.DoesNotContain(
            AppDomain.CurrentDomain.GetAssemblies(),
            a => a.GetName().Name == "System.Drawing.Common");
    }

    [Fact]
    public void GetDefaultProperties_WithFields_ReturnsRequestedColumnsInOrder()
    {
        var props = ForPurchaseOrder().GetDefaultProperties("Helvetica Neue", 24.4f, "Status,Order #");

        Assert.Equal(new[] { "Status", "Name" }, props.Select(p => p.Name).ToArray());
        Assert.All(props, p => Assert.True(p.DisplayWidth > 0));
    }

    private sealed class FixedMeasurer : ITextMeasurer
    {
        public float MeasureWidth(string text, string fontFamily, float fontSizePx) => 123f;
    }

    [Fact]
    public void GetDefaultProperties_HonorsSuppliedMeasurer()
    {
        var props = ForPurchaseOrder().GetDefaultProperties("Helvetica Neue", 24.4f, new FixedMeasurer());
        Assert.All(props, p => Assert.Equal(123, p.DisplayWidth));

        var withFields = ForPurchaseOrder().GetDefaultProperties("Helvetica Neue", 24.4f, "Order #", new FixedMeasurer());
        Assert.Single(withFields);
        Assert.Equal(123, withFields[0].DisplayWidth);
    }

    [Fact]
    public void GetTextSize_StringOverload_DelegatesToEstimator()
    {
        var viaExtension = "Customer".GetTextSize("Helvetica Neue", 24.4f);
        var viaEstimator = EstimatingTextMeasurer.Instance.MeasureWidth("Customer", "Helvetica Neue", 24.4f);
        Assert.Equal(viaEstimator, viaExtension);
    }
}
