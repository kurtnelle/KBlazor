using KBlazor.Attributes;
using System.Linq.Expressions;
using System.Reflection;

namespace KBlazor.Models
{
    public static class SortAndFilterEngine
    {
        public static IQueryable<T> ApplySort<T>(this IQueryable<T> query, ListViewSetting viewSetting)
        {
            if (viewSetting == null) return query;

            foreach (var prop in viewSetting.DisplaySettings)
            {
                if (prop.SortState == SortState.None) continue;
                bool descending = prop.SortState == SortState.Down;

                var attr = prop.PropertyInfo?.GetCustomAttribute<SortAndFilterOnAttribute>();
                var sortPath = attr?.SortPath;

                if (!string.IsNullOrEmpty(sortPath))
                {
                    query = ApplyOrderByPath(query, sortPath, descending);
                }
                else
                {
                    query = prop.GenerateOrderBy(query);
                }
            }

            return query;
        }

        public static IQueryable<T> ApplyFilter<T>(this IQueryable<T> query, ListViewSetting viewSetting)
        {
            if (viewSetting == null) return query;

            foreach (var prop in viewSetting.DisplaySettings)
            {
                if (string.IsNullOrEmpty(prop.Filter)) continue;

                var attr = prop.PropertyInfo?.GetCustomAttribute<SortAndFilterOnAttribute>();
                var filterPath = attr?.FilterPath;

                if (!string.IsNullOrEmpty(filterPath))
                {
                    query = ApplyFilterByPath(query, filterPath, prop);
                }
                else
                {
                    query = prop.GenerateWhere(query);
                }
            }

            return query;
        }

        private static IQueryable<T> ApplyOrderByPath<T>(IQueryable<T> query, string path, bool descending)
        {
            var param = Expression.Parameter(typeof(T), "o");
            var body = BuildPropertyAccess(param, path);
            var lambda = Expression.Lambda(body, param);

            string methodName = descending ? "OrderByDescending" : "OrderBy";
            var method = typeof(Queryable).GetMethods()
                .Where(m => m.Name == methodName && m.GetParameters().Length == 2)
                .Single()
                .MakeGenericMethod(typeof(T), body.Type);

            return (IQueryable<T>)method.Invoke(null, new object[] { query, lambda });
        }

        private static IQueryable<T> ApplyFilterByPath<T>(IQueryable<T> query, string path, PropertySetting prop)
        {
            var param = Expression.Parameter(typeof(T), "o");
            var body = BuildPropertyAccess(param, path);

            if (body.Type == typeof(Guid) || body.Type == typeof(Guid?))
            {
                // FK lookup: selectedIds.Contains(value). For a nullable FK (Guid?),
                // guard on HasValue and compare the underlying Guid, so the expression
                // is valid and rows with a null FK are simply excluded.
                var filterEntries = prop.GetFilterEntries();
                var constant = Expression.Constant(filterEntries);
                var containsMethod = typeof(Enumerable).GetMethods()
                    .Where(m => m.Name == "Contains" && m.GetParameters().Length == 2)
                    .Single()
                    .MakeGenericMethod(typeof(Guid));

                Expression predicate;
                if (body.Type == typeof(Guid?))
                {
                    var hasValue = Expression.Property(body, "HasValue");
                    var value = Expression.Property(body, "Value");
                    var contains = Expression.Call(containsMethod, constant, value);
                    predicate = Expression.AndAlso(hasValue, contains);
                }
                else
                {
                    predicate = Expression.Call(containsMethod, constant, body);
                }

                var lambda = Expression.Lambda<Func<T, bool>>(predicate, param);
                return query.Where(lambda);
            }
            else if (body.Type == typeof(string))
            {
                // String contains (case-insensitive via ToLower)
                var toLower = Expression.Call(body, typeof(string).GetMethod("ToLower", Type.EmptyTypes)!);
                var filterValue = Expression.Constant(prop.Filter.ToLower());
                var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) })!;
                var containsCall = Expression.Call(toLower, containsMethod, filterValue);
                var lambda = Expression.Lambda<Func<T, bool>>(containsCall, param);
                return query.Where(lambda);
            }
            else if (body.Type == typeof(bool))
            {
                // Bool equality
                var filterValue = Expression.Constant(prop.Filter.ToLower() == "true");
                var equals = Expression.Equal(body, filterValue);
                var lambda = Expression.Lambda<Func<T, bool>>(equals, param);
                return query.Where(lambda);
            }
            else
            {
                // Fall back to GenerateWhere for other types
                return prop.GenerateWhere(query);
            }
        }

        private static Expression BuildPropertyAccess(Expression root, string path)
        {
            Expression current = root;
            foreach (var segment in path.Split('.'))
            {
                current = Expression.Property(current, segment);
            }
            return current;
        }
    }
}
