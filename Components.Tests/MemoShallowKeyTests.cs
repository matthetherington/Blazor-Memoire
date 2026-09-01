using Bunit;

namespace BlazorMemoire.Components.Tests;

/// <summary>
/// Covers the default (non-deep) comparison mode, where key elements are compared with a
/// per-element null-safe <see cref="object.Equals(object?)"/>. Reference-type collections
/// therefore compare by reference: equal-content-but-distinct instances count as a change.
/// </summary>
public class MemoShallowKeyTests : MemoTestBase
{
    [Fact]
    public void EqualContentDistinctLists_ReRender_ByDefault()
    {
        // Two List<int> instances with identical contents are not reference-equal, and
        // List<T> does not override Equals, so the default mode treats them as changed.
        var cut = Render<MemoParent>(p =>
            p.Add(c => c.Keys, [new List<int> { 1, 2, 3 }]).Add(c => c.ChildText, "a")
        );

        cut.Render(p => p.Add(c => c.Keys, [new List<int> { 1, 2, 3 }]).Add(c => c.ChildText, "b"));

        AssertChildRenders(cut.Instance.Child!, 2);
    }

    [Fact]
    public void SameListReference_Freezes_ByDefault()
    {
        var list = new List<int> { 1, 2, 3 };

        var cut = Render<MemoParent>(p => p.Add(c => c.Keys, [list]).Add(c => c.ChildText, "a"));

        cut.Render(p => p.Add(c => c.Keys, [list]).Add(c => c.ChildText, "b"));

        AssertChildRenders(cut.Instance.Child!, 1);
    }

    [Fact]
    public void EqualContentDistinctLists_Freeze_WhenDeep()
    {
        // Opting into Deep switches to structural comparison, so equal-content lists freeze.
        var cut = Render<MemoParent>(p =>
            p.Add(c => c.Deep, true)
                .Add(c => c.Keys, [new List<int> { 1, 2, 3 }])
                .Add(c => c.ChildText, "a")
        );

        cut.Render(p =>
            p.Add(c => c.Deep, true)
                .Add(c => c.Keys, [new List<int> { 1, 2, 3 }])
                .Add(c => c.ChildText, "b")
        );

        AssertChildRenders(cut.Instance.Child!, 1);
    }

    [Fact]
    public void ScalarKeys_Freeze_ByDefault()
    {
        // Primitives override Equals by value, so the default mode freezes equal scalars.
        var cut = Render<MemoParent>(p =>
            p.Add(c => c.Keys, [1, "two", 3.0]).Add(c => c.ChildText, "a")
        );

        cut.Render(p => p.Add(c => c.Keys, [1, "two", 3.0]).Add(c => c.ChildText, "b"));

        AssertChildRenders(cut.Instance.Child!, 1);
    }
}
