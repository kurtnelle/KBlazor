using KBlazor.Services;

namespace KBlazor.WasmSample.Services;

public class InMemoryFlexTableSettings : IFlexTableSettings
{
    public bool EnablePersonalViews => true;
    public string[] AdminRoles => Array.Empty<string>();
}
