// KBlazor.Showcase/Docs/DocContent.cs
namespace KBlazor.Showcase.Docs;

public static class DocContent
{
    public const string FlexTableUsage = """
        <FlexTable TItem="PurchaseOrder"
                   Items="@orders"
                   Fields="Order #,Customer,Status,Amount,Order Date"
                   SelectionChanged="OnRowClicked"
                   SortFilter="OnSortFilter" />
        """;

    public const string FlexTableExplained = """
        FlexTable renders any IQueryable&lt;T&gt; data source. Decorate your model with
        [Display] attributes — that's all it needs to build columns. The SortFilter callback
        receives a ListViewSetting so you can re-query with the user's active sort and filters applied.
        """;

    public const string KanbanUsage = """
        <FlexTable TItem="PurchaseOrder"
                   Items="@orders"
                   DefaultViewMode="FlexTableViewMode.Kanban"
                   KanbanGroupField="Status"
                   KanbanColumns="New,Pending,InProgress,Delivered,Cancelled"
                   KanbanCardDisplayField="Order #"
                   RenderTemplates="@_templates" />
        """;

    public const string KanbanExplained = """
        Switch FlexTable to Kanban mode with DefaultViewMode. KanbanGroupField names the property
        that determines which column a card appears in. KanbanColumns controls column order and labels.
        RenderTemplates lets you replace column headers with custom markup — great for colored status badges.
        """;

    public const string BasicEditUsage = """
        <BasicEdit TItem="PurchaseOrder"
                   Item="@selectedOrder"
                   Save="SaveOrder"
                   Close="CloseEditor"
                   Columns="2" />
        """;

    public const string BasicEditExplained = """
        BasicEdit auto-generates a form from your model's [Display] attributes.
        [MemoDisplay] renders a textarea. [EnableTime] adds a time picker to DateTime fields.
        [AutoComplete] wires a field to IEntityLookupProvider for live search.
        [ReadOnlyOnEdit] locks a field when editing existing records.
        """;

    public const string CycleStateUsage = """
        <CycleStateButton @bind-Value="order.Status"
                          States="@_states"
                          Labels="@_labels" />
        """;

    public const string CycleStateExplained = """
        CycleStateButton cycles through a list of integer states on each click.
        Bind it to any int or enum property. Supply parallel States and Labels arrays
        to control the cycle order and display text.
        """;

    public const string DatePickerUsage = """
        <RelativeDatePicker @bind-Value="selectedDate"
                            Label="Filter by date" />
        """;

    public const string DatePickerExplained = """
        RelativeDatePicker supports both absolute date selection and relative expressions:
        Today, This Week, Last Month, Next Year, and more. FlexTable uses it automatically
        for DateTime column filters when SortFilter is wired up.
        """;
}
