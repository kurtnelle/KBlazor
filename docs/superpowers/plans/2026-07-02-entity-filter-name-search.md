# Entity Filter Name Search Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a name-search field to FlexTable's entity checkbox filter so entities past the 100-item cap can be found and selected, and make it testable on the live showcase demo.

**Architecture:** A pure helper (`EntityFilterList.Build`) computes pinned-selected + name-matched + visible sets from an `IQueryable<IKBusinessEntity>`. A thin shared razor component (`EntityCheckboxFilter`) renders a search box + those sets and replaces two duplicated inline blocks in `FlexTable.razor`. Transient search text lives on `PropertySetting`. The showcase seeds >100 customers and exposes `PurchaseOrder.Customer` as a filterable entity column, routing demo filtering through `SortAndFilterEngine`.

**Tech Stack:** Blazor (.NET), MudBlazor, xUnit (`KBlazor.Showcase.Tests`), Newtonsoft.Json (used by `PropertySetting`).

---

## File Structure

**Library (`KBlazor`):**
- `KBlazor/Models/EntityFilterList.cs` — NEW. Pure helper: builds pinned/matches/visible. Testable.
- `KBlazor/Models/ListViewSetting.cs` — MODIFY. Add `EntitySearchText` transient property to `PropertySetting`.
- `KBlazor/Components/EntityCheckboxFilter.razor` — NEW. Search box + checkbox list render shell.
- `KBlazor/Components/FlexTable.razor` — MODIFY. Replace the two inline entity-checkbox blocks with `<EntityCheckboxFilter>`.

**Showcase (`KBlazor.Showcase`) — live-demo test surface:**
- `KBlazor.Showcase/Data/SeedData.cs` — MODIFY. Append ~145 deterministic customers.
- `KBlazor.Showcase/Domain/PurchaseOrder.cs` — MODIFY. Annotate `Customer` nav prop as a filterable entity column.
- `KBlazor.Showcase/Pages/Demo/FlexTableDemo.razor` — MODIFY. Add column to `Fields`; route `OnSortFilter` through the engine.

**Tests (`KBlazor.Showcase.Tests`):**
- `KBlazor.Showcase.Tests/EntityFilterListTests.cs` — NEW.
- `KBlazor.Showcase.Tests/PropertySettingSearchTests.cs` — NEW.
- `KBlazor.Showcase.Tests/SeedDataTests.cs` — MODIFY (customer count changed 5 → 150).

---

## Task 1: `EntityFilterList` pure helper

**Files:**
- Create: `KBlazor/Models/EntityFilterList.cs`
- Test: `KBlazor.Showcase.Tests/EntityFilterListTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `KBlazor.Showcase.Tests/EntityFilterListTests.cs`:

```csharp
using KBlazor.Models;
using KBlazor.Showcase.Domain;
using Xunit;

namespace KBlazor.Showcase.Tests;

public class EntityFilterListTests
{
    private static IQueryable<IKBusinessEntity> MakeCustomers(int count) =>
        Enumerable.Range(1, count)
            .Select(i => (IKBusinessEntity)new Customer { Id = Guid.NewGuid(), Name = $"Cust {i:000}" })
            .AsQueryable();

    [Fact]
    public void EmptySearch_ReturnsFirstCapAsMatches()
    {
        var list = MakeCustomers(150);
        var result = EntityFilterList.Build(list, Array.Empty<Guid>(), search: null, cap: 100);
        Assert.Empty(result.Pinned);
        Assert.Equal(100, result.Matches.Count);
        Assert.Equal(100, result.Visible.Count);
    }

    [Fact]
    public void Search_FiltersByNameContains_BeyondCap()
    {
        var list = MakeCustomers(150).ToList();
        var target = list[141]; // "Cust 142", index 141 (beyond first 100)
        var result = EntityFilterList.Build(list.AsQueryable(), Array.Empty<Guid>(), search: "142", cap: 100);
        Assert.Contains(result.Matches, m => m.Id == target.Id);
    }

    [Fact]
    public void SelectedItems_ArePinned_RegardlessOfSearch()
    {
        var list = MakeCustomers(150).ToList();
        var selected = list[141]; // "Cust 142"
        var result = EntityFilterList.Build(
            list.AsQueryable(), new[] { selected.Id }, search: "999-no-match", cap: 100);
        Assert.Contains(result.Pinned, p => p.Id == selected.Id);
        Assert.Contains(result.Visible, v => v.Id == selected.Id);
    }

    [Fact]
    public void PinnedItems_AreNotDuplicatedInMatches()
    {
        var list = MakeCustomers(10).ToList();
        var selected = list[0]; // "Cust 001" — also matches empty search
        var result = EntityFilterList.Build(
            list.AsQueryable(), new[] { selected.Id }, search: null, cap: 100);
        Assert.Single(result.Pinned);
        Assert.DoesNotContain(result.Matches, m => m.Id == selected.Id);
        Assert.Equal(10, result.Visible.Count); // 1 pinned + 9 matches, no dupes
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test KBlazor.Showcase.Tests --filter EntityFilterListTests`
Expected: FAIL — `EntityFilterList` does not exist (compile error).

- [ ] **Step 3: Write the implementation**

Create `KBlazor/Models/EntityFilterList.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;

namespace KBlazor.Models
{
    /// <summary>
    /// Pure computation for the entity checkbox filter: given the candidate
    /// entities, the currently-selected ids, and a name search string, produce
    /// the pinned (always-visible selected), matched (name search, capped), and
    /// combined visible sets. Kept free of UI so it can be unit tested.
    /// </summary>
    public static class EntityFilterList
    {
        public static EntityFilterResult Build(
            IQueryable<IKBusinessEntity> list,
            Guid[] selectedIds,
            string? search,
            int cap = 100)
        {
            var selected = selectedIds ?? Array.Empty<Guid>();

            var pinned = list
                .Where(w => selected.Contains(w.Id))
                .ToList()
                .OrderBy(p => p.ToString(), StringComparer.Ordinal)
                .ToList();

            IQueryable<IKBusinessEntity> matchQuery = string.IsNullOrWhiteSpace(search)
                ? list
                : list.Where(w => w.Name.Contains(search));

            var matches = matchQuery
                .Take(cap)
                .ToList()
                .Where(m => !selected.Contains(m.Id))
                .OrderBy(m => m.ToString(), StringComparer.Ordinal)
                .ToList();

            var visible = pinned.Concat(matches).ToList();

            return new EntityFilterResult(pinned, matches, visible);
        }
    }

    public sealed record EntityFilterResult(
        IReadOnlyList<IKBusinessEntity> Pinned,
        IReadOnlyList<IKBusinessEntity> Matches,
        IReadOnlyList<IKBusinessEntity> Visible);
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test KBlazor.Showcase.Tests --filter EntityFilterListTests`
Expected: PASS (4 tests).

- [ ] **Step 5: Commit**

```bash
git add KBlazor/Models/EntityFilterList.cs KBlazor.Showcase.Tests/EntityFilterListTests.cs
git commit -m "feat: add EntityFilterList pure helper for entity filter search"
```

---

## Task 2: `EntitySearchText` transient property on `PropertySetting`

**Files:**
- Modify: `KBlazor/Models/ListViewSetting.cs` (near the existing `AutoCompleteText`, ~line 344)
- Test: `KBlazor.Showcase.Tests/PropertySettingSearchTests.cs`

- [ ] **Step 1: Write the failing test**

Create `KBlazor.Showcase.Tests/PropertySettingSearchTests.cs`:

```csharp
using System.Reflection;
using KBlazor.Models;
using Xunit;

namespace KBlazor.Showcase.Tests;

public class PropertySettingSearchTests
{
    [Fact]
    public void EntitySearchText_Exists_AndIsJsonIgnored()
    {
        var prop = typeof(PropertySetting).GetProperty("EntitySearchText");
        Assert.NotNull(prop);
        // Must be [JsonIgnore] so UI scratch state never leaks into shared/persisted views.
        var jsonIgnore = prop!.GetCustomAttribute<Newtonsoft.Json.JsonIgnoreAttribute>();
        Assert.NotNull(jsonIgnore);
    }

    [Fact]
    public void EntitySearchText_IsReadWrite()
    {
        var setting = new PropertySetting { EntitySearchText = "abc" };
        Assert.Equal("abc", setting.EntitySearchText);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test KBlazor.Showcase.Tests --filter PropertySettingSearchTests`
Expected: FAIL — `EntitySearchText` does not exist (compile error).

- [ ] **Step 3: Add the property**

In `KBlazor/Models/ListViewSetting.cs`, immediately after the existing `AutoCompleteText` property (search for `public string AutoCompleteText { get; set; }`), add:

```csharp
        [JsonIgnore]
        public string EntitySearchText { get; set; }
```

(The file already has `using Newtonsoft.Json;`, so `[JsonIgnore]` resolves to Newtonsoft's — matching `FilterDialogIsOpen` and `AutoCompleteText`.)

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test KBlazor.Showcase.Tests --filter PropertySettingSearchTests`
Expected: PASS (2 tests).

- [ ] **Step 5: Commit**

```bash
git add KBlazor/Models/ListViewSetting.cs KBlazor.Showcase.Tests/PropertySettingSearchTests.cs
git commit -m "feat: add transient EntitySearchText to PropertySetting"
```

---

## Task 3: `EntityCheckboxFilter` render component

**Files:**
- Create: `KBlazor/Components/EntityCheckboxFilter.razor`

This is a razor UI component (no unit test — verified by build + the Task 8 manual demo). Depends on Task 1 (`EntityFilterList`) and Task 2 (`EntitySearchText`).

- [ ] **Step 1: Create the component**

Create `KBlazor/Components/EntityCheckboxFilter.razor`:

```razor
@* Shared entity checkbox filter with name search. Used by FlexTable's filter dialogs. *@

@{
    var selectedIds = Setting.GetFilterEntries();
    var model = EntityFilterList.Build(List, selectedIds, Setting.EntitySearchText);
    var visibleIds = model.Visible.Select(v => v.Id).ToList();
    var selectedVisible = model.Visible.Count(v => selectedIds.Contains(v.Id));
    bool? selectAllValue = selectedVisible == 0
        ? false
        : selectedVisible == model.Visible.Count ? true : (bool?)null;
}

<MudTextField T="string"
              @bind-Value="Setting.EntitySearchText"
              Label="Search by name"
              Immediate="true"
              DebounceInterval="300"
              Clearable="true"
              FullWidth />

<MudCheckBox T="bool?" Dense="true" TriState="true"
             Value="@selectAllValue"
             ValueChanged="@(e => OnSelectAll(e, visibleIds, selectedIds))">
    Select All (visible)
</MudCheckBox>
<br />

<div style="max-height:400px; overflow-y:auto;">
    @foreach (var item in model.Pinned)
    {
        <MudCheckBox T="bool" Dense="true"
                     Value="@true"
                     ValueChanged="@(e => { if (!e) { Setting.RemoveFromFilter(item.Id); } })">
            @item.ToString()
        </MudCheckBox>
        <br />
    }
    @if (model.Pinned.Any() && model.Matches.Any())
    {
        <MudDivider Style="margin:6px 0;" />
    }
    @foreach (var item in model.Matches)
    {
        <MudCheckBox T="bool" Dense="true"
                     Value="@false"
                     ValueChanged="@(e => { if (e) { Setting.AddToFilter(item.Id); } })">
            @item.ToString()
        </MudCheckBox>
        <br />
    }
</div>

@code {
    [Parameter, EditorRequired]
    public PropertySetting Setting { get; set; } = default!;

    [Parameter, EditorRequired]
    public IQueryable<IKBusinessEntity> List { get; set; } = default!;

    private void OnSelectAll(bool? value, List<Guid> visibleIds, Guid[] selectedIds)
    {
        if (value != false)
        {
            var union = selectedIds.Concat(visibleIds).Distinct();
            Setting.Filter = string.Join(',', union);
        }
        else
        {
            var remaining = selectedIds.Except(visibleIds);
            Setting.Filter = string.Join(',', remaining);
        }
    }
}
```

Notes:
- Pinned items render as always-checked; unchecking one calls `RemoveFromFilter`.
- Match items render as unchecked (pinned/selected are excluded from `Matches` by `EntityFilterList`); checking one calls `AddToFilter`. After the state change Blazor re-renders, so a just-checked match becomes a pinned item on the next render.
- `_Imports.razor` already brings in `KBlazor.Models`, `KBlazor.Components`, and `MudBlazor`, so no `@using` lines are needed.

- [ ] **Step 2: Build to verify it compiles**

Run: `dotnet build KBlazor`
Expected: Build succeeded, 0 errors.

- [ ] **Step 3: Commit**

```bash
git add KBlazor/Components/EntityCheckboxFilter.razor
git commit -m "feat: add EntityCheckboxFilter component with name search"
```

---

## Task 4: Use `EntityCheckboxFilter` in both FlexTable filter dialogs

**Files:**
- Modify: `KBlazor/Components/FlexTable.razor` (card-view block ~line 271-291; table-view block ~line 439-490)

- [ ] **Step 1: Replace the card-view entity block**

In `KBlazor/Components/FlexTable.razor`, find the FIRST entity block (inside the card-view filter dialog). It currently reads:

```razor
                    else if (EntityProvider.IsKnownEntityType(setting.PropertyInfo.PropertyType))
                    {
                        var p = setting.PropertyInfo;
                        var alsoInclude = (AlsoIncludeAttribute)p.PropertyType.GetCustomAttribute(typeof(AlsoIncludeAttribute));
                        IQueryable<IKBusinessEntity> list = alsoInclude != null
                            ? EntityProvider.GetEntitiesWithInclude(p.PropertyType, alsoInclude.Name)
                            : EntityProvider.GetEntities(p.PropertyType);
                        var listItems = list.Take(100);
                        var completeListStr = string.Join(',', listItems.Select(s => s.Id).ToList());
                        <div style="max-height:400px; overflow-y:auto;">
                            <MudCheckBox T="bool?" Dense="true" TriState="true"
                                         Value="@(string.IsNullOrEmpty(setting.Filter) ? false : setting.Filter.Split(',').Length == listItems.Count() ? true : null)"
                                         ValueChanged="@(e => { if (e != false) { setting.Filter = completeListStr; } else { setting.Filter = string.Empty; } })">Select All</MudCheckBox><br />
                            @foreach (var item in listItems.ToList().OrderBy(o => o.ToString()))
                            {
                                <MudCheckBox T="bool" Dense="true"
                                             Value="@(setting.Filter.Contains(item.Id.ToString()))"
                                             ValueChanged="@(e => { if (e) { setting.AddToFilter(item.Id); } else { setting.RemoveFromFilter(item.Id); } })">@item.ToString()</MudCheckBox><br />
                            }
                        </div>
                    }
```

Replace the entire block with:

```razor
                    else if (EntityProvider.IsKnownEntityType(setting.PropertyInfo.PropertyType))
                    {
                        var p = setting.PropertyInfo;
                        var alsoInclude = (AlsoIncludeAttribute)p.PropertyType.GetCustomAttribute(typeof(AlsoIncludeAttribute));
                        IQueryable<IKBusinessEntity> list = alsoInclude != null
                            ? EntityProvider.GetEntitiesWithInclude(p.PropertyType, alsoInclude.Name)
                            : EntityProvider.GetEntities(p.PropertyType);
                        <EntityCheckboxFilter Setting="setting" List="list" />
                    }
```

- [ ] **Step 2: Replace the table-view checkbox branch**

Find the SECOND entity block (inside the table-view filter dialog). It has a `useAutoComplete` branch followed by an `else` branch. Replace ONLY the `else` branch, which currently reads:

```razor
                                        else
                                        {
                                            var listItems = list.Take(100);
                                            var completeList = string.Join(',', listItems.Select(s => s.Id).ToList());
                                            <div style="height:800px; width:400px;">
                                                <MudCheckBox T="bool?" Dense="true"
                                                             TriState="true"
                                                             Value="@(string.IsNullOrEmpty(setting.Filter) ? false : setting.Filter.Split(',').Length == listItems.Count() ? true : null)"
                                                             ValueChanged="@(e => { if (e != false) { setting.Filter = completeList; } else { setting.Filter = string.Empty; } })">
                                                    Select All
                                                </MudCheckBox>
                                                <br />
                                                @foreach (var item in listItems.ToList().OrderBy(o => o.ToString()))
                                                {
                                                    <MudCheckBox T="bool" Dense="true"
                                                                 Value="@(setting.Filter.Contains(item.Id.ToString()))"
                                                                 ValueChanged="@(e => { if (e) { setting.AddToFilter(item.Id); } else { setting.RemoveFromFilter(item.Id); } })">
                                                        @item.ToString()
                                                    </MudCheckBox>
                                                    <br />
                                                }
                                            </div>
                                        }
```

Replace that `else { ... }` block with:

```razor
                                        else
                                        {
                                            <div style="width:400px;">
                                                <EntityCheckboxFilter Setting="setting" List="list" />
                                            </div>
                                        }
```

Leave the `useAutoComplete` branch (the `MudAutocomplete` + chips) exactly as-is.

- [ ] **Step 3: Build to verify it compiles**

Run: `dotnet build KBlazor`
Expected: Build succeeded, 0 errors.

- [ ] **Step 4: Commit**

```bash
git add KBlazor/Components/FlexTable.razor
git commit -m "refactor: use EntityCheckboxFilter in both FlexTable filter dialogs"
```

---

## Task 5: Seed >100 lookup customers

**Files:**
- Modify: `KBlazor.Showcase/Data/SeedData.cs`
- Test: `KBlazor.Showcase.Tests/SeedDataTests.cs`

- [ ] **Step 1: Update the failing tests**

In `KBlazor.Showcase.Tests/SeedDataTests.cs`, replace the `Seed_Produces5Customers` test with the following two tests (leave the other tests unchanged):

```csharp
    [Fact]
    public void Seed_Produces150Customers()
    {
        var store = SeedData.Create();
        Assert.Equal(150, store.Customers.Count);
    }

    [Fact]
    public void Seed_HasSearchableCustomerBeyondFirst100()
    {
        var store = SeedData.Create();
        // A customer whose name matches "142" must exist AND sit beyond the
        // first 100 in store order, so it is only reachable via search.
        var index = store.Customers.FindIndex(c => c.Name.Contains("142"));
        Assert.True(index >= 100, $"Expected a '142' customer beyond index 100, found at {index}.");
    }
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test KBlazor.Showcase.Tests --filter SeedDataTests`
Expected: FAIL — `Seed_Produces150Customers` (still 5) and `Seed_HasSearchableCustomerBeyondFirst100` (index -1).

- [ ] **Step 3: Append generated customers**

In `KBlazor.Showcase/Data/SeedData.cs`, immediately after the existing line
`store.Customers.AddRange(new[] { acme, globex, initech, umbrella, soylent });`
add:

```csharp
        // Lookup fillers: exercise the entity filter's name search past the
        // 100-item cap. Appended AFTER the 5 named customers, so numbers >100
        // land beyond the first 100 in store order and are only reachable via
        // search. Only the 5 named customers above receive orders.
        for (int i = 6; i <= 150; i++)
        {
            store.Customers.Add(new Customer
            {
                Id = Guid.NewGuid(),
                Name = $"Test Customer {i:000}",
                Email = $"customer{i:000}@example.com",
                Country = "USA"
            });
        }
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test KBlazor.Showcase.Tests --filter SeedDataTests`
Expected: PASS (all SeedDataTests, including the two updated).

- [ ] **Step 5: Commit**

```bash
git add KBlazor.Showcase/Data/SeedData.cs KBlazor.Showcase.Tests/SeedDataTests.cs
git commit -m "test: seed 150 customers to exercise entity filter search past cap"
```

---

## Task 6: Expose `Customer` as a filterable entity column

**Files:**
- Modify: `KBlazor.Showcase/Domain/PurchaseOrder.cs`

- [ ] **Step 1: Annotate the navigation property**

In `KBlazor.Showcase/Domain/PurchaseOrder.cs`, the `Customer` nav property currently reads:

```csharp
    public virtual Customer? Customer { get; set; }
```

Replace it with:

```csharp
    [Display(Name = "Customer (lookup)", Order = 2)]
    [SortAndFilterOn(FilterPath = "CustomerId", SortPath = "Customer.Name")]
    public virtual Customer? Customer { get; set; }
```

Rationale: `FilterPath = "CustomerId"` makes `SortAndFilterEngine.ApplyFilterByPath` build `selectedIds.Contains(o.CustomerId)`; `SortPath = "Customer.Name"` prevents `GenerateOrderBy` from throwing on the complex `Customer` type. (`using KBlazor.Attributes;` and `using System.ComponentModel.DataAnnotations;` are already present.)

- [ ] **Step 2: Build to verify it compiles**

Run: `dotnet build KBlazor.Showcase`
Expected: Build succeeded, 0 errors.

- [ ] **Step 3: Commit**

```bash
git add KBlazor.Showcase/Domain/PurchaseOrder.cs
git commit -m "feat(demo): expose PurchaseOrder.Customer as a filterable entity column"
```

---

## Task 7: Wire the demo column and route filtering through the engine

**Files:**
- Modify: `KBlazor.Showcase/Pages/Demo/FlexTableDemo.razor`

- [ ] **Step 1: Add the column to the FlexTable `Fields`**

In `KBlazor.Showcase/Pages/Demo/FlexTableDemo.razor`, the `<FlexTable>` has:

```razor
                           Fields="Order #,Customer,Status,Amount,Order Date,Urgent"
```

Replace it with (adds the new entity column after the existing string one):

```razor
                           Fields="Order #,Customer,Customer (lookup),Status,Amount,Order Date,Urgent"
```

- [ ] **Step 2: Route `OnSortFilter` through the engine**

In the same file, the `OnSortFilter` method currently reads:

```csharp
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
```

Replace the whole method body with:

```csharp
    private void OnSortFilter(ListViewSetting setting)
    {
        // Route through the library engine so entity columns annotated with
        // [SortAndFilterOn(FilterPath=...)] actually filter (selectedIds.Contains).
        // Existing columns set no FilterPath, so they fall back to GenerateWhere
        // exactly as before — behavior-preserving.
        _orders = Store.Orders.AsQueryable()
            .ApplyFilter(setting)
            .ApplySort(setting);
        StateHasChanged();
    }
```

(`ApplyFilter`/`ApplySort` are extension methods in `KBlazor.Models.SortAndFilterEngine`; `KBlazor.Models` is already imported in the Showcase `_Imports.razor`.)

- [ ] **Step 3: Build to verify it compiles**

Run: `dotnet build KBlazor.Showcase`
Expected: Build succeeded, 0 errors.

- [ ] **Step 4: Commit**

```bash
git add KBlazor.Showcase/Pages/Demo/FlexTableDemo.razor
git commit -m "feat(demo): show Customer lookup column and filter via engine"
```

---

## Task 8: Full verification (build, tests, live demo)

**Files:** none (verification only)

- [ ] **Step 1: Full solution build**

Run: `dotnet build`
Expected: Build succeeded, 0 errors across all projects.

- [ ] **Step 2: Full test run**

Run: `dotnet test`
Expected: PASS — all tests including `EntityFilterListTests`, `PropertySettingSearchTests`, and updated `SeedDataTests`.

- [ ] **Step 3: Live demo verification**

Run the showcase (`dotnet run --project KBlazor.Showcase`) and open `/demo/flextable`. Confirm each:

1. The **"Customer (lookup)"** column appears and cells show the customer name.
2. Opening its filter shows the **search box + checkbox list** (the new `EntityCheckboxFilter`), first 100 customers listed.
3. Typing `142` surfaces **"Test Customer 142"** (which is beyond the first 100). Check it.
4. Changing the search to `999` (no match) — the checked **"Test Customer 142" stays pinned/visible** at the top.
5. Clicking **OK** filters the grid to that customer's orders; reopening the dialog shows the selection persisted.
6. **Regression:** the existing text "Customer" column still text-filters; Status/Order Date/Urgent still filter and sort.

- [ ] **Step 4: Final commit (if any verification tweaks were needed)**

```bash
git add -A
git commit -m "chore: entity filter name search verified end-to-end"
```

(If no changes were needed in Step 3, skip this commit.)
