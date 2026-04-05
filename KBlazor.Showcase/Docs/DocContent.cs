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
                   RenderTemplates="@_templates" />
        """;

    public const string FlexTableCodeBehind = """
        @inject DataStore Store

        @code {
            private IQueryable<PurchaseOrder> _orders = default!;

            protected override void OnInitialized()
                => _orders = Store.Orders.AsQueryable();

            private void OnRowClicked(PurchaseOrder order, string command)
            {
                // handle row click / command
            }

            private void OnSortFilter(ListViewSetting setting)
            {
                var query = Store.Orders.AsQueryable();

                foreach (var prop in setting.DisplaySettings
                    .Where(w => !string.IsNullOrEmpty(w.Filter)))
                    query = prop.GenerateWhere(query);

                var sorted = setting.DisplaySettings
                    .Where(w => w.SortState != SortState.None)
                    .OrderBy(o => o.SortPriority);

                foreach (var prop in sorted)
                    query = prop.GenerateOrderBy(query);

                _orders = query;
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
        <code>RenderTemplates</code> lets you replace column headers with custom markup &mdash; great for colored status badges.
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
            private int _columns = 2;

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
                          OnStateChanged="@(v => _state = v % 5)"
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
        <RelativeDatePicker @bind-Value="selectedDate" />
        """;

    public const string DatePickerCodeBehind = """
        @code {
            private string selectedDate = string.Empty;
        }
        """;

    public const string DatePickerExplained = """
        RelativeDatePicker supports both absolute date selection and relative expressions:
        <code>Today</code>, <code>This Week</code>, <code>Last Month</code>, <code>Next Year</code>, and more.
        FlexTable uses it automatically for DateTime column filters when <code>SortFilter</code> is wired up.
        """;
}
