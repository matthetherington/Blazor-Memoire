using Bunit;

namespace BlazorMemoire.Components.Tests;

public class MemoScalarKeyTests : MemoTestBase
{
    [Fact]
    public void SameStringKey_Freezes()
    {
        var cut = Render<MemoParent>(p => p.Add(c => c.Keys, ["hello"]).Add(c => c.ChildText, "a"));

        cut.Render(p => p.Add(c => c.Keys, ["hello"]).Add(c => c.ChildText, "b"));

        AssertChildRenders(cut.Instance.Child!, 1);
    }

    [Fact]
    public void DifferentStringKey_ReRenders()
    {
        var cut = Render<MemoParent>(p => p.Add(c => c.Keys, ["hello"]).Add(c => c.ChildText, "a"));

        cut.Render(p => p.Add(c => c.Keys, ["world"]).Add(c => c.ChildText, "a"));

        AssertChildRenders(cut.Instance.Child!, 2);
    }

    [Fact]
    public void SameIntKey_Freezes()
    {
        var cut = Render<MemoParent>(p => p.Add(c => c.Keys, [42]).Add(c => c.ChildText, "a"));

        cut.Render(p => p.Add(c => c.Keys, [42]).Add(c => c.ChildText, "b"));

        AssertChildRenders(cut.Instance.Child!, 1);
    }

    [Fact]
    public void DifferentIntKey_ReRenders()
    {
        var cut = Render<MemoParent>(p => p.Add(c => c.Keys, [1]).Add(c => c.ChildText, "a"));

        cut.Render(p => p.Add(c => c.Keys, [2]).Add(c => c.ChildText, "a"));

        AssertChildRenders(cut.Instance.Child!, 2);
    }

    [Fact]
    public void NullKeyToValue_ReRenders()
    {
        var cut = Render<MemoParent>(p => p.Add(c => c.Keys, [null]).Add(c => c.ChildText, "a"));

        cut.Render(p => p.Add(c => c.Keys, ["hello"]).Add(c => c.ChildText, "a"));

        AssertChildRenders(cut.Instance.Child!, 2);
    }

    [Fact]
    public void ValueKeyToNull_ReRenders()
    {
        var cut = Render<MemoParent>(p => p.Add(c => c.Keys, ["hello"]).Add(c => c.ChildText, "a"));

        cut.Render(p => p.Add(c => c.Keys, [null]).Add(c => c.ChildText, "a"));

        AssertChildRenders(cut.Instance.Child!, 2);
    }

    [Fact]
    public void BothNullKeys_Freezes()
    {
        var cut = Render<MemoParent>(p => p.Add(c => c.Keys, [null]).Add(c => c.ChildText, "a"));

        cut.Render(p => p.Add(c => c.Keys, [null]).Add(c => c.ChildText, "b"));

        AssertChildRenders(cut.Instance.Child!, 1);
    }

    [Fact]
    public void KeyChangesType_ReRenders()
    {
        var cut = Render<MemoParent>(p => p.Add(c => c.Keys, [1]).Add(c => c.ChildText, "a"));

        cut.Render(p => p.Add(c => c.Keys, ["1"]).Add(c => c.ChildText, "a"));

        AssertChildRenders(cut.Instance.Child!, 2);
    }
}
