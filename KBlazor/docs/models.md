# KBlazor Models

Models in `KBlazor.Models` are used by KBlazor components for view configuration, sorting, filtering, and event handling.

## ListViewSetting

Represents a saved table view configuration — which columns are visible, their order, sort state, filters, and page size.

```csharp
public class ListViewSetting
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string ForEntity { get; set; }           // Assembly-qualified type name
    public string Definition { get; set; }           // JSON-serialized PropertySetting list
    public int PageSize { get; set; }
    public string? CustomizedForUser { get; set; }   // null = shared view, "username" = personal
    public Guid? ParentViewId { get; set; }          // Reference to parent/default view
    public FlexTableViewMode ViewMode { get; set; }  // Current view mode
    public List<PropertySetting> DisplaySettings { get; }
    public List<PropertySetting> AllDisplayableFields { get; }
}
```

**Key methods:**
- `InitilizeDefinition()` — deserializes `Definition` JSON into `DisplaySettings` and resolves `PropertyInfo` references. Must be called after loading from the database.
- `UpdateDefinition()` — serializes `DisplaySettings` back to `Definition` JSON. Call before saving to the database.
- `GetDefaultProperties(fontFamily, fontSize)` — returns all `[Display]`-decorated properties for the entity type.
- `GetDefaultProperties(fontFamily, fontSize, fields)` — returns specific fields by their display name (comma-separated).
- `Clone(newName)` — creates a copy with a new name, linked to this view as parent.
- `ResetToParent()` — reverts to parent view's settings.

## PropertySetting

Represents a single column/field configuration within a `ListViewSetting`.

```csharp
public class PropertySetting
{
    public Guid Id { get; set; }
    public PropertyInfo PropertyInfo { get; set; }  // Resolved reflection info
    public string Name { get; set; }                // Property name
    public string DisplayName { get; set; }         // Column header text
    public int DisplayWidth { get; set; }           // Column width in pixels
    public string Filter { get; set; }              // Active filter value
    public SortState SortState { get; set; }        // Current sort direction
    public int SortPriority { get; set; }           // Multi-column sort order
}
```

**Filter values** are type-dependent:
- String properties: plain text search
- DateTime properties: `"yyyy-MM-dd,yyyy-MM-dd"` range or relative date string (e.g., `"Today"`, `"This Week"`)
- Numeric properties: `"lower,upper"` range
- Bool properties: `"true"` or `"false"`
- Enum properties: comma-separated integer values

**Key methods:**
- `GenerateWhere<T>(IQueryable<T>)` — applies this property's filter as a LINQ Where clause
- `GenerateOrderBy<T>(IQueryable<T>)` — applies this property's sort as a LINQ OrderBy clause
- `AddToFilter(int/Guid)` / `RemoveFromFilter(int/Guid)` — for multi-value enum/FK filters

## FlexTableViewMode

```csharp
public enum FlexTableViewMode
{
    Table = 0,
    Chip = 1,
    Kanban = 2
}
```

## SortState

```csharp
public enum SortState
{
    None = 0,
    Up = 1,      // Ascending
    Down = 2     // Descending
}
```

## SortChangeEvent

Emitted by the `Sort` callback on FlexTable (simpler alternative to `SortFilter`).

```csharp
public class SortChangeEvent
{
    public string? SortId { get; set; }
    public SortDirection Direction { get; set; }
}

public enum SortDirection
{
    Ascending,
    Descending
}
```

## RelativeDateCalc

Utility for parsing relative date strings used in FlexTable filters.

**Supported formats:** Combinations of `PartA + PartC`
- PartA: `"Today"`, `"This"`, `"Last"`, `"Next"`
- PartC: `"Week"`, `"Month"`, `"Year"`

Examples: `"Today"`, `"This Week"`, `"Last Month"`, `"Next Year"`

```csharp
RelativeDateCalc.IsValidRelativeDate("This Week");  // true
RelativeDateCalc.GetLowerDate("This Week");          // start of current week
RelativeDateCalc.GetUpperDate("This Week");          // end of current week
```
