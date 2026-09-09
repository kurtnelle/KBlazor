# WebAssembly Compatibility Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the single KBlazor package build and run in both Blazor Server and Blazor WebAssembly hosts, with no required changes to existing Server consumers, and ship it as 1.1.0.

**Architecture:** Remove the two host-bound dependencies (the `Microsoft.AspNetCore.App` framework reference and `System.Drawing.Common`) and put the three host-specific concerns (client timezone, text measurement, authentication state) behind optional services that `FlexTable` resolves from `IServiceProvider` with built-in defaults. Column-width defaults come from a pure-C# estimator; user-initiated auto-size uses canvas `measureText` via JS interop. A standalone WebAssembly sample project in the solution proves the result.

**Tech Stack:** .NET 10 Razor Class Library, Blazor Server + WebAssembly, MudBlazor 8.15, EF Core 10.0.3, Newtonsoft.Json, xUnit (`KBlazor.Showcase.Tests`).

**Spec:** `docs/superpowers/specs/2026-09-08-wasm-compatibility-design.md`

## Global Constraints

- Target framework everywhere: `net10.0`. Local SDK is 10.0.401.
- New package versions: `Microsoft.AspNetCore.Components.Web` **10.0.12**, `Microsoft.AspNetCore.Components.Authorization` **10.0.12**, `Microsoft.AspNetCore.Components.WebAssembly` **10.0.12**, `Microsoft.AspNetCore.Components.WebAssembly.DevServer` **10.0.12**. MudBlazor stays **8.15.0**.
- Font sizes are **CSS pixels everywhere**. The old constant `18.288f` (points) becomes `24.4f` (pixels) wherever it appears.
- Default when no `IClientTimeZoneProvider` is registered: offset **0** (no DateTime adjustment).
- Default when no `ITextMeasurer` is registered: `EstimatingTextMeasurer.Instance`.
- `AuthenticationStateProvider` unregistered ⇒ `IsAdmin = false`, `currentUsername = "anonymous"` when personal views are enabled.
- No `.Result` / `.Wait()` on tasks in component lifecycle code (deadlocks on WASM).
- Package version for this work: **1.1.0**.
- Commit messages end with `Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>`.
- Repo root: `H:\Source\repos\KBlazor`. All paths below are relative to it. Commands are for Git Bash (`/h/Source/repos/KBlazor`).
- All tests live in `KBlazor.Showcase.Tests` (xUnit). Run with `dotnet test KBlazor.sln --nologo -v q`. Baseline before this plan: 28 passing.

## File Structure

**Library (`KBlazor/`)**
- `Services/ITextMeasurer.cs` — NEW. Interface: width in CSS px for text at a font.
- `Services/EstimatingTextMeasurer.cs` — NEW. Default pure-C# implementation (character-class factors).
- `Services/IClientTimeZoneProvider.cs` — NEW. Interface: minutes to add to a stored DateTime for display.
- `Services/BrowserTimeZoneProvider.cs` — NEW. Opt-in implementation via JS interop.
- `Models/ExtensionMethods.cs` — MODIFY. Drop `System.Drawing`; `GetTextSize` delegates to the estimator.
- `Models/ListViewSetting.cs` — MODIFY. Drop `System.Drawing`; `GetDefaultProperties` overloads take an `ITextMeasurer`.
- `Components/FlexTable.razor.cs` — MODIFY. `IServiceProvider` resolution, `OnInitializedAsync`, font-field fix, async auto-size, timezone field.
- `Components/FlexTable.razor` — MODIFY. Null guard on `listViewSetting`; timezone read from field.
- `wwwroot/kblazor.js` — MODIFY. Add `window.KBlazor.getTimezoneOffsetMinutes` and `window.KBlazor.measureText`.
- `KBlazor.csproj` — MODIFY. Drop framework reference and System.Drawing.Common; add Components packages; version 1.1.0; description.
- `docs/*.md`, `CLAUDE.md` — MODIFY. Hosting-model wording, optional services, dependency table.

**WASM sample (`KBlazor.WasmSample/`)** — NEW project, added to `KBlazor.sln`, not packed, not deployed.

**Tests (`KBlazor.Showcase.Tests/`)**
- `EstimatingTextMeasurerTests.cs` — NEW.
- `ListViewSettingWidthTests.cs` — NEW.
- `BrowserTimeZoneProviderTests.cs` — NEW.

**Showcase (`KBlazor.Showcase/`)** — MODIFY `Pages/GettingStarted.razor`, `Pages/Home.razor`, `Docs/DocContent.cs`.

**Root** — MODIFY `README.md`, `.claude/launch.json` (add `wasm-sample` entry).

---

### Task 1: `ITextMeasurer` and `EstimatingTextMeasurer`

**Files:**
- Create: `KBlazor/Services/ITextMeasurer.cs`
- Create: `KBlazor/Services/EstimatingTextMeasurer.cs`
- Test: `KBlazor.Showcase.Tests/EstimatingTextMeasurerTests.cs`

**Interfaces:**
- Produces: `KBlazor.Services.ITextMeasurer` with `float MeasureWidth(string text, string fontFamily, float fontSizePx)`; `KBlazor.Services.EstimatingTextMeasurer` (sealed) with `public static readonly EstimatingTextMeasurer Instance`. Later tasks call `EstimatingTextMeasurer.Instance.MeasureWidth(...)`.

- [ ] **Step 1: Write the failing tests**

Create `KBlazor.Showcase.Tests/EstimatingTextMeasurerTests.cs`:

```csharp
using KBlazor.Services;
using Xunit;

namespace KBlazor.Showcase.Tests;

public class EstimatingTextMeasurerTests
{
    private static readonly ITextMeasurer Sut = EstimatingTextMeasurer.Instance;

    [Fact]
    public void EmptyOrNullText_ReturnsZero()
    {
        Assert.Equal(0f, Sut.MeasureWidth(string.Empty, "Helvetica Neue", 24.4f));
        Assert.Equal(0f, Sut.MeasureWidth(null!, "Helvetica Neue", 24.4f));
    }

    [Fact]
    public void LongerText_IsWider()
    {
        var shortWidth = Sut.MeasureWidth("Hello", "Helvetica Neue", 24.4f);
        var longWidth = Sut.MeasureWidth("Hello World", "Helvetica Neue", 24.4f);
        Assert.True(longWidth > shortWidth, $"expected {longWidth} > {shortWidth}");
    }

    [Fact]
    public void WideGlyphs_AreWiderThanNarrowGlyphs()
    {
        var wide = Sut.MeasureWidth("MMMM", "Helvetica Neue", 24.4f);
        var narrow = Sut.MeasureWidth("iiii", "Helvetica Neue", 24.4f);
        Assert.True(wide > narrow, $"expected {wide} > {narrow}");
    }

    [Fact]
    public void Width_ScalesLinearlyWithFontSize()
    {
        var at12 = Sut.MeasureWidth("Order #", "Helvetica Neue", 12f);
        var at24 = Sut.MeasureWidth("Order #", "Helvetica Neue", 24f);
        Assert.Equal(at12 * 2f, at24, precision: 3);
    }

    [Fact]
    public void KnownString_MatchesFactorTable()
    {
        // "Ab 1." = A(0.66) + b(0.52) + space(0.28) + 1(0.55) + .(0.28) = 2.29 em
        var width = Sut.MeasureWidth("Ab 1.", "Helvetica Neue", 10f);
        Assert.Equal(22.9f, width, precision: 3);
    }

    [Fact]
    public void FontFamily_IsIgnoredByEstimator()
    {
        var a = Sut.MeasureWidth("Customer", "Helvetica Neue", 24.4f);
        var b = Sut.MeasureWidth("Customer", "Times New Roman", 24.4f);
        Assert.Equal(a, b);
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `cd /h/Source/repos/KBlazor && dotnet test KBlazor.sln --nologo -v q --filter "FullyQualifiedName~EstimatingTextMeasurerTests" 2>&1 | grep -v warning | tail -5`
Expected: build error `CS0246: The type or namespace name 'ITextMeasurer' could not be found`.

- [ ] **Step 3: Create the interface**

Create `KBlazor/Services/ITextMeasurer.cs`:

```csharp
namespace KBlazor.Services;

/// <summary>
/// Measures rendered text width so FlexTable can pick default column widths.
/// Register an implementation to override the built-in estimator; when none is
/// registered, <see cref="EstimatingTextMeasurer.Instance"/> is used.
/// </summary>
public interface ITextMeasurer
{
    /// <summary>
    /// Approximate rendered width of <paramref name="text"/> in CSS pixels.
    /// </summary>
    /// <param name="text">The text to measure. Null or empty returns 0.</param>
    /// <param name="fontFamily">CSS font family (may be ignored by estimators).</param>
    /// <param name="fontSizePx">Font size in CSS pixels.</param>
    float MeasureWidth(string text, string fontFamily, float fontSizePx);
}
```

- [ ] **Step 4: Create the estimator**

Create `KBlazor/Services/EstimatingTextMeasurer.cs`:

```csharp
namespace KBlazor.Services;

/// <summary>
/// Platform-independent width estimator. Sums a per-character factor (in em,
/// approximating Helvetica/Arial metrics) and multiplies by the font size.
/// Used when the host registers no <see cref="ITextMeasurer"/>.
/// </summary>
public sealed class EstimatingTextMeasurer : ITextMeasurer
{
    public static readonly EstimatingTextMeasurer Instance = new();

    public float MeasureWidth(string text, string fontFamily, float fontSizePx)
    {
        if (string.IsNullOrEmpty(text) || fontSizePx <= 0f)
        {
            return 0f;
        }

        float em = 0f;
        foreach (var c in text)
        {
            em += Factor(c);
        }
        return em * fontSizePx;
    }

    private static float Factor(char c) => c switch
    {
        'i' or 'j' or 'l' or 't' or 'f' or 'r' => 0.30f,
        'm' or 'w' => 0.80f,
        >= 'a' and <= 'z' => 0.52f,
        'I' => 0.30f,
        'M' or 'W' => 0.85f,
        >= 'A' and <= 'Z' => 0.66f,
        >= '0' and <= '9' => 0.55f,
        ' ' => 0.28f,
        '.' or ',' or ':' or ';' or '\'' or '|' or '!' => 0.28f,
        _ => 0.60f
    };
}
```

- [ ] **Step 5: Run the tests to verify they pass**

Run: `cd /h/Source/repos/KBlazor && dotnet test KBlazor.sln --nologo -v q --filter "FullyQualifiedName~EstimatingTextMeasurerTests" 2>&1 | grep -v warning | tail -5`
Expected: `Passed! - Failed: 0, Passed: 6`

- [ ] **Step 6: Commit**

```bash
cd /h/Source/repos/KBlazor
git add KBlazor/Services/ITextMeasurer.cs KBlazor/Services/EstimatingTextMeasurer.cs KBlazor.Showcase.Tests/EstimatingTextMeasurerTests.cs
git commit -m "feat: add ITextMeasurer with platform-independent EstimatingTextMeasurer

Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>"
```

---

### Task 2: Remove `System.Drawing` from width measurement

**Files:**
- Modify: `KBlazor/Models/ExtensionMethods.cs` (usings at top; `GetTextSize` methods around lines 34–46)
- Modify: `KBlazor/Models/ListViewSetting.cs` (usings; `AllDisplayableFields` ~line 60; both `GetDefaultProperties` ~lines 140–199)
- Modify: `KBlazor/Components/FlexTable.razor.cs:21` (`fontSize` default)
- Modify: `KBlazor/KBlazor.csproj` (remove `System.Drawing.Common`)
- Test: `KBlazor.Showcase.Tests/ListViewSettingWidthTests.cs`

**Interfaces:**
- Consumes: `EstimatingTextMeasurer.Instance`, `ITextMeasurer` from Task 1.
- Produces: `ListViewSetting.GetDefaultProperties(string fontFamily, float fontSizePx, ITextMeasurer measurer)` and `ListViewSetting.GetDefaultProperties(string fontFamily, float fontSizePx, string fields, ITextMeasurer measurer)`. The existing two-arg and three-arg overloads remain and delegate with the default estimator. `ExtensionMethods.GetTextSize(this string, string fontName, float fontSizePx)` remains; the `Font` overload is removed.

- [ ] **Step 1: Write the failing test**

Create `KBlazor.Showcase.Tests/ListViewSettingWidthTests.cs`:

```csharp
using KBlazor.Models;
using KBlazor.Services;
using KBlazor.Showcase.Domain;
using Xunit;

namespace KBlazor.Showcase.Tests;

public class ListViewSettingWidthTests
{
    private static ListViewSetting ForPurchaseOrder() => new()
    {
        Name = "Default",
        ForEntity = typeof(PurchaseOrder).FullName!
    };

    [Fact]
    public void GetDefaultProperties_UsesEstimator_AndDoesNotLoadSystemDrawingCommon()
    {
        var props = ForPurchaseOrder().GetDefaultProperties("Helvetica Neue", 24.4f);

        Assert.NotEmpty(props);
        Assert.All(props, p => Assert.True(p.DisplayWidth > 0, $"{p.Name} has width {p.DisplayWidth}"));
        Assert.DoesNotContain(
            AppDomain.CurrentDomain.GetAssemblies(),
            a => a.GetName().Name == "System.Drawing.Common");
    }

    [Fact]
    public void GetDefaultProperties_WithFields_ReturnsRequestedColumnsInOrder()
    {
        var props = ForPurchaseOrder().GetDefaultProperties("Helvetica Neue", 24.4f, "Status,Order #");

        Assert.Equal(new[] { "Status", "Name" }, props.Select(p => p.Name).ToArray());
        Assert.All(props, p => Assert.True(p.DisplayWidth > 0));
    }

    private sealed class FixedMeasurer : ITextMeasurer
    {
        public float MeasureWidth(string text, string fontFamily, float fontSizePx) => 123f;
    }

    [Fact]
    public void GetDefaultProperties_HonorsSuppliedMeasurer()
    {
        var props = ForPurchaseOrder().GetDefaultProperties("Helvetica Neue", 24.4f, new FixedMeasurer());
        Assert.All(props, p => Assert.Equal(123, p.DisplayWidth));

        var withFields = ForPurchaseOrder().GetDefaultProperties("Helvetica Neue", 24.4f, "Order #", new FixedMeasurer());
        Assert.Single(withFields);
        Assert.Equal(123, withFields[0].DisplayWidth);
    }

    [Fact]
    public void GetTextSize_StringOverload_DelegatesToEstimator()
    {
        var viaExtension = "Customer".GetTextSize("Helvetica Neue", 24.4f);
        var viaEstimator = EstimatingTextMeasurer.Instance.MeasureWidth("Customer", "Helvetica Neue", 24.4f);
        Assert.Equal(viaEstimator, viaExtension);
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `cd /h/Source/repos/KBlazor && dotnet test KBlazor.sln --nologo -v q --filter "FullyQualifiedName~ListViewSettingWidthTests" 2>&1 | grep -v warning | tail -5`
Expected: build error `CS1503`/`CS1501` on the `GetDefaultProperties(..., ITextMeasurer)` overloads (they don't exist yet).

- [ ] **Step 3: Rewrite `ExtensionMethods.GetTextSize`**

In `KBlazor/Models/ExtensionMethods.cs`, replace the using block at the top with:

```csharp
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using KBlazor.Services;
```

Replace the two `GetTextSize` methods (the `(this string source, string fontName, float fontSize)` one and the `(this string source, Font font)` one) with this single method:

```csharp
        /// <summary>
        /// Approximate rendered width in CSS pixels, via <see cref="EstimatingTextMeasurer"/>.
        /// Kept for source compatibility; prefer injecting <see cref="ITextMeasurer"/>.
        /// </summary>
        public static float GetTextSize(this string source, string fontName, float fontSizePx)
        {
            return EstimatingTextMeasurer.Instance.MeasureWidth(source, fontName, fontSizePx);
        }
```

- [ ] **Step 4: Rewrite `ListViewSetting` width code**

In `KBlazor/Models/ListViewSetting.cs`:

(a) Remove the line `using System.Drawing;` and add `using KBlazor.Services;` to the usings.

(b) Replace the `AllDisplayableFields` property body so it reads:

```csharp
        [NotMapped]
        public List<PropertySetting> AllDisplayableFields
        {
            get
            {
                return GetDefaultProperties("Helvetica Neue", 24.4f).ToList();
            }
        }
```

(c) Replace both existing `GetDefaultProperties` methods (`(string fontFamily, float fontSize)` and `(string fontFamily, float fontSize, string fields)`) with these four:

```csharp
        public List<PropertySetting> GetDefaultProperties(string fontFamily, float fontSizePx)
            => GetDefaultProperties(fontFamily, fontSizePx, EstimatingTextMeasurer.Instance);

        public List<PropertySetting> GetDefaultProperties(string fontFamily, float fontSizePx, ITextMeasurer measurer)
        {
            List<PropertySetting> list = new List<PropertySetting>();

            var type = ResolveType(ForEntity);
            var properties = type.GetProperties()
                .Where(w => w.GetCustomAttributes(typeof(DisplayAttribute), false).Any())
                .OrderBy(o => (o.GetCustomAttributes(typeof(DisplayAttribute), false).FirstOrDefault() as DisplayAttribute).GetOrder())
                .ToList();

            properties.ForEach(f =>
            {
                list.Add(new PropertySetting()
                {
                    DisplayWidth = (int)measurer.MeasureWidth(f.DisplayNameOrDefault(), fontFamily, fontSizePx),
                    Id = Guid.NewGuid(),
                    Name = f.Name,
                    DisplayName = f.GetCustomAttribute<DisplayAttribute>().Name ?? f.Name,
                    PropertyInfo = f
                });
            });
            return list;
        }

        public List<PropertySetting> GetDefaultProperties(string fontFamily, float fontSizePx, string fields)
            => GetDefaultProperties(fontFamily, fontSizePx, fields, EstimatingTextMeasurer.Instance);

        public List<PropertySetting> GetDefaultProperties(string fontFamily, float fontSizePx, string fields, ITextMeasurer measurer)
        {
            var list = new List<PropertySetting>();

            var type = ResolveType(ForEntity);
            var properties = type.GetProperties()
                .Where(w => w.GetCustomAttributes(typeof(DisplayAttribute), false).Any())
                .ToList();
            if (!string.IsNullOrEmpty(fields))
            {
                foreach (var field in fields.Split(","))
                {
                    var f = properties.Where(w => ((DisplayAttribute)w.GetCustomAttribute(typeof(DisplayAttribute))).Name == field).FirstOrDefault();
                    list.Add(new PropertySetting()
                    {
                        DisplayWidth = (int)measurer.MeasureWidth(f.DisplayNameOrDefault(), fontFamily, fontSizePx),
                        Id = Guid.NewGuid(),
                        Name = f.Name,
                        DisplayName = f.GetCustomAttribute<DisplayAttribute>().Name ?? f.Name,
                        PropertyInfo = f
                    });
                }
            }
            else
            {
                properties.ForEach(f => list.Add(new PropertySetting()
                {
                    DisplayWidth = (int)measurer.MeasureWidth(f.DisplayNameOrDefault(), fontFamily, fontSizePx),
                    Id = Guid.NewGuid(),
                    Name = f.Name,
                    DisplayName = f.GetCustomAttribute<DisplayAttribute>()?.Name ?? f.Name,
                    PropertyInfo = f
                }));
            }
            return list;
        }
```

- [ ] **Step 5: Switch FlexTable's default font size to pixels**

In `KBlazor/Components/FlexTable.razor.cs`, change line 21 from `float fontSize = 18.288f;` to:

```csharp
        float fontSize = 24.4f; // CSS pixels (was 18.288pt). Overwritten from GetComputedFont after first render.
```

- [ ] **Step 6: Drop the `System.Drawing.Common` package**

In `KBlazor/KBlazor.csproj`, delete the line:

```xml
    <PackageReference Include="System.Drawing.Common" Version="9.0.3" />
```

- [ ] **Step 7: Build and run all tests**

Run: `cd /h/Source/repos/KBlazor && dotnet test KBlazor.sln --nologo -v q 2>&1 | grep -v warning | tail -5`
Expected: `Passed! - Failed: 0, Passed: 38` (28 baseline + 6 from Task 1 + 4 here). No `CA1416` warnings remain in the build output (run `dotnet build KBlazor/KBlazor.csproj --nologo 2>&1 | grep -c CA1416` → `0`).

- [ ] **Step 8: Commit**

```bash
cd /h/Source/repos/KBlazor
git add KBlazor/Models/ExtensionMethods.cs KBlazor/Models/ListViewSetting.cs KBlazor/Components/FlexTable.razor.cs KBlazor/KBlazor.csproj KBlazor.Showcase.Tests/ListViewSettingWidthTests.cs
git commit -m "refactor: measure column widths with ITextMeasurer, drop System.Drawing.Common

Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>"
```

---

### Task 3: `IClientTimeZoneProvider`, `BrowserTimeZoneProvider`, and `kblazor.js` helpers

**Files:**
- Create: `KBlazor/Services/IClientTimeZoneProvider.cs`
- Create: `KBlazor/Services/BrowserTimeZoneProvider.cs`
- Modify: `KBlazor/wwwroot/kblazor.js` (append)
- Test: `KBlazor.Showcase.Tests/BrowserTimeZoneProviderTests.cs`

**Interfaces:**
- Produces: `KBlazor.Services.IClientTimeZoneProvider` with `ValueTask<int> GetOffsetMinutesAsync()`; `KBlazor.Services.BrowserTimeZoneProvider(IJSRuntime js)`; JS `window.KBlazor.getTimezoneOffsetMinutes()` → int; JS `window.KBlazor.measureText(texts: string[], fontFamily: string, fontSizePx: number)` → number[]. Task 4 calls both JS functions.

- [ ] **Step 1: Write the failing tests**

Create `KBlazor.Showcase.Tests/BrowserTimeZoneProviderTests.cs`:

```csharp
using KBlazor.Services;
using Microsoft.JSInterop;
using Xunit;

namespace KBlazor.Showcase.Tests;

public class BrowserTimeZoneProviderTests
{
    private sealed class FakeJs : IJSRuntime
    {
        private readonly Func<string, object> _handler;
        public FakeJs(Func<string, object> handler) => _handler = handler;

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => new((TValue)_handler(identifier));

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            => new((TValue)_handler(identifier));
    }

    [Fact]
    public async Task ReturnsOffsetFromJs()
    {
        var js = new FakeJs(id =>
        {
            Assert.Equal("KBlazor.getTimezoneOffsetMinutes", id);
            return -300;
        });
        var sut = new BrowserTimeZoneProvider(js);
        Assert.Equal(-300, await sut.GetOffsetMinutesAsync());
    }

    [Fact]
    public async Task ReturnsZero_WhenJsInteropFails()
    {
        var js = new FakeJs(_ => throw new InvalidOperationException("JavaScript interop calls cannot be issued at this time"));
        var sut = new BrowserTimeZoneProvider(js);
        Assert.Equal(0, await sut.GetOffsetMinutesAsync());
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `cd /h/Source/repos/KBlazor && dotnet test KBlazor.sln --nologo -v q --filter "FullyQualifiedName~BrowserTimeZoneProviderTests" 2>&1 | grep -v warning | tail -5`
Expected: build error `CS0246: 'BrowserTimeZoneProvider' could not be found`.

- [ ] **Step 3: Create the interface**

Create `KBlazor/Services/IClientTimeZoneProvider.cs`:

```csharp
namespace KBlazor.Services;

/// <summary>
/// Supplies the offset used to display stored <see cref="DateTime"/> values in
/// the user's local time. Optional: when no implementation is registered,
/// FlexTable applies no adjustment. Register <see cref="BrowserTimeZoneProvider"/>
/// to use the browser's timezone on Server or WebAssembly, or implement this to
/// read a cookie / user profile on the host.
/// </summary>
public interface IClientTimeZoneProvider
{
    /// <summary>
    /// Minutes to add to a stored DateTime to display it in the user's local time.
    /// </summary>
    ValueTask<int> GetOffsetMinutesAsync();
}
```

- [ ] **Step 4: Create the browser provider**

Create `KBlazor/Services/BrowserTimeZoneProvider.cs`:

```csharp
using Microsoft.JSInterop;

namespace KBlazor.Services;

/// <summary>
/// Reads the browser's timezone offset through kblazor.js. Requires
/// <c>&lt;script src="_content/KBlazor/kblazor.js"&gt;&lt;/script&gt;</c> in the host page.
/// Falls back to 0 when interop is unavailable (prerendering, missing script).
/// </summary>
public sealed class BrowserTimeZoneProvider : IClientTimeZoneProvider
{
    private readonly IJSRuntime _js;

    public BrowserTimeZoneProvider(IJSRuntime js)
    {
        _js = js;
    }

    public async ValueTask<int> GetOffsetMinutesAsync()
    {
        try
        {
            return await _js.InvokeAsync<int>("KBlazor.getTimezoneOffsetMinutes");
        }
        catch (Exception)
        {
            return 0;
        }
    }
}
```

- [ ] **Step 5: Add the JS helpers**

Append to the end of `KBlazor/wwwroot/kblazor.js`:

```javascript

// ── Namespaced helpers (1.1.0+) ─────────────────────────────────────────
// Kept separate from the legacy globals above so host pages that already
// reference ActivateTableResize / GetComputedFont keep working.
window.KBlazor = window.KBlazor || {};

// Minutes to ADD to a UTC DateTime to get the browser's local time.
// JS getTimezoneOffset() is local→UTC (positive west of UTC), so negate it.
window.KBlazor.getTimezoneOffsetMinutes = function () {
    return -new Date().getTimezoneOffset();
};

// Measure an array of strings with a canvas, in CSS pixels.
// One call per auto-size; returns widths in the same order as `texts`.
window.KBlazor.measureText = function (texts, fontFamily, fontSizePx) {
    var canvas = document.createElement('canvas');
    var ctx = canvas.getContext('2d');
    ctx.font = fontSizePx + 'px "' + fontFamily + '"';
    return (texts || []).map(function (t) {
        return ctx.measureText(t == null ? '' : String(t)).width;
    });
};
```

- [ ] **Step 6: Run the tests to verify they pass**

Run: `cd /h/Source/repos/KBlazor && dotnet test KBlazor.sln --nologo -v q 2>&1 | grep -v warning | tail -5`
Expected: `Passed! - Failed: 0, Passed: 40`

- [ ] **Step 7: Commit**

```bash
cd /h/Source/repos/KBlazor
git add KBlazor/Services/IClientTimeZoneProvider.cs KBlazor/Services/BrowserTimeZoneProvider.cs KBlazor/wwwroot/kblazor.js KBlazor.Showcase.Tests/BrowserTimeZoneProviderTests.cs
git commit -m "feat: add optional IClientTimeZoneProvider with browser implementation and canvas measureText helper

Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>"
```

---

### Task 4: Make `FlexTable` host-agnostic and drop the framework reference

**Files:**
- Modify: `KBlazor/KBlazor.csproj`
- Modify: `KBlazor/Components/FlexTable.razor.cs` (usings; fields lines 16–35; `OnInitialized` lines 254–284; `OnAfterRenderAsync` lines 286–307; `AutoSizeDiv` lines 368–385; five `GetDefaultProperties(fontFamily, fontSize...)` call sites at ~lines 332, 343, 558, 592, 630)
- Modify: `KBlazor/Components/FlexTable.razor` (outer guard line 104; timezone block lines 543–548)

**Interfaces:**
- Consumes: `ITextMeasurer`, `EstimatingTextMeasurer.Instance` (Task 1); `GetDefaultProperties(..., ITextMeasurer)` overloads (Task 2); `IClientTimeZoneProvider` and JS `KBlazor.measureText` (Task 3).
- Produces: `FlexTable` no longer requires `IHttpContextAccessor` or `AuthenticationStateProvider` registrations. `AutoSizeDiv` is `async Task`.

- [ ] **Step 1: Update the csproj**

In `KBlazor/KBlazor.csproj`, delete this item group entirely:

```xml
  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>
```

and add these two lines inside the `<ItemGroup>` that contains the MudBlazor `PackageReference`:

```xml
    <PackageReference Include="Microsoft.AspNetCore.Components.Web" Version="10.0.12" />
    <PackageReference Include="Microsoft.AspNetCore.Components.Authorization" Version="10.0.12" />
```

- [ ] **Step 2: Build to see the expected breakage**

Run: `cd /h/Source/repos/KBlazor && dotnet build KBlazor/KBlazor.csproj --nologo 2>&1 | grep -E "error" | head -5`
Expected: errors mentioning `Microsoft.AspNetCore.Http` / `IHttpContextAccessor` in `FlexTable.razor.cs` (the server-only namespace is gone). This confirms the framework reference was the only thing supplying it.

- [ ] **Step 3: Update usings, fields, and injections in `FlexTable.razor.cs`**

Replace the using block (lines 1–10) with:

```csharp
using KBlazor.Models;
using KBlazor.Services;
using KBlazor.Attributes;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components.Web;
using System.Reflection;
```

Replace the field/injection block from `string currentUsername = string.Empty;` through `[Inject] IHttpContextAccessor HttpContextAccessor { get; set; }` with:

```csharp
        string currentUsername = string.Empty;
        string currentListViewName = string.Empty;
        List<PropertySetting> defaultProperties = null;
        ListViewSetting listViewSetting;

        // CSS font used for width estimates. Overwritten from GetComputedFont after the first render.
        string fontFamily = "Helvetica Neue";
        float fontSize = 24.4f; // CSS pixels (was 18.288pt)

        // Optional host services, resolved from DI with built-in fallbacks so the
        // component works on Blazor Server and WebAssembly without extra registrations.
        ITextMeasurer _measurer = EstimatingTextMeasurer.Instance;
        int _timezoneOffsetMinutes = 0;

        DotNetObjectReference<FlexTable<TItem>> dotNetObjectReference = null;

        // True only while LoadView is running. LoadView calls SortAndFilter(), which calls
        // AutoSaveView() — without this guard, merely opening a page would auto-persist (and,
        // for a shared view, spawn a personal clone) before the user changes anything.
        bool _loadingView = false;

        [Inject] IListViewSettingStore ViewStore { get; set; }
        [Inject] IEntityLookupProvider EntityProvider { get; set; }
        [Inject] IFlexTableSettings FlexSettings { get; set; }
        [Inject] IJSRuntime Js { get; set; }
        [Inject] IServiceProvider Services { get; set; }
```

- [ ] **Step 4: Replace `OnInitialized` with `OnInitializedAsync`**

Replace the whole `protected override void OnInitialized() { ... }` method with:

```csharp
        protected override async Task OnInitializedAsync()
        {
            currentViewMode = DefaultViewMode;
            ValidateKanbanConfig();
            _measurer = Services.GetService<ITextMeasurer>() ?? EstimatingTextMeasurer.Instance;
            bool enablePersonalViews = FlexSettings.EnablePersonalViews;

            // AuthenticationStateProvider is registered by default on Blazor Server but not on
            // WebAssembly apps without authentication, so resolve it optionally. Never block on
            // the task: WebAssembly is single-threaded and .Result would deadlock.
            var authProvider = Services.GetService<AuthenticationStateProvider>();
            if (authProvider != null)
            {
                try
                {
                    var authenticationState = await authProvider.GetAuthenticationStateAsync();
                    IsAdmin = FlexSettings.AdminRoles.Any(role => authenticationState.User.IsInRole(role));
                    if (enablePersonalViews)
                    {
                        currentUsername = authenticationState.User.Identity?.Name ?? "anonymous";
                    }
                }
                catch (Exception)
                {
                    IsAdmin = false;
                    if (enablePersonalViews)
                    {
                        currentUsername = "anonymous";
                    }
                }
            }
            else if (enablePersonalViews)
            {
                currentUsername = "anonymous";
            }
            UserCanUpdate = IsAdmin || enablePersonalViews;

            dotNetObjectReference = DotNetObjectReference.Create(this);
            if (enablePersonalViews)
            {
                LoadView(Guid.Empty);
            }
            else
            {
                LoadView(ViewStore.GetIdByNameAndEntity(ViewName, typeof(TItem).FullName));
            }
            RefreshAvailableViews();
        }
```

- [ ] **Step 5: Replace `OnAfterRenderAsync`**

Replace the whole `protected override async Task OnAfterRenderAsync(bool firstRender) { ... }` method with:

```csharp
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            try
            {
                await Js.InvokeVoidAsync("ActivateTableResize", dotNetObjectReference);
                var computedFont = (await Js.InvokeAsync<string>("GetComputedFont", new object[] { "fontComputer" }))
                    .Split(',', StringSplitOptions.TrimEntries);
                if (computedFont.Length >= 2)
                {
                    // Assign to the fields (an earlier version declared shadowing locals here,
                    // so the computed font never reached the width code).
                    fontFamily = computedFont[0];
                    var digits = new string(computedFont[1].TakeWhile(w => Char.IsDigit(w) || w == '.').ToArray());
                    if (float.TryParse(digits, System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out var parsedPx) && parsedPx > 0)
                    {
                        fontSize = parsedPx;
                    }
                }
            }
            catch (Exception)
            {
                // kblazor.js not loaded, or interop unavailable — keep the defaults.
            }

            if (firstRender)
            {
                var timeZone = Services.GetService<IClientTimeZoneProvider>();
                if (timeZone != null)
                {
                    var offset = await timeZone.GetOffsetMinutesAsync();
                    if (offset != _timezoneOffsetMinutes)
                    {
                        _timezoneOffsetMinutes = offset;
                        StateHasChanged();
                    }
                }

                var lastViewId = await GetLastViewId();
                if (lastViewId != Guid.Empty && lastViewId != listViewSetting?.Id)
                {
                    var lastView = ViewStore.GetById(lastViewId);
                    if (lastView != null && lastView.ForEntity == typeof(TItem).FullName)
                    {
                        LoadView(lastViewId);
                        RefreshAvailableViews();
                        StateHasChanged();
                    }
                }
            }
        }
```

- [ ] **Step 6: Pass the measurer at every `GetDefaultProperties` call site**

There are five calls in `FlexTable.razor.cs`. Change each as follows (search for the exact old text):

1. In `LoadView`: `listViewSetting.GetDefaultProperties(fontFamily, fontSize, Fields)` → `listViewSetting.GetDefaultProperties(fontFamily, fontSize, Fields, _measurer)`
2. In `LoadView`: `defaultProperties = listViewSetting.GetDefaultProperties(fontFamily, fontSize).Where(` → `defaultProperties = listViewSetting.GetDefaultProperties(fontFamily, fontSize, _measurer).Where(`
3. In `SaveEditMode`: `defaultProperties = listViewSetting.GetDefaultProperties(fontFamily, fontSize)` → `defaultProperties = listViewSetting.GetDefaultProperties(fontFamily, fontSize, _measurer)`
4. In `CreateNewView`: same replacement as 3.
5. In `ResetView`: same replacement as 3.

Verify with: `grep -n "GetDefaultProperties(fontFamily, fontSize" KBlazor/Components/FlexTable.razor.cs` — every line must end the argument list with `_measurer)`.

- [ ] **Step 7: Replace `AutoSizeDiv` with the async canvas version**

Replace the whole `void AutoSizeDiv(PropertySetting propertySetting) { ... }` method with:

```csharp
        /// <summary>
        /// Double-click on a header: size the column to its widest visible value, measured
        /// with the real computed font via canvas measureText in kblazor.js.
        /// </summary>
        async Task AutoSizeDiv(PropertySetting propertySetting)
        {
            var values = ViewItems
                .Select(s => propertySetting.PropertyInfo.GetValue(s))
                .Select(v => v == null ? null
                           : v is DateTime dt && dt == DateTime.MinValue ? "N/A"
                           : v.ToString())
                .ToList();

            float[] widths;
            try
            {
                var texts = values.Where(t => t != null).Cast<string>().ToArray();
                widths = await Js.InvokeAsync<float[]>("KBlazor.measureText", texts, fontFamily, fontSize);
            }
            catch (Exception)
            {
                return; // kblazor.js missing or interop unavailable: leave the width unchanged
            }

            float max = widths.Length > 0 ? widths.Max() : 0f;
            if (values.Any(t => t == null))
            {
                max = Math.Max(max, 200f); // same floor as before for null cells
            }
            if (max <= 0f)
            {
                return;
            }

            propertySetting.DisplayWidth = Math.Max((int)max - 100, 1); // same padding rule as before, never negative
            AutoSaveView();
            StateHasChanged();
        }
```

- [ ] **Step 8: Update `FlexTable.razor`**

(a) Change the outer guard at line 104 from `@if (Items != null)` to:

```razor
@if (Items != null && listViewSetting != null)
```

(b) Replace the timezone block inside the cell loop:

```razor
                        var token = HttpContextAccessor?.HttpContext?.Request.Cookies["TimezoneOffset"];
                        if (rawValue != null && rawValue.GetType() == typeof(DateTime) && !string.IsNullOrEmpty(token))
                        {
                            var offsetInMinutes = Convert.ToInt32(token);
                            rawValue = ((DateTime)rawValue).AddMinutes(offsetInMinutes);
                        }
                        else if (setting.PropertyInfo.PropertyType.IsEnum && rawValue != null && renderTemplate == null)
```

with:

```razor
                        if (rawValue is DateTime dateValue && _timezoneOffsetMinutes != 0)
                        {
                            rawValue = dateValue.AddMinutes(_timezoneOffsetMinutes);
                        }
                        else if (setting.PropertyInfo.PropertyType.IsEnum && rawValue != null && renderTemplate == null)
```

- [ ] **Step 9: Build, confirm no server-only references remain, run tests**

Run:
```bash
cd /h/Source/repos/KBlazor
dotnet build KBlazor.sln --nologo 2>&1 | grep -E "error|Build succeeded" | head -5
grep -rn "IHttpContextAccessor\|Microsoft.AspNetCore.Http\|System.Drawing\|\.Result\b" KBlazor --include=*.cs --include=*.razor | grep -v "/obj/\|/bin/"
dotnet test KBlazor.sln --nologo -v q 2>&1 | grep -v warning | tail -3
```
Expected: `Build succeeded`; the grep prints nothing; `Passed! - Failed: 0, Passed: 40`.

- [ ] **Step 10: Server regression check on the showcase**

Run the showcase and fetch the prerendered FlexTable demo:
```bash
cd /h/Source/repos/KBlazor
(dotnet run --project KBlazor.Showcase --launch-profile http > /tmp/showcase.log 2>&1 &) ; sleep 25
curl -s http://localhost:5166/demo/flextable | grep -c "ORD-0041"
curl -s http://localhost:5166/demo/flextable | grep -c "unhandled error"
grep -ci "exception" /tmp/showcase.log
```
Expected: `1` or more for ORD-0041, `0` for unhandled error, `0` exceptions. Then stop it: `taskkill //F //IM KBlazor.Showcase.exe` (Git Bash) or `Stop-Process -Name KBlazor.Showcase` (PowerShell).

If a browser pane is available, additionally open `http://localhost:5166/demo/flextable`, double-click the "Customer" column header, and confirm the column width changes and the console has no errors.

- [ ] **Step 11: Commit**

```bash
cd /h/Source/repos/KBlazor
git add KBlazor/KBlazor.csproj KBlazor/Components/FlexTable.razor.cs KBlazor/Components/FlexTable.razor
git commit -m "feat: make FlexTable host-agnostic (optional auth/timezone/measurer), drop Microsoft.AspNetCore.App reference

Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>"
```

---

### Task 5: WebAssembly smoke sample

**Files:**
- Create: `KBlazor.WasmSample/KBlazor.WasmSample.csproj`
- Create: `KBlazor.WasmSample/Program.cs`
- Create: `KBlazor.WasmSample/App.razor`
- Create: `KBlazor.WasmSample/_Imports.razor`
- Create: `KBlazor.WasmSample/Layout/MainLayout.razor`
- Create: `KBlazor.WasmSample/Pages/Index.razor`
- Create: `KBlazor.WasmSample/wwwroot/index.html`
- Create: `KBlazor.WasmSample/Properties/launchSettings.json`
- Create: `KBlazor.WasmSample/Domain/Customer.cs`, `Domain/OrderStatus.cs`, `Domain/PurchaseOrder.cs`
- Create: `KBlazor.WasmSample/Services/InMemoryFlexTableSettings.cs`, `Services/InMemoryListViewSettingStore.cs`, `Services/InMemoryEntityLookupProvider.cs`
- Create: `KBlazor.WasmSample/Data/DataStore.cs`, `Data/SeedData.cs`
- Modify: `KBlazor.sln` (via `dotnet sln add`)
- Modify: `.claude/launch.json` (add `wasm-sample`)

**Interfaces:**
- Consumes: everything from Tasks 1–4; `BrowserTimeZoneProvider`.
- Produces: a buildable, runnable WASM app on port 5290 used for verification in this task and Task 7.

- [ ] **Step 1: Create the project file**

Create `KBlazor.WasmSample/KBlazor.WasmSample.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk.BlazorWebAssembly">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>KBlazor.WasmSample</RootNamespace>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly" Version="10.0.12" />
    <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly.DevServer" Version="10.0.12" PrivateAssets="all" />
    <PackageReference Include="MudBlazor" Version="8.15.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\KBlazor\KBlazor.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 2: Create the domain, data, and service files**

Create `KBlazor.WasmSample/Domain/Customer.cs`:

```csharp
using System.ComponentModel.DataAnnotations;
using KBlazor.Models;
using Newtonsoft.Json;

namespace KBlazor.WasmSample.Domain;

public class Customer : IKBusinessEntity
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Display(Name = "Customer Name", Order = 1)]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Email", Order = 2)]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Country", Order = 3)]
    public string Country { get; set; } = string.Empty;

    public bool Equals(IKBusinessEntity? other) => Id == other?.Id;
    public override string ToString() => Name;
    public string ToJson() => JsonConvert.SerializeObject(this);
}
```

Create `KBlazor.WasmSample/Domain/OrderStatus.cs`:

```csharp
namespace KBlazor.WasmSample.Domain;

public enum OrderStatus
{
    New = 0,
    Pending = 1,
    InProgress = 2,
    Delivered = 3,
    Cancelled = 4
}
```

Create `KBlazor.WasmSample/Domain/PurchaseOrder.cs`:

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using KBlazor.Attributes;
using KBlazor.Models;
using Newtonsoft.Json;

namespace KBlazor.WasmSample.Domain;

public class PurchaseOrder : IKBusinessEntity
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Display(Name = "Order #", Order = 1)]
    [ReadOnlyOnEdit]
    public string Name { get; set; } = string.Empty;

    [ForeignKey("Customer")]
    public Guid? CustomerId { get; set; }

    [Display(Name = "Customer (lookup)", Order = 2)]
    [SortAndFilterOn(FilterPath = "CustomerId", SortPath = "Customer.Name")]
    public virtual Customer? Customer { get; set; }

    [Display(Name = "Customer", Order = 2)]
    public string CustomerName => Customer?.Name ?? string.Empty;

    [Display(Name = "Status", Order = 3)]
    public OrderStatus Status { get; set; }

    [Display(Name = "Order Date", Order = 4)]
    public DateTime OrderDate { get; set; }

    [Display(Name = "Delivery Date", Order = 5)]
    public DateTime? DeliveryDate { get; set; }

    [Display(Name = "Amount", Order = 6)]
    public decimal Amount { get; set; }

    [Display(Name = "Urgent", Order = 8)]
    [AllowInlineEdit]
    public bool IsUrgent { get; set; }

    [Display(Name = "Notes", Order = 9)]
    [MemoDisplay]
    public string Notes { get; set; } = string.Empty;

    public bool Equals(IKBusinessEntity? other) => Id == other?.Id;
    public override string ToString() => Name;
    public string ToJson() => JsonConvert.SerializeObject(this);
}
```

Create `KBlazor.WasmSample/Data/DataStore.cs`:

```csharp
using KBlazor.WasmSample.Domain;

namespace KBlazor.WasmSample.Data;

public class DataStore
{
    public List<Customer> Customers { get; } = new();
    public List<PurchaseOrder> Orders { get; } = new();
}
```

Create `KBlazor.WasmSample/Data/SeedData.cs`:

```csharp
using KBlazor.WasmSample.Domain;

namespace KBlazor.WasmSample.Data;

public static class SeedData
{
    public static DataStore Create()
    {
        var store = new DataStore();

        var acme     = new Customer { Name = "Acme Corp",    Email = "orders@acme.com",     Country = "USA" };
        var globex   = new Customer { Name = "Globex Inc",   Email = "orders@globex.com",   Country = "Canada" };
        var initech  = new Customer { Name = "Initech LLC",  Email = "orders@initech.com",  Country = "USA" };
        var umbrella = new Customer { Name = "Umbrella Co",  Email = "orders@umbrella.com", Country = "UK" };
        var soylent  = new Customer { Name = "Soylent Corp", Email = "orders@soylent.com",  Country = "Australia" };
        store.Customers.AddRange(new[] { acme, globex, initech, umbrella, soylent });

        // Filler customers so the entity filter's name search past the 100-item cap is exercised.
        for (int i = 6; i <= 150; i++)
        {
            store.Customers.Add(new Customer
            {
                Name = $"Test Customer {i:000}",
                Email = $"customer{i:000}@example.com",
                Country = "USA"
            });
        }

        var named = new[] { acme, globex, initech, umbrella, soylent };
        var statuses = new[] { OrderStatus.Delivered, OrderStatus.Pending, OrderStatus.New, OrderStatus.Cancelled, OrderStatus.InProgress };
        var baseDate = new DateTime(2026, 3, 1);
        for (int i = 0; i < 20; i++)
        {
            var customer = named[i % named.Length];
            var status = statuses[i % statuses.Length];
            var order = new PurchaseOrder
            {
                Name = $"ORD-{41 + i:0000}",
                CustomerId = customer.Id,
                Customer = customer,
                Status = status,
                OrderDate = baseDate.AddDays(i),
                DeliveryDate = status == OrderStatus.Delivered ? baseDate.AddDays(i + 5) : null,
                Amount = 500m + i * 375m,
                IsUrgent = i % 3 == 0,
                Notes = i % 4 == 0 ? "Priority account." : string.Empty
            };
            store.Orders.Add(order);
        }
        return store;
    }
}
```

Create `KBlazor.WasmSample/Services/InMemoryFlexTableSettings.cs`:

```csharp
using KBlazor.Services;

namespace KBlazor.WasmSample.Services;

public class InMemoryFlexTableSettings : IFlexTableSettings
{
    public bool EnablePersonalViews => true;
    public string[] AdminRoles => Array.Empty<string>();
}
```

Create `KBlazor.WasmSample/Services/InMemoryListViewSettingStore.cs`:

```csharp
using KBlazor.Models;
using KBlazor.Services;

namespace KBlazor.WasmSample.Services;

public class InMemoryListViewSettingStore : IListViewSettingStore
{
    private readonly List<ListViewSetting> _settings = new();

    public ListViewSetting? GetById(Guid id) => _settings.FirstOrDefault(s => s.Id == id);

    public ListViewSetting? GetByUserAndView(string entityType, string userName, string viewName) =>
        _settings.FirstOrDefault(s => s.ForEntity == entityType && s.CustomizedForUser == userName && s.Name == viewName);

    public ListViewSetting? GetByNameAndEntity(string viewName, string entityType) =>
        _settings.FirstOrDefault(s => s.Name == viewName && s.ForEntity == entityType);

    public Guid GetIdByNameAndEntity(string viewName, string entityType) =>
        GetByNameAndEntity(viewName, entityType)?.Id ?? Guid.Empty;

    public void Add(ListViewSetting setting) => _settings.Add(setting);

    public void Update(ListViewSetting setting)
    {
        var index = _settings.FindIndex(s => s.Id == setting.Id);
        if (index >= 0) _settings[index] = setting;
    }

    public void Delete(Guid id) => _settings.RemoveAll(s => s.Id == id);

    public List<ListViewSetting> GetAllForEntity(string entityType, string? currentUsername) =>
        _settings.Where(s => s.ForEntity == entityType).ToList();

    public void SaveChanges() { }
}
```

Create `KBlazor.WasmSample/Services/InMemoryEntityLookupProvider.cs`:

```csharp
using KBlazor.Models;
using KBlazor.Services;
using KBlazor.WasmSample.Data;
using KBlazor.WasmSample.Domain;

namespace KBlazor.WasmSample.Services;

public class InMemoryEntityLookupProvider : IEntityLookupProvider
{
    private readonly DataStore _store;

    public InMemoryEntityLookupProvider(DataStore store) => _store = store;

    public bool IsKnownEntityType(Type type) =>
        type == typeof(Customer) || type == typeof(PurchaseOrder);

    public IQueryable<IKBusinessEntity> GetEntities(Type entityType)
    {
        if (entityType == typeof(Customer)) return _store.Customers.AsQueryable();
        if (entityType == typeof(PurchaseOrder)) return _store.Orders.AsQueryable();
        return Enumerable.Empty<IKBusinessEntity>().AsQueryable();
    }

    public IQueryable<IKBusinessEntity> GetEntitiesWithInclude(Type entityType, string includeName) =>
        GetEntities(entityType);
}
```

- [ ] **Step 3: Create the app shell**

Create `KBlazor.WasmSample/Program.cs`:

```csharp
using KBlazor.Services;
using KBlazor.WasmSample;
using KBlazor.WasmSample.Data;
using KBlazor.WasmSample.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddMudServices();

// Seed data, shared for the app lifetime
builder.Services.AddSingleton(SeedData.Create());

// The three services KBlazor requires
builder.Services.AddScoped<IFlexTableSettings, InMemoryFlexTableSettings>();
builder.Services.AddSingleton<IListViewSettingStore, InMemoryListViewSettingStore>();
builder.Services.AddScoped<IEntityLookupProvider, InMemoryEntityLookupProvider>();

// Optional: show DateTimes in the browser's local time (exercises the JS interop path)
builder.Services.AddScoped<IClientTimeZoneProvider, BrowserTimeZoneProvider>();

// Deliberately no authentication services: FlexTable must cope without AuthenticationStateProvider.

await builder.Build().RunAsync();
```

Create `KBlazor.WasmSample/App.razor`:

```razor
<Router AppAssembly="@typeof(App).Assembly">
    <Found Context="routeData">
        <RouteView RouteData="@routeData" DefaultLayout="@typeof(Layout.MainLayout)" />
    </Found>
    <NotFound>
        <LayoutView Layout="@typeof(Layout.MainLayout)">
            <p role="alert">Sorry, there's nothing at this address.</p>
        </LayoutView>
    </NotFound>
</Router>
```

Create `KBlazor.WasmSample/_Imports.razor`:

```razor
@using System.Net.Http
@using Microsoft.AspNetCore.Components.Forms
@using Microsoft.AspNetCore.Components.Routing
@using Microsoft.AspNetCore.Components.Web
@using Microsoft.JSInterop
@using MudBlazor
@using KBlazor.Models
@using KBlazor.Attributes
@using KBlazor.Services
@using KBlazor.Components
@using KBlazor.WasmSample
@using KBlazor.WasmSample.Data
@using KBlazor.WasmSample.Domain
```

Create `KBlazor.WasmSample/Layout/MainLayout.razor`:

```razor
@inherits LayoutComponentBase

<MudThemeProvider />
<MudPopoverProvider />
<MudDialogProvider />
<MudSnackbarProvider />

<div style="padding:16px; font-family: Inter, 'Helvetica Neue', Arial, sans-serif;">
    @Body
</div>
```

Create `KBlazor.WasmSample/Pages/Index.razor`:

```razor
@page "/"
@inject DataStore Store

<PageTitle>KBlazor WebAssembly Sample</PageTitle>

<h2 id="sample-title">KBlazor on WebAssembly</h2>
<p>Standalone Blazor WebAssembly host referencing the KBlazor package. No authentication services registered.</p>

<MudPaper Elevation="1" Style="padding:8px;">
    <FlexTable TItem="PurchaseOrder"
               Items="@_orders"
               Fields="Order #,Customer,Customer (lookup),Status,Amount,Order Date,Urgent"
               ViewName="WasmSample"
               PageSize="10"
               SelectionChanged="OnRowClicked"
               SortFilter="OnSortFilter"
               SelectedItem="@_selected" />
</MudPaper>

@if (_selected != null)
{
    <MudPaper Elevation="1" Style="padding:8px; margin-top:16px;">
        <BasicEdit TItem="PurchaseOrder" Item="@_selected" Columns="2"
                   Save="@(() => StateHasChanged())"
                   Close="@(() => { _selected = null; StateHasChanged(); })" />
    </MudPaper>
}

@code {
    private IQueryable<PurchaseOrder> _orders = default!;
    private PurchaseOrder? _selected;

    protected override void OnInitialized()
    {
        _orders = Store.Orders.AsQueryable();
    }

    private void OnRowClicked(PurchaseOrder order, string command)
    {
        _selected = order;
    }

    private void OnSortFilter(ListViewSetting setting)
    {
        _orders = Store.Orders.AsQueryable().ApplyFilter(setting).ApplySort(setting);
        StateHasChanged();
    }
}
```

Create `KBlazor.WasmSample/wwwroot/index.html`:

```html
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no" />
    <title>KBlazor WebAssembly Sample</title>
    <base href="/" />
    <link href="https://fonts.googleapis.com/css?family=Roboto:300,400,500,700&display=swap" rel="stylesheet" />
    <link href="_content/MudBlazor/MudBlazor.min.css" rel="stylesheet" />
    <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.7.2/css/all.min.css" />
</head>
<body>
    <div id="app">
        <div style="padding:24px; font-family: sans-serif;">Loading KBlazor sample…</div>
    </div>

    <div id="blazor-error-ui" style="display:none; position:fixed; bottom:0; left:0; right:0; background:#b32121; color:#fff; padding:8px 16px;">
        An unhandled error has occurred.
        <a href="" class="reload">Reload</a>
        <a class="dismiss" style="cursor:pointer; float:right;">🗙</a>
    </div>

    <script src="_framework/blazor.webassembly.js"></script>
    <script src="_content/MudBlazor/MudBlazor.min.js"></script>
    <script src="_content/KBlazor/kblazor.js"></script>
</body>
</html>
```

Create `KBlazor.WasmSample/Properties/launchSettings.json`:

```json
{
  "profiles": {
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": false,
      "inspectUri": "{wsProtocol}://{url.hostname}:{url.port}/_framework/debug/ws-proxy?browser={browserInspectUri}",
      "applicationUrl": "http://localhost:5290",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

- [ ] **Step 4: Add the project to the solution and the launch config**

```bash
cd /h/Source/repos/KBlazor
dotnet sln KBlazor.sln add KBlazor.WasmSample/KBlazor.WasmSample.csproj
```

Replace the contents of `.claude/launch.json` with:

```json
{
  "version": "0.0.1",
  "configurations": [
    {
      "name": "showcase",
      "runtimeExecutable": "dotnet",
      "runtimeArgs": ["run", "--project", "KBlazor.Showcase", "--launch-profile", "http"],
      "port": 5166
    },
    {
      "name": "wasm-sample",
      "runtimeExecutable": "dotnet",
      "runtimeArgs": ["run", "--project", "KBlazor.WasmSample", "--launch-profile", "http"],
      "port": 5290
    }
  ]
}
```

- [ ] **Step 5: Build (this is the NETSDK1082 check)**

Run: `cd /h/Source/repos/KBlazor && dotnet build KBlazor.sln --nologo 2>&1 | grep -E "error|NETSDK|Build succeeded" | head -5`
Expected: `Build succeeded` and no `NETSDK1082`.

- [ ] **Step 6: Publish (default Blazor trimming)**

Run: `cd /h/Source/repos/KBlazor && dotnet publish KBlazor.WasmSample -c Release -o "$TMPDIR/kblazor-wasm-publish" --nologo 2>&1 | grep -E "error|IL2|Build succeeded|->" | head -10`
Expected: `Build succeeded` (trim warnings `IL2xxx` may appear; errors may not). Confirm `ls "$TMPDIR/kblazor-wasm-publish/wwwroot/_framework" | grep -c "KBlazor"` prints at least `1`.

If `$TMPDIR` is unset, use the session scratchpad directory or `C:/Users/shawn/AppData/Local/Temp/kblazor-wasm-publish`.

- [ ] **Step 7: Runtime check on the trimmed publish output**

Serve the published `wwwroot` with Python and open it in the browser pane (or the in-app Browser):
```bash
cd "$TMPDIR/kblazor-wasm-publish/wwwroot" && python -m http.server 5291 > /dev/null 2>&1 &
```
Open `http://localhost:5291/` and verify, with the console error list empty after each action:
1. The table renders rows `ORD-0041` … `ORD-0050` and the toolbar shows the `WASMSAMPLE` view menu.
2. Click the sort icon on the "Customer" header: rows reorder.
3. Open the "Customer (lookup)" filter, type `142`, tick `Test Customer 142`, OK: the table shows 0 rows (no orders for filler customers), and "Total Items: 0". Clear the filter with the clear button.
4. Open the view editor (column icon), toggle "Notes" on, Save: the table gains a Notes column. Then use the view menu to create "New View" (Save it), and switch back to the first view via the menu: the Notes column is still present. Switching views calls `LoadView`, which deserializes the stored JSON through Newtonsoft, so this proves the serialization round-trip survives trimming. (A page reload is not a valid check: the store is in-memory in the browser and resets.)
5. Double-click the "Customer" header: the column width changes.
6. Click a row number: the BasicEdit panel appears below with the order's fields, and the "Order Date" picker shows a date.

If step 1 fails with a JSON/reflection error in the console (e.g. `MissingMethodException`, `Could not create an instance of type`), the trimmer removed members Newtonsoft needs. Fix by adding to `KBlazor/KBlazor.csproj`:

```xml
  <ItemGroup>
    <EmbeddedResource Include="ILLink.Descriptors.xml">
      <LogicalName>ILLink.Descriptors.xml</LogicalName>
    </EmbeddedResource>
  </ItemGroup>
```

and creating `KBlazor/ILLink.Descriptors.xml`:

```xml
<linker>
  <assembly fullname="KBlazor">
    <type fullname="KBlazor.Models.PropertySetting" preserve="all" />
    <type fullname="KBlazor.Models.ViewDefinition" preserve="all" />
    <type fullname="KBlazor.Models.ListViewSetting" preserve="all" />
  </assembly>
</linker>
```

then re-run Steps 6–7.

Stop the Python server when done (`taskkill //F //IM python.exe` in Git Bash, or `Stop-Process -Name python` in PowerShell).

- [ ] **Step 8: Commit**

```bash
cd /h/Source/repos/KBlazor
git add KBlazor.sln KBlazor.WasmSample .claude/launch.json
git add KBlazor/ILLink.Descriptors.xml KBlazor/KBlazor.csproj 2>/dev/null
git commit -m "test: add KBlazor.WasmSample standalone WebAssembly smoke project

Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>"
```

---

### Task 6: Docs, website, version 1.1.0

**Files:**
- Modify: `KBlazor/KBlazor.csproj` (`<Version>`, `<Description>`)
- Modify: `KBlazor/docs/getting-started.md`, `KBlazor/docs/service-registration.md`, `KBlazor/docs/flextable.md`, `KBlazor/docs/models.md`, `KBlazor/CLAUDE.md`
- Modify: `README.md`
- Modify: `KBlazor.Showcase/Pages/GettingStarted.razor`, `KBlazor.Showcase/Pages/Home.razor`, `KBlazor.Showcase/Docs/DocContent.cs`

**Interfaces:**
- Consumes: names from Tasks 1–4: `IClientTimeZoneProvider`, `BrowserTimeZoneProvider`, `ITextMeasurer`, `EstimatingTextMeasurer`.

- [ ] **Step 1: Bump the package version and description**

In `KBlazor/KBlazor.csproj`:
- `<Version>1.0.5</Version>` → `<Version>1.1.0</Version>`
- `<Description>AI-ready Blazor components with FlexTable, BasicEdit, and DatePicker. Supports Table, Card, and Kanban views with zero configuration.</Description>` → `<Description>AI-ready Blazor components for Blazor Server and WebAssembly: FlexTable, BasicEdit, and RelativeDatePicker. Table, Chips, and Kanban views with zero configuration.</Description>`

- [ ] **Step 2: Update `KBlazor/docs/getting-started.md`**

(a) Replace the Overview paragraph:

```markdown
KBlazor is a Razor Class Library targeting .NET 10+ that provides reusable, data-driven UI components for Blazor. It runs in both **Blazor Server** and **Blazor WebAssembly** hosts from a single package. It is built on top of MudBlazor and uses Entity Framework Core for persistence of view settings.
```

(b) Replace the Dependencies table with:

```markdown
| Package | Version | Purpose |
|---------|---------|---------|
| MudBlazor | 8.15.0 | UI component framework |
| Microsoft.AspNetCore.Components.Web | 10.0.12 | Blazor component model |
| Microsoft.AspNetCore.Components.Authorization | 10.0.12 | Optional role checks for view management |
| Microsoft.EntityFrameworkCore | 10.0.3 | ORM for view persistence |
| Microsoft.EntityFrameworkCore.Relational | 10.0.3 | Relational DB support |
| Newtonsoft.Json | 13.0.4 | View definition serialization |

KBlazor no longer references the `Microsoft.AspNetCore.App` shared framework or `System.Drawing.Common`, which is what makes WebAssembly hosting possible (1.1.0+).
```

(c) Replace the "### 2. Include KBlazor JavaScript" section body with:

```markdown
FlexTable requires a JavaScript file for column resizing, font measurement, and the optional browser timezone provider. Add this script reference to your host page:

| Host | File |
|------|------|
| Blazor Server (Razor Pages host) | `Pages/_Host.cshtml` or `Pages/_Layout.cshtml` |
| Blazor Server / Web App (.NET 8+) | `Components/App.razor` |
| Blazor WebAssembly | `wwwroot/index.html` |

```html
<script src="_content/KBlazor/kblazor.js"></script>
```

This is a static asset served automatically by the Razor Class Library — no manual file copying needed.
```

(d) In "### 5. Register Required Services", after the code block add:

```markdown
Two further services are **optional** and have built-in defaults; see [Service Registration — Optional services](service-registration.md#optional-services):

- `IClientTimeZoneProvider` — display `DateTime` values in the user's local time (default: no adjustment).
- `ITextMeasurer` — column width estimation (default: built-in estimator).

`AuthenticationStateProvider` is used when present (Blazor Server registers one automatically) and skipped when absent, so WebAssembly apps without authentication work unchanged.
```

- [ ] **Step 3: Update `KBlazor/docs/service-registration.md`**

(a) Change the opening sentence to:

```markdown
KBlazor requires three service interfaces to be implemented by the consuming application and registered in the DI container, and offers two optional ones with built-in defaults. These interfaces decouple KBlazor from your specific database, authentication, hosting model, and configuration concerns.
```

(b) Append this section at the end of the file:

```markdown
## Optional services

Both of these are resolved with `IServiceProvider.GetService`; when nothing is registered, FlexTable uses the built-in default. Existing hosts need not register either.

### IClientTimeZoneProvider

```csharp
public interface IClientTimeZoneProvider
{
    /// Minutes to add to a stored DateTime to display it in the user's local time.
    ValueTask<int> GetOffsetMinutesAsync();
}
```

- **Default (unregistered):** offset 0 — `DateTime` values render as stored.
- **Browser timezone (Server or WebAssembly):** ships in the package and reads the offset via `kblazor.js`:

```csharp
builder.Services.AddScoped<IClientTimeZoneProvider, BrowserTimeZoneProvider>();
```

- **Cookie-based (Blazor Server hosts that set a `TimezoneOffset` cookie):** implement it in the host, where `IHttpContextAccessor` is available:

```csharp
using KBlazor.Services;
using Microsoft.AspNetCore.Http;

public sealed class CookieTimeZoneProvider : IClientTimeZoneProvider
{
    private readonly IHttpContextAccessor _http;
    public CookieTimeZoneProvider(IHttpContextAccessor http) => _http = http;

    public ValueTask<int> GetOffsetMinutesAsync()
    {
        var cookie = _http.HttpContext?.Request.Cookies["TimezoneOffset"];
        return new ValueTask<int>(int.TryParse(cookie, out var minutes) ? minutes : 0);
    }
}

// Program.cs
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IClientTimeZoneProvider, CookieTimeZoneProvider>();
```

FlexTable reads the offset once after its first render and applies it to every `DateTime` cell. It is not applied during prerendering.

### ITextMeasurer

```csharp
public interface ITextMeasurer
{
    /// Approximate rendered width of text in CSS pixels.
    float MeasureWidth(string text, string fontFamily, float fontSizePx);
}
```

- **Default (unregistered):** `EstimatingTextMeasurer`, a pure-C# estimator based on per-character widths. Used for the initial column widths when a view is first created.
- Register your own implementation to change how default widths are chosen. Double-click auto-size does not use this interface; it measures the actual cell text in the browser with canvas `measureText`.
```

- [ ] **Step 4: Update `KBlazor/docs/flextable.md`**

Replace the "## Injected Services" section with:

```markdown
## Injected Services

FlexTable injects these from DI:
- `IListViewSettingStore` — view persistence (required)
- `IEntityLookupProvider` — entity resolution (required)
- `IFlexTableSettings` — feature flags (required)
- `IJSRuntime` — browser interop for column resizing, font detection, and auto-size

And resolves these optionally through `IServiceProvider`, with defaults when absent:
- `AuthenticationStateProvider` — role checks for view management; absent on WebAssembly apps without authentication
- `IClientTimeZoneProvider` — `DateTime` display offset (default 0)
- `ITextMeasurer` — default column widths (default: built-in estimator)

Double-clicking a column header auto-sizes it to the widest visible value, measured in the browser with canvas `measureText` using the table's computed font.
```

- [ ] **Step 5: Update `KBlazor/docs/models.md`**

In the ListViewSetting "Key methods" list, replace the two `GetDefaultProperties` bullets with:

```markdown
- `GetDefaultProperties(fontFamily, fontSizePx)` — returns all `[Display]`-decorated properties for the entity type, with widths from the built-in `EstimatingTextMeasurer`.
- `GetDefaultProperties(fontFamily, fontSizePx, fields)` — returns specific fields by their display name (comma-separated).
- `GetDefaultProperties(fontFamily, fontSizePx, measurer)` / `GetDefaultProperties(fontFamily, fontSizePx, fields, measurer)` — same, using a supplied `ITextMeasurer`. Font sizes are CSS pixels.
```

- [ ] **Step 6: Update `KBlazor/CLAUDE.md` and `README.md`**

In `KBlazor/CLAUDE.md`:
- Replace the first paragraph with: `KBlazor is a reusable Razor Class Library providing data-driven UI components for Blazor Server and Blazor WebAssembly applications. It includes a powerful table/list component (FlexTable), auto-generated form editor (BasicEdit), and supporting infrastructure.`
- Replace `**Dependencies:** MudBlazor, Entity Framework Core, Newtonsoft.Json, System.Drawing.Common` with `**Dependencies:** MudBlazor, Microsoft.AspNetCore.Components.Web/Authorization, Entity Framework Core, Newtonsoft.Json`
- After the "Three required service implementations" list add: `**Optional services (built-in defaults):** ` + "`IClientTimeZoneProvider` (DateTime display offset), `ITextMeasurer` (default column widths)."

In `README.md`:
- Replace `AI-ready Blazor components for data-driven applications. Built on [MudBlazor](https://mudblazor.com/) and Entity Framework Core, KBlazor gives you a powerful table, an auto-generated form editor, and the supporting plumbing to wire models straight into UI with attributes alone.` with `AI-ready Blazor components for data-driven applications, for both **Blazor Server** and **Blazor WebAssembly**. Built on [MudBlazor](https://mudblazor.com/) and Entity Framework Core, KBlazor gives you a powerful table, an auto-generated form editor, and the supporting plumbing to wire models straight into UI with attributes alone.`
- `Version="1.0.5"` → `Version="1.1.0"`
- Replace `In `_Host.cshtml` (Server) or `index.html` (WASM):` with `In `_Host.cshtml` / `App.razor` (Server) or `wwwroot/index.html` (WebAssembly):`
- In the Repository Layout block add a line after the tests line: `KBlazor.WasmSample/      Standalone WebAssembly smoke app (not published)`

- [ ] **Step 7: Update the website**

`KBlazor.Showcase/Pages/GettingStarted.razor`:

(a) Hero paragraph: replace `KBlazor is published on <strong>NuGet.org</strong>. Follow the steps below to add it to your Blazor Server project.` with `KBlazor is published on <strong>NuGet.org</strong> and runs in both <strong>Blazor Server</strong> and <strong>Blazor WebAssembly</strong>. Follow the steps below to add it to your project.`

(b) In Step 2 (services), replace the `Program.cs` code block content with:

```html
                <pre class="gs-code lang-cs"><span class="ck">using</span> KBlazor.Services;
<span class="ck">using</span> MudBlazor.Services;

<span class="cx">// MudBlazor (required by KBlazor components)</span>
builder.Services.<span class="cv">AddMudServices</span>();

<span class="cx">// KBlazor — provide your own implementations or use the in-memory stubs</span>
builder.Services.AddScoped&lt;<span class="cv">IFlexTableSettings</span>,    YourFlexTableSettings&gt;();
builder.Services.AddScoped&lt;<span class="cv">IListViewSettingStore</span>, YourListViewSettingStore&gt;();
builder.Services.AddScoped&lt;<span class="cv">IEntityLookupProvider</span>, YourEntityLookupProvider&gt;();

<span class="cx">// Optional — show DateTimes in the browser's local time (Server or WebAssembly)</span>
builder.Services.AddScoped&lt;<span class="cv">IClientTimeZoneProvider</span>, BrowserTimeZoneProvider&gt;();</pre>
```

and change the intro line `<p>KBlazor needs three scoped services and MudBlazor's service collection.</p>` to `<p>KBlazor needs three scoped services and MudBlazor's service collection. Two more (<code>IClientTimeZoneProvider</code>, <code>ITextMeasurer</code>) are optional with built-in defaults.</p>`

(c) Insert a new step between Step 2 (services) and Step 3 (imports) for the script include, and renumber the following steps (imports → 4, model → 5, done stays the ✓ step). The new step's markup:

```html
    <!-- ── Step 3: Include the script ─────────────────────────────────── -->
    <div class="gs-step">
        <div class="gs-step-num">3</div>
        <div class="gs-step-body">
            <h2>Include <code>kblazor.js</code></h2>
            <p>FlexTable uses a small script for column resizing, font detection, and auto-size. Add it to your host page.</p>

            <div class="gs-tab-group">
                <div class="gs-tab-label">Blazor Server — _Host.cshtml / App.razor</div>
                <pre class="gs-code lang-xml"><span class="cx">&lt;script src="<span class="cv">_content/KBlazor/kblazor.js</span>"&gt;&lt;/script&gt;</span></pre>
            </div>

            <div class="gs-tab-group" style="margin-top:12px;">
                <div class="gs-tab-label">Blazor WebAssembly — wwwroot/index.html</div>
                <pre class="gs-code lang-xml"><span class="cx">&lt;script src="_framework/blazor.webassembly.js"&gt;&lt;/script&gt;</span>
<span class="cx">&lt;script src="_content/MudBlazor/MudBlazor.min.js"&gt;&lt;/script&gt;</span>
<span class="cx">&lt;script src="<span class="cv">_content/KBlazor/kblazor.js</span>"&gt;&lt;/script&gt;</span></pre>
            </div>
        </div>
    </div>
```

Update the comment headers and `gs-step-num` values so the sequence reads 1 (install), 2 (services), 3 (script), 4 (imports), 5 (model), ✓ (done).

`KBlazor.Showcase/Pages/Home.razor`: in the `_features` array, change the last feature's two strings from `"3 View Modes", "Table, Chips, and Kanban"` to `"Server & WebAssembly", "One package, both hosting models"`. Keep four features; leave the SVG icon as is.

`KBlazor.Showcase/Docs/DocContent.cs`: in `FlexTableExplained`, append this sentence before the closing `""";`:

```
        The same package runs on Blazor Server and Blazor WebAssembly; a WebAssembly host registers the same three services.
```

- [ ] **Step 8: Build, test, and check the site**

```bash
cd /h/Source/repos/KBlazor
dotnet test KBlazor.sln --nologo -v q 2>&1 | grep -v warning | tail -3
(dotnet run --project KBlazor.Showcase --launch-profile http > /tmp/showcase.log 2>&1 &) ; sleep 25
curl -s http://localhost:5166/getting-started | grep -o "Include <code>kblazor.js</code>\|Blazor WebAssembly — wwwroot/index.html\|IClientTimeZoneProvider" | sort -u
curl -s http://localhost:5166/ | grep -c "Server &amp; WebAssembly"
```
Expected: `Passed! - Failed: 0, Passed: 40`; the three strings printed; `1`. Stop the showcase afterwards.

- [ ] **Step 9: Commit**

```bash
cd /h/Source/repos/KBlazor
git add KBlazor/KBlazor.csproj KBlazor/docs KBlazor/CLAUDE.md README.md KBlazor.Showcase/Pages/GettingStarted.razor KBlazor.Showcase/Pages/Home.razor KBlazor.Showcase/Docs/DocContent.cs
git commit -m "Release 1.1.0: Blazor WebAssembly support, optional timezone/text-measurer services, docs and site updates

Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>"
```

---

### Task 7: Final regression pass

**Files:** none modified unless a check fails.

- [ ] **Step 1: Clean build of everything**

Run: `cd /h/Source/repos/KBlazor && dotnet clean KBlazor.sln --nologo -v q && dotnet build KBlazor.sln --nologo 2>&1 | grep -E "error|Build succeeded|NETSDK1082"`
Expected: `Build succeeded`, no errors, no NETSDK1082.

- [ ] **Step 2: Full test run**

Run: `cd /h/Source/repos/KBlazor && dotnet test KBlazor.sln --nologo -v q 2>&1 | grep -v warning | tail -3`
Expected: `Passed! - Failed: 0, Passed: 40`.

- [ ] **Step 3: Pack the library**

Run: `cd /h/Source/repos/KBlazor && dotnet pack KBlazor/KBlazor.csproj -c Release -o "$TMPDIR/kblazor-pack" --nologo 2>&1 | grep -E "error|Successfully created" `
Expected: `Successfully created package '.../KBlazor.1.1.0.nupkg'`. Confirm it contains no `System.Drawing.Common` dependency: `unzip -p "$TMPDIR/kblazor-pack/KBlazor.1.1.0.nupkg" KBlazor.nuspec | grep -c "System.Drawing"` → `0`, and does contain the Components packages: `unzip -p "$TMPDIR/kblazor-pack/KBlazor.1.1.0.nupkg" KBlazor.nuspec | grep -c "Microsoft.AspNetCore.Components"` → `2`.

- [ ] **Step 4: Browser check of both hosts (if a browser pane is available)**

- Showcase: start `showcase` from `.claude/launch.json`, open `/demo/flextable`, sort a column, open the "Customer (lookup)" filter and search `Acme`, tick it, OK; double-click a header; confirm no console errors. Stop it.
- WASM sample: start `wasm-sample`, open `http://localhost:5290/`, repeat the same interactions plus clicking a row to show BasicEdit; confirm no console errors. Stop it.

Record the results (pass/fail per interaction) in the final report. If any interaction fails, fix in the relevant task's files, re-run Steps 1–2, and commit with a `fix:` message.

- [ ] **Step 5: Report**

Summarize: version 1.1.0 built and packed; test count; both hosts verified; list any deviations from the plan (e.g. whether the ILLink descriptor was needed). Do **not** push — the user decides when to publish (pushing `main` triggers the NuGet workflow).
