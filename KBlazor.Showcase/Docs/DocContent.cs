// KBlazor.Showcase/Docs/DocContent.cs
namespace KBlazor.Showcase.Docs;

public static class DocContent
{
    // ── Shared model ────────────────────────────────────────────────────

    public const string PurchaseOrderModel = """
        using System.ComponentModel.DataAnnotations;
        using KBlazor.Attributes;
        using KBlazor.Models;

        // ── Related entity ──────────────────────────────────────────
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
        }

        // ── Main entity ─────────────────────────────────────────────
        public class PurchaseOrder : IKBusinessEntity
        {
            [Key]
            public Guid Id { get; set; } = Guid.NewGuid();

            [Display(Name = "Order #", Order = 1)]
            [ReadOnlyOnEdit]
            public string Name { get; set; } = string.Empty;

            // Foreign key — [AutoComplete] wires to IEntityLookupProvider
            [AutoComplete]
            public Guid? CustomerId { get; set; }
            public virtual Customer? Customer { get; set; }

            // Computed display property — exposes the customer name
            [Display(Name = "Customer", Order = 2)]
            [SortAndFilterOn(Member = "Customer.Name")]
            public string CustomerName => Customer?.Name ?? string.Empty;

            [Display(Name = "Status", Order = 3)]
            [SortAndFilterOn(Member = "Status")]
            public OrderStatus Status { get; set; }

            [Display(Name = "Order Date", Order = 4)]
            [EnableTime]
            public DateTime OrderDate { get; set; }

            [Display(Name = "Delivery Date", Order = 5)]
            public DateTime? DeliveryDate { get; set; }

            [Display(Name = "Amount", Order = 6)]
            [DisplayNoWrap]
            public decimal Amount { get; set; }

            [Display(Name = "Reference", Order = 7)]
            [LinkOnField]
            public string Reference { get; set; } = string.Empty;

            [Display(Name = "Urgent", Order = 8)]
            [AllowInlineEdit]
            public bool IsUrgent { get; set; }

            [Display(Name = "Notes", Order = 9)]
            [MemoDisplay]
            public string Notes { get; set; } = string.Empty;
        }
        """;

    // ── FlexTable ──────────────────────────────────────────────────────

    public const string FlexTableUsage = """
        <FlexTable TItem="PurchaseOrder"
                   Items="@_orders"
                   Fields="Order #,Customer,Status,Amount,Order Date"
                   ViewName="MyView"
                   PageSize="10"
                   SelectionChanged="OnRowClicked"
                   SortFilter="OnSortFilter"
                   AdditionalCommands="GetCommands"
                   RenderTemplates="@_templates" />
        """;

    public const string FlexTableCodeBehind = """
        @inject DataStore Store

        @code {
            private IQueryable<PurchaseOrder> _orders = default!;

            protected override void OnInitialized()
                => _orders = Store.Orders.AsQueryable();

            // Comma-separated FontAwesome icons. Append "|Tooltip" to any
            // command to show a hover tooltip; the tooltip text is stripped
            // before the click handler runs.
            private string GetCommands(PurchaseOrder order) =>
                "fa-solid fa-eye|View,fa-solid fa-pen-to-square|Edit,fa-solid fa-trash|Delete";

            private void OnRowClicked(PurchaseOrder order, string command)
            {
                // `command` is the icon class only (e.g. "fa-solid fa-eye").
            }

            private void OnSortFilter(ListViewSetting setting)
            {
                // Route through the engine so entity (foreign-key) columns
                // annotated with [SortAndFilterOn(FilterPath=...)] actually
                // filter. Non-entity columns behave exactly as before.
                _orders = Store.Orders.AsQueryable()
                    .ApplyFilter(setting)
                    .ApplySort(setting);
                StateHasChanged();
            }
        }
        """;

    public const string FlexTableExplained = """
        FlexTable renders any <code>IQueryable&lt;T&gt;</code> data source.
        Decorate your model with <code>[Display]</code> attributes &mdash; that's all it needs to build columns.
        The <code>SortFilter</code> callback receives a <code>ListViewSetting</code> so you can
        re-query with the user's active sort and filters applied.
        Use <code>RenderTemplates</code> to customise how specific types (like enums) render in cells.
        Add per-row action icons with <code>AdditionalCommands</code>: return comma-separated
        FontAwesome classes, and append <code>|Tooltip</code> to any icon to show a hover tooltip
        &mdash; try hovering the row icons above. The click handler receives the icon class only.
        Columns typed as a related entity (like <code>Customer (lookup)</code> above) get a
        <strong>name-search filter</strong>: open that column's filter to search entities by name and
        pick from the matches, even beyond the first 100 rows. Enable it with
        <code>[SortAndFilterOn(FilterPath = "CustomerId", SortPath = "Customer.Name")]</code> and route
        <code>SortFilter</code> through <code>ApplyFilter</code>/<code>ApplySort</code> (search is
        case-insensitive on every provider &mdash; <code>EF.Functions.Like</code> on SQL, ordinal in-memory).
        """;

    // ── Kanban ──────────────────────────────────────────────────────────

    public const string KanbanUsage = """
        <FlexTable TItem="PurchaseOrder"
                   Items="@_orders"
                   Fields="Order #,Customer,Amount"
                   DefaultViewMode="FlexTableViewMode.Kanban"
                   KanbanGroupField="Status"
                   KanbanColumns="New,Pending,InProgress,Delivered,Cancelled"
                   KanbanCardDisplayField="Order #"
                   SelectionChanged="OnRowClicked"
                   RenderTemplates="@_templates" />
        """;

    public const string KanbanCodeBehind = """
        @code {
            private Dictionary<Type, RenderFragment<object?>> _templates = new()
            {
                [typeof(OrderStatus)] = val =>
                {
                    var status = (OrderStatus?)val;
                    var label = status switch
                    {
                        OrderStatus.New        => "New",
                        OrderStatus.Pending    => "Pending",
                        OrderStatus.InProgress => "In Progress",
                        OrderStatus.Delivered  => "Delivered",
                        OrderStatus.Cancelled  => "Cancelled",
                        _                      => val?.ToString() ?? ""
                    };
                    return @<span class="status-badge">@label</span>;
                }
            };
        }
        """;

    public const string KanbanExplained = """
        Switch FlexTable to Kanban mode with <code>DefaultViewMode</code>.
        <code>KanbanGroupField</code> names the property that determines which column a card appears in.
        <code>KanbanColumns</code> controls column order and labels.
        <code>RenderTemplates</code> lets you customise how specific types render in cells &mdash; great for colored status badges.
        """;

    // ── BasicEdit ───────────────────────────────────────────────────────

    public const string BasicEditUsage = """
        <BasicEdit TItem="PurchaseOrder"
                   Item="@_editing"
                   Fields="Order #,Customer,Status,Order Date,Amount,,Urgent,Delivery Date,Reference,Notes"
                   Save="SaveOrder"
                   Close="CloseEditor"
                   Columns="2" />
        """;

    public const string BasicEditCodeBehind = """
        @code {
            private PurchaseOrder _editing = new();

            private void SaveOrder()
            {
                // persist _editing to your data store
            }

            private void CloseEditor()
            {
                // navigate away or hide the editor
            }
        }
        """;

    public const string BasicEditExplained = """
        BasicEdit auto-generates a form from your model's <code>[Display]</code> attributes.
        Use <code>Columns="2"</code> (or 3) to lay fields out in a grid.
        The <code>Fields</code> parameter controls which fields appear and their order &mdash;
        an empty entry (<code>,,</code>) inserts a blank cell, letting you push the next field to a specific column position.
        <code>[MemoDisplay]</code> renders a textarea. <code>[EnableTime]</code> adds a time picker.
        <code>[AutoComplete]</code> wires to <code>IEntityLookupProvider</code>.
        <code>[ReadOnlyOnEdit]</code> locks a field on edit.
        """;

    // ── CycleStateButton ────────────────────────────────────────────────

    public const string CycleStateUsage = """
        <CycleStateButton State="@_state"
                          OnStateChanged="@(v => { _state = v % 5; })"
                          Label="@_labels[_state]"
                          Tooltip="Click to advance status" />
        """;

    public const string CycleStateCodeBehind = """
        @code {
            private int _state = 0;

            private string[] _labels = new[]
            {
                "New", "Pending", "In Progress", "Delivered", "Cancelled"
            };
        }
        """;

    public const string CycleStateExplained = """
        CycleStateButton increments an integer <code>State</code> on each click and fires <code>OnStateChanged</code>.
        Use the modulo operator in the callback to wrap back to zero.
        <code>Label</code> shows the current state's display text; <code>Tooltip</code> appears on hover.
        """;

    // ── RelativeDatePicker ──────────────────────────────────────────────

    public const string DatePickerUsage = """
        <RelativeDatePicker @bind-Value="selectedDate"
                            @bind-StartDate="startDate"
                            @bind-EndDate="endDate" />
        """;

    public const string DatePickerCodeBehind = """
        @code {
            private string selectedDate = string.Empty;
            private DateTime? startDate;
            private DateTime? endDate;

            // startDate and endDate are automatically resolved
            // from the relative expression (e.g. "Last Week")
            // via RelativeDateCalc.GetLowerDate / GetUpperDate.
            //
            // Use them directly in queries:
            // query.Where(o => o.OrderDate >= startDate
            //               && o.OrderDate <= endDate);
        }
        """;

    public const string DatePickerExplained = """
        RelativeDatePicker supports both absolute date selection and relative expressions:
        <code>Today</code>, <code>This Week</code>, <code>Last Month</code>, <code>Next Year</code>, and more.
        Bind <code>StartDate</code> and <code>EndDate</code> to get the resolved <code>DateTime</code> range
        automatically &mdash; no need to call <code>RelativeDateCalc</code> yourself.
        FlexTable uses it internally for DateTime column filters when <code>SortFilter</code> is wired up.
        """;

    // ── Chips View ─────────────────────────────────────────────────────

    public const string ChipsUsage = """
        <FlexTable TItem="PurchaseOrder"
                   Items="@_orders"
                   Fields="Order #,Customer,Status,Amount"
                   DefaultViewMode="FlexTableViewMode.Chips"
                   ChipDisplayField="Name"
                   ChipColor="@GetChipColor"
                   SelectionChanged="OnChipClicked" />
        """;

    public const string ChipsCodeBehind = """
        @inject DataStore Store

        @code {
            private IQueryable<PurchaseOrder> _orders = default!;
            private PurchaseOrder? _selected;

            protected override void OnInitialized()
                => _orders = Store.Orders.AsQueryable();

            private MudBlazor.Color GetChipColor(PurchaseOrder order)
                => order.Status switch
                {
                    OrderStatus.New        => MudBlazor.Color.Default,
                    OrderStatus.Pending    => MudBlazor.Color.Warning,
                    OrderStatus.InProgress => MudBlazor.Color.Info,
                    OrderStatus.Delivered  => MudBlazor.Color.Success,
                    OrderStatus.Cancelled  => MudBlazor.Color.Error,
                    _                      => MudBlazor.Color.Default,
                };

            private void OnChipClicked(PurchaseOrder order, string command)
            {
                _selected = order;
            }
        }
        """;

    public const string ChipsExplained = """
        Set <code>DefaultViewMode="FlexTableViewMode.Chips"</code> to render items as clickable chips instead of table rows.
        <code>ChipDisplayField</code> names the property whose value appears as the chip label.
        <code>ChipColor</code> accepts a <code>Func&lt;TItem, Color&gt;</code> to colour-code chips by any logic &mdash; great for status indicators.
        For full control, use <code>ChipTemplate</code> to supply a custom <code>RenderFragment&lt;TItem&gt;</code> for each chip.
        """;
}
