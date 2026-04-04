// KBlazor.Showcase/Data/SeedData.cs
using KBlazor.Showcase.Domain;

namespace KBlazor.Showcase.Data;

public static class SeedData
{
    public static DataStore Create()
    {
        var store = new DataStore();

        var acme    = new Customer { Id = Guid.NewGuid(), Name = "Acme Corp",    Email = "orders@acme.com",    Country = "USA" };
        var globex  = new Customer { Id = Guid.NewGuid(), Name = "Globex Inc",   Email = "orders@globex.com",  Country = "Canada" };
        var initech = new Customer { Id = Guid.NewGuid(), Name = "Initech LLC",  Email = "orders@initech.com", Country = "USA" };
        var umbrella= new Customer { Id = Guid.NewGuid(), Name = "Umbrella Co",  Email = "orders@umbrella.com",Country = "UK" };
        var soylent = new Customer { Id = Guid.NewGuid(), Name = "Soylent Corp", Email = "orders@soylent.com", Country = "Australia" };

        store.Customers.AddRange(new[] { acme, globex, initech, umbrella, soylent });

        var baseDate = new DateTime(2026, 3, 1);
        var orders = new[]
        {
            MakeOrder("ORD-0041", acme,     OrderStatus.Delivered, baseDate.AddDays(0),  baseDate.AddDays(5),  4200.00m,  false, "Delivered on time."),
            MakeOrder("ORD-0042", globex,   OrderStatus.Pending,   baseDate.AddDays(1),  null,                 1750.00m,  false, "Awaiting supplier confirmation."),
            MakeOrder("ORD-0043", initech,  OrderStatus.New,       baseDate.AddDays(2),  null,                 8900.00m,  true,  "Rush order."),
            MakeOrder("ORD-0044", umbrella, OrderStatus.Cancelled, baseDate.AddDays(3),  null,                  620.00m,  false, "Customer cancelled."),
            MakeOrder("ORD-0045", soylent,  OrderStatus.Delivered, baseDate.AddDays(4),  baseDate.AddDays(9),  3310.00m,  false, ""),
            MakeOrder("ORD-0046", acme,     OrderStatus.InProgress,baseDate.AddDays(5),  null,                 7100.00m,  true,  "Expedited shipping."),
            MakeOrder("ORD-0047", globex,   OrderStatus.New,       baseDate.AddDays(6),  null,                  980.00m,  false, ""),
            MakeOrder("ORD-0048", initech,  OrderStatus.Pending,   baseDate.AddDays(7),  null,                 2250.00m,  false, "Partial stock available."),
            MakeOrder("ORD-0049", umbrella, OrderStatus.Delivered, baseDate.AddDays(8),  baseDate.AddDays(13), 5670.00m,  false, ""),
            MakeOrder("ORD-0050", soylent,  OrderStatus.InProgress,baseDate.AddDays(9),  null,                 3340.00m,  true,  "Priority account."),
            MakeOrder("ORD-0051", acme,     OrderStatus.New,       baseDate.AddDays(10), null,                 1120.00m,  false, ""),
            MakeOrder("ORD-0052", globex,   OrderStatus.Cancelled, baseDate.AddDays(11), null,                  430.00m,  false, "Out of stock."),
            MakeOrder("ORD-0053", initech,  OrderStatus.Delivered, baseDate.AddDays(12), baseDate.AddDays(17), 9400.00m,  false, ""),
            MakeOrder("ORD-0054", umbrella, OrderStatus.Pending,   baseDate.AddDays(13), null,                 2800.00m,  true,  "Urgent restock."),
            MakeOrder("ORD-0055", soylent,  OrderStatus.New,       baseDate.AddDays(14), null,                  760.00m,  false, ""),
            MakeOrder("ORD-0056", acme,     OrderStatus.InProgress,baseDate.AddDays(15), null,                 6200.00m,  false, ""),
            MakeOrder("ORD-0057", globex,   OrderStatus.Delivered, baseDate.AddDays(16), baseDate.AddDays(21), 4850.00m,  false, ""),
            MakeOrder("ORD-0058", initech,  OrderStatus.Pending,   baseDate.AddDays(17), null,                 1390.00m,  false, ""),
            MakeOrder("ORD-0059", umbrella, OrderStatus.New,       baseDate.AddDays(18), null,                 3050.00m,  true,  "New product launch."),
            MakeOrder("ORD-0060", soylent,  OrderStatus.Cancelled, baseDate.AddDays(19), null,                  510.00m,  false, "Duplicate order."),
        };

        // Wire up navigation properties
        foreach (var o in orders)
            o.Customer = store.Customers.First(c => c.Id == o.CustomerId);

        store.Orders.AddRange(orders);
        return store;
    }

    private static PurchaseOrder MakeOrder(
        string name, Customer customer, OrderStatus status,
        DateTime orderDate, DateTime? deliveryDate,
        decimal amount, bool isUrgent, string notes) => new()
    {
        Id          = Guid.NewGuid(),
        Name        = name,
        CustomerId  = customer.Id,
        Status      = status,
        OrderDate   = orderDate,
        DeliveryDate= deliveryDate,
        Amount      = amount,
        IsUrgent    = isUrgent,
        Notes       = notes,
        Reference   = $"https://example.com/ref/{name.ToLower()}"
    };
}
