# LINQ Extension Methods

`KBlazor.Models.LinqExtensionMethods` provides dynamic LINQ helpers for building sort and filter queries at runtime. These are used internally by `PropertySetting.GenerateWhere` and `PropertySetting.GenerateOrderBy`, and you can use them directly in your `SortAndFilter` callbacks.

## OrderBy Extensions

Sort a queryable by a property determined at runtime.

```csharp
// By property name (string)
query = query.OrderBy("Name", desc: false);

// By PropertyInfo
query = query.OrderBy(propertyInfo, desc: true);

// By LambdaExpression
query = query.OrderBy(lambdaExpression, desc: false);

// By FieldInfo
query = query.OrderBy(fieldInfo, desc: true);

// Typed overload with bool descending flag
query = query.OrderBy(x => x.Name, decending: false);
```

**ThenBy** for secondary sorts:

```csharp
var ordered = query.OrderBy(x => x.Status, false);
ordered = ordered.ThenBy(x => x.Name, false);
```

## Where Extensions

Filter a queryable by property value, determined at runtime.

### String search (case-insensitive, uses SQL collation)

```csharp
query = query.Where(propertyInfo, "search term");
```

### DateTime range

```csharp
query = query.Where(propertyInfo,
    greaterThanOrEqual: new DateTime(2024, 1, 1),
    lessThanOrEqual: new DateTime(2024, 12, 31));
```

### Numeric range (double)

```csharp
query = query.Where(propertyInfo,
    greaterThanOrEqual: 10.0,
    lessThanOrEqual: 100.0);
```

### Numeric range (int)

```csharp
query = query.Where(propertyInfo,
    greaterThanOrEqual: 1,
    lessThanOrEqual: 50);
```

### TimeSpan range

```csharp
query = query.Where(type, lambdaExpression,
    greaterThanOrEqual: TimeSpan.FromHours(8),
    lessThanOrEqual: TimeSpan.FromHours(17));
```

### Boolean

```csharp
query = query.Where(propertyInfo, value: true);
```

### Multi-value (int[] for enums)

```csharp
query = query.Where(propertyInfo, new int[] { 1, 2, 3 });
```

### Multi-value (Guid[] for FK collections)

```csharp
query = query.Where(propertyInfo, new Guid[] { guid1, guid2 });
```

## Usage in SortAndFilter Callback

The most common pattern is in a `SortAndFilter` callback from FlexTable:

```csharp
protected void SortAndFilter(ListViewSetting listViewSetting)
{
    // Start with base query
    var query = db.PurchaseOrders.AsQueryable();

    // Apply each active filter
    foreach (var prop in listViewSetting.DisplaySettings
        .Where(w => !string.IsNullOrEmpty(w.Filter)))
    {
        query = prop.GenerateWhere(query);
    }

    // Apply sorting
    var sortedProps = listViewSetting.DisplaySettings
        .Where(w => w.SortState != SortState.None)
        .OrderBy(o => o.SortPriority);

    foreach (var prop in sortedProps)
    {
        query = prop.GenerateOrderBy(query);
    }

    Items = query;
    StateHasChanged();
}
```

`PropertySetting.GenerateWhere` and `GenerateOrderBy` use these extension methods internally, so you rarely need to call them directly. But they're available if you need custom query logic beyond what the standard callbacks provide.

## Date Extension Methods

`KBlazor.Models.ExtensionMethods` also includes date helpers used by relative date filters:

```csharp
DateTime.Now.EndOfDay()       // 23:59:59.999
DateTime.Now.StartOfWeek()    // Monday 00:00
DateTime.Now.EndOfWeek()      // Sunday 23:59:59
DateTime.Now.StartOfMonth()   // 1st 00:00
DateTime.Now.EndOfMonth()     // Last day 23:59:59
DateTime.Now.StartOfYear()    // Jan 1 00:00
DateTime.Now.EndOfYear()      // Dec 31 23:59:59
DateTime.Now.Yesterday()      // Previous day
DateTime.Now.LastWeek()       // Start of previous week
DateTime.Now.LastMonth()      // Start of previous month
DateTime.Now.LastYear()       // Start of previous year
```
