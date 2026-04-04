# KBlazor Showcase Site — Design Spec

**Date:** 2026-04-04
**Status:** Approved

---

## Overview

A public-facing Blazor Server showcase site hosted at `kblazor.com`. It demonstrates all KBlazor components in a realistic, working demo domain (Purchase Orders) with seed data, while surfacing inline documentation drawn from the existing markdown files. The site is designed for developers evaluating the library.

---

## Goals

- Show every KBlazor component working with real interactions (sort, filter, form save, view switching)
- Surface the "how to use" documentation inline — code snippets and attribute explanations alongside live demos
- Distinctive dark UI (not generic) with purple accent (`#5749F4`), navy backgrounds
- Deployable to Cloudflare Pages or a cheap Blazor Server host (Railway / Render / Fly.io) with Cloudflare DNS in front

---

## Technical Decisions

| Decision | Choice | Reason |
|----------|--------|--------|
| Blazor mode | **Server** | KBlazor targets Blazor Server; avoids modifying the library |
| Data | **In-memory seed data** | Showcase only — no real DB needed |
| Hosting | **Railway or Render** (~$5–7/mo) | Supports Blazor Server (.NET), always-on, zero server management. Cloudflare Pages is excluded — it does not support persistent WebSocket connections required by Blazor Server. |
| DNS/SSL | **Cloudflare** (free) | DNS proxy + SSL termination in front of the Railway/Render host |
| Persistence (views) | **In-memory IListViewSettingStore** | Users can save views per session; resets on reload |

---

## Seed Domain: Purchase Orders

All KBlazor features are demonstrated through a `PurchaseOrder` entity. Supporting entities: `Customer`, `OrderLine`.

### PurchaseOrder model properties

| Property | Type | Attributes | Demo Feature |
|----------|------|------------|--------------|
| `Name` | `string` | `[Display(Order=1)]` | Table column |
| `Customer` | `Customer` (FK) | `[Display]`, `[AutoComplete]` | BasicEdit dropdown |
| `Status` | `OrderStatus` (enum) | `[Display]`, `[SortAndFilterOn]` | Kanban group, RenderTemplate badge |
| `OrderDate` | `DateTime` | `[Display]`, `[EnableTime]` | Table column, RelativeDatePicker filter |
| `DeliveryDate` | `DateTime?` | `[Display]` | Table column |
| `Notes` | `string` | `[Display]`, `[MemoDisplay]` | BasicEdit textarea |
| `Amount` | `decimal` | `[Display]`, `[DisplayNoWrap]` | Table column |
| `Reference` | `string` | `[Display]`, `[LinkOnField]` | Table link |
| `IsUrgent` | `bool` | `[Display]` | Table column, CycleStateButton |

### OrderStatus enum values
`New`, `Pending`, `InProgress`, `Delivered`, `Cancelled`

### Seed data
20 purchase orders across all statuses, spread across 5 customers (Acme Corp, Globex Inc, Initech LLC, Umbrella Co, Soylent Corp).

---

## Site Structure

### Page 1 — Landing / Hero (`/`)

**Sections:**
1. **Top Nav** — KBlazor logo, Components / Docs / GitHub links, "Get Started" button
2. **Hero** — Headline, subline, two CTAs ("See Live Demo" → `/demo`, "Read Docs" → `/docs`), live orders table preview on right
3. **Demo Section** — Sidebar (component list), tab switcher (Table / Chip / Kanban), split panel: left = code snippet + doc text from `flextable.md`, right = live mini-table
4. **Attribute Strip** — 4 key attributes (`[Display]`, `[AllowInlineEdit]`, `[SortAndFilterOn]`, `[MemoDisplay]`)

### Page 2 — FlexTable Demo (`/demo/flextable`)

Full-width FlexTable showing all 20 orders. Features active on this page:
- Column sort and multi-column sort
- Per-column text/date/enum filter (with RelativeDatePicker for date columns)
- Pagination (10 per page)
- View mode switcher (Table → Chip → Kanban)
- `RenderTemplates` for `OrderStatus` (colored badges)
- `AdditionalCommands` (edit icon → opens BasicEdit, delete icon)
- `[AllowInlineEdit]` on `IsUrgent`
- Saved views (in-memory `IListViewSettingStore`)
- Inline doc panel (collapsible) sourced from `flextable.md`

### Page 3 — Kanban View (`/demo/kanban`)

Same FlexTable component, `DefaultViewMode="FlexTableViewMode.Kanban"`, `KanbanGroupField="Status"`, columns = `New,Pending,InProgress,Delivered,Cancelled`. Kanban column headers use `RenderTemplates` for status badges.

### Page 4 — BasicEdit Demo (`/demo/basicedit`)

Click any order row → slide-in panel with `<BasicEdit TItem="PurchaseOrder">`. Demonstrates:
- Auto-generated form from `[Display]` attributes
- `[MemoDisplay]` → textarea for Notes
- `[EnableTime]` → datetime picker for OrderDate
- `[AutoComplete]` → Customer lookup via `IEntityLookupProvider`
- `[ReadOnlyOnEdit]` on Name (set on creation only)
- Multi-column layout (`Columns="2"`)
- Inline doc from `basicedit.md`

### Page 5 — CycleStateButton (`/demo/cyclestate`)

Standalone demo of `<CycleStateButton>` cycling through `OrderStatus` values. Shows usage code from `attributes.md`.

### Page 6 — RelativeDatePicker (`/demo/datepicker`)

Standalone demo of `<RelativeDatePicker>` with supported relative date strings. Shows usage and output.

### Page 7 — Attributes Reference (`/demo/attributes`)

Card grid — one card per KBlazor attribute. Each card shows:
- Attribute name + usage code snippet
- Effect description (from `attributes.md`)
- Live example on a sample PurchaseOrder field

---

## Service Implementations (In-Memory)

### `InMemoryFlexTableSettings`
```csharp
public class InMemoryFlexTableSettings : IFlexTableSettings
{
    public bool EnablePersonalViews => true;
    public string[] AdminRoles => new[] { "admin" };
}
```

### `InMemoryListViewSettingStore`
Uses a `List<ListViewSetting>` stored in a scoped service. Views survive a user session but reset on server restart.

### `InMemoryEntityLookupProvider`
Returns hardcoded `Customer` and `OrderStatus` collections. `IsKnownEntityType` returns true for `Customer` and `PurchaseOrder`.

---

## Layout & Visual Design

- **Palette:** Background `#0F0E1A`, card `#1A182E`, sidebar `#13112A`, accent `#5749F4`, text primary `#FFFFFF`, text muted `#888799`
- **Typography:** Inter, weights 400/600/800
- **Component demo pages:** Two-zone layout — left sidebar (component nav) + right content area
- **Inline docs:** Collapsible panel below each live demo. Content is hardcoded as C# string constants in a `DocContent.cs` static class, copied verbatim from the relevant sections of the `.md` files. No runtime file parsing.
- **Status badges:** Color-coded pill badges via `RenderTemplates`

---

## Not In Scope

- User authentication / login
- Real database persistence
- Multi-tenant views
- Mobile-responsive layout (desktop-first for a developer tool showcase)
- Dark/light mode toggle
