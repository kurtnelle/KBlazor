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

The search uses plain `w.Name.Contains(search)` with no `StringComparison`
overload, because the overload does not translate in EF/SQL query providers.
Effective case sensitivity therefore follows the provider:

- SQLite / SQL Server `LIKE` — case-insensitive by default.
- The in-memory showcase provider (`InMemoryEntityLookupProvider`) — ordinal,
  case-sensitive.

This is acceptable for the goal (`%text%` matching). Guaranteed cross-provider
case-insensitivity is a documented follow-up (normalized name column or
provider-side `EF.Functions.Like`), out of scope here.

## Testing

- **Showcase manual verification:** a demo with a >100-row entity type (or a
  temporarily reduced cap) confirming an item past the first 100 is reachable by
  typing its name, selectable, and stays selected after clearing the search.
- **Unit coverage** (where the existing test project allows): `PropertySetting`
  add/remove filter round-trips with `EntitySearchText` set, and that
  `EntitySearchText` is not serialized (`[JsonIgnore]`).
- **Regression:** both dialogs still open, filter, and persist correctly; empty
  search behaves identically to the previous `Take(100)` list.

## Files touched

- `KBlazor/Components/EntityCheckboxFilter.razor` — new shared component.
- `KBlazor/Components/FlexTable.razor` — replace the two inline entity checkbox
  blocks with the new component.
- `KBlazor/Models/ListViewSetting.cs` — add `EntitySearchText`.
