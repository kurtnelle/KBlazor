# Entity Filter Name Search — Design

**Date:** 2026-07-02
**Status:** Approved (pending spec review)
**Component area:** `KBlazor/Components/FlexTable.razor`

## Problem

The column filter dialogs in `FlexTable` render a checkbox list of related
entities for any property whose type is a known business entity
(`EntityProvider.IsKnownEntityType(...)`). That list is hard-capped at
`list.Take(100)`. When an entity table has more than 100 rows, entities beyond
the first 100 cannot be found or selected as a filter value — there is no way
to reach them.

There is an existing `[AutoComplete]` attribute path that swaps the checkbox
list for a `MudAutocomplete`, but it (a) requires decorating the property, (b)
replaces the multi-select checkbox UX entirely, and (c) searches on
`ToString().Contains(...)`, which does not translate to SQL.

## Goal

Add a name search field to the entity checkbox filter so users can type part of
an entity's name and select from matching results, without the 100-item ceiling
hiding the entity they want. Preserve the existing checkbox multi-select UX.

## Non-Goals

- No change to the `[AutoComplete]` path.
- No change to `BasicEdit` entity selection.
- No guaranteed cross-provider case-insensitivity (see Case Sensitivity below).
- No server-side paging beyond the existing `Take(100)` cap on results.

## Approach

**Extract a shared component.** The card-view filter dialog (~line 279) and the
table-view filter dialog (~line 474) each render a near-identical entity
checkbox block. Both are replaced by a single new component:

```
KBlazor/Components/EntityCheckboxFilter.razor
```

Rationale: the two blocks are duplicates that must stay in sync; consolidating
them removes the drift risk that recent view-state fixes (commits `482c3d3`,
`f43052b`) were addressing, and gives the new search logic exactly one home.

### Component contract

`EntityCheckboxFilter`

- **Parameters:**
  - `PropertySetting Setting` — the per-column setting holding `Filter` (the
    comma-separated selected Guid list) and the new transient search text.
  - `IQueryable<IKBusinessEntity> List` — the candidate entities (already
    resolved by the caller via `GetEntities` / `GetEntitiesWithInclude`, so the
    `AlsoInclude` handling stays in the dialog and out of this component).
- **Renders:** search text field, pinned selected checkboxes, matched
  checkboxes, and a "Select All" tri-state checkbox.
- **Depends on:** MudBlazor components only; no service injection. All state
  lives on the passed-in `Setting`.

The caller keeps responsibility for resolving `List` (including the
`AlsoIncludeAttribute` branch) and for calling `SortAndFilter()` on dialog OK,
exactly as today.

## State

Add one transient field to `PropertySetting` in
`KBlazor/Models/ListViewSetting.cs`, mirroring the existing `AutoCompleteText`
and `FilterDialogIsOpen`:

```csharp
[JsonIgnore]
public string EntitySearchText { get; set; }
```

`[JsonIgnore]` keeps it out of persisted/shared views — it is UI-only scratch
state, reset naturally when a new setting object is loaded.

## Search / render logic

Computed on each render inside `EntityCheckboxFilter`:

```
var search = Setting.EntitySearchText;
var selectedIds = Setting.GetFilterEntries();          // Guid[]

// Pinned: always-visible current selections, regardless of search.
var pinned = List.Where(w => selectedIds.Contains(w.Id)).ToList();

// Matches: name search, capped, excluding already-pinned items.
var matches = (string.IsNullOrWhiteSpace(search)
        ? List
        : List.Where(w => w.Name.Contains(search)))
    .Take(100)
    .ToList()
    .Where(m => !selectedIds.Contains(m.Id))
    .OrderBy(m => m.ToString())
    .ToList();

var visible = pinned.Concat(matches).ToList();          // Select-All scope
```

Render order:

1. Search `MudTextField` bound to `Setting.EntitySearchText`,
   `Immediate="true"`, `DebounceInterval="300"`, with a clear adornment.
2. "Select All" tri-state `MudCheckBox` (see semantics below).
3. Pinned selected items (checked), then a thin divider, then matched items.

Each item is a `MudCheckBox` whose checked state is
`selectedIds.Contains(item.Id)`, toggling `Setting.AddToFilter(item.Id)` /
`Setting.RemoveFromFilter(item.Id)` — identical to today's per-item behavior.

## "Select All" semantics

"Select All" operates on the **currently visible set** (`visible` = pinned +
matches), never the entire table.

- **Checked value:** the completed id list = `visible.Select(v => v.Id)`.
- **Tri-state:**
  - unchecked when no visible item is selected,
  - checked when all visible items are selected,
  - indeterminate when some are.
- **On check:** add all visible ids to the filter. **On uncheck:** clear the
  filter (matches existing behavior).

For an empty search this yields the same result as today (first 100 items),
so there is no regression for the common small-table case.

## Case sensitivity

The search is **case-insensitive across all providers**. It lowercases both
sides: `w.Name.ToLower().Contains(searchLower)`. This avoids the
`StringComparison` overload (which doesn't translate in EF/SQL providers) while
still being translatable — EF renders it as `LOWER(Name) LIKE '%...%'` — and it
works ordinally on the in-memory showcase provider.

(An earlier revision used plain `w.Name.Contains(search)` and left case
sensitivity to the provider; that made the in-memory showcase provider
case-sensitive — e.g. "soy" missed "Soylent Corp" — so the search now lowercases
explicitly.)

## Live demo test surface

The feature cannot be exercised on the current showcase: the `FlexTable` demo
(`/demo/flextable`) has **no entity-typed column**, so the entity checkbox
filter never renders. Its "Customer" column is `PurchaseOrder.CustomerName`
(a `string`), which produces a *text* filter, not the entity selector. And even
if an entity column existed, the demo seeds only 5 customers — far below the
100 cap. Three showcase changes make it testable:

### 1. Seed >100 lookup customers (`KBlazor.Showcase/Data/SeedData.cs`)

Keep the 5 existing named customers, then programmatically append ~145 more with
deterministic, searchable names (e.g. `"Test Customer 006"` … `"Test Customer
150"`). They are **appended after** the 5 named customers, so entities numbered
past ~095 fall **beyond the first 100 in store/insertion order** — which is the
order `list.Take(100)` samples *before* the display `OrderBy`. That guarantees a
tester can only reach, say, `"Test Customer 142"` by searching for it, proving
the ceiling is lifted. Only the original 5 need orders; the rest exist purely to
populate the lookup (realistic: many customers, few orders shown).

### 2. Expose an entity-typed column (`KBlazor.Showcase/Domain/PurchaseOrder.cs`)

Annotate the existing `Customer` navigation property (type `Customer`, a known
entity) so it renders as a filterable column:

```csharp
[Display(Name = "Customer (lookup)", Order = 2)]
[SortAndFilterOn(FilterPath = "CustomerId", SortPath = "Customer.Name")]
public virtual Customer? Customer { get; set; }
```

`FilterPath = "CustomerId"` routes filtering through
`SortAndFilterEngine.ApplyFilterByPath`, whose `Guid?` branch builds
`selectedIds.Contains(o.CustomerId)` — so selecting customers in the new search
selector actually filters the grid. `SortPath = "Customer.Name"` avoids
`GenerateOrderBy` throwing on a complex type. Add `"Customer (lookup)"` to the
demo's `Fields` list (the existing string "Customer" column stays, giving a nice
side-by-side of text-filter vs. entity-search-filter).

### 3. Route demo filtering through the engine (`FlexTableDemo.razor`)

`OnSortFilter` currently calls `prop.GenerateWhere(query)` in a manual loop,
which bypasses `FilterPath` and has **no branch for entity-typed columns**
(it returns the query unchanged). Replace the manual filter/sort loops with the
library engine:

```csharp
query = query.ApplyFilter(setting).ApplySort(setting);
```

For every existing column this is behavior-preserving (none set `FilterPath`, so
they fall back to `GenerateWhere` exactly as today); for the new `Customer`
column it activates the `CustomerId`-Contains path. This also demonstrates the
library's *intended* consumption pattern.

> Note on a library gap discovered here: `PropertySetting.GenerateWhere` has no
> case for entity-typed properties (falls through to `return query`). Entity
> filtering is only supported via `[SortAndFilterOn(FilterPath=...)]` +
> `SortAndFilterEngine`. Closing that gap generically is a possible follow-up;
> out of scope for this change.

### Manual test steps

1. Run the showcase, open `/demo/flextable`.
2. Open the filter on the **"Customer (lookup)"** column → the entity selector
   with the new search field appears.
3. With an empty search, confirm the first 100 customers show as checkboxes.
4. Type `"142"` (or another number > 100) → the matching customer appears even
   though it is not in the first 100. Check it.
5. Change the search to something else → the checked customer stays pinned/
   visible (always-show-selected).
6. Click OK → the grid filters to orders for the selected customer(s), and the
   selection persists when reopening the dialog.

## Testing

- **Live demo:** the manual steps above (the primary acceptance check).
- **Unit coverage** (where the existing test project allows): `PropertySetting`
  add/remove filter round-trips with `EntitySearchText` set, and that
  `EntitySearchText` is not serialized (`[JsonIgnore]`).
- **Regression:** both dialogs still open, filter, and persist correctly; empty
  search behaves identically to the previous `Take(100)` list; existing demo
  columns (text/date/enum/bool) still filter and sort after the `OnSortFilter`
  switch to the engine.

## Files touched

**Library:**
- `KBlazor/Components/EntityCheckboxFilter.razor` — new shared component.
- `KBlazor/Components/FlexTable.razor` — replace the two inline entity checkbox
  blocks with the new component.
- `KBlazor/Models/ListViewSetting.cs` — add `EntitySearchText`.

**Showcase (live-demo test surface):**
- `KBlazor.Showcase/Data/SeedData.cs` — append ~145 deterministic customers.
- `KBlazor.Showcase/Domain/PurchaseOrder.cs` — expose `Customer` as a filterable
  entity column.
- `KBlazor.Showcase/Pages/Demo/FlexTableDemo.razor` — add the column to `Fields`
  and route `OnSortFilter` through `ApplyFilter`/`ApplySort`.
