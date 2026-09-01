using BlazorMemoire.Components;
using Bunit;

namespace BlazorMemoire.Components.Tests;

public class MemoArrayKeyTests : MemoTestBase
{
    [Fact]
    public void SameArrayKey_Freezes()
    {
        var cut = Render<MemoParent>(p =>
            p.Add(c => c.Deep, true)
                .Add(c => c.Keys, [new[] { "x", "y", "z" }])
                .Add(c => c.ChildText, "a")
        );

        cut.Render(p =>
            p.Add(c => c.Deep, true)
                .Add(c => c.Keys, [new[] { "x", "y", "z" }])
                .Add(c => c.ChildText, "b")
        );

        AssertChildRenders(cut.Instance.Child!, 1);
    }

    [Fact]
    public void DifferentArrayKey_ReRenders()
    {
        var cut = Render<MemoParent>(p =>
            p.Add(c => c.Deep, true)
                .Add(c => c.Keys, [new[] { "x", "y" }])
                .Add(c => c.ChildText, "a")
        );

        cut.Render(p =>
            p.Add(c => c.Deep, true)
                .Add(c => c.Keys, [new[] { "x", "changed" }])
                .Add(c => c.ChildText, "a")
        );

        AssertChildRenders(cut.Instance.Child!, 2);
    }

    [Fact]
    public void SameValueTypeArrayKey_Freezes()
    {
        var cut = Render<MemoParent>(p =>
            p.Add(c => c.Deep, true)
                .Add(c => c.Keys, [new[] { 1, 2, 3 }])
                .Add(c => c.ChildText, "a")
        );

        cut.Render(p =>
            p.Add(c => c.Deep, true)
                .Add(c => c.Keys, [new[] { 1, 2, 3 }])
                .Add(c => c.ChildText, "b")
        );

        AssertChildRenders(cut.Instance.Child!, 1);
    }

    [Fact]
    public void ListAndArrayWithSameContents_Freezes()
    {
        // The fast paths only fire when both sides are the same shape; a List and an array
        // with the same contents must still fall through to the general element comparison.
        var cut = Render<MemoParent>(p =>
            p.Add(c => c.Deep, true)
                .Add(c => c.Keys, [new List<int> { 1, 2, 3 }])
                .Add(c => c.ChildText, "a")
        );

        cut.Render(p =>
            p.Add(c => c.Deep, true)
                .Add(c => c.Keys, [new[] { 1, 2, 3 }])
                .Add(c => c.ChildText, "b")
        );

        AssertChildRenders(cut.Instance.Child!, 1);
    }
}
