namespace KBlazor.Services;

/// <summary>
/// Supplies the offset used to display stored <see cref="DateTime"/> values in
/// the user's local time. Optional: when no implementation is registered,
/// FlexTable applies no adjustment. Register <see cref="BrowserTimeZoneProvider"/>
/// to use the browser's timezone on Server or WebAssembly, or implement this to
/// read a cookie / user profile on the host.
/// </summary>
public interface IClientTimeZoneProvider
{
    /// <summary>
    /// Minutes to add to a stored DateTime to display it in the user's local time.
    /// </summary>
    ValueTask<int> GetOffsetMinutesAsync();
}
