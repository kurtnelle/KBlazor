# FlexTable Component

`FlexTable<TItem>` is the primary data display component. It renders entities in Table, Chip, or Kanban view modes with built-in sorting, filtering, pagination, and saved view management.

## Basic Usage

```razor
<FlexTable TItem="PurchaseOrder"
           Items="@orders"
           Fields="Name,Status,Order Date,Delivery Date"
           SelectionChanged="OnRowClicked"
           SortFilter="OnSortFilter" />
```

```csharp
private IQueryable<PurchaseOrder> orders;

protected override void OnInitialized()
{
    orders = db.PurchaseOrders.AsQueryable();
}

protected void OnRowClicked(PurchaseOrder item, string command)
{
    navManager.NavigateTo($"/order/{item.Id}");
}

protected void OnSortFilter(ListViewSetting listViewSetting)
{
    // Re-query with sort/filter applied
    orders = db.PurchaseOrders.AsQueryable();

    foreach (var prop in listViewSetting.DisplaySettings.Where(w => !string.IsNullOrEmpty(w.Filter)))
    {
        orders = orders.Where(prop.PropertyInfo, prop.Filter);
    }

    var sorted = listViewSetting.DisplaySettings
        .Where(w => w.SortState != SortState.None)
        .OrderBy(o => o.SortPriority);

    foreach (var prop in sorted)
    {
        orders = orders.OrderBy(prop.PropertyInfo, prop.SortState == SortState.Down);
    }

    StateHasChanged();
}
```

## Parameters

### Data

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `TItem` | type param | required | Entity type implementing `IKBusinessEntity` |
| `Items` | `IQueryable<TItem>` | required | Data source |
| `Fields` | `string` | `null` | Comma-separated display field names matching `[Display(Name="...")]` |
| `ViewName` | `string` | `"Default"` | View identifier for saving/loading view configurations |
| `PageSize` | `int` | `50` | Items per page |

### Callbacks

| Parameter | Type | Description |
|-----------|------|-------------|
| `SelectionChanged` | `Action<TItem, string>` | Fired on row click. `string` is the command (icon name for additional commands, or default click) |
| `SortFilter` | `Action<ListViewSetting>` | Fired when user changes sort/filter. Receives the current view settings for you to re-query |
| `Sort` | `Action<SortChangeEvent>` | Alternative simpler sort callback |
| `SortRowIconClicked` | `Action<string>` | Fired when header row icon is clicked |

### Appearance

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `AllowSelection` | `bool` | `true` | Enable row click selection |
| `IsPrintView` | `bool` | `false` | Print-optimized layout |
| `RowStyle` | `Func<TItem, string>` | `null` | CSS style per row (e.g., conditional coloring) |
| `RowClass` | `Func<TItem, string>` | `null` | CSS class per row |
| `SelectedItem` | `TItem` | `null` | Currently selected item (for highlighting) |
| `DefaultViewMode` | `FlexTableViewMode` | `Table` | Initial view mode: `Table`, `Chip`, or `Kanban` |

### Row Commands

| Parameter | Type | Description |
|-----------|------|-------------|
| `AdditionalCommands` | `Func<TItem, string>` | Returns FontAwesome icon classes per row. Multiple icons comma-separated (whitespace around commas is trimmed). Each command may append an optional `\|Tooltip` suffix to show a hover tooltip (see below). Clicking fires `SelectionChanged` with the **icon class only** (the tooltip text is stripped). |
| `AdditionalSortRowCommands` | `string` | FontAwesome icons added to the header row |

#### Command syntax and tooltips

Each command is `iconClass` with an optional `|Tooltip` suffix, separated by a pipe (`|`):

```
"fa-solid fa-eye|View, fa-solid fa-pen-to-square|Edit, fa-solid fa-trash|Delete"
```

- **Icon class** — one or more FontAwesome classes (e.g. `fa-solid fa-eye`). This is the *only* part passed to `SelectionChanged`, so switch on it in your handler.
- **`|Tooltip`** (optional) — text shown in a `MudTooltip` when the user hovers the icon. Omit the pipe and the button renders with no tooltip. This is fully backward compatible: existing `AdditionalCommands` that return bare icon classes are unaffected.

```razor
<FlexTable TItem="PurchaseOrder"
           Items="@_orders"
           Fields="Order #,Customer,Status"
           AdditionalCommands="GetCommands"
           SelectionChanged="OnRowClicked" />
```

```csharp
// Comma-separated icons; append "|Tooltip" to any command to show a hover tooltip.
private string GetCommands(PurchaseOrder order) =>
    "fa-solid fa-eye|View,fa-solid fa-pen-to-square|Edit,fa-solid fa-trash|Delete";

private void OnRowClicked(PurchaseOrder order, string command)
{
    // `command` is the icon class only — the "|Tooltip" text is stripped.
    switch (command)
    {
        case "fa-solid fa-eye":            /* view  */ break;
        case "fa-solid fa-pen-to-square":  /* edit  */ break;
        case "fa-solid fa-trash":          /* delete */ break;
    }
}
```

> **Requires FontAwesome.** The host app must load a FontAwesome stylesheet for the icons to render. `MudTooltip` ships with MudBlazor, so no extra dependency is needed for the tooltips themselves.

### Inline Editing

| Parameter | Type | Description |
|-----------|------|-------------|
| `InlineEditor` | `string` | Property name to make editable inline. The property must have `[AllowInlineEdit]` attribute. |
| `EditPath` | `string` | URL path for full edit navigation |

### Chip View

| Parameter | Type | Description |
|-----------|------|-------------|
| `ChipDisplayField` | `string?` | Property name to show as chip text |
| `ChipTemplate` | `RenderFragment<TItem>?` | Custom chip content template |
| `ChipColor` | `Func<TItem, MudBlazor.Color>?` | Chip color per item |

### Custom Cell Rendering

| Parameter | Type | Description |
|-----------|------|-------------|
| `RenderTemplates` | `Dictionary<Type, RenderFragment<object?>>?` | Per-type custom cell templates. When a column's property type matches a key, the template renders instead of the default text. The template receives the raw property value (not the formatted string). Also applies to Kanban column headers when the `KanbanGroupField` type has a registered template. |

### Row Details

| Parameter | Type | Description |
|-----------|------|-------------|
| `DetailsTemplate` | `RenderFragment<TItem>?` | Optional collapsible detail panel rendered below the row, spanning all columns. When set, each row gets a leading chevron toggle. Multiple rows can be expanded simultaneously; expansion state resets on sort, filter, or data change. Table view only. |

```razor
<FlexTable TItem="PurchaseOrder"
           Items="@_orders"
           Fields="Order #,Customer,Status"
           DetailsTemplate="@(order => @<BasicEdit TItem=\"PurchaseOrder\" Item=\"@order\" Columns=\"3\" />)" />
```

The chevron column only renders when `DetailsTemplate` is non-null, so existing tables are unaffected.

### Entity column filtering & name search

When a column's property type is a known business entity (one that implements `IKBusinessEntity` and is registered with the `IEntityLookupProvider`), its filter dialog renders a **checkbox list of related entities with a name-search field** instead of a text box. Type part of an entity's `Name` and select from the matches — this lifts the previous 100-row ceiling, so entities beyond the first 100 are reachable by searching.

**How to make an entity column filterable.** Annotate the navigation property with `[SortAndFilterOn]`, giving a `FilterPath` (the foreign-key id on `TItem`) and a `SortPath` (a scalar to sort by):

```csharp
[Display(Name = "Customer", Order = 2)]
[SortAndFilterOn(FilterPath = "CustomerId", SortPath = "Customer.Name")]
public virtual Customer? Customer { get; set; }
```

**Route the callback through the engine.** Entity filtering is only applied by `SortAndFilterEngine`, so the host's `SortFilter` handler must use `ApplyFilter`/`ApplySort` rather than a manual `GenerateWhere` loop (which ignores `FilterPath` and leaves the query unchanged for entity columns):

```csharp
private void OnSortFilter(ListViewSetting setting)
{
    _orders = _dbContext.Orders.AsQueryable()
        .ApplyFilter(setting)   // honors [SortAndFilterOn(FilterPath=...)] → selectedIds.Contains(o.CustomerId)
        .ApplySort(setting);    // honors SortPath so ordering targets a scalar, not the entity
    StateHasChanged();
}
```

Non-entity columns are unaffected: they set no `FilterPath`, so the engine falls back to `GenerateWhere`/`GenerateOrderBy` exactly as before.

**Search behavior (provider-aware, case-insensitive on every provider).** The search matches on the entity's `Name` and adapts to the query source automatically:

| Source | Matching strategy | Why |
|--------|-------------------|-----|
| Real EF query (relational) | `EF.Functions.Like(Name, "%term%")` | Stays sargable (can use an index on `Name`) and is case-insensitive by the column's collation. |
| In-memory (`EnumerableQuery`) | `Name.Contains(term, StringComparison.OrdinalIgnoreCase)` | `EF.Functions.Like` would throw when enumerating a LINQ-to-objects source. |

Currently-selected entities stay pinned and visible regardless of the search text; matched (unselected) results are capped at 100 below a divider. "Select All" operates on the visible set only, never the whole table. This logic lives in `EntityFilterList.Build` and is fully unit-tested.

> **Note:** Entity filtering is supported *only* via `[SortAndFilterOn(FilterPath=...)]` + the engine. `PropertySetting.GenerateWhere` has no built-in case for entity-typed properties (it returns the query unchanged).

### Kanban View

| Parameter | Type | Description |
|-----------|------|-------------|
| `KanbanGroupField` | `string?` | Property name to group items into columns |
| `KanbanColumns` | `string?` | Comma-separated column values (e.g., `"New,Pending,Complete"`) |
| `KanbanCardDisplayField` | `string?` | Property name for card title |
| `KanbanCardTemplate` | `RenderFragment<TItem>?` | Custom card content template |

## View Modes

### Table (default)
Standard data grid with sortable/filterable columns, pagination, and column management.

### Chip
Renders items as MudBlazor chips. Useful for compact/tag-like displays.

```razor
<FlexTable TItem="Category"
           Items="@categories"
           Fields="Name"
           DefaultViewMode="FlexTableViewMode.Chip"
           ChipDisplayField="Name"
           ChipColor="@(item => item.IsActive ? Color.Primary : Color.Default)" />
```

### Kanban
Groups items into columns by a property value.

```razor
<FlexTable TItem="Task"
           Items="@tasks"
           Fields="Name,Assignee"
           DefaultViewMode="FlexTableViewMode.Kanban"
           KanbanGroupField="Status"
           KanbanColumns="Todo,In Progress,Done"
           KanbanCardDisplayField="Name" />
```

## AdditionalCommands Example

```csharp
// In your page code-behind
protected string GetAdditionalCommands(PurchaseOrder order)
{
    if (order.Status == Status.New)
        return "fa-regular fa-square-check, fa-solid fa-trash";
    return "fa-regular fa-square-check";
}

protected void SelectionChangedEvent(PurchaseOrder row, string command)
{
    switch (command)
    {
        case "fa-regular fa-square-check":
            row.IsSelected = !row.IsSelected;
            StateHasChanged();
            break;
        case "fa-solid fa-trash":
            DeleteOrder(row);
            break;
        default:
            navManager.NavigateTo($"/order/{row.Id}");
            break;
    }
}
```

## RenderTemplates Example

Replace default text rendering for specific types with custom Blazor markup (icons, color badges, etc.):

```razor
<FlexTable TItem="PurchaseOrder"
           Items="@orders"
           Fields="Name,Store,Status"
           RenderTemplates="@_templates" />

@code {
    Dictionary<Type, RenderFragment<object?>> _templates = new()
    {
        [typeof(OrderStatus)] = val =>
        {
            var style = ((OrderStatus?)val) switch
            {
                OrderStatus.New => "background-color: #e8f5e9; color: #1b5e20;",
                OrderStatus.Cancelled => "background-color: #ffebee; color: #b71c1c;",
                _ => ""
            };
            return @<span style="display:inline-flex; align-items:center; gap:6px; padding:2px 8px; border-radius:4px; @style">
                @switch ((OrderStatus?)val)
                {
                    case OrderStatus.New:
                        <i class="fa-solid fa-star"></i> <span>New</span>
                        break;
                    case OrderStatus.Cancelled:
                        <i class="fa-solid fa-circle-xmark"></i> <span>Cancelled</span>
                        break;
                    default:
                        <span>@val</span>
                        break;
                }
            </span>;
        }
    };
}
```

Templates work with any type, not just enums. The template receives `object?` (the raw property value before formatting). When used with Kanban view, Kanban column headers also render through the template if the `KanbanGroupField` type has a registered entry.

## Saved Views

When `EnablePersonalViews` is true in `IFlexTableSettings`, users can:
- Save current column/sort/filter configuration as a named view
- Switch between saved views
- Reset to default view

Views are persisted via `IListViewSettingStore`. The `ViewName` parameter identifies which default view to load.

## JavaScript Requirement

FlexTable requires `_content/KBlazor/kblazor.js` for column resizing and font measurement. This is included as a static asset in the KBlazor package. Add to your host page:

```html
<script src="_content/KBlazor/kblazor.js"></script>
```

See [Getting Started](getting-started.md) for full setup instructions.

## Injected Services

FlexTable injects these automatically from DI:
- `IListViewSettingStore` — view persistence
- `IEntityLookupProvider` — entity resolution
- `IFlexTableSettings` — feature flags
- `AuthenticationStateProvider` — role checks for view management
- `IJSRuntime` — browser interop for column resizing
- `IHttpContextAccessor` — timezone offset from cookies
