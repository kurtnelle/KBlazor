# KBlazor

KBlazor is a reusable Razor Class Library providing data-driven UI components for Blazor Server applications. It includes a powerful table/list component (FlexTable), auto-generated form editor (BasicEdit), and supporting infrastructure.

## Documentation

- [Getting Started](docs/getting-started.md) - Setup, dependencies, project configuration
- [Service Registration](docs/service-registration.md) - Required DI interfaces and implementation guide
- [Entity Contract](docs/entity-contract.md) - IKBusinessEntity interface your models must implement
- [FlexTable](docs/flextable.md) - Table, Chip, and Kanban view component
- [BasicEdit](docs/basicedit.md) - Auto-generated form editor component
- [Attributes](docs/attributes.md) - Display and behavior attributes for model properties
- [Models](docs/models.md) - ListViewSetting, PropertySetting, and related types
- [LINQ Extensions](docs/linq-extensions.md) - Dynamic OrderBy/Where helpers for SortAndFilter

## Quick Reference

**Dependencies:** MudBlazor, Entity Framework Core, Newtonsoft.Json, System.Drawing.Common

**Three required service implementations:**
1. `IFlexTableSettings` - Feature flags and role configuration
2. `IListViewSettingStore` - Persistence for saved table views
3. `IEntityLookupProvider` - Entity resolution for lookups

**Key components:**
- `<FlexTable TItem="T">` - Data table with sort, filter, pagination, and multiple view modes
- `<BasicEdit TItem="T">` - Auto-generated edit form from `[Display]` attributes
- `<CycleStateButton>` - Toggle button cycling through integer states
- `<RelativeDatePicker>` - Date picker supporting relative dates like "Today", "This Week"
