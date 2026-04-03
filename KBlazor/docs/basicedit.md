# BasicEdit Component

`BasicEdit<TItem>` auto-generates an edit form from your model's `[Display]` attributes. It renders appropriate input controls based on property types and KBlazor attributes.

## Basic Usage

```razor
<BasicEdit TItem="Store" Item="@selectedStore" Save="SaveStore" Close="CloseEditor" />
```

```csharp
private Store selectedStore;

private void SaveStore()
{
    db.SaveChanges();
}

private void CloseEditor()
{
    selectedStore = null;
    StateHasChanged();
}
```

## Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `TItem` | type param | required | Entity type (must be a class) |
| `Item` | `TItem` | required | The entity instance being edited |
| `Save` | `Action` | required | Callback when Save button is clicked |
| `Close` | `Action` | required | Callback when Close button is clicked |
| `Columns` | `int` | `1` | Number of layout columns for the form |
| `IsValid` | `bool` | `true` | Controls whether Save button is enabled |
| `Fields` | `string` | `null` | Comma-separated field names to display (matches `[Display(Name="...")]`). If null, all `[Display]`-decorated properties are shown. |

## How Fields Are Rendered

BasicEdit inspects the `TItem` type's properties and renders controls based on:

### Property Type Mapping

| Property Type | Rendered Control |
|---------------|-----------------|
| `string` | `MatTextField` |
| `string` with `[MemoDisplay]` | `MatTextField` (multiline textarea) |
| `DateTime` | `MatDatePicker` |
| `DateTime` with `[EnableTime]` | `MatDatePicker` with time enabled |
| `bool` | `MatSlideToggle` |
| `int`, `double`, `decimal` | `MatTextField` (numeric) |
| `Guid?` (FK to known entity) | `MatSelect` dropdown populated via `IEntityLookupProvider` |
| `enum` | `MatSelect` with enum values |

### Attribute Effects on BasicEdit

| Attribute | Effect |
|-----------|--------|
| `[Display(Name="Label")]` | Sets the field label |
| `[ReadOnlyOnEdit]` | Field is disabled when editing existing items (`!IsNew`) |
| `[MemoDisplay]` | Renders as multiline text area |
| `[EnableTime]` | Enables time selection on date pickers |
| `[AutoComplete]` | Renders as autocomplete field with suggestions from `IEntityLookupProvider` |
| `[CasscadeLookup]` | Cascade dropdown that filters based on another field's value |

## Field Selection

Use the `Fields` parameter to control which properties appear and their order:

```razor
<!-- Show only these 3 fields -->
<BasicEdit TItem="Store" Item="@store" Fields="Name,Location,Phone" Save="Save" Close="Close" />

<!-- Show all [Display] properties (default) -->
<BasicEdit TItem="Store" Item="@store" Save="Save" Close="Close" />
```

## Multi-Column Layout

```razor
<BasicEdit TItem="PurchaseOrder" Item="@order" Columns="2" Save="Save" Close="Close" />
```

This arranges fields in a 2-column grid layout.

## Scoped Fields

For cascade lookups, you can set the scope of a field to filter its options based on a related entity:

```csharp
// In your code-behind after the component renders
basicEditRef.SetScopeFor("ItemId", selectedStore);
```
