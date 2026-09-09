# Service Registration

KBlazor requires three service interfaces to be implemented by the consuming application and registered in the DI container, and offers two optional ones with built-in defaults. These interfaces decouple KBlazor from your specific database, authentication, hosting model, and configuration concerns.

## Required Interfaces

### 1. IFlexTableSettings

Controls feature flags and role-based access for FlexTable.

```csharp
public interface IFlexTableSettings
{
    bool EnablePersonalViews { get; }
    string[] AdminRoles { get; }
}
```

- `EnablePersonalViews` — when `true`, users can create and save personal view configurations
- `AdminRoles` — role names that grant full view management access (create/edit/delete shared views). FlexTable checks `authenticationState.User.IsInRole(role)` against each entry.

**Example implementation (from IMIO):**

```csharp
public class JimioFlexTableSettings : IFlexTableSettings
{
    private readonly DatabaseContext _db;

    public JimioFlexTableSettings(DatabaseContext db)
    {
        _db = db;
    }

    public bool EnablePersonalViews =>
        Setting.Get(_db, Setting.ENABLE_PERSONAL_VIEWS, true).BoolValue;

    public string[] AdminRoles => new[] { "adminuser", "manageruser" };
}
```

### 2. IListViewSettingStore

Persistence layer for saved table views (column selection, sort order, filters, page size). FlexTable calls this to load/save/delete view configurations.

```csharp
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
```

Your implementation maps between `KBlazor.Models.ListViewSetting` and your database entity. If your DB entity IS `KBlazor.Models.ListViewSetting`, the mapping is direct. Otherwise, convert between them.

**Example implementation (from IMIO):**

```csharp
using KListViewSetting = KBlazor.Models.ListViewSetting;

public class JimioListViewSettingStore : IListViewSettingStore
{
    private readonly DatabaseContext _db;

    public JimioListViewSettingStore(DatabaseContext db)
    {
        _db = db;
    }

    public KListViewSetting? GetById(Guid id)
    {
        var entity = _db.ListViewSettings
            .Where(w => w.Id == id).FirstOrDefault();
        return entity == null ? null : ToKBlazor(entity);
    }

    public KListViewSetting? GetByUserAndView(string entityType, string userName, string viewName)
    {
        var entity = _db.ListViewSettings
            .Where(w => w.ForEntity == entityType && w.CustomizedForUser == userName && w.Name == viewName)
            .FirstOrDefault();
        return entity == null ? null : ToKBlazor(entity);
    }

    public void Add(KListViewSetting setting)
    {
        var entity = FromKBlazor(setting);
        _db.ListViewSettings.Add(entity);
        _db.SaveChanges();
    }

    public void SaveChanges() => _db.SaveChanges();

    // ... implement remaining methods

    private static KListViewSetting ToKBlazor(ListViewSetting src)
    {
        var dest = new KListViewSetting
        {
            Id = src.Id,
            Name = src.Name,
            ForEntity = src.ForEntity,
            Definition = src.Definition,
            PageSize = src.PageSize,
            CustomizedForUser = src.CustomizedForUser,
            ParentViewId = src.ParentViewId
        };
        dest.InitilizeDefinition();
        return dest;
    }

    private static ListViewSetting FromKBlazor(KListViewSetting src)
    {
        return new ListViewSetting
        {
            Id = src.Id,
            Name = src.Name,
            ForEntity = src.ForEntity,
            Definition = src.Definition,
            PageSize = src.PageSize,
            CustomizedForUser = src.CustomizedForUser,
            ParentViewId = src.ParentViewId
        };
    }
}
```

### 3. IEntityLookupProvider

Resolves entity types to their DbSet queryables. Used by BasicEdit for autocomplete and cascade lookup fields.

```csharp
public interface IEntityLookupProvider
{
    bool IsKnownEntityType(Type type);
    IQueryable<IKBusinessEntity> GetEntities(Type entityType);
    IQueryable<IKBusinessEntity> GetEntitiesWithInclude(Type entityType, string includeName);
}
```

**Example implementation (from IMIO):**

```csharp
public class JimioEntityLookupProvider : IEntityLookupProvider
{
    private readonly DatabaseContext _db;

    public JimioEntityLookupProvider(DatabaseContext db)
    {
        _db = db;
    }

    public bool IsKnownEntityType(Type type)
    {
        return DatabaseContext.TypeList.Contains(type);
    }

    public IQueryable<IKBusinessEntity> GetEntities(Type entityType)
    {
        var dbSet = typeof(DatabaseContext).GetProperties()
            .Where(w => w.PropertyType.IsGenericType
                && w.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
            .Where(w => w.PropertyType.GetGenericArguments()[0] == entityType)
            .FirstOrDefault();

        if (dbSet == null)
            return Enumerable.Empty<IKBusinessEntity>().AsQueryable();

        return ((IQueryable<IKBusinessEntity>)dbSet.GetValue(_db));
    }

    public IQueryable<IKBusinessEntity> GetEntitiesWithInclude(Type entityType, string includeName)
    {
        var entities = GetEntities(entityType);
        return ((IQueryable<IKBusinessEntity>)entities.Include(includeName));
    }
}
```

## Registration in Program.cs

```csharp
// KBlazor service registrations
builder.Services.AddScoped<IListViewSettingStore, JimioListViewSettingStore>();
builder.Services.AddScoped<IEntityLookupProvider, JimioEntityLookupProvider>();
builder.Services.AddScoped<IFlexTableSettings, JimioFlexTableSettings>();
```

All three must be registered as `Scoped` (not Singleton) since they typically depend on a scoped `DbContext`.

## Optional services

Both of these are resolved with `IServiceProvider.GetService`; when nothing is registered, FlexTable uses the built-in default. Existing hosts need not register either.

### IClientTimeZoneProvider

```csharp
public interface IClientTimeZoneProvider
{
    /// Minutes to add to a stored DateTime to display it in the user's local time.
    ValueTask<int> GetOffsetMinutesAsync();
}
```

- **Default (unregistered):** offset 0 — `DateTime` values render as stored.
- **Browser timezone (Server or WebAssembly):** ships in the package and reads the offset via `kblazor.js`:

```csharp
builder.Services.AddScoped<IClientTimeZoneProvider, BrowserTimeZoneProvider>();
```

- **Cookie-based (Blazor Server hosts that set a `TimezoneOffset` cookie):** implement it in the host, where `IHttpContextAccessor` is available:

```csharp
using KBlazor.Services;
using Microsoft.AspNetCore.Http;

public sealed class CookieTimeZoneProvider : IClientTimeZoneProvider
{
    private readonly IHttpContextAccessor _http;
    public CookieTimeZoneProvider(IHttpContextAccessor http) => _http = http;

    public ValueTask<int> GetOffsetMinutesAsync()
    {
        var cookie = _http.HttpContext?.Request.Cookies["TimezoneOffset"];
        return new ValueTask<int>(int.TryParse(cookie, out var minutes) ? minutes : 0);
    }
}

// Program.cs
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IClientTimeZoneProvider, CookieTimeZoneProvider>();
```

FlexTable reads the offset once after its first render and applies it to every `DateTime` cell. It is not applied during prerendering.

### ITextMeasurer

```csharp
public interface ITextMeasurer
{
    /// Approximate rendered width of text in CSS pixels.
    float MeasureWidth(string text, string fontFamily, float fontSizePx);
}
```

- **Default (unregistered):** `EstimatingTextMeasurer`, a pure-C# estimator based on per-character widths. Used for the initial column widths when a view is first created.
- Register your own implementation to change how default widths are chosen. Double-click auto-size does not use this interface; it measures the actual cell text in the browser with canvas `measureText`.
