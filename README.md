# KBlazor

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![MudBlazor](https://img.shields.io/badge/MudBlazor-8.15-594AE2)](https://mudblazor.com/)
[![Live Demo](https://img.shields.io/badge/Live%20Demo-kblazor.com-2ea44f)](https://kblazor.com/)

AI-ready Blazor components for data-driven applications. Built on [MudBlazor](https://mudblazor.com/) and Entity Framework Core, KBlazor gives you a powerful table, an auto-generated form editor, and the supporting plumbing to wire models straight into UI with attributes alone.

**Live demo:** https://kblazor.com/

## Components

| Component | Purpose |
|-----------|---------|
| `<FlexTable TItem="T">` | Data table with sort, filter, pagination, column resize, and Table / Card / Kanban view modes. Per-user saved views. |
| `<BasicEdit TItem="T">` | Auto-generated edit form built from `[Display]`, `[Editable]`, `[ReadOnlyOnEdit]`, and related attributes. |
| `<CycleStateButton>` | Toggle button that cycles through integer states (e.g. status flows). |
| `<RelativeDatePicker>` | Date picker that supports relative dates like "Today" or "This Week". |

## Quick Start

### 1. Reference the package

```xml
<PackageReference Include="KBlazor" Version="1.0.0" />
```

### 2. Include the JS asset

In `_Host.cshtml` (Server) or `index.html` (WASM):

```html
<script src="_content/KBlazor/kblazor.js"></script>
```

### 3. Add MudBlazor providers to `MainLayout.razor`

```razor
<MudThemeProvider />
<MudPopoverProvider />
<MudDialogProvider />
<MudSnackbarProvider />
```

### 4. Register required services

```csharp
builder.Services.AddMudServices();
builder.Services.AddScoped<IListViewSettingStore, YourListViewSettingStore>();
builder.Services.AddScoped<IEntityLookupProvider, YourEntityLookupProvider>();
builder.Services.AddScoped<IFlexTableSettings, YourFlexTableSettings>();
```

### 5. Render a table

```razor
<FlexTable TItem="Customer"
           Items="@customers"
           Fields="Name,Status,Created"
           SelectionChanged="OnRowClicked"
           SortFilter="OnSortFilter" />
```

Your `Customer` model implements `IKBusinessEntity` and decorates properties with `[Display]`. That's it — sort, filter, view modes, and saved layouts come for free.

## Documentation

Full guides live in [`KBlazor/docs/`](KBlazor/docs/):

- [Getting Started](KBlazor/docs/getting-started.md) — setup, dependencies, project configuration
- [Service Registration](KBlazor/docs/service-registration.md) — required DI interfaces
- [Entity Contract](KBlazor/docs/entity-contract.md) — `IKBusinessEntity` interface
- [FlexTable](KBlazor/docs/flextable.md) — table, chip, and kanban view component
- [BasicEdit](KBlazor/docs/basicedit.md) — auto-generated form editor
- [Attributes](KBlazor/docs/attributes.md) — display and behavior attributes
- [Models](KBlazor/docs/models.md) — `ListViewSetting`, `PropertySetting`, etc.
- [LINQ Extensions](KBlazor/docs/linq-extensions.md) — dynamic OrderBy/Where helpers

## Showcase

A live deployment is at **https://kblazor.com/** — every component with interactive examples and copy-pasteable code.

The `KBlazor.Showcase` project is the same site, runnable locally:

```bash
dotnet run --project KBlazor.Showcase
```

## Repository Layout

```
KBlazor/                  Razor Class Library (the package)
KBlazor.Showcase/         Demo site
KBlazor.Showcase.Tests/   Unit tests
docs/                     Design specs and plans
```

## Contributing

Issues and pull requests are welcome. Please run `dotnet build KBlazor.sln` and `dotnet test` before submitting.

## License

[MIT](LICENSE) © Shawn K. Lewis
