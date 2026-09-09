using KBlazor.Models;
using KBlazor.Services;
using KBlazor.WasmSample.Data;
using KBlazor.WasmSample.Domain;

namespace KBlazor.WasmSample.Services;

public class InMemoryEntityLookupProvider : IEntityLookupProvider
{
    private readonly DataStore _store;

    public InMemoryEntityLookupProvider(DataStore store) => _store = store;

    public bool IsKnownEntityType(Type type) =>
        type == typeof(Customer) || type == typeof(PurchaseOrder);

    public IQueryable<IKBusinessEntity> GetEntities(Type entityType)
    {
        if (entityType == typeof(Customer)) return _store.Customers.AsQueryable();
        if (entityType == typeof(PurchaseOrder)) return _store.Orders.AsQueryable();
        return Enumerable.Empty<IKBusinessEntity>().AsQueryable();
    }

    public IQueryable<IKBusinessEntity> GetEntitiesWithInclude(Type entityType, string includeName) =>
        GetEntities(entityType);
}
