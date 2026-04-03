using KBlazor.Models;

namespace KBlazor.Services;

public interface IEntityLookupProvider
{
    bool IsKnownEntityType(Type type);

    IQueryable<IKBusinessEntity> GetEntities(Type entityType);

    IQueryable<IKBusinessEntity> GetEntitiesWithInclude(Type entityType, string includeName);
}
