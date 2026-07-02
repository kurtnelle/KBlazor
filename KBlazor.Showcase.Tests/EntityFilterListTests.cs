using KBlazor.Models;
using KBlazor.Showcase.Domain;
using Xunit;

namespace KBlazor.Showcase.Tests;

public class EntityFilterListTests
{
    private static IQueryable<IKBusinessEntity> MakeCustomers(int count) =>
        Enumerable.Range(1, count)
            .Select(i => (IKBusinessEntity)new Customer { Id = Guid.NewGuid(), Name = $"Cust {i:000}" })
            .AsQueryable();

    [Fact]
    public void EmptySearch_ReturnsFirstCapAsMatches()
    {
        var list = MakeCustomers(150);
        var result = EntityFilterList.Build(list, Array.Empty<Guid>(), search: null, cap: 100);
        Assert.Empty(result.Pinned);
        Assert.Equal(100, result.Matches.Count);
        Assert.Equal(100, result.Visible.Count);
    }

    [Fact]
    public void Search_FiltersByNameContains_BeyondCap()
    {
        var list = MakeCustomers(150).ToList();
        var target = list[141]; // "Cust 142", index 141 (beyond first 100)
        var result = EntityFilterList.Build(list.AsQueryable(), Array.Empty<Guid>(), search: "142", cap: 100);
        Assert.Contains(result.Matches, m => m.Id == target.Id);
    }

    [Fact]
    public void SelectedItems_ArePinned_RegardlessOfSearch()
    {
        var list = MakeCustomers(150).ToList();
        var selected = list[141]; // "Cust 142"
        var result = EntityFilterList.Build(
            list.AsQueryable(), new[] { selected.Id }, search: "999-no-match", cap: 100);
        Assert.Contains(result.Pinned, p => p.Id == selected.Id);
        Assert.Contains(result.Visible, v => v.Id == selected.Id);
    }

    [Fact]
    public void PinnedItems_AreNotDuplicatedInMatches()
    {
        var list = MakeCustomers(10).ToList();
        var selected = list[0]; // "Cust 001" — also matches empty search
        var result = EntityFilterList.Build(
            list.AsQueryable(), new[] { selected.Id }, search: null, cap: 100);
        Assert.Single(result.Pinned);
        Assert.DoesNotContain(result.Matches, m => m.Id == selected.Id);
        Assert.Equal(10, result.Visible.Count); // 1 pinned + 9 matches, no dupes
    }
}
