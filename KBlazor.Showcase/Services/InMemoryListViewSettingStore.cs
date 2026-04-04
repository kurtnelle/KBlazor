// KBlazor.Showcase/Services/InMemoryListViewSettingStore.cs
using KBlazor.Models;
using KBlazor.Services;

namespace KBlazor.Showcase.Services;

public class InMemoryListViewSettingStore : IListViewSettingStore
{
    private readonly List<ListViewSetting> _settings = new();

    public ListViewSetting? GetById(Guid id) =>
        _settings.FirstOrDefault(s => s.Id == id);

    public ListViewSetting? GetByUserAndView(string entityType, string userName, string viewName) =>
        _settings.FirstOrDefault(s =>
            s.ForEntity == entityType &&
            s.CustomizedForUser == userName &&
            s.Name == viewName);

    public ListViewSetting? GetByNameAndEntity(string viewName, string entityType) =>
        _settings.FirstOrDefault(s => s.Name == viewName && s.ForEntity == entityType);

    public Guid GetIdByNameAndEntity(string viewName, string entityType) =>
        _settings.FirstOrDefault(s => s.Name == viewName && s.ForEntity == entityType)?.Id ?? Guid.Empty;

    public void Add(ListViewSetting setting) => _settings.Add(setting);

    public void Update(ListViewSetting setting)
    {
        var index = _settings.FindIndex(s => s.Id == setting.Id);
        if (index >= 0) _settings[index] = setting;
    }

    public void Delete(Guid id) => _settings.RemoveAll(s => s.Id == id);

    public List<ListViewSetting> GetAllForEntity(string entityType, string? currentUsername) =>
        _settings.Where(s => s.ForEntity == entityType).ToList();

    public void SaveChanges() { /* no-op for in-memory */ }
}
