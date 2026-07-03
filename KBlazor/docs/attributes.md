# KBlazor Attributes

KBlazor provides custom attributes in `KBlazor.Attributes` that control how model properties behave in FlexTable and BasicEdit components. Apply them alongside `[Display]` on your model properties.

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

Tells BasicEdit to eager-load a related navigation property when resolving lookups.

```csharp
[AlsoInclude(Name = "Quantities")]
public virtual ICollection<ItemQuantity> Quantities { get; set; }
```

**Property:** `Name` (string) — the navigation property name to include.

### AutoCompleteAttribute

Renders the field as an autocomplete input in BasicEdit, with suggestions populated from `IEntityLookupProvider`.

```csharp
[Display(Name = "Item")]
[AutoComplete]
public string ItemCode { get; set; }
```

### CasscadeLookupAttribute

Creates a dependent/cascading dropdown in BasicEdit. The options filter based on another property's value.

```csharp
[Display(Name = "Sub-Category")]
[CasscadeLookup(AdditionalProperties = "CategoryId")]
public Guid? SubCategoryId { get; set; }
```

**Property:** `AdditionalProperties` (string) — comma-separated property names that this lookup depends on.

### DisplayNoWrapAttribute

Prevents text wrapping in the FlexTable column for this property.

```csharp
[Display(Name = "Order Number")]
[DisplayNoWrap]
public string OrderNumber { get; set; }
```

### EnableTimeAttribute

Enables time selection on a DateTime field in BasicEdit (default is date-only).

```csharp
[Display(Name = "Scheduled At")]
[EnableTime]
public DateTime ScheduledAt { get; set; }
```

### LinkOnFieldAttribute

Renders the field value as a clickable link in FlexTable.

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

Specifies that sorting and filtering should operate on a different member than the displayed property. Useful when the display property is computed, or when the column's type is a related entity that can't be sorted/filtered directly.

```csharp
[Display(Name = "Status")]
[SortAndFilterOn(Member = "StatusCode")]
public string StatusDisplay { get; set; }
```

**Properties:**

| Property | Type | Description |
|----------|------|-------------|
| `Member` | `string` | Legacy: the actual property name to sort/filter on instead of the displayed one. |
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

Displays a tooltip sourced from another property when hovering over this field in FlexTable.

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
