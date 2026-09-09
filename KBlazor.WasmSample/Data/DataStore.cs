using KBlazor.WasmSample.Domain;

namespace KBlazor.WasmSample.Data;

public class DataStore
{
    public List<Customer> Customers { get; } = new();
    public List<PurchaseOrder> Orders { get; } = new();
}
