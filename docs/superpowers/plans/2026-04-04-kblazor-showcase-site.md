# KBlazor Showcase Site Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a Blazor Server showcase site (`KBlazor.Showcase`) that demonstrates all KBlazor components with live Purchase Order seed data and inline docs, deployable to Railway/Render with Cloudflare DNS.

**Architecture:** A standalone Blazor Server app (`KBlazor.Showcase`) references `KBlazor` as a project. Three in-memory service implementations satisfy KBlazor's required DI interfaces. All data is seeded at startup into a singleton `DataStore`. Pages use `<FlexTable>`, `<BasicEdit>`, `<CycleStateButton>`, and `<RelativeDatePicker>` directly from KBlazor against live `IQueryable` over the in-memory lists.

**Tech Stack:** .NET 10, Blazor Server, MudBlazor 8.15.0, KBlazor (project reference), xUnit for service tests, Docker for deployment

---

## File Map

```
KBlazor.Showcase/
├── KBlazor.Showcase.csproj
├── Program.cs
├── App.razor
├── _Imports.razor
├── Domain/
│   ├── Customer.cs               # IKBusinessEntity — FK target for orders
│   ├── OrderStatus.cs            # enum: New Pending InProgress Delivered Cancelled
│   └── PurchaseOrder.cs          # IKBusinessEntity with all [Display] + KBlazor attrs
├── Data/
│   ├── DataStore.cs              # Singleton holding List<Customer> + List<PurchaseOrder>
│   └── SeedData.cs               # Static factory: 5 customers, 20 orders
├── Services/
│   ├── InMemoryFlexTableSettings.cs     # EnablePersonalViews=true, AdminRoles=[]
│   ├── InMemoryListViewSettingStore.cs  # Scoped, List<ListViewSetting> in-memory
│   └── InMemoryEntityLookupProvider.cs # Returns IQueryable over DataStore lists
├── Docs/
│   └── DocContent.cs             # Static strings from the .md files, per component
├── Components/
│   └── Layout/
│       ├── MainLayout.razor      # Nav + body + MudBlazor providers
│       └── NavMenu.razor         # Left sidebar: component links
└── Pages/
    ├── Home.razor                 # Landing: hero + table preview + attribute strip
    ├── Demo/
    │   ├── FlexTableDemo.razor    # Full table view + inline doc panel
    │   ├── KanbanDemo.razor       # Kanban view + inline doc panel
    │   ├── BasicEditDemo.razor    # Slide-in BasicEdit form + inline doc panel
    │   ├── CycleStateDemo.razor   # CycleStateButton demo + doc
    │   ├── DatePickerDemo.razor   # RelativeDatePicker demo + doc
    │   └── AttributesDemo.razor   # Card grid of all 11 attributes
    └── Shared/
        └── DocPanel.razor         # Collapsible doc panel used across demo pages

KBlazor.Showcase.Tests/
├── KBlazor.Showcase.Tests.csproj
├── InMemoryListViewSettingStoreTests.cs
├── InMemoryEntityLookupProviderTests.cs
└── SeedDataTests.cs
```

---

## Task 1: Create the Showcase project

**Files:**
- Create: `KBlazor.Showcase/KBlazor.Showcase.csproj`
- Create: `KBlazor.Showcase.Tests/KBlazor.Showcase.Tests.csproj`
- Modify: `KBlazor.sln`

- [ ] **Step 1: Scaffold the Blazor Server app**

```bash
cd "I:/Source/repos/KBlazor/.claude/worktrees/amazing-benz"
dotnet new blazorserver -n KBlazor.Showcase -o KBlazor.Showcase --framework net10.0
```

- [ ] **Step 2: Scaffold the test project**

```bash
dotnet new xunit -n KBlazor.Showcase.Tests -o KBlazor.Showcase.Tests --framework net10.0
```

- [ ] **Step 3: Add both projects to the solution**

```bash
dotnet sln KBlazor.sln add KBlazor.Showcase/KBlazor.Showcase.csproj
dotnet sln KBlazor.sln add KBlazor.Showcase.Tests/KBlazor.Showcase.Tests.csproj
```

- [ ] **Step 4: Replace the generated csproj with the correct one**

Replace the entire contents of `KBlazor.Showcase/KBlazor.Showcase.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>KBlazor.Showcase</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\KBlazor\KBlazor.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="MudBlazor" Version="8.15.0" />
  </ItemGroup>

</Project>
```

- [ ] **Step 5: Replace the test csproj**

Replace the entire contents of `KBlazor.Showcase.Tests/KBlazor.Showcase.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\KBlazor.Showcase\KBlazor.Showcase.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
  </ItemGroup>

</Project>
```

- [ ] **Step 6: Delete the scaffolded boilerplate files we will replace**

```bash
rm -rf KBlazor.Showcase/Pages
rm -rf KBlazor.Showcase/Shared
rm -rf KBlazor.Showcase/Data
rm -f KBlazor.Showcase/App.razor
rm -f KBlazor.Showcase/wwwroot/css/bootstrap
rm -f KBlazor.Showcase/_Imports.razor
rm -f KBlazor.Showcase/Program.cs
```

- [ ] **Step 7: Verify it builds (will fail on missing files — that's expected)**

```bash
dotnet build KBlazor.Showcase/KBlazor.Showcase.csproj
```

Expected: Compile errors for missing files. That's fine — we'll add them in subsequent tasks.

- [ ] **Step 8: Commit**

```bash
git add KBlazor.Showcase/ KBlazor.Showcase.Tests/ KBlazor.sln
git commit -m "feat: scaffold KBlazor.Showcase and test projects"
```

---

## Task 2: Domain models

**Files:**
- Create: `KBlazor.Showcase/Domain/Customer.cs`
- Create: `KBlazor.Showcase/Domain/OrderStatus.cs`
- Create: `KBlazor.Showcase/Domain/PurchaseOrder.cs`

- [ ] **Step 1: Create `Customer.cs`**

```csharp
// KBlazor.Showcase/Domain/Customer.cs
using System.ComponentModel.DataAnnotations;
using KBlazor.Models;
using Newtonsoft.Json;

namespace KBlazor.Showcase.Domain;

public class Customer : IKBusinessEntity
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Display(Name = "Customer Name", Order = 1)]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Email", Order = 2)]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Country", Order = 3)]
    public string Country { get; set; } = string.Empty;

    public bool Equals(IKBusinessEntity? other) => Id == other?.Id;
    public override string ToString() => Name;
    public string ToJson() => JsonConvert.SerializeObject(this);
}
```

- [ ] **Step 2: Create `OrderStatus.cs`**

```csharp
// KBlazor.Showcase/Domain/OrderStatus.cs
namespace KBlazor.Showcase.Domain;

public enum OrderStatus
{
    New = 0,
    Pending = 1,
    InProgress = 2,
    Delivered = 3,
    Cancelled = 4
}
```

- [ ] **Step 3: Create `PurchaseOrder.cs`**

```csharp
// KBlazor.Showcase/Domain/PurchaseOrder.cs
using System.ComponentModel.DataAnnotations;
using KBlazor.Attributes;
using KBlazor.Models;
using Newtonsoft.Json;

namespace KBlazor.Showcase.Domain;

public class PurchaseOrder : IKBusinessEntity
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Display(Name = "Order #", Order = 1)]
    [ReadOnlyOnEdit]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Customer", Order = 2)]
    [AutoComplete]
    public Guid? CustomerId { get; set; }

    public virtual Customer? Customer { get; set; }

    [Display(Name = "Status", Order = 3)]
    [SortAndFilterOn(Member = "Status")]
    public OrderStatus Status { get; set; }

    [Display(Name = "Order Date", Order = 4)]
    [EnableTime]
    public DateTime OrderDate { get; set; }

    [Display(Name = "Delivery Date", Order = 5)]
    public DateTime? DeliveryDate { get; set; }

    [Display(Name = "Amount", Order = 6)]
    [DisplayNoWrap]
    public decimal Amount { get; set; }

    [Display(Name = "Reference", Order = 7)]
    [LinkOnField]
    public string Reference { get; set; } = string.Empty;

    [Display(Name = "Urgent", Order = 8)]
    [AllowInlineEdit]
    public bool IsUrgent { get; set; }

    [Display(Name = "Notes", Order = 9)]
    [MemoDisplay]
    public string Notes { get; set; } = string.Empty;

    public bool Equals(IKBusinessEntity? other) => Id == other?.Id;
    public override string ToString() => Name;
    public string ToJson() => JsonConvert.SerializeObject(this);
}
```

- [ ] **Step 4: Commit**

```bash
git add KBlazor.Showcase/Domain/
git commit -m "feat: add PurchaseOrder, Customer, OrderStatus domain models"
```

---

## Task 3: Seed data and DataStore

**Files:**
- Create: `KBlazor.Showcase/Data/DataStore.cs`
- Create: `KBlazor.Showcase/Data/SeedData.cs`
- Create: `KBlazor.Showcase.Tests/SeedDataTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
// KBlazor.Showcase.Tests/SeedDataTests.cs
using KBlazor.Showcase.Data;

namespace KBlazor.Showcase.Tests;

public class SeedDataTests
{
    [Fact]
    public void Seed_Produces20Orders()
    {
        var store = SeedData.Create();
        Assert.Equal(20, store.Orders.Count);
    }

    [Fact]
    public void Seed_Produces5Customers()
    {
        var store = SeedData.Create();
        Assert.Equal(5, store.Customers.Count);
    }

    [Fact]
    public void Seed_AllOrdersHaveValidCustomerId()
    {
        var store = SeedData.Create();
        var customerIds = store.Customers.Select(c => c.Id).ToHashSet();
        Assert.All(store.Orders, o => Assert.Contains(o.CustomerId!.Value, customerIds));
    }

    [Fact]
    public void Seed_AllStatusesRepresented()
    {
        var store = SeedData.Create();
        var statuses = store.Orders.Select(o => o.Status).Distinct().ToList();
        Assert.Equal(5, statuses.Count);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test KBlazor.Showcase.Tests/ --no-build 2>&1 | head -20
```

Expected: Build error — `SeedData` not found.

- [ ] **Step 3: Create `DataStore.cs`**

```csharp
// KBlazor.Showcase/Data/DataStore.cs
using KBlazor.Showcase.Domain;

namespace KBlazor.Showcase.Data;

public class DataStore
{
    public List<Customer> Customers { get; } = new();
    public List<PurchaseOrder> Orders { get; } = new();
}
```

- [ ] **Step 4: Create `SeedData.cs`**

```csharp
// KBlazor.Showcase/Data/SeedData.cs
using KBlazor.Showcase.Domain;

namespace KBlazor.Showcase.Data;

public static class SeedData
{
    public static DataStore Create()
    {
        var store = new DataStore();

        var acme    = new Customer { Id = Guid.NewGuid(), Name = "Acme Corp",    Email = "orders@acme.com",    Country = "USA" };
        var globex  = new Customer { Id = Guid.NewGuid(), Name = "Globex Inc",   Email = "orders@globex.com",  Country = "Canada" };
        var initech = new Customer { Id = Guid.NewGuid(), Name = "Initech LLC",  Email = "orders@initech.com", Country = "USA" };
        var umbrella= new Customer { Id = Guid.NewGuid(), Name = "Umbrella Co",  Email = "orders@umbrella.com",Country = "UK" };
        var soylent = new Customer { Id = Guid.NewGuid(), Name = "Soylent Corp", Email = "orders@soylent.com", Country = "Australia" };

        store.Customers.AddRange(new[] { acme, globex, initech, umbrella, soylent });

        var baseDate = new DateTime(2026, 3, 1);
        var orders = new[]
        {
            MakeOrder("ORD-0041", acme,     OrderStatus.Delivered, baseDate.AddDays(0),  baseDate.AddDays(5),  4200.00m,  false, "Delivered on time."),
            MakeOrder("ORD-0042", globex,   OrderStatus.Pending,   baseDate.AddDays(1),  null,                 1750.00m,  false, "Awaiting supplier confirmation."),
            MakeOrder("ORD-0043", initech,  OrderStatus.New,       baseDate.AddDays(2),  null,                 8900.00m,  true,  "Rush order."),
            MakeOrder("ORD-0044", umbrella, OrderStatus.Cancelled, baseDate.AddDays(3),  null,                  620.00m,  false, "Customer cancelled."),
            MakeOrder("ORD-0045", soylent,  OrderStatus.Delivered, baseDate.AddDays(4),  baseDate.AddDays(9),  3310.00m,  false, ""),
            MakeOrder("ORD-0046", acme,     OrderStatus.InProgress,baseDate.AddDays(5),  null,                 7100.00m,  true,  "Expedited shipping."),
            MakeOrder("ORD-0047", globex,   OrderStatus.New,       baseDate.AddDays(6),  null,                  980.00m,  false, ""),
            MakeOrder("ORD-0048", initech,  OrderStatus.Pending,   baseDate.AddDays(7),  null,                 2250.00m,  false, "Partial stock available."),
            MakeOrder("ORD-0049", umbrella, OrderStatus.Delivered, baseDate.AddDays(8),  baseDate.AddDays(13), 5670.00m,  false, ""),
            MakeOrder("ORD-0050", soylent,  OrderStatus.InProgress,baseDate.AddDays(9),  null,                 3340.00m,  true,  "Priority account."),
            MakeOrder("ORD-0051", acme,     OrderStatus.New,       baseDate.AddDays(10), null,                 1120.00m,  false, ""),
            MakeOrder("ORD-0052", globex,   OrderStatus.Cancelled, baseDate.AddDays(11), null,                  430.00m,  false, "Out of stock."),
            MakeOrder("ORD-0053", initech,  OrderStatus.Delivered, baseDate.AddDays(12), baseDate.AddDays(17), 9400.00m,  false, ""),
            MakeOrder("ORD-0054", umbrella, OrderStatus.Pending,   baseDate.AddDays(13), null,                 2800.00m,  true,  "Urgent restock."),
            MakeOrder("ORD-0055", soylent,  OrderStatus.New,       baseDate.AddDays(14), null,                  760.00m,  false, ""),
            MakeOrder("ORD-0056", acme,     OrderStatus.InProgress,baseDate.AddDays(15), null,                 6200.00m,  false, ""),
            MakeOrder("ORD-0057", globex,   OrderStatus.Delivered, baseDate.AddDays(16), baseDate.AddDays(21), 4850.00m,  false, ""),
            MakeOrder("ORD-0058", initech,  OrderStatus.Pending,   baseDate.AddDays(17), null,                 1390.00m,  false, ""),
            MakeOrder("ORD-0059", umbrella, OrderStatus.New,       baseDate.AddDays(18), null,                 3050.00m,  true,  "New product launch."),
            MakeOrder("ORD-0060", soylent,  OrderStatus.Cancelled, baseDate.AddDays(19), null,                  510.00m,  false, "Duplicate order."),
        };

        // Wire up navigation properties
        foreach (var o in orders)
            o.Customer = store.Customers.First(c => c.Id == o.CustomerId);

        store.Orders.AddRange(orders);
        return store;
    }

    private static PurchaseOrder MakeOrder(
        string name, Customer customer, OrderStatus status,
        DateTime orderDate, DateTime? deliveryDate,
        decimal amount, bool isUrgent, string notes) => new()
    {
        Id          = Guid.NewGuid(),
        Name        = name,
        CustomerId  = customer.Id,
        Status      = status,
        OrderDate   = orderDate,
        DeliveryDate= deliveryDate,
        Amount      = amount,
        IsUrgent    = isUrgent,
        Notes       = notes,
        Reference   = $"https://example.com/ref/{name.ToLower()}"
    };
}
```

- [ ] **Step 5: Run tests**

```bash
dotnet test KBlazor.Showcase.Tests/ -v minimal
```

Expected: 4 tests pass.

- [ ] **Step 6: Commit**

```bash
git add KBlazor.Showcase/Data/ KBlazor.Showcase.Tests/SeedDataTests.cs
git commit -m "feat: add DataStore and SeedData with 5 customers and 20 orders"
```

---

## Task 4: In-memory service implementations

**Files:**
- Create: `KBlazor.Showcase/Services/InMemoryFlexTableSettings.cs`
- Create: `KBlazor.Showcase/Services/InMemoryListViewSettingStore.cs`
- Create: `KBlazor.Showcase/Services/InMemoryEntityLookupProvider.cs`
- Create: `KBlazor.Showcase.Tests/InMemoryListViewSettingStoreTests.cs`
- Create: `KBlazor.Showcase.Tests/InMemoryEntityLookupProviderTests.cs`

- [ ] **Step 1: Write failing tests**

```csharp
// KBlazor.Showcase.Tests/InMemoryListViewSettingStoreTests.cs
using KBlazor.Models;
using KBlazor.Showcase.Services;

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
```

```csharp
// KBlazor.Showcase.Tests/InMemoryEntityLookupProviderTests.cs
using KBlazor.Showcase.Data;
using KBlazor.Showcase.Domain;
using KBlazor.Showcase.Services;

namespace KBlazor.Showcase.Tests;

public class InMemoryEntityLookupProviderTests
{
    private static InMemoryEntityLookupProvider MakeProvider()
    {
        var store = SeedData.Create();
        return new InMemoryEntityLookupProvider(store);
    }

    [Fact]
    public void IsKnownEntityType_Customer_ReturnsTrue()
    {
        var provider = MakeProvider();
        Assert.True(provider.IsKnownEntityType(typeof(Customer)));
    }

    [Fact]
    public void IsKnownEntityType_Unknown_ReturnsFalse()
    {
        var provider = MakeProvider();
        Assert.False(provider.IsKnownEntityType(typeof(string)));
    }

    [Fact]
    public void GetEntities_Customer_Returns5()
    {
        var provider = MakeProvider();
        var result = provider.GetEntities(typeof(Customer)).ToList();
        Assert.Equal(5, result.Count);
    }

    [Fact]
    public void GetEntitiesWithInclude_Customer_Returns5()
    {
        var provider = MakeProvider();
        // Include is a no-op for in-memory; just verify it doesn't throw
        var result = provider.GetEntitiesWithInclude(typeof(Customer), "Orders").ToList();
        Assert.Equal(5, result.Count);
    }
}
```

- [ ] **Step 2: Run tests — expect build failure**

```bash
dotnet test KBlazor.Showcase.Tests/ -v minimal 2>&1 | head -10
```

Expected: Build error — service types not found.

- [ ] **Step 3: Create `InMemoryFlexTableSettings.cs`**

```csharp
// KBlazor.Showcase/Services/InMemoryFlexTableSettings.cs
using KBlazor.Services;

namespace KBlazor.Showcase.Services;

public class InMemoryFlexTableSettings : IFlexTableSettings
{
    public bool EnablePersonalViews => true;
    public string[] AdminRoles => Array.Empty<string>();
}
```

- [ ] **Step 4: Create `InMemoryListViewSettingStore.cs`**

```csharp
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
```

- [ ] **Step 5: Create `InMemoryEntityLookupProvider.cs`**

```csharp
// KBlazor.Showcase/Services/InMemoryEntityLookupProvider.cs
using KBlazor.Models;
using KBlazor.Services;
using KBlazor.Showcase.Data;
using KBlazor.Showcase.Domain;

namespace KBlazor.Showcase.Services;

public class InMemoryEntityLookupProvider : IEntityLookupProvider
{
    private readonly DataStore _store;

    public InMemoryEntityLookupProvider(DataStore store) => _store = store;

    public bool IsKnownEntityType(Type type) =>
        type == typeof(Customer) || type == typeof(PurchaseOrder);

    public IQueryable<IKBusinessEntity> GetEntities(Type entityType)
    {
        if (entityType == typeof(Customer))
            return _store.Customers.AsQueryable();
        if (entityType == typeof(PurchaseOrder))
            return _store.Orders.AsQueryable();
        return Enumerable.Empty<IKBusinessEntity>().AsQueryable();
    }

    public IQueryable<IKBusinessEntity> GetEntitiesWithInclude(Type entityType, string includeName) =>
        GetEntities(entityType); // Navigation props already wired in SeedData
}
```

- [ ] **Step 6: Run all tests**

```bash
dotnet test KBlazor.Showcase.Tests/ -v minimal
```

Expected: 13 tests pass (4 seed + 5 store + 4 provider).

- [ ] **Step 7: Commit**

```bash
git add KBlazor.Showcase/Services/ KBlazor.Showcase.Tests/
git commit -m "feat: add in-memory service implementations with passing tests"
```

---

## Task 5: Program.cs, App.razor, _Imports.razor

**Files:**
- Create: `KBlazor.Showcase/Program.cs`
- Create: `KBlazor.Showcase/App.razor`
- Create: `KBlazor.Showcase/_Imports.razor`

- [ ] **Step 1: Create `Program.cs`**

```csharp
// KBlazor.Showcase/Program.cs
using KBlazor.Services;
using KBlazor.Showcase.Data;
using KBlazor.Showcase.Services;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddMudServices();
builder.Services.AddHttpContextAccessor();

// KBlazor required services
builder.Services.AddScoped<IFlexTableSettings, InMemoryFlexTableSettings>();
builder.Services.AddScoped<IListViewSettingStore, InMemoryListViewSettingStore>();
builder.Services.AddScoped<IEntityLookupProvider, InMemoryEntityLookupProvider>();

// Singleton data store — seeded once at startup
builder.Services.AddSingleton(SeedData.Create());

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
```

- [ ] **Step 2: Create `App.razor`**

```razor
@* KBlazor.Showcase/App.razor *@
<Router AppAssembly="@typeof(App).Assembly">
    <Found Context="routeData">
        <RouteView RouteData="@routeData" DefaultLayout="@typeof(MainLayout)" />
        <FocusOnNavigate RouteData="@routeData" Selector="h1" />
    </Found>
    <NotFound>
        <PageTitle>Not found</PageTitle>
        <LayoutView Layout="@typeof(MainLayout)">
            <p role="alert">Sorry, there's nothing at this address.</p>
        </LayoutView>
    </NotFound>
</Router>
```

- [ ] **Step 3: Create `_Imports.razor`**

```razor
@* KBlazor.Showcase/_Imports.razor *@
@using System.Net.Http
@using Microsoft.AspNetCore.Authorization
@using Microsoft.AspNetCore.Components.Authorization
@using Microsoft.AspNetCore.Components.Forms
@using Microsoft.AspNetCore.Components.Routing
@using Microsoft.AspNetCore.Components.Web
@using Microsoft.AspNetCore.Components.Web.Virtualization
@using Microsoft.JSInterop
@using MudBlazor
@using KBlazor.Models
@using KBlazor.Attributes
@using KBlazor.Services
@using KBlazor.Components
@using KBlazor.Showcase
@using KBlazor.Showcase.Domain
@using KBlazor.Showcase.Data
@using KBlazor.Showcase.Services
@using KBlazor.Showcase.Docs
```

- [ ] **Step 4: Create the Razor Pages host page**

Create `KBlazor.Showcase/Pages/_Host.cshtml`:

```cshtml
@page "/"
@namespace KBlazor.Showcase.Pages
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
@{
    Layout = "_Layout";
}

<component type="typeof(App)" render-mode="ServerPrerendered" />
```

Create `KBlazor.Showcase/Pages/_Layout.cshtml`:

```cshtml
@using Microsoft.AspNetCore.Components.Web
@namespace KBlazor.Showcase.Pages
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers

<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>KBlazor — Blazor Component Library</title>
    <base href="~/" />
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;600;800&display=swap" rel="stylesheet" />
    <link href="_content/MudBlazor/MudBlazor.min.css" rel="stylesheet" />
    <link href="css/app.css" rel="stylesheet" />
    <script src="_content/KBlazor/kblazor.js"></script>
</head>
<body>
    @RenderBody()

    <script src="_framework/blazor.server.js"></script>
    <script src="_content/MudBlazor/MudBlazor.min.js"></script>
</body>
</html>
```

- [ ] **Step 5: Create the base CSS file**

Create `KBlazor.Showcase/wwwroot/css/app.css`:

```css
:root {
    --bg: #0F0E1A;
    --bg-card: #1A182E;
    --bg-sidebar: #13112A;
    --accent: #5749F4;
    --text-primary: #FFFFFF;
    --text-muted: #888799;
    --border: #2B283D;
}

* { box-sizing: border-box; }

html, body {
    margin: 0;
    padding: 0;
    background: var(--bg);
    color: var(--text-primary);
    font-family: 'Inter', sans-serif;
    min-height: 100vh;
}

a { color: var(--accent); text-decoration: none; }
a:hover { text-decoration: underline; }

code, pre {
    font-family: 'Fira Mono', 'Consolas', monospace;
    font-size: 13px;
    background: #0A0918;
    border-radius: 6px;
    padding: 2px 6px;
    color: #E8E8EA;
}

pre { padding: 16px; overflow-x: auto; }
```

- [ ] **Step 6: Commit**

```bash
git add KBlazor.Showcase/Program.cs KBlazor.Showcase/App.razor KBlazor.Showcase/_Imports.razor KBlazor.Showcase/Pages/ KBlazor.Showcase/wwwroot/
git commit -m "feat: wire up Program.cs, App.razor, host pages and base CSS"
```

---

## Task 6: Layout and navigation

**Files:**
- Create: `KBlazor.Showcase/Components/Layout/MainLayout.razor`
- Create: `KBlazor.Showcase/Components/Layout/NavMenu.razor`

- [ ] **Step 1: Create `MainLayout.razor`**

```razor
@* KBlazor.Showcase/Components/Layout/MainLayout.razor *@
@inherits LayoutComponentBase

<MudThemeProvider Theme="_theme" IsDarkMode="true" />
<MudPopoverProvider />
<MudDialogProvider />
<MudSnackbarProvider />

<div class="showcase-shell">
    <nav class="top-nav">
        <div class="nav-logo">
            <div class="logo-mark"></div>
            <span class="logo-text">KBlazor</span>
        </div>
        <div class="nav-links">
            <NavLink href="/demo/flextable">Components</NavLink>
            <NavLink href="/docs">Docs</NavLink>
            <a href="https://github.com/your-org/KBlazor" target="_blank">GitHub</a>
        </div>
        <NavLink href="/demo/flextable" class="btn-primary-sm">Get Started</NavLink>
    </nav>

    <main class="main-content">
        @Body
    </main>
</div>

<style>
    .showcase-shell { display: flex; flex-direction: column; min-height: 100vh; }

    .top-nav {
        display: flex;
        align-items: center;
        justify-content: space-between;
        height: 64px;
        padding: 0 40px;
        background: var(--bg);
        border-bottom: 1px solid var(--border);
        position: sticky; top: 0; z-index: 100;
    }

    .nav-logo { display: flex; align-items: center; gap: 10px; }
    .logo-mark { width: 28px; height: 28px; background: var(--accent); border-radius: 6px; }
    .logo-text { font-size: 18px; font-weight: 800; color: var(--text-primary); }

    .nav-links { display: flex; gap: 32px; }
    .nav-links a { font-size: 14px; color: var(--text-muted); text-decoration: none; }
    .nav-links a:hover { color: var(--text-primary); }

    .btn-primary-sm {
        display: flex; align-items: center;
        height: 36px; padding: 0 16px;
        background: var(--accent); color: #fff !important;
        border-radius: 6px; font-size: 14px; font-weight: 600;
        text-decoration: none !important;
    }

    .main-content { flex: 1; }
</style>

@code {
    private MudTheme _theme = new()
    {
        PaletteLight = new PaletteLight { Primary = "#5749F4" },
        PaletteDark = new PaletteDark { Primary = "#5749F4", Background = "#0F0E1A", Surface = "#1A182E" }
    };
}
```

- [ ] **Step 2: Create `NavMenu.razor`** (used inside demo pages as a sidebar)

```razor
@* KBlazor.Showcase/Components/Layout/NavMenu.razor *@
<nav class="demo-sidebar">
    <div class="sidebar-section-title">COMPONENTS</div>
    <NavLink href="/demo/flextable" Match="NavLinkMatch.All" class="sidebar-item">
        <span class="sidebar-indicator"></span> FlexTable
    </NavLink>
    <NavLink href="/demo/kanban" Match="NavLinkMatch.All" class="sidebar-item">
        <span class="sidebar-indicator"></span> Kanban View
    </NavLink>
    <NavLink href="/demo/basicedit" Match="NavLinkMatch.All" class="sidebar-item">
        <span class="sidebar-indicator"></span> BasicEdit
    </NavLink>
    <NavLink href="/demo/cyclestate" Match="NavLinkMatch.All" class="sidebar-item">
        <span class="sidebar-indicator"></span> CycleStateButton
    </NavLink>
    <NavLink href="/demo/datepicker" Match="NavLinkMatch.All" class="sidebar-item">
        <span class="sidebar-indicator"></span> RelativeDatePicker
    </NavLink>
    <NavLink href="/demo/attributes" Match="NavLinkMatch.All" class="sidebar-item">
        <span class="sidebar-indicator"></span> Attributes
    </NavLink>
</nav>

<style>
    .demo-sidebar {
        width: 200px; min-width: 200px;
        background: var(--bg-sidebar);
        padding: 16px 0;
        display: flex; flex-direction: column; gap: 2px;
        border-right: 1px solid var(--border);
        min-height: calc(100vh - 64px);
    }
    .sidebar-section-title {
        font-size: 10px; font-weight: 700; letter-spacing: 1.5px;
        color: #4A4868; padding: 0 16px 8px;
    }
    .sidebar-item {
        display: flex; align-items: center; gap: 10px;
        height: 36px; padding: 0 16px;
        font-size: 13px; color: var(--text-muted);
        text-decoration: none; border-radius: 6px;
    }
    .sidebar-item:hover { color: var(--text-primary); background: rgba(87,73,244,0.1); }
    .sidebar-item.active { color: var(--text-primary); background: #1E1A3A; font-weight: 600; }
    .sidebar-item.active .sidebar-indicator {
        display: block; width: 3px; height: 20px;
        background: var(--accent); border-radius: 2px;
    }
    .sidebar-indicator { display: block; width: 3px; height: 20px; }
</style>
```

- [ ] **Step 3: Commit**

```bash
git add KBlazor.Showcase/Components/
git commit -m "feat: add MainLayout and NavMenu sidebar"
```

---

## Task 7: DocContent and DocPanel

**Files:**
- Create: `KBlazor.Showcase/Docs/DocContent.cs`
- Create: `KBlazor.Showcase/Pages/Shared/DocPanel.razor`

- [ ] **Step 1: Create `DocContent.cs`**

```csharp
// KBlazor.Showcase/Docs/DocContent.cs
namespace KBlazor.Showcase.Docs;

public static class DocContent
{
    public const string FlexTableUsage = """
        <FlexTable TItem="PurchaseOrder"
                   Items="@orders"
                   Fields="Order #,Customer,Status,Amount,Order Date"
                   SelectionChanged="OnRowClicked"
                   SortFilter="OnSortFilter" />
        """;

    public const string FlexTableExplained = """
        FlexTable renders any IQueryable&lt;T&gt; data source. Decorate your model with
        [Display] attributes — that's all it needs to build columns. The SortFilter callback
        receives a ListViewSetting so you can re-query with the user's active sort and filters applied.
        """;

    public const string KanbanUsage = """
        <FlexTable TItem="PurchaseOrder"
                   Items="@orders"
                   DefaultViewMode="FlexTableViewMode.Kanban"
                   KanbanGroupField="Status"
                   KanbanColumns="New,Pending,InProgress,Delivered,Cancelled"
                   KanbanCardDisplayField="Order #"
                   RenderTemplates="@_templates" />
        """;

    public const string KanbanExplained = """
        Switch FlexTable to Kanban mode with DefaultViewMode. KanbanGroupField names the property
        that determines which column a card appears in. KanbanColumns controls column order and labels.
        RenderTemplates lets you replace column headers with custom markup — great for colored status badges.
        """;

    public const string BasicEditUsage = """
        <BasicEdit TItem="PurchaseOrder"
                   Item="@selectedOrder"
                   Save="SaveOrder"
                   Close="CloseEditor"
                   Columns="2" />
        """;

    public const string BasicEditExplained = """
        BasicEdit auto-generates a form from your model's [Display] attributes.
        [MemoDisplay] renders a textarea. [EnableTime] adds a time picker to DateTime fields.
        [AutoComplete] wires a field to IEntityLookupProvider for live search.
        [ReadOnlyOnEdit] locks a field when editing existing records.
        """;

    public const string CycleStateUsage = """
        <CycleStateButton @bind-Value="order.Status"
                          States="@_states"
                          Labels="@_labels" />
        """;

    public const string CycleStateExplained = """
        CycleStateButton cycles through a list of integer states on each click.
        Bind it to any int or enum property. Supply parallel States and Labels arrays
        to control the cycle order and display text.
        """;

    public const string DatePickerUsage = """
        <RelativeDatePicker @bind-Value="selectedDate"
                            Label="Filter by date" />
        """;

    public const string DatePickerExplained = """
        RelativeDatePicker supports both absolute date selection and relative expressions:
        Today, This Week, Last Month, Next Year, and more. FlexTable uses it automatically
        for DateTime column filters when SortFilter is wired up.
        """;
}
```

- [ ] **Step 2: Create `DocPanel.razor`**

```razor
@* KBlazor.Showcase/Pages/Shared/DocPanel.razor *@
<div class="doc-panel @(IsExpanded ? "expanded" : "")">
    <button class="doc-toggle" @onclick="Toggle">
        <span>@(IsExpanded ? "▲" : "▼")</span>
        <span>How to use @ComponentName</span>
    </button>
    @if (IsExpanded)
    {
        <div class="doc-body">
            <div class="doc-columns">
                <div class="doc-col">
                    <div class="doc-label">USAGE</div>
                    <pre><code>@UsageCode</code></pre>
                </div>
                <div class="doc-col">
                    <div class="doc-label">EXPLANATION</div>
                    <p>@((MarkupString)Explanation)</p>
                </div>
            </div>
        </div>
    }
</div>

<style>
    .doc-panel {
        border-top: 1px solid var(--border);
        background: var(--bg-sidebar);
    }
    .doc-toggle {
        display: flex; align-items: center; gap: 8px;
        width: 100%; padding: 12px 24px;
        background: none; border: none; color: var(--text-muted);
        font-size: 13px; cursor: pointer; text-align: left;
    }
    .doc-toggle:hover { color: var(--text-primary); }
    .doc-body { padding: 0 24px 24px; }
    .doc-columns { display: flex; gap: 40px; }
    .doc-col { flex: 1; }
    .doc-label { font-size: 10px; font-weight: 700; letter-spacing: 1.5px; color: #4A4868; margin-bottom: 8px; }
    .doc-col p { font-size: 14px; color: var(--text-muted); line-height: 1.7; margin: 0; }
</style>

@code {
    [Parameter, EditorRequired] public string ComponentName { get; set; } = "";
    [Parameter, EditorRequired] public string UsageCode { get; set; } = "";
    [Parameter, EditorRequired] public string Explanation { get; set; } = "";

    private bool IsExpanded = false;
    private void Toggle() => IsExpanded = !IsExpanded;
}
```

- [ ] **Step 3: Commit**

```bash
git add KBlazor.Showcase/Docs/ KBlazor.Showcase/Pages/Shared/
git commit -m "feat: add DocContent strings and DocPanel collapsible component"
```

---

## Task 8: Landing page (Home.razor)

**Files:**
- Create: `KBlazor.Showcase/Pages/Home.razor`

- [ ] **Step 1: Create `Home.razor`**

```razor
@* KBlazor.Showcase/Pages/Home.razor *@
@page "/"
@inject DataStore Store

<PageTitle>KBlazor — Blazor Component Library</PageTitle>

<div class="hero">
    <div class="hero-left">
        <div class="hero-badge">
            <span class="badge-dot"></span>
            Blazor Server Component Library
        </div>
        <h1>Build data-driven UIs<br />in minutes</h1>
        <p class="hero-sub">
            KBlazor gives you a powerful table, auto-generated forms, and rich data controls
            — all driven by your model attributes.
        </p>
        <div class="hero-btns">
            <NavLink href="/demo/flextable" class="btn-primary">See Live Demo</NavLink>
            <a href="https://github.com/your-org/KBlazor" target="_blank" class="btn-secondary">Read Docs</a>
        </div>
    </div>

    <div class="hero-right">
        <div class="table-preview">
            <div class="tp-header">
                <span class="th">Order #</span>
                <span class="th">Customer</span>
                <span class="th">Status</span>
                <span class="th th-right">Amount</span>
            </div>
            @foreach (var order in Store.Orders.Take(5))
            {
                <div class="tp-row">
                    <span class="td">@order.Name</span>
                    <span class="td">@order.Customer?.Name</span>
                    <span class="td"><span class="status-badge @order.Status.ToString().ToLower()">@order.Status</span></span>
                    <span class="td td-right">$@order.Amount.ToString("N2")</span>
                </div>
            }
            <div class="tp-fade"></div>
        </div>
    </div>
</div>

<div class="attr-strip">
    @foreach (var attr in _attrs)
    {
        <div class="attr-item">
            <span class="attr-icon"></span>
            <span>@attr</span>
        </div>
    }
</div>

<style>
    .hero { display: flex; align-items: center; gap: 80px; padding: 80px 40px; min-height: calc(100vh - 64px - 64px); }
    .hero-left { width: 520px; min-width: 520px; display: flex; flex-direction: column; gap: 24px; }
    .hero-badge { display: inline-flex; align-items: center; gap: 8px; background: #1E1A3A; border-radius: 999px; padding: 6px 14px; font-size: 12px; color: var(--text-muted); width: fit-content; }
    .badge-dot { width: 8px; height: 8px; background: var(--accent); border-radius: 50%; display: inline-block; }
    .hero-left h1 { font-size: 52px; font-weight: 800; line-height: 1.1; margin: 0; color: #fff; }
    .hero-sub { font-size: 16px; color: var(--text-muted); line-height: 1.7; margin: 0; }
    .hero-btns { display: flex; gap: 12px; }
    .btn-primary { display: flex; align-items: center; height: 44px; padding: 0 24px; background: var(--accent); color: #fff !important; border-radius: 8px; font-size: 15px; font-weight: 600; text-decoration: none !important; }
    .btn-secondary { display: flex; align-items: center; height: 44px; padding: 0 24px; background: #1E1A3A; color: var(--text-muted) !important; border-radius: 8px; font-size: 15px; font-weight: 600; text-decoration: none !important; }
    .hero-right { flex: 1; }

    .table-preview { background: var(--bg-card); border-radius: 12px; overflow: hidden; position: relative; }
    .tp-header { display: flex; background: #13112A; padding: 0 16px; height: 40px; align-items: center; }
    .th { flex: 1; font-size: 11px; font-weight: 700; color: var(--accent); text-transform: uppercase; letter-spacing: 0.5px; }
    .th-right { text-align: right; }
    .tp-row { display: flex; padding: 0 16px; height: 40px; align-items: center; border-top: 1px solid var(--border); }
    .tp-row:nth-child(odd) { background: #17152B; }
    .td { flex: 1; font-size: 13px; color: #E8E8EA; }
    .td-right { text-align: right; }
    .tp-fade { height: 60px; background: linear-gradient(to bottom, transparent, var(--bg-card)); }

    .status-badge { display: inline-block; padding: 2px 10px; border-radius: 999px; font-size: 11px; font-weight: 600; }
    .status-badge.new { background: #1C1A3A; color: #B2CCFF; }
    .status-badge.pending { background: #2A2414; color: #FFD9B2; }
    .status-badge.inprogress { background: #1A2040; color: #93C5FD; }
    .status-badge.delivered { background: #1E3A2A; color: #A1E5A1; }
    .status-badge.cancelled { background: #3A1A1A; color: #FFBFB2; }

    .attr-strip { display: flex; align-items: center; justify-content: space-around; height: 64px; background: #13112A; border-top: 1px solid var(--border); padding: 0 40px; }
    .attr-item { display: flex; align-items: center; gap: 10px; font-size: 13px; color: var(--text-muted); }
    .attr-icon { width: 20px; height: 20px; background: var(--accent); border-radius: 4px; display: inline-block; }
</style>

@code {
    private string[] _attrs = new[]
    {
        "[Display] driven columns",
        "[AllowInlineEdit] in-row editing",
        "[SortAndFilterOn] backing fields",
        "[MemoDisplay] multiline forms"
    };
}
```

- [ ] **Step 2: Build and verify**

```bash
dotnet build KBlazor.Showcase/KBlazor.Showcase.csproj
```

Expected: Build succeeds with 0 errors.

- [ ] **Step 3: Run the app and open browser**

```bash
cd KBlazor.Showcase && dotnet run
```

Open `https://localhost:5001` — verify the landing page renders with hero, table preview, and attribute strip.

- [ ] **Step 4: Commit**

```bash
git add KBlazor.Showcase/Pages/Home.razor
git commit -m "feat: add landing page with hero and order table preview"
```

---

## Task 9: FlexTable demo page

**Files:**
- Create: `KBlazor.Showcase/Pages/Demo/FlexTableDemo.razor`

- [ ] **Step 1: Create `FlexTableDemo.razor`**

```razor
@* KBlazor.Showcase/Pages/Demo/FlexTableDemo.razor *@
@page "/demo/flextable"
@inject DataStore Store

<PageTitle>FlexTable — KBlazor</PageTitle>

<div class="demo-layout">
    <NavMenu />
    <div class="demo-body">
        <div class="demo-header">
            <h2>FlexTable</h2>
            <p class="demo-sub">A data table with sort, filter, pagination, saved views, and three display modes.</p>
        </div>

        <div class="demo-content">
            <FlexTable TItem="PurchaseOrder"
                       Items="@_orders"
                       Fields="Order #,Customer,Status,Amount,Order Date,Urgent"
                       ViewName="FlexTableDemo"
                       PageSize="10"
                       SelectionChanged="OnRowClicked"
                       SortFilter="OnSortFilter"
                       InlineEditor="IsUrgent"
                       AdditionalCommands="GetCommands"
                       RenderTemplates="@_templates"
                       SelectedItem="@_selected" />
        </div>

        <DocPanel ComponentName="FlexTable"
                  UsageCode="@DocContent.FlexTableUsage"
                  Explanation="@DocContent.FlexTableExplained" />
    </div>
</div>

<style>
    .demo-layout { display: flex; min-height: calc(100vh - 64px); }
    .demo-body { flex: 1; display: flex; flex-direction: column; }
    .demo-header { padding: 32px 32px 16px; border-bottom: 1px solid var(--border); }
    .demo-header h2 { margin: 0 0 8px; font-size: 28px; font-weight: 800; }
    .demo-sub { margin: 0; font-size: 15px; color: var(--text-muted); }
    .demo-content { flex: 1; padding: 24px 32px; }
</style>

@code {
    private IQueryable<PurchaseOrder> _orders = default!;
    private PurchaseOrder? _selected;

    private Dictionary<Type, RenderFragment<object?>> _templates = new()
    {
        [typeof(OrderStatus)] = val =>
        {
            var status = (OrderStatus?)val;
            var (bg, fg, label) = status switch
            {
                OrderStatus.New        => ("#1C1A3A", "#B2CCFF", "New"),
                OrderStatus.Pending    => ("#2A2414", "#FFD9B2", "Pending"),
                OrderStatus.InProgress => ("#1A2040", "#93C5FD", "In Progress"),
                OrderStatus.Delivered  => ("#1E3A2A", "#A1E5A1", "Delivered"),
                OrderStatus.Cancelled  => ("#3A1A1A", "#FFBFB2", "Cancelled"),
                _                      => ("#333", "#ccc", val?.ToString() ?? "")
            };
            return @<span style="display:inline-block;padding:2px 10px;border-radius:999px;font-size:11px;font-weight:600;background:@bg;color:@fg">@label</span>;
        }
    };

    protected override void OnInitialized()
    {
        _orders = Store.Orders.AsQueryable();
    }

    private string GetCommands(PurchaseOrder order) => "fa-regular fa-pen-to-square";

    private void OnRowClicked(PurchaseOrder order, string command)
    {
        _selected = order;
    }

    private void OnSortFilter(ListViewSetting setting)
    {
        var query = Store.Orders.AsQueryable();

        foreach (var prop in setting.DisplaySettings.Where(w => !string.IsNullOrEmpty(w.Filter)))
            query = prop.GenerateWhere(query);

        var sorted = setting.DisplaySettings
            .Where(w => w.SortState != SortState.None)
            .OrderBy(o => o.SortPriority);

        foreach (var prop in sorted)
            query = prop.GenerateOrderBy(query);

        _orders = query;
        StateHasChanged();
    }
}
```

- [ ] **Step 2: Run and verify**

```bash
cd KBlazor.Showcase && dotnet run
```

Navigate to `/demo/flextable`. Verify:
- Table renders 10 orders with pagination
- Status column shows colored badges
- Clicking column headers sorts
- "Urgent" column is inline-editable
- DocPanel collapses/expands at the bottom

- [ ] **Step 3: Commit**

```bash
git add KBlazor.Showcase/Pages/Demo/FlexTableDemo.razor
git commit -m "feat: add FlexTable demo page with sort, filter, inline edit, and status badges"
```

---

## Task 10: Kanban demo page

**Files:**
- Create: `KBlazor.Showcase/Pages/Demo/KanbanDemo.razor`

- [ ] **Step 1: Create `KanbanDemo.razor`**

```razor
@* KBlazor.Showcase/Pages/Demo/KanbanDemo.razor *@
@page "/demo/kanban"
@inject DataStore Store

<PageTitle>Kanban View — KBlazor</PageTitle>

<div class="demo-layout">
    <NavMenu />
    <div class="demo-body">
        <div class="demo-header">
            <h2>Kanban View</h2>
            <p class="demo-sub">FlexTable in Kanban mode — group items into columns by any property.</p>
        </div>
        <div class="demo-content">
            <FlexTable TItem="PurchaseOrder"
                       Items="@_orders"
                       Fields="Order #,Customer,Amount"
                       DefaultViewMode="FlexTableViewMode.Kanban"
                       KanbanGroupField="Status"
                       KanbanColumns="New,Pending,InProgress,Delivered,Cancelled"
                       KanbanCardDisplayField="Order #"
                       SelectionChanged="OnRowClicked"
                       RenderTemplates="@_templates" />
        </div>
        <DocPanel ComponentName="FlexTable (Kanban)"
                  UsageCode="@DocContent.KanbanUsage"
                  Explanation="@DocContent.KanbanExplained" />
    </div>
</div>

<style>
    .demo-layout { display: flex; min-height: calc(100vh - 64px); }
    .demo-body { flex: 1; display: flex; flex-direction: column; }
    .demo-header { padding: 32px 32px 16px; border-bottom: 1px solid var(--border); }
    .demo-header h2 { margin: 0 0 8px; font-size: 28px; font-weight: 800; }
    .demo-sub { margin: 0; font-size: 15px; color: var(--text-muted); }
    .demo-content { flex: 1; padding: 24px 32px; overflow-x: auto; }
</style>

@code {
    private IQueryable<PurchaseOrder> _orders = default!;

    private Dictionary<Type, RenderFragment<object?>> _templates = new()
    {
        [typeof(OrderStatus)] = val =>
        {
            var status = (OrderStatus?)val;
            var (bg, fg, label) = status switch
            {
                OrderStatus.New        => ("#1C1A3A", "#B2CCFF", "New"),
                OrderStatus.Pending    => ("#2A2414", "#FFD9B2", "Pending"),
                OrderStatus.InProgress => ("#1A2040", "#93C5FD", "In Progress"),
                OrderStatus.Delivered  => ("#1E3A2A", "#A1E5A1", "Delivered"),
                OrderStatus.Cancelled  => ("#3A1A1A", "#FFBFB2", "Cancelled"),
                _                      => ("#333", "#ccc", val?.ToString() ?? "")
            };
            return @<span style="display:inline-block;padding:2px 10px;border-radius:999px;font-size:11px;font-weight:600;background:@bg;color:@fg">@label</span>;
        }
    };

    protected override void OnInitialized() =>
        _orders = Store.Orders.AsQueryable();

    private void OnRowClicked(PurchaseOrder order, string command) { }
}
```

- [ ] **Step 2: Run and verify**

Navigate to `/demo/kanban`. Verify 5 columns render with status headers as colored badges.

- [ ] **Step 3: Commit**

```bash
git add KBlazor.Showcase/Pages/Demo/KanbanDemo.razor
git commit -m "feat: add Kanban demo page"
```

---

## Task 11: BasicEdit demo page

**Files:**
- Create: `KBlazor.Showcase/Pages/Demo/BasicEditDemo.razor`

- [ ] **Step 1: Create `BasicEditDemo.razor`**

```razor
@* KBlazor.Showcase/Pages/Demo/BasicEditDemo.razor *@
@page "/demo/basicedit"
@inject DataStore Store

<PageTitle>BasicEdit — KBlazor</PageTitle>

<div class="demo-layout">
    <NavMenu />
    <div class="demo-body">
        <div class="demo-header">
            <h2>BasicEdit</h2>
            <p class="demo-sub">Auto-generated forms from [Display] attributes. Click any row to edit.</p>
        </div>
        <div class="demo-content">
            <div class="split-demo">
                <div class="order-list">
                    @foreach (var order in Store.Orders.Take(8))
                    {
                        <div class="order-row @(_editing?.Id == order.Id ? "active" : "")"
                             @onclick="() => SelectOrder(order)">
                            <span class="order-name">@order.Name</span>
                            <span class="order-customer">@order.Customer?.Name</span>
                            <span class="status-badge @order.Status.ToString().ToLower()">@order.Status</span>
                        </div>
                    }
                </div>
                <div class="edit-panel @(_editing != null ? "visible" : "")">
                    @if (_editing != null)
                    {
                        <BasicEdit TItem="PurchaseOrder"
                                   Item="@_editing"
                                   Save="SaveOrder"
                                   Close="CloseEditor"
                                   Columns="2" />
                    }
                    else
                    {
                        <div class="edit-placeholder">
                            <p>← Select an order to edit</p>
                        </div>
                    }
                </div>
            </div>
        </div>
        <DocPanel ComponentName="BasicEdit"
                  UsageCode="@DocContent.BasicEditUsage"
                  Explanation="@DocContent.BasicEditExplained" />
    </div>
</div>

<style>
    .demo-layout { display: flex; min-height: calc(100vh - 64px); }
    .demo-body { flex: 1; display: flex; flex-direction: column; }
    .demo-header { padding: 32px 32px 16px; border-bottom: 1px solid var(--border); }
    .demo-header h2 { margin: 0 0 8px; font-size: 28px; font-weight: 800; }
    .demo-sub { margin: 0; font-size: 15px; color: var(--text-muted); }
    .demo-content { flex: 1; padding: 24px 32px; }
    .split-demo { display: flex; gap: 24px; height: 100%; }
    .order-list { width: 320px; display: flex; flex-direction: column; gap: 4px; }
    .order-row { display: flex; align-items: center; gap: 12px; padding: 12px 16px; border-radius: 8px; cursor: pointer; border: 1px solid var(--border); background: var(--bg-card); }
    .order-row:hover { border-color: var(--accent); }
    .order-row.active { border-color: var(--accent); background: #1E1A3A; }
    .order-name { font-weight: 600; font-size: 13px; flex: 1; }
    .order-customer { font-size: 12px; color: var(--text-muted); flex: 1; }
    .edit-panel { flex: 1; background: var(--bg-card); border-radius: 12px; padding: 24px; border: 1px solid var(--border); }
    .edit-placeholder { display: flex; align-items: center; justify-content: center; height: 200px; color: var(--text-muted); }
    .status-badge { display: inline-block; padding: 2px 10px; border-radius: 999px; font-size: 11px; font-weight: 600; }
    .status-badge.new { background: #1C1A3A; color: #B2CCFF; }
    .status-badge.pending { background: #2A2414; color: #FFD9B2; }
    .status-badge.inprogress { background: #1A2040; color: #93C5FD; }
    .status-badge.delivered { background: #1E3A2A; color: #A1E5A1; }
    .status-badge.cancelled { background: #3A1A1A; color: #FFBFB2; }
</style>

@code {
    private PurchaseOrder? _editing;

    private void SelectOrder(PurchaseOrder order) => _editing = order;

    private void SaveOrder()
    {
        // In-memory: changes are already applied to the object in the DataStore list
        _editing = null;
        StateHasChanged();
    }

    private void CloseEditor()
    {
        _editing = null;
        StateHasChanged();
    }
}
```

- [ ] **Step 2: Run and verify**

Navigate to `/demo/basicedit`. Click a row — verify the form panel appears with the correct fields (Notes as textarea, OrderDate with time picker, etc.).

- [ ] **Step 3: Commit**

```bash
git add KBlazor.Showcase/Pages/Demo/BasicEditDemo.razor
git commit -m "feat: add BasicEdit demo with split list/form layout"
```

---

## Task 12: CycleStateButton, RelativeDatePicker, and Attributes demo pages

**Files:**
- Create: `KBlazor.Showcase/Pages/Demo/CycleStateDemo.razor`
- Create: `KBlazor.Showcase/Pages/Demo/DatePickerDemo.razor`
- Create: `KBlazor.Showcase/Pages/Demo/AttributesDemo.razor`

- [ ] **Step 1: Create `CycleStateDemo.razor`**

```razor
@* KBlazor.Showcase/Pages/Demo/CycleStateDemo.razor *@
@page "/demo/cyclestate"

<PageTitle>CycleStateButton — KBlazor</PageTitle>

<div class="demo-layout">
    <NavMenu />
    <div class="demo-body">
        <div class="demo-header">
            <h2>CycleStateButton</h2>
            <p class="demo-sub">A button that cycles through integer states on each click. Bind it to any int or enum property.</p>
        </div>
        <div class="demo-content">
            <div class="cycle-demo-grid">
                @for (int i = 0; i < 5; i++)
                {
                    var idx = i;
                    <div class="cycle-demo-card">
                        <div class="cycle-label">Order @(idx + 1) Status</div>
                        <CycleStateButton @bind-Value="_values[idx]"
                                          States="@_states"
                                          Labels="@_labels" />
                        <div class="cycle-value">Current: <strong>@((OrderStatus)_values[idx])</strong></div>
                    </div>
                }
            </div>
        </div>
        <DocPanel ComponentName="CycleStateButton"
                  UsageCode="@DocContent.CycleStateUsage"
                  Explanation="@DocContent.CycleStateExplained" />
    </div>
</div>

<style>
    .demo-layout { display: flex; min-height: calc(100vh - 64px); }
    .demo-body { flex: 1; display: flex; flex-direction: column; }
    .demo-header { padding: 32px 32px 16px; border-bottom: 1px solid var(--border); }
    .demo-header h2 { margin: 0 0 8px; font-size: 28px; font-weight: 800; }
    .demo-sub { margin: 0; font-size: 15px; color: var(--text-muted); }
    .demo-content { flex: 1; padding: 24px 32px; }
    .cycle-demo-grid { display: flex; flex-wrap: wrap; gap: 16px; }
    .cycle-demo-card { background: var(--bg-card); border: 1px solid var(--border); border-radius: 12px; padding: 24px; display: flex; flex-direction: column; gap: 16px; min-width: 220px; }
    .cycle-label { font-size: 12px; font-weight: 600; color: var(--text-muted); text-transform: uppercase; letter-spacing: 1px; }
    .cycle-value { font-size: 13px; color: var(--text-muted); }
    .cycle-value strong { color: var(--text-primary); }
</style>

@code {
    private int[] _values = new[] { 0, 1, 2, 3, 4 };
    private int[] _states = new[] { 0, 1, 2, 3, 4 };
    private string[] _labels = new[] { "New", "Pending", "In Progress", "Delivered", "Cancelled" };
}
```

- [ ] **Step 2: Create `DatePickerDemo.razor`**

```razor
@* KBlazor.Showcase/Pages/Demo/DatePickerDemo.razor *@
@page "/demo/datepicker"

<PageTitle>RelativeDatePicker — KBlazor</PageTitle>

<div class="demo-layout">
    <NavMenu />
    <div class="demo-body">
        <div class="demo-header">
            <h2>RelativeDatePicker</h2>
            <p class="demo-sub">Supports both absolute date selection and relative expressions like "This Week" or "Last Month".</p>
        </div>
        <div class="demo-content">
            <div class="picker-demo">
                <div class="picker-card">
                    <div class="picker-label">Selected value</div>
                    <RelativeDatePicker @bind-Value="_date" Label="Filter by date" />
                    <div class="picker-output">
                        @if (_date.HasValue)
                        {
                            <span>@_date.Value.ToString("yyyy-MM-dd HH:mm")</span>
                        }
                        else
                        {
                            <span style="color:var(--text-muted)">No date selected</span>
                        }
                    </div>
                </div>
                <div class="relative-examples">
                    <div class="example-label">Relative date strings supported by FlexTable filters:</div>
                    @foreach (var example in _examples)
                    {
                        <div class="example-chip">@example</div>
                    }
                </div>
            </div>
        </div>
        <DocPanel ComponentName="RelativeDatePicker"
                  UsageCode="@DocContent.DatePickerUsage"
                  Explanation="@DocContent.DatePickerExplained" />
    </div>
</div>

<style>
    .demo-layout { display: flex; min-height: calc(100vh - 64px); }
    .demo-body { flex: 1; display: flex; flex-direction: column; }
    .demo-header { padding: 32px 32px 16px; border-bottom: 1px solid var(--border); }
    .demo-header h2 { margin: 0 0 8px; font-size: 28px; font-weight: 800; }
    .demo-sub { margin: 0; font-size: 15px; color: var(--text-muted); }
    .demo-content { flex: 1; padding: 24px 32px; }
    .picker-demo { display: flex; gap: 40px; align-items: flex-start; flex-wrap: wrap; }
    .picker-card { background: var(--bg-card); border: 1px solid var(--border); border-radius: 12px; padding: 24px; display: flex; flex-direction: column; gap: 16px; min-width: 280px; }
    .picker-label { font-size: 12px; font-weight: 600; color: var(--text-muted); text-transform: uppercase; letter-spacing: 1px; }
    .picker-output { font-size: 14px; color: var(--text-primary); padding: 10px; background: var(--bg-sidebar); border-radius: 6px; }
    .relative-examples { display: flex; flex-direction: column; gap: 8px; }
    .example-label { font-size: 12px; font-weight: 600; color: var(--text-muted); text-transform: uppercase; letter-spacing: 1px; margin-bottom: 4px; }
    .example-chip { display: inline-block; background: #1E1A3A; color: #B2CCFF; padding: 4px 12px; border-radius: 999px; font-size: 13px; width: fit-content; }
</style>

@code {
    private DateTime? _date;
    private string[] _examples = new[]
    {
        "Today", "This Week", "This Month", "This Year",
        "Last Week", "Last Month", "Last Year",
        "Next Week", "Next Month", "Next Year"
    };
}
```

- [ ] **Step 3: Create `AttributesDemo.razor`**

```razor
@* KBlazor.Showcase/Pages/Demo/AttributesDemo.razor *@
@page "/demo/attributes"

<PageTitle>Attributes — KBlazor</PageTitle>

<div class="demo-layout">
    <NavMenu />
    <div class="demo-body">
        <div class="demo-header">
            <h2>Attributes</h2>
            <p class="demo-sub">KBlazor attributes decorate model properties to control how they appear in FlexTable and BasicEdit.</p>
        </div>
        <div class="demo-content">
            <div class="attr-grid">
                @foreach (var attr in _attrs)
                {
                    <div class="attr-card">
                        <div class="attr-name">@attr.Name</div>
                        <p class="attr-desc">@attr.Description</p>
                        <pre><code>@attr.Example</code></pre>
                        <div class="attr-applies">
                            @foreach (var tag in attr.AppliesTo)
                            {
                                <span class="applies-tag">@tag</span>
                            }
                        </div>
                    </div>
                }
            </div>
        </div>
    </div>
</div>

<style>
    .demo-layout { display: flex; min-height: calc(100vh - 64px); }
    .demo-body { flex: 1; display: flex; flex-direction: column; }
    .demo-header { padding: 32px 32px 16px; border-bottom: 1px solid var(--border); }
    .demo-header h2 { margin: 0 0 8px; font-size: 28px; font-weight: 800; }
    .demo-sub { margin: 0; font-size: 15px; color: var(--text-muted); }
    .demo-content { flex: 1; padding: 24px 32px; }
    .attr-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(340px, 1fr)); gap: 16px; }
    .attr-card { background: var(--bg-card); border: 1px solid var(--border); border-radius: 12px; padding: 20px; display: flex; flex-direction: column; gap: 10px; }
    .attr-name { font-size: 15px; font-weight: 700; color: var(--accent); }
    .attr-desc { margin: 0; font-size: 13px; color: var(--text-muted); line-height: 1.6; }
    .attr-card pre { margin: 0; font-size: 12px; }
    .attr-applies { display: flex; gap: 6px; flex-wrap: wrap; }
    .applies-tag { font-size: 11px; background: #1E1A3A; color: #B2CCFF; padding: 2px 8px; border-radius: 999px; }
</style>

@code {
    private record AttrInfo(string Name, string Description, string Example, string[] AppliesTo);

    private AttrInfo[] _attrs = new[]
    {
        new AttrInfo("[AllowInlineEdit]",
            "Marks a property editable directly within the FlexTable row. Use with the InlineEditor parameter.",
            "[Display(Name = \"Urgent\")]\n[AllowInlineEdit]\npublic bool IsUrgent { get; set; }",
            new[] { "FlexTable" }),

        new AttrInfo("[AlsoInclude]",
            "Tells BasicEdit to eager-load a related navigation property when resolving lookups.",
            "[AlsoInclude(Name = \"Lines\")]\npublic virtual ICollection<OrderLine> Lines { get; set; }",
            new[] { "BasicEdit" }),

        new AttrInfo("[AutoComplete]",
            "Renders the field as an autocomplete input in BasicEdit, populated from IEntityLookupProvider.",
            "[Display(Name = \"Customer\")]\n[AutoComplete]\npublic Guid? CustomerId { get; set; }",
            new[] { "BasicEdit" }),

        new AttrInfo("[CasscadeLookup]",
            "Creates a dependent dropdown filtered by another property's value.",
            "[Display(Name = \"Sub-Category\")]\n[CasscadeLookup(AdditionalProperties = \"CategoryId\")]\npublic Guid? SubCategoryId { get; set; }",
            new[] { "BasicEdit" }),

        new AttrInfo("[DisplayNoWrap]",
            "Prevents text wrapping in the FlexTable column for this property.",
            "[Display(Name = \"Amount\")]\n[DisplayNoWrap]\npublic decimal Amount { get; set; }",
            new[] { "FlexTable" }),

        new AttrInfo("[EnableTime]",
            "Enables time selection on a DateTime field in BasicEdit (default is date-only).",
            "[Display(Name = \"Order Date\")]\n[EnableTime]\npublic DateTime OrderDate { get; set; }",
            new[] { "BasicEdit" }),

        new AttrInfo("[LinkOnField]",
            "Renders the field value as a clickable link in FlexTable.",
            "[Display(Name = \"Reference\")]\n[LinkOnField]\npublic string Reference { get; set; }",
            new[] { "FlexTable" }),

        new AttrInfo("[MemoDisplay]",
            "Renders the field as a multiline textarea in BasicEdit instead of a single-line input.",
            "[Display(Name = \"Notes\")]\n[MemoDisplay]\npublic string Notes { get; set; }",
            new[] { "BasicEdit" }),

        new AttrInfo("[ReadOnlyOnEdit]",
            "Makes the field read-only when editing an existing entity (IsNew == false).",
            "[Display(Name = \"Order #\")]\n[ReadOnlyOnEdit]\npublic string Name { get; set; }",
            new[] { "BasicEdit" }),

        new AttrInfo("[SortAndFilterOn]",
            "Specifies that sort and filter should operate on a different member than the displayed property.",
            "[Display(Name = \"Status\")]\n[SortAndFilterOn(Member = \"Status\")]\npublic OrderStatus Status { get; set; }",
            new[] { "FlexTable" }),

        new AttrInfo("[ToolTipOnField]",
            "Displays a tooltip sourced from another property when hovering this field in FlexTable.",
            "[Display(Name = \"Name\")]\n[ToolTipOnField(PropertyName = \"Description\")]\npublic string Name { get; set; }",
            new[] { "FlexTable" }),
    };
}
```

- [ ] **Step 4: Build and run**

```bash
dotnet build KBlazor.Showcase/KBlazor.Showcase.csproj
cd KBlazor.Showcase && dotnet run
```

Verify all 6 sidebar links work and each demo page renders correctly.

- [ ] **Step 5: Commit**

```bash
git add KBlazor.Showcase/Pages/Demo/
git commit -m "feat: add CycleStateButton, RelativeDatePicker, and Attributes demo pages"
```

---

## Task 13: Docker + deployment config

**Files:**
- Create: `KBlazor.Showcase/Dockerfile`
- Create: `railway.json` (or `render.yaml`)

- [ ] **Step 1: Create `Dockerfile`**

```dockerfile
# KBlazor.Showcase/Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY KBlazor/KBlazor.csproj KBlazor/
COPY KBlazor.Showcase/KBlazor.Showcase.csproj KBlazor.Showcase/
RUN dotnet restore KBlazor.Showcase/KBlazor.Showcase.csproj

COPY KBlazor/ KBlazor/
COPY KBlazor.Showcase/ KBlazor.Showcase/
RUN dotnet publish KBlazor.Showcase/KBlazor.Showcase.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "KBlazor.Showcase.dll"]
```

- [ ] **Step 2: Create `railway.json`** (at repo root)

```json
{
  "$schema": "https://railway.app/railway.schema.json",
  "build": {
    "builder": "DOCKERFILE",
    "dockerfilePath": "KBlazor.Showcase/Dockerfile"
  },
  "deploy": {
    "startCommand": "dotnet KBlazor.Showcase.dll",
    "healthcheckPath": "/",
    "restartPolicyType": "ON_FAILURE"
  }
}
```

- [ ] **Step 3: Build the Docker image locally to verify**

```bash
cd "I:/Source/repos/KBlazor/.claude/worktrees/amazing-benz"
docker build -f KBlazor.Showcase/Dockerfile -t kblazor-showcase .
```

Expected: Build succeeds, image created.

- [ ] **Step 4: Test the Docker image**

```bash
docker run -p 8080:8080 kblazor-showcase
```

Open `http://localhost:8080` — verify the site runs from the Docker image.

- [ ] **Step 5: Add `.dockerignore`**

Create `KBlazor.Showcase/.dockerignore`:

```
**/bin/
**/obj/
**/.vs/
**/.git/
```

- [ ] **Step 6: Commit**

```bash
git add KBlazor.Showcase/Dockerfile KBlazor.Showcase/.dockerignore railway.json
git commit -m "feat: add Dockerfile and Railway deployment config"
```

---

## Task 14: Final run and PR

- [ ] **Step 1: Run all tests**

```bash
dotnet test KBlazor.Showcase.Tests/ -v normal
```

Expected: 13 tests pass, 0 failures.

- [ ] **Step 2: Full build**

```bash
dotnet build KBlazor.sln
```

Expected: 0 errors, 0 warnings.

- [ ] **Step 3: Smoke-test every page**

Run `dotnet run` from `KBlazor.Showcase/` and verify each route:
- `/` — Landing page with hero and table preview
- `/demo/flextable` — Table with sort, filter, inline edit, badges
- `/demo/kanban` — Kanban with 5 status columns
- `/demo/basicedit` — List + form split, form renders correctly
- `/demo/cyclestate` — 5 cycle buttons all cycling
- `/demo/datepicker` — Picker renders, relative examples shown
- `/demo/attributes` — 11 attribute cards in grid

- [ ] **Step 4: Commit and create PR**

```bash
git add -A
git commit -m "feat: complete KBlazor showcase site"
```

---

## Self-Review

**Spec coverage check:**

| Spec requirement | Task |
|---|---|
| Purchase Order domain with all attributes | Task 2 |
| 5 customers, 20 orders seed data | Task 3 |
| In-memory IFlexTableSettings | Task 4 |
| In-memory IListViewSettingStore | Task 4 |
| In-memory IEntityLookupProvider | Task 4 |
| Program.cs DI wiring | Task 5 |
| Dark themed layout + nav | Task 6 |
| Collapsible doc panels | Task 7 |
| DocContent strings from .md files | Task 7 |
| Landing page with hero | Task 8 |
| FlexTable demo (sort, filter, RenderTemplates, InlineEdit) | Task 9 |
| Kanban demo | Task 10 |
| BasicEdit demo | Task 11 |
| CycleStateButton demo | Task 12 |
| RelativeDatePicker demo | Task 12 |
| Attributes reference page (all 11 attributes) | Task 12 |
| Docker + Railway deployment | Task 13 |
