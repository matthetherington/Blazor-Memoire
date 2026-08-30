using System.Runtime.InteropServices;

namespace BlazorMemoire.Components;

/// <summary>
/// Span-based comparison for arrays and lists of known primitive types.
///
/// <b>Only types with sealed BCL Equals may be added to the switch.</b> This guarantees
/// two things: (a) skipping recursive comparison is correct, and (b) no user code runs
/// while <see cref="CollectionsMarshal.AsSpan{T}"/> holds a span over a list's backing
/// array, so the list cannot be mutated during the comparison.
///
/// Enum arrays match via CLR array covariance (ECMA-335 I.8.7.1) — e.g.
/// <c>DayOfWeek[]</c> matches <c>case int[]</c>. Generics have no such rule, so
/// <c>List&lt;DayOfWeek&gt;</c> is not handled. <c>Nullable&lt;T&gt;</c> can't appear
/// here — it doesn't implement <see cref="IEquatable{T}"/>.
/// </summary>
internal static class SequenceComparer
{
    /// <summary>
    /// Returns <c>true</c> if the comparison was handled (answer in <paramref name="result"/>),
    /// <c>false</c> if the caller should fall through to element-wise comparison.
    /// </summary>
    internal static bool TryFastEqual(object oldValue, object newValue, out bool result)
    {
        switch (oldValue)
        {
            case string[] value:
                return ArrayEqual(value, newValue, out result);
            case int[] value:
                return ArrayEqual(value, newValue, out result);
            case long[] value:
                return ArrayEqual(value, newValue, out result);
            case double[] value:
                return ArrayEqual(value, newValue, out result);
            case float[] value:
                return ArrayEqual(value, newValue, out result);
            case decimal[] value:
                return ArrayEqual(value, newValue, out result);
            case bool[] value:
                return ArrayEqual(value, newValue, out result);
            case byte[] value:
                return ArrayEqual(value, newValue, out result);
            case Guid[] value:
                return ArrayEqual(value, newValue, out result);
            case DateTime[] value:
                return ArrayEqual(value, newValue, out result);
            case DateTimeOffset[] value:
                return ArrayEqual(value, newValue, out result);
            case DateOnly[] value:
                return ArrayEqual(value, newValue, out result);
            case TimeOnly[] value:
                return ArrayEqual(value, newValue, out result);
            case TimeSpan[] value:
                return ArrayEqual(value, newValue, out result);
            case List<string> value:
                return ListEqual(value, newValue, out result);
            case List<int> value:
                return ListEqual(value, newValue, out result);
            case List<long> value:
                return ListEqual(value, newValue, out result);
            case List<double> value:
                return ListEqual(value, newValue, out result);
            case List<float> value:
                return ListEqual(value, newValue, out result);
            case List<decimal> value:
                return ListEqual(value, newValue, out result);
            case List<bool> value:
                return ListEqual(value, newValue, out result);
            case List<byte> value:
                return ListEqual(value, newValue, out result);
            case List<Guid> value:
                return ListEqual(value, newValue, out result);
            case List<DateTime> value:
                return ListEqual(value, newValue, out result);
            case List<DateTimeOffset> value:
                return ListEqual(value, newValue, out result);
            case List<DateOnly> value:
                return ListEqual(value, newValue, out result);
            case List<TimeOnly> value:
                return ListEqual(value, newValue, out result);
            case List<TimeSpan> value:
                return ListEqual(value, newValue, out result);
            default:
                result = false;
                return false;
        }
    }

    private static bool ArrayEqual<T>(T[] oldArray, object newValue, out bool result)
        where T : IEquatable<T>
    {
        if (newValue is not T[] newArray)
        {
            result = false;
            return false; // type mismatch (e.g. List vs array); not handled
        }

        result = oldArray.AsSpan().SequenceEqual(newArray);
        return true;
    }

    /// <summary>
    /// Compares lists via <see cref="CollectionsMarshal.AsSpan{T}"/>. The span is only valid
    /// while the list isn't resized — callers must ensure no concurrent mutation.
    /// </summary>
    private static bool ListEqual<T>(List<T> oldList, object newValue, out bool result)
        where T : IEquatable<T>
    {
        if (newValue is not List<T> newList)
        {
            result = false;
            return false; // type mismatch (e.g. array vs List); not handled
        }

        result = CollectionsMarshal
            .AsSpan(oldList)
            .SequenceEqual(CollectionsMarshal.AsSpan(newList));
        return true;
    }
}
