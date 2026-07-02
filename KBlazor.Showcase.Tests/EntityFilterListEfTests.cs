using KBlazor.Models;
using KBlazor.Showcase.Domain;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace KBlazor.Showcase.Tests;

// Verifies the EF (relational) branch of EntityFilterList.Build: over a real EF
// query the search must use EF.Functions.Like, translate to SQL over the
// IKBusinessEntity interface, and be case-insensitive — without throwing.
public class EntityFilterListEfTests
{
    private sealed class TestContext : DbContext
    {
        public TestContext(DbContextOptions<TestContext> options) : base(options) { }
        public DbSet<Customer> Customers => Set<Customer>();
    }

    private static TestContext NewContext(SqliteConnection conn)
    {
        var options = new DbContextOptionsBuilder<TestContext>().UseSqlite(conn).Options;
        var ctx = new TestContext(options);
        ctx.Database.EnsureCreated();
        return ctx;
    }

    [Fact]
    public void Build_OverRealEfQuery_UsesLike_CaseInsensitive_AndDoesNotThrow()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var ctx = NewContext(conn);
        ctx.Customers.AddRange(
            new Customer { Id = Guid.NewGuid(), Name = "Soylent Corp" },
            new Customer { Id = Guid.NewGuid(), Name = "Acme Corp" });
        ctx.SaveChanges();

        // Real EF query typed as IQueryable<IKBusinessEntity>: Provider is NOT
        // EnumerableQuery, so Build takes the EF.Functions.Like path. This must
        // translate to SQL (not throw) and match case-insensitively.
        IQueryable<IKBusinessEntity> list = ctx.Customers.Cast<IKBusinessEntity>();

        var result = EntityFilterList.Build(list, Array.Empty<Guid>(), search: "soy", cap: 100);

        Assert.Contains(result.Matches, m => m.Name == "Soylent Corp");
        Assert.DoesNotContain(result.Matches, m => m.Name == "Acme Corp");
    }
}
