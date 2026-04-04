// KBlazor.Showcase/Services/InMemoryFlexTableSettings.cs
using KBlazor.Services;

namespace KBlazor.Showcase.Services;

public class InMemoryFlexTableSettings : IFlexTableSettings
{
    public bool EnablePersonalViews => true;
    public string[] AdminRoles => Array.Empty<string>();
}
