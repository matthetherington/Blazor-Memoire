using Bunit;

namespace BlazorMemoire.Components.Tests;

public class MemoDictionaryKeyTests : MemoTestBase
{
    [Fact]
    public void SameDictionaryKey_DifferentInsertionOrder_Freezes()
    {
        var cut = Render<MemoParent>(p =>
            p.Add(
                    c => c.Keys,
                    [
                        new Dictionary<string, int>(StringComparer.Ordinal)
                        {
                            ["a"] = 1,
                            ["b"] = 2,
                            ["c"] = 3,
                        },
                    ]
                )
                .Add(c => c.ChildText, "x")
        );

        cut.Render(p =>
            p.Add(
                    c => c.Keys,
                    [
                        new Dictionary<string, int>(StringComparer.Ordinal)
                        {
                            ["c"] = 3,
                            ["b"] = 2,
                            ["a"] = 1,
                        },
                    ]
                )
                .Add(c => c.ChildText, "y")
        );

        AssertChildRenders(cut.Instance.Child!, 1);
    }

    [Fact]
    public void DifferentDictionaryValue_ReRenders()
    {
        var cut = Render<MemoParent>(p =>
            p.Add(
                    c => c.Keys,
                    [new Dictionary<string, int>(StringComparer.Ordinal) { ["a"] = 1, ["b"] = 2 }]
                )
                .Add(c => c.ChildText, "x")
        );

        cut.Render(p =>
            p.Add(
                    c => c.Keys,
                    [new Dictionary<string, int>(StringComparer.Ordinal) { ["a"] = 1, ["b"] = 99 }]
                )
                .Add(c => c.ChildText, "x")
        );

        AssertChildRenders(cut.Instance.Child!, 2);
    }

    [Fact]
    public void DifferentDictionaryKey_ReRenders()
    {
        var cut = Render<MemoParent>(p =>
            p.Add(
                    c => c.Keys,
                    [new Dictionary<string, int>(StringComparer.Ordinal) { ["a"] = 1, ["b"] = 2 }]
                )
                .Add(c => c.ChildText, "x")
        );

        cut.Render(p =>
            p.Add(
                    c => c.Keys,
                    [new Dictionary<string, int>(StringComparer.Ordinal) { ["a"] = 1, ["z"] = 2 }]
                )
                .Add(c => c.ChildText, "x")
        );

        AssertChildRenders(cut.Instance.Child!, 2);
    }

    [Fact]
    public void DifferentDictionarySize_ReRenders()
    {
        var cut = Render<MemoParent>(p =>
            p.Add(c => c.Keys, [new Dictionary<string, int>(StringComparer.Ordinal) { ["a"] = 1 }])
                .Add(c => c.ChildText, "x")
        );

        cut.Render(p =>
            p.Add(
                    c => c.Keys,
                    [new Dictionary<string, int>(StringComparer.Ordinal) { ["a"] = 1, ["b"] = 2 }]
                )
                .Add(c => c.ChildText, "x")
        );

        AssertChildRenders(cut.Instance.Child!, 2);
    }

    [Fact]
    public void CaseInsensitiveDictionaryKey_IsNotDetected()
    {
        // Pins a documented limitation. Keys are matched with the dictionary's own comparer.
        var cut = Render<MemoParent>(p =>
            p.Add(
                    c => c.Keys,
                    [new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) { ["a"] = 1 }]
                )
                .Add(c => c.ChildText, "x")
        );

        cut.Render(p =>
            p.Add(
                    c => c.Keys,
                    [new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) { ["A"] = 1 }]
                )
                .Add(c => c.ChildText, "x")
        );

        AssertChildRenders(cut.Instance.Child!, 1);
    }

    [Fact]
    public void ReadOnlyDictionary_FallsBackToPositionalComparison()
    {
        // A dictionary that doesn't implement non-generic IDictionary misses the by-key
        // comparison, so a pure reordering costs a redundant render.
        var cut = Render<MemoParent>(p =>
            p.Add(
                    c => c.Keys,
                    [
                        new ReadOnlyLookup(
                            new Dictionary<string, int>(StringComparer.Ordinal)
                            {
                                ["a"] = 1,
                                ["b"] = 2,
                            }
                        ),
                    ]
                )
                .Add(c => c.ChildText, "x")
        );

        // Same order — freezes
        cut.Render(p =>
            p.Add(
                    c => c.Keys,
                    [
                        new ReadOnlyLookup(
                            new Dictionary<string, int>(StringComparer.Ordinal)
                            {
                                ["a"] = 1,
                                ["b"] = 2,
                            }
                        ),
                    ]
                )
                .Add(c => c.ChildText, "y")
        );
        AssertChildRenders(cut.Instance.Child!, 1);

        // Different order — positional fallback reports changed
        cut.Render(p =>
            p.Add(
                    c => c.Keys,
                    [
                        new ReadOnlyLookup(
                            new Dictionary<string, int>(StringComparer.Ordinal)
                            {
                                ["b"] = 2,
                                ["a"] = 1,
                            }
                        ),
                    ]
                )
                .Add(c => c.ChildText, "y")
        );
        AssertChildRenders(cut.Instance.Child!, 2);
    }
}
