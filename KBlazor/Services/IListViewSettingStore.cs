using KBlazor.Models;

namespace KBlazor.Services;

public interface IListViewSettingStore
{
    ListViewSetting? GetById(Guid id);

    ListViewSetting? GetByUserAndView(string entityType, string userName, string viewName);

    ListViewSetting? GetByNameAndEntity(string viewName, string entityType);

    Guid GetIdByNameAndEntity(string viewName, string entityType);

    void Add(ListViewSetting setting);

    void Update(ListViewSetting setting);

    void Delete(Guid id);

    List<ListViewSetting> GetAllForEntity(string entityType, string? currentUsername);

    void SaveChanges();
}
