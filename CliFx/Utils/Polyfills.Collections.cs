// ReSharper disable CheckNamespace

#if NETSTANDARD2_0
using System.Collections.Generic;

internal static class CollectionPolyfills
{
    public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> pair, out TKey key, out TValue value)
    {
        key = pair.Key;
        value = pair.Value;
    }

    public static TValue GetValueOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dic, TKey key) =>
        dic.TryGetValue(key!, out var result) ? result! : default!;
}

namespace System.Linq
{
    internal static class LinqPolyfills
    {
        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source, IEqualityComparer<T> comparer) =>
            new(source, comparer);
    }
}
#endif