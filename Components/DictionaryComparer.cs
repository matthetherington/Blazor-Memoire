namespace BlazorMemoire.Components;

/// <summary>
/// Allocation-free comparison for common concrete dictionary shapes. Typed enumeration and
/// lookup avoid boxing the enumerator, keys, and values through non-generic IDictionary.
/// Dictionaries must share the same comparer instance to compare equal.
/// </summary>
internal static class DictionaryComparer
{
    /// <summary>
    /// Returns <see langword="true"/> if the comparison was handled (answer in <paramref name="result"/>),
    /// <see langword="false"/> if the caller should use the general dictionary comparison.
    /// </summary>
    internal static bool TryFastEqual(object oldValue, object newValue, int depth, out bool result)
    {
        switch (oldValue)
        {
            case Dictionary<string, object?> value:
                return ObjectDictionaryEqual(value, newValue, depth, out result);
            case Dictionary<string, string> value:
                return DictionaryEqual(value, newValue, out result);
            case Dictionary<string, int> value:
                return DictionaryEqual(value, newValue, out result);
            case Dictionary<int, string> value:
                return DictionaryEqual(value, newValue, out result);
            case Dictionary<int, int> value:
                return DictionaryEqual(value, newValue, out result);
            default:
                result = false;
                return false;
        }
    }

    private static bool DictionaryEqual<TKey, TValue>(
        Dictionary<TKey, TValue> oldDictionary,
        object newValue,
        out bool result
    )
        where TKey : notnull
    {
        if (newValue is not Dictionary<TKey, TValue> newDictionary)
        {
            result = false;
            return false;
        }

        if (!ReferenceEquals(oldDictionary.Comparer, newDictionary.Comparer))
        {
            result = false;
            return true;
        }

        if (oldDictionary.Count != newDictionary.Count)
        {
            result = false;
            return true;
        }

        foreach (var pair in oldDictionary)
        {
            if (
                !newDictionary.TryGetValue(pair.Key, out var newItem)
                || !EqualityComparer<TValue>.Default.Equals(pair.Value, newItem)
            )
            {
                result = false;
                return true;
            }
        }

        result = true;
        return true;
    }

    private static bool ObjectDictionaryEqual(
        Dictionary<string, object?> oldDictionary,
        object newValue,
        int depth,
        out bool result
    )
    {
        if (newValue is not Dictionary<string, object?> newDictionary)
        {
            result = false;
            return false;
        }

        if (!ReferenceEquals(oldDictionary.Comparer, newDictionary.Comparer))
        {
            result = false;
            return true;
        }

        if (oldDictionary.Count != newDictionary.Count)
        {
            result = false;
            return true;
        }

        foreach (var pair in oldDictionary)
        {
            if (
                !newDictionary.TryGetValue(pair.Key, out var newItem)
                || !ValueComparer.ValuesEqual(pair.Value, newItem, depth + 1)
            )
            {
                result = false;
                return true;
            }
        }

        result = true;
        return true;
    }
}
