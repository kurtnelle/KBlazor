using KBlazor.Services;

namespace KBlazor.Showcase.Services;

/// <summary>
/// Always-licensed provider for the Showcase demo site.
/// </summary>
public class DemoLicenseProvider : ILicenseProvider
{
    public bool IsLicensed => true;
    public string StatusMessage => "Showcase Demo — all features enabled";
    public string MachineFingerprint => "DEMO";
}
