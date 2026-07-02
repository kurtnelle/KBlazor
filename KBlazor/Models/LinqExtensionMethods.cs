using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace KBlazor.Models
{
    public static class LinqExtensionMethods
    {
        const string CASE_INSENSITIVE_COLLATION = "Latin1_General_CI_AI";
        public static IQueryable<T> OrderBy<T>(this IQueryable<T> source, string orderByProperty, bool desc) =>
           source.OrderBy(typeof(T).GetProperty(orderByProperty), desc);

        public static IQueryable<T> OrderBy<T>(this IQueryable<T> source, PropertyInfo property, bool desc)
        {
            string command = desc ? "OrderByDescending" : "OrderBy";
            var type = typeof(T);
            var parameter = Expression.Parameter(type, "p");


            var propertyAccess = Expression.MakeMemberAccess(parameter, property);
            var propertyType = property.PropertyType;
            if (property.PropertyType.GetProperty("Name") != null)
            {
                propertyAccess = Expression.MakeMemberAccess(propertyAccess, property.PropertyType.GetProperty("Name"));
                propertyType = property.PropertyType.GetProperty("Name").PropertyType;
            }
            var orderByExpression = Expression.Lambda(propertyAccess, parameter);
            var resultExpression = Expression.Call(typeof(Queryable), command, new Type[] { type, propertyType },
                source.Expression, Expression.Quote(orderByExpression));
            return source.Provider.CreateQuery<T>(resultExpression);
        }

        public static IQueryable<T> OrderBy<T>(this IQueryable<T> source, LambdaExpression lambdaExpression, bool desc)
        {
            string command = desc ? "OrderByDescending" : "OrderBy";
            var type = typeof(T);
            var orderByExpression = lambdaExpression;
            var resultExpression = Expression.Call(typeof(Queryable), command, new Type[] { type, lambdaExpression.ReturnType },
                source.Expression, Expression.Quote(orderByExpression));
            return source.Provider.CreateQuery<T>(resultExpression);
        }

        public static IQueryable<T> OrderBy<T>(this IQueryable<T> source, FieldInfo field, bool desc)
        {
            string command = desc ? "OrderByDescending" : "OrderBy";
            var type = typeof(T);
            var parameter = Expression.Parameter(type, "p");
            var fieldAccess = Expression.MakeMemberAccess(parameter, field);
            var orderByExpression = Expression.Lambda(fieldAccess, parameter);
            var resultExpression = Expression.Call(typeof(Queryable), command, new Type[] { type, field.GetType() },
                source.Expression, Expression.Quote(orderByExpression));
            return source.Provider.CreateQuery<T>(resultExpression);
        }

        public static IOrderedQueryable<TSource> OrderBy<TSource, TKey>(this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, bool decending)
        {
            if (decending)
            {
                return source.OrderByDescending(keySelector);
            }
            else
            {
                return source.OrderBy(keySelector);
            }
        }

        public static IOrderedQueryable<TSource> ThenBy<TSource, TKey>(this IOrderedQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, bool decending)
        {
            if (decending)
            {
                return source.ThenByDescending(keySelector);
            }
            else
            {
                return source.ThenBy(keySelector);
            }
        }

        public static IQueryable<T> Where<T>(this IQueryable<T> source, PropertyInfo property, string searchTerm)
        {
            // EF.Functions.Collate/Like (below) only translate on a relational EF provider; on an
            // in-memory (LINQ-to-objects) source they throw at enumeration. For that case fall back
            // to a provider-agnostic, null-safe, case-insensitive Contains.
            if (source.Provider is EnumerableQuery)
            {
                var term = searchTerm.Trim('%').ToLowerInvariant();
                return source
                    .AsEnumerable()
                    .Where(item =>
                    {
                        var value = property.GetValue(item)?.ToString();
                        return value != null && value.ToLowerInvariant().Contains(term);
                    })
                    .AsQueryable();
            }

            if (!searchTerm.EndsWith("%"))
            {
                searchTerm += "%";
            }
            if (!searchTerm.StartsWith("%"))
            {
                searchTerm = "%" + searchTerm;
            }
            var itemParameter = Expression.Parameter(typeof(T), "item");

            var functions = Expression.Property(null, typeof(EF).GetProperty(nameof(EF.Functions)));
            var like = typeof(DbFunctionsExtensions).GetMethod(nameof(DbFunctionsExtensions.Like), new Type[] { functions.Type, typeof(string), typeof(string) });
            var collate = typeof(RelationalDbFunctionsExtensions).GetMethod(nameof(RelationalDbFunctionsExtensions.Collate))?
                .MakeGenericMethod(new Type[] {typeof(string) });

            Expression expressionProperty = Expression.Property(itemParameter, property.Name);

            if (property.PropertyType != typeof(string))
            {
                expressionProperty = Expression.Call(expressionProperty, typeof(object).GetMethod(nameof(object.ToString), new Type[0]));
            }

            var selector = Expression.Call(
                       null,
                       collate,
                       functions,
                       expressionProperty,
                       Expression.Constant(CASE_INSENSITIVE_COLLATION));

            selector = Expression.Call(
                        null,
                        like,
                        functions,
                        selector,
                        Expression.Constant(searchTerm)
                        );
            return source.Where(Expression.Lambda<Func<T, bool>>(selector, itemParameter));
        }

        public static IQueryable<T> Where<T>(this IQueryable<T> source, PropertyInfo property, DateTime greaterThanOrEqual, DateTime lessThanOrEqual)
        {
            var itemParameter = Expression.Parameter(property.DeclaringType, "w");
            var propertyExpression = Expression.Property(itemParameter, property);
            var greaterThanOrEqualConstantExpression = Expression.Constant(greaterThanOrEqual);
            var lessThanOrEqualConstantExpression = Expression.Constant(lessThanOrEqual);
            var greaterThanOrEqualExpression = Expression.GreaterThanOrEqual(propertyExpression, greaterThanOrEqualConstantExpression);
            var lessThanOrEqualExpression = Expression.LessThanOrEqual(propertyExpression, lessThanOrEqualConstantExpression);
            var andExpression = Expression.And(greaterThanOrEqualExpression, lessThanOrEqualExpression);

            return source.Where(Expression.Lambda<Func<T, bool>>(andExpression, itemParameter));
        }

        public static IQueryable<T> Where<T>(this IQueryable<T> source, PropertyInfo property, double greaterThanOrEqual, double lessThanOrEqual)
        {
            var itemParameter = Expression.Parameter(property.DeclaringType, "w");
            var propertyExpression = Expression.Property(itemParameter, property);
            var greaterThanOrEqualConstantExpression = Expression.Constant(greaterThanOrEqual);
            var lessThanOrEqualConstantExpression = Expression.Constant(lessThanOrEqual);
            var greaterThanOrEqualExpression = Expression.GreaterThanOrEqual(propertyExpression, greaterThanOrEqualConstantExpression);
            var lessThanOrEqualExpression = Expression.LessThanOrEqual(propertyExpression, lessThanOrEqualConstantExpression);
            var andExpression = Expression.And(greaterThanOrEqualExpression, lessThanOrEqualExpression);

            return source.Where(Expression.Lambda<Func<T, bool>>(andExpression, itemParameter));
        }

        public static IQueryable<T> Where<T>(this IQueryable<T> source, PropertyInfo property, int greaterThanOrEqual, int lessThanOrEqual)
        {
            var itemParameter = Expression.Parameter(property.DeclaringType, "w");
            var propertyExpression = Expression.Property(itemParameter, property);
            var greaterThanOrEqualConstantExpression = Expression.Constant(greaterThanOrEqual);
            var lessThanOrEqualConstantExpression = Expression.Constant(lessThanOrEqual);
            var greaterThanOrEqualExpression = Expression.GreaterThanOrEqual(propertyExpression, greaterThanOrEqualConstantExpression);
            var lessThanOrEqualExpression = Expression.LessThanOrEqual(propertyExpression, lessThanOrEqualConstantExpression);
            var andExpression = Expression.And(greaterThanOrEqualExpression, lessThanOrEqualExpression);

            return source.Where(Expression.Lambda<Func<T, bool>>(andExpression, itemParameter));
        }

        public static IQueryable<T> Where<T>(this IQueryable<T> source, Type type, LambdaExpression lambdaExpression, TimeSpan greaterThanOrEqual, TimeSpan lessThanOrEqual)
        {
            var itemParameter = Expression.Parameter(type, "w");
            var propertyExpression = Expression.Lambda(lambdaExpression, itemParameter);
            var greaterThanOrEqualConstantExpression = Expression.Constant(greaterThanOrEqual);
            var lessThanOrEqualConstantExpression = Expression.Constant(lessThanOrEqual);
            var greaterThanOrEqualExpression = Expression.GreaterThanOrEqual(propertyExpression, greaterThanOrEqualConstantExpression);
            var lessThanOrEqualExpression = Expression.LessThanOrEqual(propertyExpression, lessThanOrEqualConstantExpression);
            var andExpression = Expression.And(greaterThanOrEqualExpression, lessThanOrEqualExpression);

            return source.Where(Expression.Lambda<Func<T, bool>>(andExpression, itemParameter));
        }

        public static IQueryable<T> Where<T>(this IQueryable<T> source, PropertyInfo property, bool value)
        {
            var itemParameter = Expression.Parameter(property.DeclaringType, "w");
            var propertyExpression = Expression.Property(itemParameter, property);
            var boolConstantExpression = Expression.Constant(value);
            var equalExpression = Expression.Equal(propertyExpression, boolConstantExpression);

            return source.Where(Expression.Lambda<Func<T, bool>>(equalExpression, itemParameter));
        }

        public static IQueryable<T> Where<T>(this IQueryable<T> source, PropertyInfo property, int[] values)
        {
            var itemParameter = Expression.Parameter(property.DeclaringType, "w");
            var propertyExpression = Expression.Property(itemParameter, property);
            var binaryExpressions = new Stack<BinaryExpression>(values.Select(s => Expression.Equal(propertyExpression, Expression.Convert(Expression.Constant(s), property.PropertyType))).ToList());

            while (binaryExpressions.Count > 1)
            {
                binaryExpressions.Push(Expression.OrElse(binaryExpressions.Pop(), binaryExpressions.Pop()));
            }

            return source.Where(Expression.Lambda<Func<T, bool>>(binaryExpressions.First(), itemParameter));
        }

        public static IQueryable<T> Where<T>(this IQueryable<T> source, PropertyInfo property, Guid[] values)
        {
            var itemParameter = Expression.Parameter(property.DeclaringType, "w");
            var propertyExpression = Expression.Property(itemParameter, property);
            var binaryExpressions = new Stack<BinaryExpression>(values.Select(s => Expression.Equal(propertyExpression, Expression.Convert(Expression.Constant(s), property.PropertyType))).ToList());

            while (binaryExpressions.Count > 1)
            {
                binaryExpressions.Push(Expression.OrElse(binaryExpressions.Pop(), binaryExpressions.Pop()));
            }

            return source.Where(Expression.Lambda<Func<T, bool>>(binaryExpressions.First(), itemParameter));
        }
    }
}
