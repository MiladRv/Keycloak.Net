using System.Linq.Expressions;

namespace Keycloak.Net.Sdk.AspNetCore;

public static class DataScopeQueryableExtensions
{
    /// <summary>
    /// Keeps only rows whose key is in the scope. An unrestricted scope returns the query unchanged;
    /// an empty scope returns no rows. The filter is a <c>Contains</c> over a captured list, which
    /// EF Core translates to SQL <c>IN</c>.
    /// </summary>
    public static IQueryable<T> WhereInScope<T, TKey>(this IQueryable<T> query, DataScope scope, Expression<Func<T, TKey>> key)
        where TKey : IParsable<TKey>
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(key);

        if (scope.IsUnrestricted) return query;

        var allowed = scope.ValuesAs<TKey>().ToList();
        // Reference the list through a closure so query providers treat it as a parameter.
        Expression<Func<List<TKey>>> captured = () => allowed;
        var contains = typeof(List<TKey>).GetMethod(nameof(List<TKey>.Contains), [typeof(TKey)])!;
        var body = Expression.Call(captured.Body, contains, key.Body);

        return query.Where(Expression.Lambda<Func<T, bool>>(body, key.Parameters));
    }
}
