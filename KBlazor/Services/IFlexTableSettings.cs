namespace KBlazor.Services;

public interface IFlexTableSettings
{
    bool EnablePersonalViews { get; }
    string[] AdminRoles { get; }
}
