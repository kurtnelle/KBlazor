using Microsoft.JSInterop;

namespace KBlazor.Services;

/// <summary>
/// Reads the browser's timezone offset through kblazor.js. Requires
/// <c>&lt;script src="_content/KBlazor/kblazor.js"&gt;&lt;/script&gt;</c> in the host page.
/// Falls back to 0 when interop is unavailable (prerendering, missing script).
/// </summary>
public sealed class BrowserTimeZoneProvider : IClientTimeZoneProvider
{
    private readonly IJSRuntime _js;

    public BrowserTimeZoneProvider(IJSRuntime js)
    {
        _js = js;
    }

    public async ValueTask<int> GetOffsetMinutesAsync()
    {
        try
        {
            return await _js.InvokeAsync<int>("KBlazor.getTimezoneOffsetMinutes");
        }
        catch (Exception)
        {
            return 0;
        }
    }
}
