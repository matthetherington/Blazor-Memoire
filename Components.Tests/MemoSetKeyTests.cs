using Bunit;

namespace BlazorMemoire.Components.Tests;

public class MemoSetKeyTests : MemoTestBase
{
    [Fact]
    public void SameSetKey_DifferentOrder_Freezes()
    {
        var cut = Render<MemoParent>(p =>
            p.Add(c => c.Deep, true)
                .Add(c => c.Keys, [new HashSet<int> { 1, 2, 3 }])
                .Add(c => c.ChildText, "a")
        );

        cut.Render(p =>
            p.Add(c => c.Deep, true)
                .Add(c => c.Keys, [new HashSet<int> { 3, 1, 2 }])
                .Add(c => c.ChildText, "b")
        );

        AssertChildRenders(cut.Instance.Child!, 1);
    }

    [Fact]
    public void DifferentSetKey_ReRenders()
    {
        var cut = Render<MemoParent>(p =>
            p.Add(c => c.Deep, true)
                .Add(c => c.Keys, [new HashSet<int> { 1, 2, 3 }])
                .Add(c => c.ChildText, "a")
        );

        cut.Render(p =>
            p.Add(c => c.Deep, true)
                .Add(c => c.Keys, [new HashSet<int> { 1, 2, 4 }])
                .Add(c => c.ChildText, "a")
        );

        AssertChildRenders(cut.Instance.Child!, 2);
    }

    [Fact]
    public void DifferentSetSize_ReRenders()
    {
        var cut = Render<MemoParent>(p =>
            p.Add(c => c.Deep, true)
                .Add(c => c.Keys, [new HashSet<int> { 1, 2 }])
                .Add(c => c.ChildText, "a")
        );

        cut.Render(p =>
            p.Add(c => c.Deep, true)
                .Add(c => c.Keys, [new HashSet<int> { 1, 2, 3 }])
                .Add(c => c.ChildText, "a")
        );

        AssertChildRenders(cut.Instance.Child!, 2);
    }

    [Fact]
    public void EmptySetKey_Freezes()
    {
        var cut = Render<MemoParent>(p =>
            p.Add(c => c.Deep, true)
                .Add(c => c.Keys, [new HashSet<int>()])
                .Add(c => c.ChildText, "a")
        );

        cut.Render(p =>
            p.Add(c => c.Deep, true)
                .Add(c => c.Keys, [new HashSet<int>()])
                .Add(c => c.ChildText, "b")
        );

        AssertChildRenders(cut.Instance.Child!, 1);
    }

    [Fact]
    public void SortedSetInterfaceKey_ComparesUnordered()
    {
        var cut = Render<MemoParent>(p =>
            p.Add(c => c.Deep, true)
                .Add(c => c.Keys, [new SortedSet<string>(StringComparer.Ordinal) { "a", "b" }])
                .Add(c => c.ChildText, "x")
        );

        cut.Render(p =>
            p.Add(c => c.Deep, true)
                .Add(c => c.Keys, [new HashSet<string>(StringComparer.Ordinal) { "b", "a" }])
                .Add(c => c.ChildText, "y")
        );
        AssertChildRenders(cut.Instance.Child!, 1);

        cut.Render(p =>
            p.Add(c => c.Deep, true)
                .Add(c => c.Keys, [new HashSet<string>(StringComparer.Ordinal) { "b", "c" }])
                .Add(c => c.ChildText, "y")
        );
        AssertChildRenders(cut.Instance.Child!, 2);
    }

    [Fact]
    public void SetOfRecords_ComparesByValue()
    {
        var cut = Render<MemoParent>(p =>
            p.Add(c => c.Deep, true)
                .Add(c => c.Keys, [new HashSet<NestedRecord> { new("Alice", 1), new("Bob", 2) }])
                .Add(c => c.ChildText, "x")
        );

        cut.Render(p =>
            p.Add(c => c.Deep, true)
                .Add(c => c.Keys, [new HashSet<NestedRecord> { new("Bob", 2), new("Alice", 1) }])
                .Add(c => c.ChildText, "y")
        );
        AssertChildRenders(cut.Instance.Child!, 1);

        cut.Render(p =>
            p.Add(c => c.Deep, true)
                .Add(c => c.Keys, [new HashSet<NestedRecord> { new("Bob", 2), new("Alice", 99) }])
                .Add(c => c.ChildText, "y")
        );
        AssertChildRenders(cut.Instance.Child!, 2);
    }

    [Fact]
    public void SetLargerThanInitialBuffer_ComparesCorrectly()
    {
        var cut = Render<MemoParent>(p =>
            p.Add(c => c.Deep, true).Add(c => c.Keys, [BuildPeople(20)]).Add(c => c.ChildText, "x")
        );

        cut.Render(p =>
            p.Add(c => c.Deep, true).Add(c => c.Keys, [BuildPeople(20)]).Add(c => c.ChildText, "y")
        );
        AssertChildRenders(cut.Instance.Child!, 1);

        cut.Render(p =>
            p.Add(c => c.Deep, true)
                .Add(c => c.Keys, [BuildPeople(20, changedAt: 19)])
                .Add(c => c.ChildText, "y")
        );
        AssertChildRenders(cut.Instance.Child!, 2);
    }

    [Fact]
    public void SetLargerThanStackAllocLimit_ComparesCorrectly()
    {
        var cut = Render<MemoParent>(p =>
            p.Add(c => c.Deep, true).Add(c => c.Keys, [BuildPeople(100)]).Add(c => c.ChildText, "x")
        );

        cut.Render(p =>
            p.Add(c => c.Deep, true).Add(c => c.Keys, [BuildPeople(100)]).Add(c => c.ChildText, "y")
        );
        AssertChildRenders(cut.Instance.Child!, 1);

        cut.Render(p =>
            p.Add(c => c.Deep, true)
                .Add(c => c.Keys, [BuildPeople(100, changedAt: 0)])
                .Add(c => c.ChildText, "y")
        );
        AssertChildRenders(cut.Instance.Child!, 2);
    }

    [Fact]
    public void CaseInsensitiveSet_ComparesElementsStructurally()
    {
        var cut = Render<MemoParent>(p =>
            p.Add(c => c.Deep, true)
                .Add(c => c.Keys, [new HashSet<string>(["a"], StringComparer.OrdinalIgnoreCase)])
                .Add(c => c.ChildText, "x")
        );

        cut.Render(p =>
            p.Add(c => c.Deep, true)
                .Add(c => c.Keys, [new HashSet<string>(["A"], StringComparer.OrdinalIgnoreCase)])
                .Add(c => c.ChildText, "x")
        );
        AssertChildRenders(cut.Instance.Child!, 2);

        // Same value again — frozen
        cut.Render(p =>
            p.Add(c => c.Deep, true)
                .Add(c => c.Keys, [new HashSet<string>(["A"], StringComparer.OrdinalIgnoreCase)])
                .Add(c => c.ChildText, "x")
        );
        AssertChildRenders(cut.Instance.Child!, 2);
    }

    [Fact]
    public void OrdinalSet_TakesTheFastPath()
    {
        var cut = Render<MemoParent>(p =>
            p.Add(c => c.Deep, true)
                .Add(c => c.Keys, [new HashSet<string>(["a", "b"], StringComparer.Ordinal)])
                .Add(c => c.ChildText, "x")
        );

        cut.Render(p =>
            p.Add(c => c.Deep, true)
                .Add(c => c.Keys, [new HashSet<string>(["b", "a"], StringComparer.Ordinal)])
                .Add(c => c.ChildText, "y")
        );
        AssertChildRenders(cut.Instance.Child!, 1);

        cut.Render(p =>
            p.Add(c => c.Deep, true)
                .Add(c => c.Keys, [new HashSet<string>(["b", "c"], StringComparer.Ordinal)])
                .Add(c => c.ChildText, "y")
        );
        AssertChildRenders(cut.Instance.Child!, 2);
    }

    [Fact]
    public void LargeSetReordered_Freezes()
    {
        var people = BuildPeople(100).ToList();

        var cut = Render<MemoParent>(p =>
            p.Add(c => c.Deep, true)
                .Add(c => c.Keys, [new HashSet<NestedRecord>(people)])
                .Add(c => c.ChildText, "x")
        );

        people.Reverse();
        cut.Render(p =>
            p.Add(c => c.Deep, true)
                .Add(c => c.Keys, [new HashSet<NestedRecord>(people)])
                .Add(c => c.ChildText, "y")
        );

        AssertChildRenders(cut.Instance.Child!, 1);
    }
}
