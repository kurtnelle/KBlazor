# KBlazor Attributes

KBlazor provides custom attributes in `KBlazor.Attributes` that control how model properties behave in FlexTable and BasicEdit components. Apply them alongside `[Display]` on your model properties.

Some attributes are declared but not yet consumed by any component; each entry below states whether it is **Active** or **Declared only** as of version 1.1.0.

| Attribute | Status | Consumed by |
|-----------|--------|-------------|
| `[AllowInlineEdit]` | Active | FlexTable |
| `[AlsoInclude]` | Active | FlexTable, BasicEdit |
| `[AutoComplete]` | Active | FlexTable, BasicEdit |
| `[MemoDisplay]` | Active | BasicEdit |
| `[ReadOnlyOnEdit]` | Active | BasicEdit |
| `[SortAndFilterOn]` (`SortPath`, `FilterPath`) | Active | `SortAndFilterEngine` |
| `[SortAndFilterOn]` (`Member`) | Declared only | — |
| `[CasscadeLookup]` | Declared only | — |
| `[DisplayNoWrap]` | Declared only | — |
| `[EnableTime]` | Declared only (read by BasicEdit, no effect) | — |
| `[LinkOnField]` | Declared only (read by FlexTable, no effect) | — |
| `[ToolTipOnField]` | Declared only | — |

## Attribute Reference

### AllowInlineEditAttribute

Marks a property as editable directly within the FlexTable row (no separate edit form needed).

```csharp
[Display(Name = "Quantity")]
[AllowInlineEdit]
public int QuantityOrdered { get; set; }
```

Use with FlexTable's `InlineEditor` parameter:
```razor
<FlexTable TItem="OrderLine" Items="@lines" InlineEditor="QuantityOrdered" ... />
```

### AlsoIncludeAttribute

Applied to an **entity class**. When FlexTable or BasicEdit build a lookup list for that entity type, they call `IEntityLookupProvider.GetEntitiesWithInclude(type, Name)` instead of `GetEntities(type)`, so the named navigation property is eager-loaded (useful when the entity's `ToString()` depends on a related entity).

```csharp
[AlsoInclude(Name = "Quantities")]
public class Item : IKBusinessEntity
{
    public virtual ICollection<ItemQuantity> Quantities { get; set; }
    public override string ToString() => $"{Name} ({Quantities.Sum(q => q.OnHand)})";
    // ...
}
```

**Property:** `Name` (string) — the navigation property name to include.

### AutoCompleteAttribute

Applied to a navigation property whose type is a known entity. In BasicEdit the field renders as a `MudAutocomplete` (instead of a `MudSelect`) with suggestions from `IEntityLookupProvider`. In FlexTable the column's filter dialog renders an autocomplete-with-chips instead of the entity checkbox filter.

```csharp
[Display(Name = "Item")]
[AutoComplete]
public virtual Item? Item { get; set; }
```

It has no effect on scalar properties such as a `Guid?` foreign key or a `string`.

### CasscadeLookupAttribute

**Declared only.** Intended to create a dependent/cascading dropdown in BasicEdit whose options filter based on another property's value. No component currently reads this attribute, so applying it has no effect.

```csharp
[Display(Name = "Sub-Category")]
[CasscadeLookup(AdditionalProperties = "CategoryId")]
public Guid? SubCategoryId { get; set; }
```

**Property:** `AdditionalProperties` (string) — comma-separated property names that this lookup depends on.

### DisplayNoWrapAttribute

**Declared only.** Intended to prevent text wrapping in the FlexTable column. FlexTable does not read it; note that every table cell already renders with `white-space:nowrap` and ellipsis overflow, so the intended behavior is the default.

```csharp
[Display(Name = "Order Number")]
[DisplayNoWrap]
public string OrderNumber { get; set; }
```

### EnableTimeAttribute

**Declared only.** Intended to enable time selection on a `DateTime` field in BasicEdit. BasicEdit reads the attribute but the rendered `MudDatePicker` is still date-only.

```csharp
[Display(Name = "Scheduled At")]
[EnableTime]
public DateTime ScheduledAt { get; set; }
```

### LinkOnFieldAttribute

**Declared only.** Intended to render the field value as a clickable link in FlexTable. FlexTable reads the attribute but currently renders the value as plain text.

```csharp
[Display(Name = "Reference")]
[LinkOnField]
public string ReferenceUrl { get; set; }
```

### MemoDisplayAttribute

Renders the field as a multiline text area in BasicEdit instead of a single-line input.

```csharp
[Display(Name = "Notes")]
[MemoDisplay]
public string Notes { get; set; }
```

### ReadOnlyOnEditAttribute

Makes the field read-only when editing an existing entity (where `IsNew == false`). Useful for fields that should only be set during creation.

```csharp
[Display(Name = "Entity Type")]
[ReadOnlyOnEdit]
public string ForEntity { get; set; }
```

### SortAndFilterOnAttribute

Specifies dot-paths that `SortAndFilterEngine` uses to sort and filter a column instead of the displayed property. This is how a column whose type is a related entity becomes sortable (by a scalar) and filterable (by foreign-key id).

```csharp
[Display(Name = "Customer", Order = 2)]
[SortAndFilterOn(FilterPath = "CustomerId", SortPath = "Customer.Name")]
public virtual Customer? Customer { get; set; }
```

**Properties:**

| Property | Type | Description |
|----------|------|-------------|
| `Member` | `string` | **Legacy, no effect.** Not read by any component or by `SortAndFilterEngine`; retained only so existing models keep compiling. Use `SortPath` / `FilterPath` instead. |
| `SortPath` | `string` | Dot-path used by `SortAndFilterEngine.ApplySort` to build the `OrderBy` (e.g. `"Customer.Name"`). Use for entity/navigation columns so sorting targets a scalar instead of throwing on a complex type. |
| `FilterPath` | `string` | Dot-path used by `SortAndFilterEngine.ApplyFilter` to build the `Where`. For a foreign-key path (e.g. `"CustomerId"`) it produces `selectedIds.Contains(o.CustomerId)`. **This is what turns an entity-typed column into a searchable checkbox filter** (see [Entity column filtering](flextable.md#entity-column-filtering--name-search)). |

**Enabling entity (foreign-key) column filtering.** Annotate a navigation property so its column filters through the FK id and sorts through a scalar:

```csharp
[Display(Name = "Customer", Order = 2)]
[SortAndFilterOn(FilterPath = "CustomerId", SortPath = "Customer.Name")]
public virtual Customer? Customer { get; set; }
```

`FilterPath`/`SortPath` only take effect when the host routes the `SortFilter` callback through the engine (`query.ApplyFilter(setting).ApplySort(setting)`) — the manual `GenerateWhere` loop ignores them. See [LINQ Extensions](linq-extensions.md#usage-in-sortandfilter-callback).

### ToolTipOnFieldAttribute

**Declared only.** Intended to display a tooltip sourced from another property when hovering over this field in FlexTable. No component currently reads this attribute. (FlexTable does show a tooltip on column headers with the column's display name, and `AdditionalCommands` supports `|Tooltip` suffixes; neither uses this attribute.)

```csharp
[Display(Name = "Name")]
[ToolTipOnField(PropertyName = "Description")]
public string Name { get; set; }

public string Description { get; set; }
```

**Property:** `PropertyName` (string) — the property whose value is shown as the tooltip.

## Usage with [Display]

All KBlazor attributes work alongside the standard `[Display]` attribute. A property MUST have `[Display]` to appear in FlexTable or BasicEdit — KBlazor attributes alone are not sufficient.

```csharp
// This property will appear in components with inline edit support
[Display(Name = "Quantity", Order = 5)]
[AllowInlineEdit]
public int Quantity { get; set; }

// This property will NOT appear (no [Display])
[AllowInlineEdit]
public int InternalCount { get; set; }
```

## Namespace

All attributes are in `KBlazor.Attributes`. Add to your `_Imports.razor` or model file:

```csharp
using KBlazor.Attributes;
```
