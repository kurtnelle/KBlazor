using KBlazor.WasmSample.Domain;

namespace KBlazor.WasmSample.Data;

public static class SeedData
{
    public static DataStore Create()
    {
        var store = new DataStore();

        var acme     = new Customer { Name = "Acme Corp",    Email = "orders@acme.com",     Country = "USA" };
        var globex   = new Customer { Name = "Globex Inc",   Email = "orders@globex.com",   Country = "Canada" };
        var initech  = new Customer { Name = "Initech LLC",  Email = "orders@initech.com",  Country = "USA" };
        var umbrella = new Customer { Name = "Umbrella Co",  Email = "orders@umbrella.com", Country = "UK" };
        var soylent  = new Customer { Name = "Soylent Corp", Email = "orders@soylent.com",  Country = "Australia" };
        store.Customers.AddRange(new[] { acme, globex, initech, umbrella, soylent });

        // Filler customers so the entity filter's name search past the 100-item cap is exercised.
        for (int i = 6; i <= 150; i++)
        {
            store.Customers.Add(new Customer
            {
                Name = $"Test Customer {i:000}",
                Email = $"customer{i:000}@example.com",
                Country = "USA"
            });
        }

        var named = new[] { acme, globex, initech, umbrella, soylent };
        var statuses = new[] { OrderStatus.Delivered, OrderStatus.Pending, OrderStatus.New, OrderStatus.Cancelled, OrderStatus.InProgress };
        var baseDate = new DateTime(2026, 3, 1);
        for (int i = 0; i < 20; i++)
        {
            var customer = named[i % named.Length];
            var status = statuses[i % statuses.Length];
            var order = new PurchaseOrder
            {
                Name = $"ORD-{41 + i:0000}",
                CustomerId = customer.Id,
                Customer = customer,
                Status = status,
                OrderDate = baseDate.AddDays(i),
                DeliveryDate = status == OrderStatus.Delivered ? baseDate.AddDays(i + 5) : null,
                Amount = 500m + i * 375m,
                IsUrgent = i % 3 == 0,
                Notes = i % 4 == 0 ? "Priority account." : string.Empty
            };
            store.Orders.Add(order);
        }
        return store;
    }
}
