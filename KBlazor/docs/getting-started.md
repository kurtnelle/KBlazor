# Getting Started with KBlazor

## Overview

KBlazor is a Razor Class Library targeting .NET 10+ that provides reusable, data-driven UI components for Blazor Server applications. It is built on top of MudBlazor and uses Entity Framework Core for persistence of view settings.

## Dependencies

KBlazor requires these packages (pulled in transitively when you reference KBlazor):

| Package | Version | Purpose |
|---------|---------|---------|
| MudBlazor | 8.15.0 | UI component framework |
| Microsoft.EntityFrameworkCore | 10.0.3 | ORM for view persistence |
| Microsoft.EntityFrameworkCore.Relational | 10.0.3 | Relational DB support |
| Newtonsoft.Json | 13.0.4 | View definition serialization |
| System.Drawing.Common | 9.0.3 | Text measurement for column widths |

## Project Setup

### 1. Add Reference

```xml
<ProjectReference Include="..\KBlazor\KBlazor.csproj" />
```

Or if published as a NuGet package:

```xml
<PackageReference Include="KBlazor" Version="x.x.x" />
```

### 2. Include KBlazor JavaScript

FlexTable requires a JavaScript file for column resizing and font measurement. Add this script reference to your `_Host.cshtml` (Blazor Server) or `index.html` (Blazor WebAssembly):

```html
<script src="_content/KBlazor/kblazor.js"></script>
```

This is a static asset served automatically by the Razor Class Library — no manual file copying needed.

### 3. Configure MainLayout

KBlazor components use MudBlazor. Your `MainLayout.razor` must include the MudBlazor providers:

```razor
@inherits LayoutComponentBase

<!-- your layout content here -->
@Body

<!-- Required MudBlazor providers -->
<MudThemeProvider />
<MudPopoverProvider />
<MudDialogProvider />
<MudSnackbarProvider />
```

### 4. Update _Imports.razor

Add these to your application's `_Imports.razor`:

```razor
@using KBlazor.Models
@using KBlazor.Attributes
@using KBlazor.Services
@using KBlazor.Components
@using MudBlazor
```

### 5. Register Required Services

KBlazor requires three service implementations registered in your DI container. See [Service Registration](service-registration.md) for details.

```csharp
builder.Services.AddScoped<IListViewSettingStore, YourListViewSettingStore>();
builder.Services.AddScoped<IEntityLookupProvider, YourEntityLookupProvider>();
builder.Services.AddScoped<IFlexTableSettings, YourFlexTableSettings>();
```

### 6. Implement IKBusinessEntity on Your Models

Your entity models must implement `IKBusinessEntity`. See [Entity Contract](entity-contract.md) for details.

## Minimal Working Example

After setup, you can render a table with:

```razor
<FlexTable TItem="MyEntity"
           Items="@myQueryable"
           Fields="Name,Status,Created"
           SelectionChanged="OnRowClicked"
           SortFilter="OnSortFilter" />
```

Where `MyEntity` implements `IKBusinessEntity` and has properties decorated with `[Display]` attributes.
