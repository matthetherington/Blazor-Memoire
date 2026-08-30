using System.Buffers;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using ICollection = System.Collections.ICollection;
using IDictionary = System.Collections.IDictionary;
using IEnumerable = System.Collections.IEnumerable;

namespace BlazorMemoire.Components;

/// <summary>
/// Deep value comparison for arbitrary objects. Collections are compared element-by-element:
/// ordered collections use positional equality; sets use order-independent comparison;
/// dictionaries are compared by key so insertion order does not matter.
///
/// Caveats:
/// - <b>Mutating a collection in place is not detected.</b> Pass a new instance instead.
/// - IEnumerable values are enumerated to compare. Materialise lazy queries before using
///   them as keys.
/// - Non-primitive set elements use an O(n*m) matching pass (avoids relying on
///   GetHashCode consistency). Sets with a coarser comparer (e.g. OrdinalIgnoreCase) also
///   take this path. Fine for typical key-sized sets.
/// - <b>Dictionary keys use the dictionary's own comparer, not structural equality.</b>
///   An OrdinalIgnoreCase dictionary won't detect a key casing change. Use ordinal keys.
/// - Only dictionaries implementing non-generic <see cref="IDictionary"/> get by-key
///   comparison. A hand-rolled IReadOnlyDictionary falls back to positional comparison.
/// - Comparison stops at depth <see cref="MaxComparisonDepth"/> to prevent stack overflow
///   from cyclic graphs. Beyond that depth, reports "changed".
/// </summary>
internal static class ValueComparer
{
    /// <summary>
    /// Depth limit to prevent stack overflow from cyclic graphs. Bails out as "changed".
    /// </summary>
    private const int MaxComparisonDepth = 32;

    /// <summary>
    /// Max element count for stack-based match bookkeeping; larger sets rent from the pool.
    /// </summary>
    private const int StackAllocLimit = 64;

    /// <summary>
    /// Smallest buffer worth renting — the pool's smallest bucket is 16 elements anyway.
    /// Also used as a growth floor so doubling zero doesn't spin forever.
    /// </summary>
    private const int MinPooledLength = 16;

    private static readonly ConcurrentDictionary<Type, TypeCategory> CategoryCache = new();
    private static readonly ConcurrentDictionary<Type, object> BoxedDefaultCache = new();

    /// <summary>
    /// Comparison strategy for a type. Resolved once per type and cached so the interface
    /// walks never happen on the hot path.
    /// </summary>
    private enum TypeCategory : byte
    {
        /// <summary>Not a collection; <c>Equals</c> is authoritative.</summary>
        Value,
        Dictionary,
        Set,
        ImmutableArray,
        List,
        Enumerable,
    }

    internal static bool ValuesEqual(object? oldValue, object? newValue, int depth)
    {
        if (ReferenceEquals(oldValue, newValue))
        {
            return true;
        }

        if (oldValue is null || newValue is null)
        {
            return false;
        }

        if (depth >= MaxComparisonDepth)
        {
            return false;
        }

        // Strings are IEnumerable<char> but have correct Equals already.
        // Most common key type, so tested first.
        if (oldValue is string oldString)
        {
            return newValue is string newString
                && string.Equals(oldString, newString, StringComparison.Ordinal);
        }

        var oldCategory = GetCategory(oldValue.GetType());

        if (oldCategory == TypeCategory.Value)
        {
            return oldValue.Equals(newValue);
        }

        if (newValue is not IEnumerable)
        {
            return false; // one is a collection, the other is not
        }

        if (
            oldCategory == TypeCategory.ImmutableArray
            || GetCategory(newValue.GetType()) == TypeCategory.ImmutableArray
        )
        {
            // default(ImmutableArray<T>) throws from Count/GetEnumerator.
            // Its Equals compares the underlying array reference and never throws.
            if (IsUninitialised(oldValue) || IsUninitialised(newValue))
            {
                return oldValue.Equals(newValue);
            }
        }

        // Cheap length bail-out. HashSet<T> doesn't implement non-generic ICollection,
        // so the set path still does its own length check.
        if (
            oldValue is ICollection oldCollection
            && newValue is ICollection newCollection
            && oldCollection.Count != newCollection.Count
        )
        {
            return false;
        }

        if (oldCategory == TypeCategory.Dictionary && newValue is IDictionary newDictionary)
        {
            return DictionaryEqual((IDictionary)oldValue, newDictionary, depth);
        }

        if (oldCategory == TypeCategory.Set && GetCategory(newValue.GetType()) == TypeCategory.Set)
        {
            return SetComparer.TryFastEqual(oldValue, newValue, out var fastSetResult)
                ? fastSetResult
                : UnorderedEqual((IEnumerable)oldValue, (IEnumerable)newValue, depth);
        }

        // Span fast path for arrays/lists of primitive types — avoids the per-element
        // boxing that the IList indexer below would cost on value types.
        if (SequenceComparer.TryFastEqual(oldValue, newValue, out var fastResult))
        {
            return fastResult;
        }

        if (oldValue is IList oldList && newValue is IList newList)
        {
            var count = oldList.Count;

            if (count != newList.Count)
            {
                return false;
            }

            for (var i = 0; i < count; i++)
            {
                if (!ValuesEqual(oldList[i], newList[i], depth + 1))
                {
                    return false;
                }
            }

            return true;
        }

        return SequenceEqual((IEnumerable)oldValue, (IEnumerable)newValue, depth);
    }

    private static TypeCategory GetCategory(Type type) =>
        CategoryCache.GetOrAdd(
            type,
            static t =>
            {
                if (t.IsGenericType)
                {
                    var definition = t.GetGenericTypeDefinition();

                    if (definition == typeof(ImmutableArray<>))
                    {
                        return TypeCategory.ImmutableArray;
                    }
                }

                if (!typeof(IEnumerable).IsAssignableFrom(t))
                {
                    return TypeCategory.Value;
                }

                if (typeof(IDictionary).IsAssignableFrom(t))
                {
                    return TypeCategory.Dictionary;
                }

                if (ImplementsGenericSet(t))
                {
                    return TypeCategory.Set;
                }

                return typeof(IList).IsAssignableFrom(t)
                    ? TypeCategory.List
                    : TypeCategory.Enumerable;
            }
        );

    private static bool ImplementsGenericSet(Type type)
    {
        foreach (var contract in type.GetInterfaces())
        {
            if (contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(ISet<>))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Whether <paramref name="value"/> is <c>default(ImmutableArray&lt;T&gt;)</c>, without
    /// touching any member that throws when uninitialised.
    /// </summary>
    private static bool IsUninitialised(object value) =>
        GetCategory(value.GetType()) == TypeCategory.ImmutableArray && IsDefaultInstance(value);

    /// <summary>
    /// Whether <paramref name="value"/> is the all-zero default of its type, via a cached
    /// boxed default. Used for <c>default(ImmutableArray&lt;T&gt;)</c> where Count throws.
    /// </summary>
    private static bool IsDefaultInstance(object value) =>
        value.Equals(
            BoxedDefaultCache.GetOrAdd(value.GetType(), static t => Activator.CreateInstance(t)!)
        );

    /// <summary>
    /// Compares by key so insertion-order differences don't cause false positives.
    /// </summary>
    private static bool DictionaryEqual(
        IDictionary oldDictionary,
        IDictionary newDictionary,
        int depth
    )
    {
        if (oldDictionary.Count != newDictionary.Count)
        {
            return false;
        }

        var enumerator = oldDictionary.GetEnumerator();

        try
        {
            while (enumerator.MoveNext())
            {
                var key = enumerator.Key;

                if (!newDictionary.Contains(key))
                {
                    return false;
                }

                if (!ValuesEqual(enumerator.Value, newDictionary[key], depth + 1))
                {
                    return false;
                }
            }
        }
        finally
        {
            (enumerator as IDisposable)?.Dispose();
        }

        return true;
    }

    private static bool SequenceEqual(
        IEnumerable oldEnumerable,
        IEnumerable newEnumerable,
        int depth
    )
    {
        var oldEnumerator = oldEnumerable.GetEnumerator();
        var newEnumerator = newEnumerable.GetEnumerator();

        try
        {
            while (true)
            {
                var oldHasNext = oldEnumerator.MoveNext();
                var newHasNext = newEnumerator.MoveNext();

                if (oldHasNext != newHasNext)
                {
                    return false; // different lengths
                }

                if (!oldHasNext)
                {
                    return true; // both exhausted, every element matched
                }

                if (!ValuesEqual(oldEnumerator.Current, newEnumerator.Current, depth + 1))
                {
                    return false;
                }
            }
        }
        finally
        {
            (oldEnumerator as IDisposable)?.Dispose();
            (newEnumerator as IDisposable)?.Dispose();
        }
    }

    /// <summary>
    /// Order-independent comparison using value-equality rather than hashing, since custom
    /// types may not implement GetHashCode consistently. Buffers come from the stack or pool.
    /// </summary>
    private static bool UnorderedEqual(
        IEnumerable oldEnumerable,
        IEnumerable newEnumerable,
        int depth
    )
    {
        Span<bool> stackFlags = stackalloc bool[StackAllocLimit];
        var newItems = Materialise(newEnumerable, out var newCount);

        try
        {
            if (newCount <= StackAllocLimit)
            {
                return MatchAll(oldEnumerable, newItems, stackFlags[..newCount], depth);
            }

            var rentedFlags = ArrayPool<bool>.Shared.Rent(newCount);

            try
            {
                return MatchAll(oldEnumerable, newItems, rentedFlags.AsSpan(0, newCount), depth);
            }
            finally
            {
                ArrayPool<bool>.Shared.Return(rentedFlags);
            }
        }
        finally
        {
            ArrayPool<object?>.Shared.Return(newItems, clearArray: true);
        }
    }

    /// <summary>
    /// Pairs each old item with a distinct equal new item.
    /// <paramref name="matched"/> tracks claimed new items.
    /// </summary>
    private static bool MatchAll(
        IEnumerable oldEnumerable,
        object?[] newItems,
        Span<bool> matched,
        int depth
    )
    {
        // Pooled arrays arrive dirty and stackalloc is only zeroed by convention.
        matched.Clear();

        var newCount = matched.Length;
        var oldCount = 0;

        foreach (var oldItem in oldEnumerable)
        {
            oldCount++;

            if (oldCount > newCount)
            {
                return false;
            }

            var found = false;

            for (var i = 0; i < newCount; i++)
            {
                if (!matched[i] && ValuesEqual(oldItem, newItems[i], depth + 1))
                {
                    matched[i] = true;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                return false;
            }
        }

        return oldCount == newCount;
    }

    /// <summary>
    /// Copies <paramref name="source"/> into a pooled buffer. Caller must return it.
    /// Starts at <see cref="MinPooledLength"/> and doubles on overflow.
    /// </summary>
    private static object?[] Materialise(IEnumerable source, out int count)
    {
        var capacity = source is ICollection collection
            ? Math.Max(collection.Count, MinPooledLength)
            : MinPooledLength;

        var buffer = ArrayPool<object?>.Shared.Rent(capacity);
        var index = 0;

        foreach (var item in source)
        {
            if (index == buffer.Length)
            {
                // Floor at MinPooledLength so doubling zero doesn't spin forever.
                var larger = ArrayPool<object?>.Shared.Rent(
                    Math.Max(buffer.Length * 2, MinPooledLength)
                );

                Array.Copy(buffer, larger, index);
                ArrayPool<object?>.Shared.Return(buffer, clearArray: true);
                buffer = larger;
            }

            buffer[index++] = item;
        }

        count = index;

        return buffer;
    }
}
