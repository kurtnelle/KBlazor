using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

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

            // Case-insensitive name search, provider-aware so it is both correct and
            // performant everywhere:
            //  - Real EF query  -> EF.Functions.Like, which stays sargable (index-usable)
            //    and is case-insensitive by the column's collation. LOWER()-based matching
            //    would force a scan; the StringComparison overload doesn't translate at all.
            //  - In-memory (EnumerableQuery) -> EF.Functions.Like would throw at
            //    enumeration, so use an OrdinalIgnoreCase Contains.
            IQueryable<IKBusinessEntity> matchQuery;
            if (string.IsNullOrWhiteSpace(search))
            {
                matchQuery = list;
            }
            else if (list.Provider is EnumerableQuery)
            {
                matchQuery = list.Where(w => w.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                var pattern = "%" + search + "%";
                matchQuery = list.Where(w => EF.Functions.Like(w.Name, pattern));
            }

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
