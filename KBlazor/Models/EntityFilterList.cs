using System;
using System.Collections.Generic;
using System.Linq;

namespace KBlazor.Models
{
    /// <summary>
    /// Pure computation for the entity checkbox filter: given the candidate
    /// entities, the currently-selected ids, and a name search string, produce
    /// the pinned (always-visible selected), matched (name search, capped), and
    /// combined visible sets. Kept free of UI so it can be unit tested.
    /// </summary>
    public static class EntityFilterList
    {
        public static EntityFilterResult Build(
            IQueryable<IKBusinessEntity> list,
            Guid[] selectedIds,
            string? search,
            int cap = 100)
        {
            var selected = selectedIds ?? Array.Empty<Guid>();

            var pinned = list
                .Where(w => selected.Contains(w.Id))
                .ToList()
                .OrderBy(p => p.ToString(), StringComparer.Ordinal)
                .ToList();

            IQueryable<IKBusinessEntity> matchQuery = string.IsNullOrWhiteSpace(search)
                ? list
                : list.Where(w => w.Name.Contains(search));

            var matches = matchQuery
                .Where(m => !selected.Contains(m.Id))
                .OrderBy(m => m.Name)
                .Take(cap)
                .ToList()
                .OrderBy(m => m.ToString(), StringComparer.Ordinal)
                .ToList();

            var visible = pinned.Concat(matches).ToList();

            return new EntityFilterResult(pinned, matches, visible);
        }
    }

    public sealed record EntityFilterResult(
        IReadOnlyList<IKBusinessEntity> Pinned,
        IReadOnlyList<IKBusinessEntity> Matches,
        IReadOnlyList<IKBusinessEntity> Visible);
}
