using System.Reflection;
using KBlazor.Models;
using Xunit;

namespace KBlazor.Showcase.Tests;

public class PropertySettingSearchTests
{
    [Fact]
    public void EntitySearchText_Exists_AndIsJsonIgnored()
    {
        var prop = typeof(PropertySetting).GetProperty("EntitySearchText");
        Assert.NotNull(prop);
        // Must be [JsonIgnore] so UI scratch state never leaks into shared/persisted views.
        var jsonIgnore = prop!.GetCustomAttribute<Newtonsoft.Json.JsonIgnoreAttribute>();
        Assert.NotNull(jsonIgnore);
    }

    [Fact]
    public void EntitySearchText_IsReadWrite()
    {
        var setting = new PropertySetting { EntitySearchText = "abc" };
        Assert.Equal("abc", setting.EntitySearchText);
    }
}
