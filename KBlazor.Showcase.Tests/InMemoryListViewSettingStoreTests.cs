using KBlazor.Models;
using KBlazor.Showcase.Services;
using Xunit;

namespace KBlazor.Showcase.Tests;

public class InMemoryListViewSettingStoreTests
{
    private static InMemoryListViewSettingStore MakeStore() => new();

    [Fact]
    public void Add_ThenGetById_ReturnsSetting()
    {
        var store = MakeStore();
        var setting = new ListViewSetting { Id = Guid.NewGuid(), Name = "Default", ForEntity = "PurchaseOrder", Definition = "[]" };
        store.Add(setting);
        var result = store.GetById(setting.Id);
        Assert.NotNull(result);
        Assert.Equal("Default", result.Name);
    }

    [Fact]
    public void GetById_Missing_ReturnsNull()
    {
        var store = MakeStore();
        Assert.Null(store.GetById(Guid.NewGuid()));
    }

    [Fact]
    public void Update_ChangesName()
    {
        var store = MakeStore();
        var setting = new ListViewSetting { Id = Guid.NewGuid(), Name = "Old", ForEntity = "PurchaseOrder", Definition = "[]" };
        store.Add(setting);
        setting.Name = "New";
        store.Update(setting);
        Assert.Equal("New", store.GetById(setting.Id)!.Name);
    }

    [Fact]
    public void Delete_RemovesSetting()
    {
        var store = MakeStore();
        var setting = new ListViewSetting { Id = Guid.NewGuid(), Name = "ToDelete", ForEntity = "PurchaseOrder", Definition = "[]" };
        store.Add(setting);
        store.Delete(setting.Id);
        Assert.Null(store.GetById(setting.Id));
    }

    [Fact]
    public void GetAllForEntity_ReturnsOnlyMatchingEntity()
    {
        var store = MakeStore();
        store.Add(new ListViewSetting { Id = Guid.NewGuid(), Name = "A", ForEntity = "PurchaseOrder", Definition = "[]" });
        store.Add(new ListViewSetting { Id = Guid.NewGuid(), Name = "B", ForEntity = "Customer", Definition = "[]" });
        var result = store.GetAllForEntity("PurchaseOrder", null);
        Assert.Single(result);
        Assert.Equal("A", result[0].Name);
    }
}
