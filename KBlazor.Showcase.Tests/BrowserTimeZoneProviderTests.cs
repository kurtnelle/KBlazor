using KBlazor.Services;
using Microsoft.JSInterop;
using Xunit;

namespace KBlazor.Showcase.Tests;

public class BrowserTimeZoneProviderTests
{
    private sealed class FakeJs : IJSRuntime
    {
        private readonly Func<string, object> _handler;
        public FakeJs(Func<string, object> handler) => _handler = handler;

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => new((TValue)_handler(identifier));

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            => new((TValue)_handler(identifier));
    }

    [Fact]
    public async Task ReturnsOffsetFromJs()
    {
        var js = new FakeJs(id =>
        {
            Assert.Equal("KBlazor.getTimezoneOffsetMinutes", id);
            return -300;
        });
        var sut = new BrowserTimeZoneProvider(js);
        Assert.Equal(-300, await sut.GetOffsetMinutesAsync());
    }

    [Fact]
    public async Task ReturnsZero_WhenJsInteropFails()
    {
        var js = new FakeJs(_ => throw new InvalidOperationException("JavaScript interop calls cannot be issued at this time"));
        var sut = new BrowserTimeZoneProvider(js);
        Assert.Equal(0, await sut.GetOffsetMinutesAsync());
    }
}
