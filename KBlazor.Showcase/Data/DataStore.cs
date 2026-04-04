// KBlazor.Showcase/Data/DataStore.cs
using KBlazor.Showcase.Domain;

namespace KBlazor.Showcase.Data;

public class DataStore
{
    public List<Customer> Customers { get; } = new();
    public List<PurchaseOrder> Orders { get; } = new();
}
