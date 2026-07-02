// KBlazor.Showcase/Domain/PurchaseOrder.cs
using System.ComponentModel.DataAnnotations;
using KBlazor.Attributes;
using KBlazor.Models;
using Newtonsoft.Json;

namespace KBlazor.Showcase.Domain;

public class PurchaseOrder : IKBusinessEntity
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Display(Name = "Order #", Order = 1)]
    [ReadOnlyOnEdit]
    public string Name { get; set; } = string.Empty;

    [AutoComplete]
    public Guid? CustomerId { get; set; }

    [Display(Name = "Customer (lookup)", Order = 2)]
    [SortAndFilterOn(FilterPath = "CustomerId", SortPath = "Customer.Name")]
    public virtual Customer? Customer { get; set; }

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

    public bool Equals(IKBusinessEntity? other) => Id == other?.Id;
    public override string ToString() => Name;
    public string ToJson() => JsonConvert.SerializeObject(this);
}
