namespace KBlazor.Models;

public class SortChangeEvent
{
    public string? SortId { get; set; }

    public SortDirection Direction { get; set; } = SortDirection.None;
}

public enum SortDirection
{
    None = 0,
    Asc = 1,
    Desc = 2
}
