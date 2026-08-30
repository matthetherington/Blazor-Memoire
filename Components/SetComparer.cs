namespace BlazorMemoire.Components;

/// <summary>
/// O(n) set comparison for common primitive element types. Only used when the set's
/// comparer agrees with structural equality; otherwise falls through to the O(n*m) scan.
/// </summary>
internal static class SetComparer
{
    /// <summary>
    /// Returns <c>true</c> if the comparison was handled (answer in <paramref name="result"/>),
    /// <c>false</c> if the caller should fall through to the general matching pass.
    /// </summary>
    internal static bool TryFastEqual(object oldValue, object newValue, out bool result)
    {
        switch (oldValue)
        {
            case HashSet<string> value:
                return SetEqualCore(value, newValue, out result);
            case HashSet<int> value:
                return SetEqualCore(value, newValue, out result);
            case HashSet<long> value:
                return SetEqualCore(value, newValue, out result);
            case HashSet<Guid> value:
                return SetEqualCore(value, newValue, out result);
            default:
                result = false;
                return false;
        }
    }

    private static bool SetEqualCore<T>(HashSet<T> oldSet, object newValue, out bool result)
    {
        result = false;

        // Reject sets with a coarser comparer (e.g. OrdinalIgnoreCase) — their SetEquals
        // would report equality when elements differ by structural Equals.
        if (!IsStructuralComparer(oldSet.Comparer))
        {
            return false;
        }

        if (newValue is not IEnumerable<T> newSet)
        {
            return false; // shapes differ; let the general path decide
        }

        result = oldSet.SetEquals(newSet);
        return true;
    }

    /// <summary>
    /// Whether <paramref name="comparer"/> agrees with structural equality.
    /// Includes <see cref="StringComparer.Ordinal"/> (same behaviour as default, different instance).
    /// </summary>
    private static bool IsStructuralComparer<T>(IEqualityComparer<T> comparer) =>
        ReferenceEquals(comparer, EqualityComparer<T>.Default)
        || (typeof(T) == typeof(string) && ReferenceEquals(comparer, StringComparer.Ordinal));
}
