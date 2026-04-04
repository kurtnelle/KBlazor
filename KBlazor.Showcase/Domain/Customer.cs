// KBlazor.Showcase/Domain/Customer.cs
using System.ComponentModel.DataAnnotations;
using KBlazor.Models;
using Newtonsoft.Json;

namespace KBlazor.Showcase.Domain;

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
