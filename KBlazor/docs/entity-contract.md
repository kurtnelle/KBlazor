# Entity Contract (IKBusinessEntity)

All entity models used with KBlazor components must implement `KBlazor.Models.IKBusinessEntity`.

## Interface Definition

```csharp
public interface IKBusinessEntity : IEquatable<IKBusinessEntity>
{
    Guid Id { get; }
    bool IsNew => Id == Guid.Empty;
    string Name { get; }
    string ToString();
    string ToJson();
}
```

- `Id` — unique identifier. Used for selection tracking, navigation, and equality.
- `IsNew` — default implementation returns `true` when `Id == Guid.Empty`.
- `Name` — display name used in dropdowns, chips, and autocomplete fields.
- `ToJson()` — JSON serialization (typically via `JsonConvert.SerializeObject`).

## Implementation Pattern

### Direct Implementation

```csharp
using KBlazor.Models;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

public class Product : IKBusinessEntity
{
    [Key]
    public Guid Id { get; set; }

    [Display(Name = "Product Name")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Price")]
    public decimal Price { get; set; }

    public bool Equals(IKBusinessEntity? other) => Id == other?.Id;
    public override string ToString() => Name;
    public string ToJson() => JsonConvert.SerializeObject(this);
}
```

### Bridge Interface Pattern (from IMIO)

If you have an existing entity interface, you can bridge it to `IKBusinessEntity`:

```csharp
public interface IJimioEntity : IEquatable<IJimioEntity>, IKBusinessEntity
{
    // Your existing members...

    // Bridge the Equals
    bool IEquatable<IKBusinessEntity>.Equals(IKBusinessEntity? other)
    {
        return this.Id == other?.Id;
    }
}
```

This lets existing models satisfy `IKBusinessEntity` without changing every class.

## Display Attributes

KBlazor components use `[Display]` from `System.ComponentModel.DataAnnotations` to determine which properties to show and how to label them.

```csharp
public class PurchaseOrder : IKBusinessEntity
{
    [Key]
    public Guid Id { get; set; }

    [Display(Name = "Order #", Order = 1)]
    public string Name { get; set; }

    [Display(Name = "Status", Order = 2)]
    public OrderStatus Status { get; set; }

    [Display(Name = "Order Date", Order = 3)]
    public DateTime OrderDate { get; set; }
}
```

- `Name` — column header / field label
- `Order` — controls default column ordering
- Properties without `[Display]` are hidden from FlexTable and BasicEdit

See [Attributes](attributes.md) for additional KBlazor-specific decorators.
