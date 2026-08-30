using Bunit;

namespace BlazorMemoire.Components.Tests;

public abstract class MemoTestBase : BunitContext
{
    /// <summary>
    /// Asserts the child was rendered exactly <paramref name="expectedRenderCount"/> times and
    /// received parameters the same number of times.
    /// </summary>
    protected static void AssertChildRenders(RenderCounter child, int expectedRenderCount)
    {
        Assert.Equal(expectedRenderCount, child.ParameterSetCount);
        Assert.Equal(expectedRenderCount, child.RenderCount);
    }

    /// <summary>
    /// Calls the span fast path directly. Which path a comparison took has no observable
    /// surface — the slow path returns the same answer — but the set of element types routed
    /// here is load-bearing for both correctness and the safety of the span it takes over a
    /// list's backing array, so it is asserted rather than inferred.
    /// </summary>
    protected static (bool Handled, bool Result) InvokeFastSequenceEqual(
        object oldValue,
        object newValue
    )
    {
        var handled = SequenceComparer.TryFastEqual(oldValue, newValue, out var result);
        return (handled, result);
    }

    /// <summary>
    /// Builds a set of records, optionally altering the element at <paramref name="changedAt"/>.
    /// A record element type deliberately keeps these off the primitive SetEquals fast path so
    /// they exercise materialisation and the O(n*m) matching pass.
    /// </summary>
    protected static ISet<NestedRecord> BuildPeople(int count, int changedAt = -1) =>
        Enumerable
            .Range(0, count)
            .Select(i => new NestedRecord($"n{i}", i == changedAt ? -1 : i))
            .ToHashSet();

    /// <summary>
    /// Embed the leaf in a nest List&lt;object&gt; of depth levels.
    /// </summary>
    /// <param name="depth">The number of levels of nesting. 0 returns the leaf only with no List.</param>
    /// <param name="leaf">The contents of the List</param>
    /// <returns></returns>
    protected static object BuildNested(int depth, string leaf)
    {
        // String literals are interned, so a shared leaf instance would be
        // caught by the reference check at the top of ValueComparer.ValuesEqual and none of the nesting
        // under test would actually be walked.
        object current = new string(leaf.AsSpan());

        for (var i = 0; i < depth; i++)
        {
            current = new List<object> { current };
        }

        return current;
    }
}
