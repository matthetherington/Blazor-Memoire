using System.Collections.Immutable;
using Bunit;

namespace BlazorMemoire.Components.Tests;

public sealed class MemoTests : MemoTestBase
{
    [Fact]
    public void NullKeys_AlwaysRenders()
    {
        var cut = Render<MemoParent>(p =>
            p.Add(c => c.Keys, (object?[]?)null).Add(c => c.ChildText, "a")
        );

        var child = cut.Instance.Child!;
        AssertChildRenders(child, 1);

        cut.Render(p => p.Add(c => c.Keys, (object?[]?)null).Add(c => c.ChildText, "a"));
        AssertChildRenders(child, 2);

        cut.Render(p => p.Add(c => c.Keys, (object?[]?)null).Add(c => c.ChildText, "a"));
        AssertChildRenders(child, 3);
    }

    [Fact]
    public void EmptyKeys_RendersOnceThenFreezes()
    {
        var cut = Render<MemoParent>(p => p.Add(c => c.Keys, []).Add(c => c.ChildText, "a"));

        var child = cut.Instance.Child!;
        AssertChildRenders(child, 1);
        Assert.Equal("a-1", cut.Markup);

        cut.Render(p => p.Add(c => c.Keys, []).Add(c => c.ChildText, "changed"));
        AssertChildRenders(child, 1);
        Assert.Equal("a-1", cut.Markup);

        cut.Render(p => p.Add(c => c.Keys, []).Add(c => c.ChildText, "again"));
        AssertChildRenders(child, 1);
        Assert.Equal("a-1", cut.Markup);
    }

    [Fact]
    public void SameKeys_FreezesChildSubtree()
    {
        var cut = Render<MemoParent>(p => p.Add(c => c.Keys, ["x", 1]).Add(c => c.ChildText, "a"));

        var child = cut.Instance.Child!;
        AssertChildRenders(child, 1);
        Assert.Equal("a-1", cut.Markup);

        cut.Render(p => p.Add(c => c.Keys, ["x", 1]).Add(c => c.ChildText, "changed"));
        AssertChildRenders(child, 1);

        // Markup stays at the frozen value — the child never got the "changed" text.
        Assert.Equal("a-1", cut.Markup);
    }

    [Fact]
    public void DifferentKeys_ReRendersChildSubtree()
    {
        var cut = Render<MemoParent>(p => p.Add(c => c.Keys, ["x"]).Add(c => c.ChildText, "a"));

        var child = cut.Instance.Child!;

        cut.Render(p => p.Add(c => c.Keys, ["y"]).Add(c => c.ChildText, "b"));
        AssertChildRenders(child, 2);
        Assert.Equal("b-2", cut.Markup);
    }

    [Fact]
    public void MultipleConsecutiveIdenticalKeys_OnlyRendersOnce()
    {
        var cut = Render<MemoParent>(p =>
            p.Add(c => c.Keys, ["stable", 7]).Add(c => c.ChildText, "a")
        );

        var child = cut.Instance.Child!;

        for (var i = 0; i < 5; i++)
        {
            cut.Render(p => p.Add(c => c.Keys, ["stable", 7]).Add(c => c.ChildText, "changed"));
            Assert.Equal("a-1", cut.Markup);
        }

        AssertChildRenders(child, 1);
    }

    [Fact]
    public void KeysChangedThenStable_ReRendersOnceForTheChange()
    {
        var cut = Render<MemoParent>(p => p.Add(c => c.Keys, [1]).Add(c => c.ChildText, "first"));
        Assert.Equal("first-1", cut.Markup);

        var child = cut.Instance.Child!;

        // Change keys
        cut.Render(p => p.Add(c => c.Keys, [2]).Add(c => c.ChildText, "second"));
        AssertChildRenders(child, 2);
        Assert.Equal("second-2", cut.Markup);

        // Same keys again — frozen
        cut.Render(p => p.Add(c => c.Keys, [2]).Add(c => c.ChildText, "third"));
        AssertChildRenders(child, 2);
        Assert.Equal("second-2", cut.Markup);
    }

    [Fact]
    public void NullToNonNullKeys_TransitionsFromAlwaysRenderToMemoised()
    {
        var cut = Render<MemoParent>(p =>
            p.Add(c => c.Keys, (object?[]?)null).Add(c => c.ChildText, "a")
        );

        var child = cut.Instance.Child!;
        AssertChildRenders(child, 1);
        Assert.Equal("a-1", cut.Markup);

        // Switch to non-null keys — renders this time (first snapshot)
        cut.Render(p => p.Add(c => c.Keys, ["x"]).Add(c => c.ChildText, "b"));
        AssertChildRenders(child, 2);
        Assert.Equal("b-2", cut.Markup);

        // Same keys — now frozen
        cut.Render(p => p.Add(c => c.Keys, ["x"]).Add(c => c.ChildText, "c"));
        AssertChildRenders(child, 2);
        Assert.Equal("b-2", cut.Markup);
    }

    [Fact]
    public void NonNullToNullKeys_TransitionsFromMemoisedToAlwaysRender()
    {
        var cut = Render<MemoParent>(p => p.Add(c => c.Keys, ["x"]).Add(c => c.ChildText, "a"));

        var child = cut.Instance.Child!;

        // Same keys — frozen
        cut.Render(p => p.Add(c => c.Keys, ["x"]).Add(c => c.ChildText, "b"));
        AssertChildRenders(child, 1);
        Assert.Equal("a-1", cut.Markup);

        // Switch to null keys — renders this time and every subsequent time
        cut.Render(p => p.Add(c => c.Keys, (object?[]?)null).Add(c => c.ChildText, "c"));
        AssertChildRenders(child, 2);
        Assert.Equal("c-3", cut.Markup);

        cut.Render(p => p.Add(c => c.Keys, (object?[]?)null).Add(c => c.ChildText, "d"));
        AssertChildRenders(child, 3);
        Assert.Equal("d-4", cut.Markup);
    }

    [Fact]
    public void KeysArrayLengthChange_ReRenders()
    {
        var cut = Render<MemoParent>(p =>
            p.Add(c => c.Keys, ["a", "b"]).Add(c => c.ChildText, "a")
        );

        var child = cut.Instance.Child!;
        Assert.Equal("a-1", cut.Markup);

        cut.Render(p => p.Add(c => c.Keys, ["a"]).Add(c => c.ChildText, "b"));
        AssertChildRenders(child, 2);
        Assert.Equal("b-2", cut.Markup);
    }

    [Fact]
    public void LazyEnumerableKey_WhenStableAcrossRenders_IsEnumeratedForSnapshotAndFreezes()
    {
        var enumerations = 0;

        IEnumerable<int> Query()
        {
            enumerations++;
            yield return 1;
            yield return 2;
        }

        var cut = Render<MemoParent>(p => p.Add(c => c.Keys, [Query()]).Add(c => c.ChildText, "x"));

        var enumerationsAfterFirstRender = enumerations;

        cut.Render(p => p.Add(c => c.Keys, [Query()]).Add(c => c.ChildText, "y"));

        AssertChildRenders(cut.Instance.Child!, 1);
        Assert.True(enumerations > enumerationsAfterFirstRender);
        Assert.Equal("x-1", cut.Markup);
    }

    [Fact]
    public void LazyEnumerableKey_WhenUnstableAcrossRenders_IsEnumeratedForSnapshotAndRerenders()
    {
        var enumerations = 0;

        IEnumerable<int> Query()
        {
            enumerations++;
            yield return 1;
            yield return enumerations; // 1 after initial render, 2 on second render
        }

        var cut = Render<MemoParent>(p => p.Add(c => c.Keys, [Query()]).Add(c => c.ChildText, "x"));

        var enumerationsAfterFirstRender = enumerations;

        cut.Render(p => p.Add(c => c.Keys, [Query()]).Add(c => c.ChildText, "y"));

        AssertChildRenders(cut.Instance.Child!, 2);
        Assert.True(enumerations > enumerationsAfterFirstRender);
        Assert.Equal("y-2", cut.Markup);
    }

    [Fact]
    public void LazyEnumerableKey_WhenStableButReplacedBetweenRenders_IsEnumeratedForSnapshotAndRerenders()
    {
        var firstQueryEnumerations = 0;
        var secondQueryEnumerations = 0;

        IEnumerable<int> FirstQuery()
        {
            firstQueryEnumerations++;
            yield return 1;
            yield return 2;
        }

        IEnumerable<int> SecondQuery()
        {
            secondQueryEnumerations++;
            yield return 2;
            yield return 3;
        }

        var cut = Render<MemoParent>(p =>
            p.Add(c => c.Keys, [FirstQuery()]).Add(c => c.ChildText, "x")
        );

        var enumerationsAfterFirstRender = firstQueryEnumerations;

        cut.Render(p => p.Add(c => c.Keys, [SecondQuery()]).Add(c => c.ChildText, "y"));

        AssertChildRenders(cut.Instance.Child!, 2);
        Assert.True(
            firstQueryEnumerations + secondQueryEnumerations > enumerationsAfterFirstRender
        );
        Assert.Equal("y-2", cut.Markup);
    }

    [Fact]
    public void ImmutableArrayDefaultKey_DoesNotThrow()
    {
        var cut = Render<MemoParent>(p =>
            p.Add(c => c.Keys, [default(ImmutableArray<int>)]).Add(c => c.ChildText, "x")
        );

        cut.Render(p =>
            p.Add(c => c.Keys, [default(ImmutableArray<int>)]).Add(c => c.ChildText, "y")
        );
        AssertChildRenders(cut.Instance.Child!, 1);
        Assert.Equal("x-1", cut.Markup);

        cut.Render(p =>
            p.Add(c => c.Keys, [ImmutableArray.Create(1, 2)]).Add(c => c.ChildText, "y")
        );
        AssertChildRenders(cut.Instance.Child!, 2);
        Assert.Equal("y-3", cut.Markup);

        cut.Render(p =>
            p.Add(c => c.Keys, [ImmutableArray.Create(1, 2)]).Add(c => c.ChildText, "y")
        );
        AssertChildRenders(cut.Instance.Child!, 2);
        Assert.Equal("y-3", cut.Markup);

        cut.Render(p =>
            p.Add(c => c.Keys, [default(ImmutableArray<int>)]).Add(c => c.ChildText, "z")
        );
        AssertChildRenders(cut.Instance.Child!, 3);
        Assert.Equal("z-5", cut.Markup);
    }

    [Fact]
    public void SelfReferencingCollection_DoesNotOverflowTheStack()
    {
        var first = new List<object>();
        first.Add(first);

        var second = new List<object>();
        second.Add(second);

        var cut = Render<MemoParent>(p => p.Add(c => c.Keys, [first]).Add(c => c.ChildText, "x"));

        cut.Render(p => p.Add(c => c.Keys, [second]).Add(c => c.ChildText, "y"));

        // After max depth recursive calls, the comparison resolves as not equal so we re-render
        AssertChildRenders(cut.Instance.Child!, 2);
        Assert.Equal("y-2", cut.Markup);
    }

    [Fact]
    public void DeeplyNestedCollections_AreComparedWithinDepthLimit()
    {
        var cut = Render<MemoParent>(p =>
            p.Add(c => c.Keys, [BuildNested(8, "leaf")]).Add(c => c.ChildText, "x")
        );

        cut.Render(p => p.Add(c => c.Keys, [BuildNested(8, "leaf")]).Add(c => c.ChildText, "y"));
        AssertChildRenders(cut.Instance.Child!, 1);
        Assert.Equal("x-1", cut.Markup);

        cut.Render(p => p.Add(c => c.Keys, [BuildNested(8, "changed")]).Add(c => c.ChildText, "y"));
        AssertChildRenders(cut.Instance.Child!, 2);
        Assert.Equal("y-3", cut.Markup);
    }

    [Fact]
    public void NestingBeyondDepthLimit_ReportsChanged()
    {
        var cut = Render<MemoParent>(p =>
            p.Add(c => c.Keys, [BuildNested(31, "leaf")]).Add(c => c.ChildText, "x")
        );

        cut.Render(p => p.Add(c => c.Keys, [BuildNested(31, "leaf")]).Add(c => c.ChildText, "y"));
        AssertChildRenders(cut.Instance.Child!, 1);
        Assert.Equal("x-1", cut.Markup);

        cut.Render(p => p.Add(c => c.Keys, [BuildNested(32, "leaf")]).Add(c => c.ChildText, "y"));
        AssertChildRenders(cut.Instance.Child!, 2);
        Assert.Equal("y-3", cut.Markup);

        // Identical again, but too deep to prove it, so re-renders every time.
        cut.Render(p => p.Add(c => c.Keys, [BuildNested(32, "leaf")]).Add(c => c.ChildText, "z"));
        AssertChildRenders(cut.Instance.Child!, 3);
        Assert.Equal("z-4", cut.Markup);
    }

    [Fact]
    public void ByteArrayKey_IsComparedByValue()
    {
        var cut = Render<MemoParent>(p =>
            p.Add(c => c.Keys, [new byte[] { 1, 2, 3 }]).Add(c => c.ChildText, "x")
        );

        Assert.Equal("x-1", cut.Markup);

        cut.Render(p => p.Add(c => c.Keys, [new byte[] { 1, 2, 3 }]).Add(c => c.ChildText, "y"));
        AssertChildRenders(cut.Instance.Child!, 1);
        Assert.Equal("x-1", cut.Markup);

        cut.Render(p => p.Add(c => c.Keys, [new byte[] { 1, 2, 4 }]).Add(c => c.ChildText, "y"));
        AssertChildRenders(cut.Instance.Child!, 2);
        Assert.Equal("y-3", cut.Markup);
    }

    [Fact]
    public void TimeOnlyArrayKey_IsComparedByValue()
    {
        var cut = Render<MemoParent>(p =>
            p.Add(c => c.Keys, [new[] { new TimeOnly(9, 0) }]).Add(c => c.ChildText, "x")
        );
        Assert.Equal("x-1", cut.Markup);

        cut.Render(p =>
            p.Add(c => c.Keys, [new[] { new TimeOnly(9, 0) }]).Add(c => c.ChildText, "y")
        );
        AssertChildRenders(cut.Instance.Child!, 1);
        Assert.Equal("x-1", cut.Markup);

        cut.Render(p =>
            p.Add(c => c.Keys, [new[] { new TimeOnly(9, 30) }]).Add(c => c.ChildText, "y")
        );
        AssertChildRenders(cut.Instance.Child!, 2);
        Assert.Equal("y-3", cut.Markup);
    }
}
