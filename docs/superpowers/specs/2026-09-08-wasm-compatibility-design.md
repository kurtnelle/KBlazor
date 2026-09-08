# WebAssembly Compatibility — Design

**Date:** 2026-09-08
**Status:** Approved
**Target version:** KBlazor 1.1.0
**Component areas:** `KBlazor/KBlazor.csproj`, `KBlazor/Components/FlexTable.razor(.cs)`, `KBlazor/Models/ListViewSetting.cs`, `KBlazor/Models/ExtensionMethods.cs`, `KBlazor/Services/*`, `KBlazor/wwwroot/kblazor.js`, docs, showcase

## Problem

KBlazor 1.0.5 cannot be referenced from a Blazor WebAssembly project. The build
fails with NETSDK1082 ("no runtime pack for Microsoft.AspNetCore.App for
browser-wasm"). Two library dependencies cause this, and a third would fail at
runtime once the build passed:

1. `<FrameworkReference Include="Microsoft.AspNetCore.App" />` pulls the full
   server framework into a Razor Class Library. The only server-only type the
   library uses is `IHttpContextAccessor`, injected in `FlexTable.razor.cs` and
   read once in `FlexTable.razor` to pick up a `TimezoneOffset` cookie.
2. `System.Drawing.Common` measures header/cell text for column widths
   (`ExtensionMethods.GetTextSize`, via `Graphics.MeasureString` on a 1x1
   bitmap). It compiles under WASM but throws `PlatformNotSupportedException`
   at runtime, and has been Windows-only since .NET 7.
3. `AuthenticationStateProvider` is a hard `[Inject]`. Blazor Server registers
   one by default; a WebAssembly app without authentication does not, so
   `FlexTable` would fail to construct. `OnInitialized` also blocks on
   `GetAuthenticationStateAsync().Result`, which deadlocks on WASM's single
   thread when the provider is asynchronous.

Two related facts found during investigation:

- Nothing in this repository sets the `TimezoneOffset` cookie. It is a
  convention of an external consumer app. The showcase already runs with no
  timezone adjustment.
- The computed font returned by `GetComputedFont` is assigned to locals that
  shadow the `fontFamily` / `fontSize` fields in `OnAfterRenderAsync`, so every
  measurement today uses the hardcoded "Helvetica Neue" at 18.288. The GDI+
  measurement is therefore already an approximation in practice.

## Goal

One package that builds and runs in both Blazor Server and Blazor WebAssembly
hosts, with **no required changes** to existing Server consumers' `Program.cs`.
Host-specific behavior (cookie-based timezone, exact text measurement) stays
available as opt-in.

## Non-Goals

- A second, server-specific library. Blazor components are hosting-model
  agnostic; the server-bound code here is about fifteen lines and belongs
  behind optional seams, not in a separate package.
- Auto render mode testing.
- Implementing the declared-only attributes (`CasscadeLookup`, `LinkOnField`,
  etc.).
- Any consumer application beyond the smoke sample.

## Decisions

| Decision | Choice | Reason |
|----------|--------|--------|
| Package structure | One library | Standard for Blazor component libraries (MudBlazor is one package); avoids duplicate fixes and Auto-mode double references |
| Compatibility posture | Zero required host changes | Optional seams with built-in defaults; existing Server hosts upgrade by bumping the version |
| Default timezone behavior | No adjustment (offset 0) | Preserves today's behavior for every host in this repo; a browser-derived default would silently shift the showcase's seeded dates |
| Width measurement | Estimator for sync defaults, canvas `measureText` for user-initiated auto-size (approach C) | Default widths are needed synchronously in `OnInitialized` where JS is unavailable; accuracy goes where the user asked for it |
| Auth provider | Optional, resolved via `IServiceProvider.GetService` | Unregistered on WASM without auth |

## Section 1: Dependencies

`KBlazor/KBlazor.csproj`:

- Remove `<FrameworkReference Include="Microsoft.AspNetCore.App" />`.
- Remove `<PackageReference Include="System.Drawing.Common" />`.
- Add `<PackageReference Include="Microsoft.AspNetCore.Components.Web" Version="10.0.x" />` and
  `<PackageReference Include="Microsoft.AspNetCore.Components.Authorization" Version="10.0.x" />`,
  on the same 10.0 patch line as the EF Core 10.0.3 references (use the latest
  10.0.x available at implementation time).
- Keep MudBlazor, `Microsoft.EntityFrameworkCore`,
  `Microsoft.EntityFrameworkCore.Relational`, Newtonsoft.Json. EF Core builds
  under WASM; the `EF.Functions.Like` / `Collate` paths are already guarded by
  the `EnumerableQuery` provider check in `LinqExtensionMethods.Where` and
  `EntityFilterList.Build`.
- Keep `<SupportedPlatform Include="browser" />`.
- Remove `using Microsoft.AspNetCore.Http;` from `FlexTable.razor.cs` and the
  `IHttpContextAccessor` injection.

## Section 2: Optional seams

`FlexTable` injects `IServiceProvider` and resolves each of the following with
`GetService<T>()`, using the built-in default when nothing is registered.

### 2.1 `IClientTimeZoneProvider` (new, `KBlazor.Services`)

```csharp
public interface IClientTimeZoneProvider
{
    /// Minutes to add to a stored DateTime to display it in the user's local time.
    ValueTask<int> GetOffsetMinutesAsync();
}
```

- **Default when unregistered:** offset 0. No `DateTime` adjustment.
- **Shipped opt-in:** `BrowserTimeZoneProvider` (`KBlazor.Services`), which calls
  `KBlazor.getTimezoneOffsetMinutes()` in `kblazor.js` and returns
  `-new Date().getTimezoneOffset()`. Works on Server and WebAssembly. Registered
  by the host with
  `builder.Services.AddScoped<IClientTimeZoneProvider, BrowserTimeZoneProvider>();`.
- **Cookie behavior for external consumers:** a host-side implementation that
  reads `IHttpContextAccessor.HttpContext.Request.Cookies["TimezoneOffset"]`
  is documented as a snippet in `service-registration.md`. It is not shipped
  in the package because it requires the server framework.
- **FlexTable usage:** in `OnAfterRenderAsync(firstRender: true)`, resolve the
  provider; if present, await the offset, store it in a `_timezoneOffsetMinutes`
  field, and call `StateHasChanged()`. The cell renderer replaces the cookie
  read with this field. During prerender and before the first render the
  offset is 0.

### 2.2 `ITextMeasurer` (new, `KBlazor.Services`)

```csharp
public interface ITextMeasurer
{
    /// Approximate rendered width in CSS pixels. fontSizePx is the CSS pixel size.
    float MeasureWidth(string text, string fontFamily, float fontSizePx);
}
```

Font sizes are **CSS pixels everywhere** in the library from 1.1.0. The old
default of 18.288 was a point size (24.4px); the pixel equivalent 24.4f is
used wherever that constant appeared (`FlexTable.fontSize`,
`ListViewSetting.AllDisplayableFields`), so default widths are unchanged.

- **Default:** `EstimatingTextMeasurer` (sealed, `public static readonly
  Instance`). Pure C#. Sums a per-character factor (in em) and multiplies by
  `fontSizePx`. Factor classes:

  | Class | Characters | Factor (em) |
  |-------|-----------|-------------|
  | Narrow lowercase | `i j l t f r` | 0.30 |
  | Lowercase | other `a`–`z` | 0.52 |
  | Wide lowercase | `m w` | 0.80 |
  | Narrow uppercase | `I` | 0.30 |
  | Uppercase | other `A`–`Z` | 0.66 |
  | Wide uppercase | `M W` | 0.85 |
  | Digit | `0`–`9` | 0.55 |
  | Space | ` ` | 0.28 |
  | Narrow punctuation | `. , : ; ' \| !` | 0.28 |
  | Everything else | | 0.60 |

  The factors approximate Helvetica/Arial metrics so that default header widths
  land near today's GDI+ values at the same nominal size.
- `fontFamily` is accepted and ignored by the estimator; it exists so a
  registered measurer can use it.
- **`ListViewSetting`:** existing `GetDefaultProperties(fontFamily, fontSize)`
  and `GetDefaultProperties(fontFamily, fontSize, fields)` keep their
  signatures and delegate to new overloads that take an `ITextMeasurer`
  parameter, passing `EstimatingTextMeasurer.Instance`. `AllDisplayableFields`
  is unchanged. `using System.Drawing;` is removed.
- **`ExtensionMethods`:** `GetTextSize(this string, string fontName, float
  fontSize)` stays as a thin wrapper over `EstimatingTextMeasurer.Instance` for
  source compatibility. The `GetTextSize(this string, Font)` overload and
  `using System.Drawing;` are removed.
- **FlexTable usage:** resolve `ITextMeasurer` once in `OnInitializedAsync`
  (default if unregistered) and pass it to the `GetDefaultProperties` overloads.

### 2.3 Optional `AuthenticationStateProvider`

- Remove `[Inject] AuthenticationStateProvider AuthProvider`. Resolve it with
  `GetService<AuthenticationStateProvider>()` in `OnInitializedAsync`.
- If null: `IsAdmin = false`, `UserCanUpdate = enablePersonalViews`, and
  `currentUsername = "anonymous"` when personal views are enabled. This matches
  the outcome of today's `catch (InvalidOperationException)` branch.
- `OnInitialized` becomes `protected override async Task OnInitializedAsync()`
  and awaits `GetAuthenticationStateAsync()`. The `.Result` call is removed.
- Because `LoadView` now runs after an `await`, the component can render once
  before `listViewSetting` is set. `FlexTable.razor`'s outer guard becomes
  `@if (Items != null && listViewSetting != null)`. The view-editor dialog at
  the bottom of the file already guards on `listViewSetting != null`.

## Section 3: JavaScript and auto-size

`KBlazor/wwwroot/kblazor.js`:

- Add a `window.KBlazor` namespace object with:
  - `getTimezoneOffsetMinutes()` → `-new Date().getTimezoneOffset()`.
  - `measureText(texts, fontFamily, fontSizePx)` → creates one offscreen
    `<canvas>`, sets `ctx.font = fontSizePx + "px " + fontFamily`, and returns
    `texts.map(t => ctx.measureText(t).width)`. One interop call per auto-size.
- Existing globals `ActivateTableResize` and `GetComputedFont` are unchanged.

`FlexTable.razor.cs`:

- Fix the shadowed-font bug: `OnAfterRenderAsync` assigns the computed family
  and CSS pixel size to the `fontFamily` / `fontSize` fields instead of to
  shadowing locals. The `fontSize` field's default becomes 24.4f (pixels),
  matching the old 18.288pt. Both the estimator and `measureText` therefore
  receive the same pixel value, with no unit conversion anywhere.
- `AutoSizeDiv(PropertySetting)` becomes `async Task`. It builds the list of
  visible cell strings for the column (using "N/A" for `DateTime.MinValue`,
  and a 200px floor for null values as today), calls `KBlazor.measureText`
  with the real computed font, takes the max, subtracts 100 as today, assigns
  `DisplayWidth`, auto-saves, and calls `StateHasChanged()`.
- The `@ondblclick` handler in `FlexTable.razor` stays `@(e => AutoSizeDiv(setting))`;
  Blazor awaits the returned `Task`.

## Section 4: Verification

### 4.1 WebAssembly smoke sample

Add `KBlazor.WasmSample/` to `KBlazor.sln`: a standalone Blazor WebAssembly
app (`Microsoft.NET.Sdk.BlazorWebAssembly`, net10.0) that references
`KBlazor.csproj`. It contains:

- `Domain/Customer.cs`, `Domain/PurchaseOrder.cs`, `Domain/OrderStatus.cs` —
  copies of the showcase entities (the showcase is a Web SDK project and cannot
  be referenced from WASM).
- `Services/InMemoryListViewSettingStore.cs`, `InMemoryEntityLookupProvider.cs`,
  `InMemoryFlexTableSettings.cs` — copies of the showcase implementations.
- `Data/SeedData.cs` — the showcase seed (5 named + 145 filler customers,
  20 orders).
- `Pages/Index.razor` — one `FlexTable` (Fields including the
  `Customer (lookup)` entity column, `SortFilter` routed through
  `ApplyFilter`/`ApplySort`) and one `BasicEdit` for the selected row.
- `wwwroot/index.html` includes MudBlazor assets and
  `_content/KBlazor/kblazor.js`.
- `Program.cs` registers MudBlazor services, the three in-memory services, and
  `BrowserTimeZoneProvider` (to exercise the JS path). No authentication
  services, deliberately.

The sample is not deployed. It is not packed.

### 4.2 Checks

| Check | Command / action | Pass condition |
|-------|------------------|----------------|
| Build | `dotnet build KBlazor.sln` | Succeeds; sample builds (NETSDK1082 gone) |
| Publish | `dotnet publish KBlazor.WasmSample -c Release` | Succeeds with default Blazor trimming |
| Runtime | Serve the publish output and open it in the browser pane | FlexTable renders; sort, text filter, entity checkbox filter with name search, view editor save + reload round-trip, double-click auto-size, BasicEdit edit all work with no console errors |
| Unit tests | `dotnet test KBlazor.sln` | Existing 28 pass plus new tests below |
| Server regression | Run the showcase, open `/demo/flextable` | Renders, sort/filter work, double-click auto-size resizes the column, no console errors |

Trimming risk: Newtonsoft deserializes `PropertySetting` / `ViewDefinition` by
reflection, and `GetDefaultProperties` reflects over the entity type. If the
runtime check shows the trimmer stripped members, the fix is
`[DynamicallyAccessedMembers]` annotations on the affected types or a
`TrimmerRootDescriptor` shipped in the package. The plan includes this as a
conditional step.

### 4.3 New unit tests (`KBlazor.Showcase.Tests`)

- `EstimatingTextMeasurerTests`: empty string → 0; longer text is wider;
  `"MMMM"` wider than `"iiii"`; doubling font size doubles width.
- `ListViewSettingWidthTests`: `GetDefaultProperties` on `PurchaseOrder`
  returns non-zero `DisplayWidth` for every column and does not load
  `System.Drawing`.

## Section 5: Packaging, docs, website

### 5.1 Package

- `<Version>` → `1.1.0`. Pushing to `main` publishes via the existing
  workflow.
- `<Description>` and README badges: "Blazor Server" → "Blazor Server and
  WebAssembly".

### 5.2 Library docs (`KBlazor/docs`, packaged)

- `getting-started.md`: Overview says both hosting models. The script step
  shows `_Host.cshtml` / `App.razor` for Server and `wwwroot/index.html` for
  WebAssembly. Dependencies table drops `System.Drawing.Common`, adds the two
  Components packages. Service registration step drops
  `AddHttpContextAccessor`.
- `service-registration.md`: new "Optional services" section covering
  `IClientTimeZoneProvider` (default, `BrowserTimeZoneProvider`, and the
  cookie snippet for Server hosts) and `ITextMeasurer` (default estimator, how
  to register a custom one).
- `flextable.md`: "Injected Services" lists `IServiceProvider`-resolved
  optional services correctly; auto-size documented as canvas-measured.
- `models.md`: `GetDefaultProperties` overloads with `ITextMeasurer` added.
- `KBlazor/CLAUDE.md` and root `README.md`: hosting-model wording, remove
  `System.Drawing.Common` from dependencies, mention optional services.

### 5.3 Website (`KBlazor.Showcase`)

- `Pages/GettingStarted.razor`: hero copy says Server and WebAssembly. The
  script-include step gets Server / WebAssembly tabs. The services step drops
  `AddHttpContextAccessor` and adds a callout for the optional
  `IClientTimeZoneProvider` / `ITextMeasurer`. Requirements callout unchanged
  (MudBlazor 8.x, .NET 10).
- `Pages/Home.razor`: feature/hero copy mentions both hosting models.
- `Docs/DocContent.cs`: `FlexTableExplained` mentions that the library runs on
  Server and WebAssembly.
- The showcase remains a Blazor Server app. `AddHttpContextAccessor` may stay
  in its `Program.cs` (harmless) but is removed from displayed snippets.

## Error handling

- JS interop failures in `BrowserTimeZoneProvider` or `AutoSizeDiv` (e.g.
  `kblazor.js` not included) are caught; the timezone falls back to 0 and
  auto-size leaves the width unchanged. Both log nothing; the existing
  `SaveLastViewId` pattern already swallows interop errors.
- If `GetAuthenticationStateAsync` throws, treat as unauthenticated (same as
  today's catch).

## Compatibility summary

| Consumer | Change required |
|----------|-----------------|
| Blazor Server host with no timezone cookie (e.g. showcase) | None |
| Blazor Server host relying on the `TimezoneOffset` cookie | Register a host-side `IClientTimeZoneProvider` reading the cookie (documented snippet) |
| New Blazor WebAssembly host | Reference the package; register the three required services as on Server |
| Code calling `GetTextSize(string, Font)` | Removed; use `GetTextSize(string, string, float)` or `ITextMeasurer` |
