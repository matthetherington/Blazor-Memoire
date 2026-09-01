using Bunit;

namespace BlazorMemoire.Components.Tests;

public class MemoNestedCollectionsTests : MemoTestBase
{
    [Fact]
    public void SameNestedCollectionKey_Freezes()
    {
        var cut = Render<MemoParent>(p =>
            p.Add(c => c.Deep, true)
                .Add(
                    c => c.Keys,
                    [
                        new List<List<string>>
                        {
                            new List<string> { "a", "b" },
                            new List<string> { "c" },
                        },
                    ]
                )
                .Add(c => c.ChildText, "x")
        );

        cut.Render(p =>
            p.Add(c => c.Deep, true)
                .Add(
                    c => c.Keys,
                    [
                        new List<List<string>>
                        {
                            new List<string> { "a", "b" },
                            new List<string> { "c" },
                        },
                    ]
                )
                .Add(c => c.ChildText, "y")
        );

        AssertChildRenders(cut.Instance.Child!, 1);
    }

    [Fact]
    public void DifferentNestedCollectionKey_ReRenders()
    {
        var cut = Render<MemoParent>(p =>
            p.Add(c => c.Deep, true)
                .Add(
                    c => c.Keys,
                    [
                        new List<List<string>>
                        {
                            new List<string> { "a", "b" },
                            new List<string> { "c" },
                        },
                    ]
                )
                .Add(c => c.ChildText, "x")
        );

        cut.Render(p =>
            p.Add(c => c.Deep, true)
                .Add(
                    c => c.Keys,
                    [
                        new List<List<string>>
                        {
                            new List<string> { "a", "b" },
                            new List<string> { "d" },
                        },
                    ]
                )
                .Add(c => c.ChildText, "x")
        );

        AssertChildRenders(cut.Instance.Child!, 2);
    }
}
