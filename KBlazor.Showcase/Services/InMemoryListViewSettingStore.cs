// KBlazor.Showcase/Services/InMemoryListViewSettingStore.cs
using KBlazor.Models;
using KBlazor.Services;

namespace KBlazor.Showcase.Services;

/// <summary>
/// Singleton in-memory store — survives page refreshes and circuit reconnects
/// within a single server process lifetime.
/// </summary>
public class InMemoryListViewSettingStore : IListViewSettingStore
{
    private readonly List<ListViewSetting> _settings = new();
    private readonly object _lock = new();

    public ListViewSetting? GetById(Guid id)
    {
        lock (_lock) return _settings.FirstOrDefault(s => s.Id == id);
    }

    public ListViewSetting? GetByUserAndView(string entityType, string userName, string viewName)
    {
        lock (_lock) return _settings.FirstOrDefault(s =>
            s.ForEntity == entityType &&
            s.CustomizedForUser == userName &&
            s.Name == viewName);
    }

    public ListViewSetting? GetByNameAndEntity(string viewName, string entityType)
    {
        lock (_lock) return _settings.FirstOrDefault(s => s.Name == viewName && s.ForEntity == entityType);
    }

    public Guid GetIdByNameAndEntity(string viewName, string entityType)
    {
        lock (_lock) return _settings.FirstOrDefault(s => s.Name == viewName && s.ForEntity == entityType)?.Id ?? Guid.Empty;
    }

    public void Add(ListViewSetting setting)
    {
        lock (_lock) _settings.Add(setting);
    }

    public void Update(ListViewSetting setting)
    {
        lock (_lock)
        {
            var index = _settings.FindIndex(s => s.Id == setting.Id);
            if (index >= 0) _settings[index] = setting;
        }
    }

    public void Delete(Guid id)
    {
        lock (_lock) _settings.RemoveAll(s => s.Id == id);
    }

    public List<ListViewSetting> GetAllForEntity(string entityType, string? currentUsername)
    {
        lock (_lock) return _settings.Where(s => s.ForEntity == entityType).ToList();
    }

    public void SaveChanges() { /* no-op for in-memory */ }
}
