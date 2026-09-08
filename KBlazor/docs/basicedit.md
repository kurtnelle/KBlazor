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
| `Save` | `Action` | `null` | Callback when Save button is clicked. When `null`, the Save button is not rendered (read-only form). |
| `Close` | `Action` | required | Callback when Close button is clicked |
| `Columns` | `int` | `1` | Number of layout columns for the form |
| `IsValid` | `bool` | `true` | Controls whether Save button is enabled |
| `Fields` | `string` | `null` | Comma-separated field names to display (matches `[Display(Name="...")]`). If null, all `[Display]`-decorated properties are shown. |

## How Fields Are Rendered

BasicEdit inspects the `TItem` type's properties and renders controls based on:

### Property Type Mapping

All controls are MudBlazor components.

| Property Type | Rendered Control |
|---------------|-----------------|
| `string` | `MudTextField` |
| `string` with `[MemoDisplay]` | "Edit *Label*" button that opens a `MudDialog` containing a 10-line `MudTextField` |
| `enum` | `MudSelect` listing the enum names (PascalCase split into words). Read-only fields render as a locked `MudTextField`. |
| `DateTime` | `MudDatePicker` (date only, `MMM d, yyyy`) |
| `TimeSpan` | `MudTextField` accepting `h:mm tt` |
| `int`, `decimal`, `double` | Typed `MudTextField` (`int` honors `[DisplayFormat(DataFormatString)]`) |
| `bool` | `MudSwitch` |
| Navigation property whose type is a known entity (`IEntityLookupProvider.IsKnownEntityType`) | `MudSelect` of entities from `IEntityLookupProvider.GetEntities`. The selected `Id` is written to the foreign-key property. With `[AutoComplete]` on the navigation property, a `MudAutocomplete` (case-insensitive `ToString()` search) is rendered instead. |
| Anything else | Label only |

**Foreign keys.** The `Guid?` FK property itself is not rendered; put `[Display]` on the navigation property. BasicEdit locates the FK property either from `[ForeignKey("<FkProperty>")]` on the referenced entity's `Id`, or from `[ForeignKey("<NavigationProperty>")]` on the FK property of `TItem`. If neither is found, the field renders read-only.

### Attribute Effects on BasicEdit

| Attribute | Effect |
|-----------|--------|
| `[Display(Name="Label")]` | Sets the field label. Properties without `[Display]` are not rendered. |
| `[ReadOnlyOnEdit]` | Field is read-only when editing existing items (`!IsNew`) |
| `[MemoDisplay]` | Renders as a dialog-hosted multiline text area |
| `[AutoComplete]` | On an entity-typed navigation property, renders a `MudAutocomplete` instead of a `MudSelect` |
| `[Required]` | Read, currently has no visual effect |
| `[EnableTime]` | Read, but the date picker currently stays date-only (time selection is not yet wired up) |
| `[Editable]` (DataAnnotations) | Presence of the attribute, regardless of its value, makes the field read-only |
| `[CasscadeLookup]` | Declared in `KBlazor.Attributes` but not consumed by BasicEdit; it has no effect today |

Getter-only (computed) properties are always rendered read-only, since there is no setter to write back to.

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

## Limitations

- Cascading / scoped lookups (`[CasscadeLookup]`) are not implemented. `SetScopeFor` exists on the component but is `protected` and has no effect on rendering.
- `[EnableTime]` does not yet enable time selection on `DateTime` fields.
- Validation attributes (`[Required]`, etc.) are not enforced; use the `IsValid` parameter to gate the Save button yourself.
