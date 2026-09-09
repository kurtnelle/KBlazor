using KBlazor.Services;
using Xunit;

namespace KBlazor.Showcase.Tests;

public class EstimatingTextMeasurerTests
{
    private static readonly ITextMeasurer Sut = EstimatingTextMeasurer.Instance;

    [Fact]
    public void EmptyOrNullText_ReturnsZero()
    {
        Assert.Equal(0f, Sut.MeasureWidth(string.Empty, "Helvetica Neue", 24.4f));
        Assert.Equal(0f, Sut.MeasureWidth(null!, "Helvetica Neue", 24.4f));
    }

    [Fact]
    public void LongerText_IsWider()
    {
        var shortWidth = Sut.MeasureWidth("Hello", "Helvetica Neue", 24.4f);
        var longWidth = Sut.MeasureWidth("Hello World", "Helvetica Neue", 24.4f);
        Assert.True(longWidth > shortWidth, $"expected {longWidth} > {shortWidth}");
    }

    [Fact]
    public void WideGlyphs_AreWiderThanNarrowGlyphs()
    {
        var wide = Sut.MeasureWidth("MMMM", "Helvetica Neue", 24.4f);
        var narrow = Sut.MeasureWidth("iiii", "Helvetica Neue", 24.4f);
        Assert.True(wide > narrow, $"expected {wide} > {narrow}");
    }

    [Fact]
    public void Width_ScalesLinearlyWithFontSize()
    {
        var at12 = Sut.MeasureWidth("Order #", "Helvetica Neue", 12f);
        var at24 = Sut.MeasureWidth("Order #", "Helvetica Neue", 24f);
        Assert.Equal(at12 * 2f, at24, precision: 3);
    }

    [Fact]
    public void KnownString_MatchesFactorTable()
    {
        // "Ab 1." = A(0.66) + b(0.52) + space(0.28) + 1(0.55) + .(0.28) = 2.29 em
        var width = Sut.MeasureWidth("Ab 1.", "Helvetica Neue", 10f);
        Assert.Equal(22.9f, width, precision: 3);
    }

    [Fact]
    public void FontFamily_IsIgnoredByEstimator()
    {
        var a = Sut.MeasureWidth("Customer", "Helvetica Neue", 24.4f);
        var b = Sut.MeasureWidth("Customer", "Times New Roman", 24.4f);
        Assert.Equal(a, b);
    }
}
