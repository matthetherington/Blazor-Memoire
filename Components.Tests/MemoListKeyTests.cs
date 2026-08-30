using Bunit;

namespace BlazorMemoire.Components.Tests;

public class MemoListKeyTests : MemoTestBase
{
    [Fact]
    public void SameListKey_Freezes()
    {
        var cut = Render<MemoParent>(p =>
            p.Add(c => c.Keys, [new List<string> { "a", "b", "c" }]).Add(c => c.ChildText, "x")
        );

        cut.Render(p =>
            p.Add(c => c.Keys, [new List<string> { "a", "b", "c" }]).Add(c => c.ChildText, "y")
        );

        AssertChildRenders(cut.Instance.Child!, 1);
    }

    [Fact]
    public void DifferentListKey_ReRenders()
    {
        var cut = Render<MemoParent>(p =>
            p.Add(c => c.Keys, [new List<string> { "a", "b" }]).Add(c => c.ChildText, "x")
        );

        cut.Render(p =>
            p.Add(c => c.Keys, [new List<string> { "a", "c" }]).Add(c => c.ChildText, "x")
        );

        AssertChildRenders(cut.Instance.Child!, 2);
    }

    [Fact]
    public void DifferentListLength_ReRenders()
    {
        var cut = Render<MemoParent>(p =>
            p.Add(c => c.Keys, [new List<string> { "a" }]).Add(c => c.ChildText, "x")
        );

        cut.Render(p =>
            p.Add(c => c.Keys, [new List<string> { "a", "b" }]).Add(c => c.ChildText, "x")
        );

        AssertChildRenders(cut.Instance.Child!, 2);
    }

    [Fact]
    public void ReorderedListKey_ReRenders()
    {
        var cut = Render<MemoParent>(p =>
            p.Add(c => c.Keys, [new List<string> { "a", "b" }]).Add(c => c.ChildText, "x")
        );

        cut.Render(p =>
            p.Add(c => c.Keys, [new List<string> { "b", "a" }]).Add(c => c.ChildText, "x")
        );

        AssertChildRenders(cut.Instance.Child!, 2);
    }

    [Fact]
    public void SameValueTypeListKey_Freezes()
    {
        var cut = Render<MemoParent>(p =>
            p.Add(c => c.Keys, [new List<int> { 1, 2, 3 }]).Add(c => c.ChildText, "x")
        );

        cut.Render(p => p.Add(c => c.Keys, [new List<int> { 1, 2, 3 }]).Add(c => c.ChildText, "y"));

        AssertChildRenders(cut.Instance.Child!, 1);
    }

    [Fact]
    public void EmptyListKey_Freezes()
    {
        var cut = Render<MemoParent>(p =>
            p.Add(c => c.Keys, [new List<string>()]).Add(c => c.ChildText, "x")
        );

        cut.Render(p => p.Add(c => c.Keys, [new List<string>()]).Add(c => c.ChildText, "y"));

        AssertChildRenders(cut.Instance.Child!, 1);
    }

    [Fact]
    public void SameCollectionReference_Freezes()
    {
        var list = new List<string> { "a", "b" };

        var cut = Render<MemoParent>(p => p.Add(c => c.Keys, [list]).Add(c => c.ChildText, "x"));

        cut.Render(p => p.Add(c => c.Keys, [list]).Add(c => c.ChildText, "y"));

        AssertChildRenders(cut.Instance.Child!, 1);
    }

    [Fact]
    public void MutatedCollectionReference_IsNotDetected()
    {
        // Mutating a collection in place leaves the reference unchanged, so the change is
        // invisible and the subtree stays frozen. Callers must pass a new instance.
        var list = new List<string> { "a", "b" };

        var cut = Render<MemoParent>(p => p.Add(c => c.Keys, [list]).Add(c => c.ChildText, "x"));

        list.Add("c");
        cut.Render(p => p.Add(c => c.Keys, [list]).Add(c => c.ChildText, "y"));

        AssertChildRenders(cut.Instance.Child!, 1);
    }

    [Fact]
    public void CollectionsContainingNulls_AreCompared()
    {
        var cut = Render<MemoParent>(p =>
            p.Add(c => c.Keys, [new List<string?> { "a", null, "c" }]).Add(c => c.ChildText, "x")
        );

        cut.Render(p =>
            p.Add(c => c.Keys, [new List<string?> { "a", null, "c" }]).Add(c => c.ChildText, "y")
        );
        AssertChildRenders(cut.Instance.Child!, 1);

        cut.Render(p =>
            p.Add(c => c.Keys, [new List<string?> { "a", "b", "c" }]).Add(c => c.ChildText, "y")
        );
        AssertChildRenders(cut.Instance.Child!, 2);
    }

    [Fact]
    public void CollectionReplacedByScalar_ReRenders()
    {
        var cut = Render<MemoParent>(p =>
            p.Add(c => c.Keys, [new List<int> { 1 }]).Add(c => c.ChildText, "x")
        );

        cut.Render(p => p.Add(c => c.Keys, [1]).Add(c => c.ChildText, "x"));

        AssertChildRenders(cut.Instance.Child!, 2);
    }
}
