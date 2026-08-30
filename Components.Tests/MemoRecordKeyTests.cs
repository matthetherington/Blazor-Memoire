using Bunit;

namespace BlazorMemoire.Components.Tests;

public class MemoRecordKeyTests : MemoTestBase
{
    [Fact]
    public void SameRecordKey_Freezes()
    {
        var cut = Render<MemoParent>(p =>
            p.Add(c => c.Keys, [new NestedRecord("Alice", 10)]).Add(c => c.ChildText, "a")
        );

        cut.Render(p =>
            p.Add(c => c.Keys, [new NestedRecord("Alice", 10)]).Add(c => c.ChildText, "b")
        );

        AssertChildRenders(cut.Instance.Child!, 1);
    }

    [Fact]
    public void DifferentRecordKey_ReRenders()
    {
        var cut = Render<MemoParent>(p =>
            p.Add(c => c.Keys, [new NestedRecord("Alice", 10)]).Add(c => c.ChildText, "a")
        );

        cut.Render(p =>
            p.Add(c => c.Keys, [new NestedRecord("Bob", 10)]).Add(c => c.ChildText, "a")
        );

        AssertChildRenders(cut.Instance.Child!, 2);
    }

    [Fact]
    public void RecordHoldingList_UsesRecordEquality_NotElementComparison()
    {
        // A collection nested inside a record is compared by the record's own generated
        // Equals, which uses reference equality for the list field.
        var cut = Render<MemoParent>(p =>
            p.Add(c => c.Keys, [new RecordWithList("Alice", [1, 2])]).Add(c => c.ChildText, "a")
        );

        cut.Render(p =>
            p.Add(c => c.Keys, [new RecordWithList("Alice", [1, 2])]).Add(c => c.ChildText, "a")
        );

        AssertChildRenders(cut.Instance.Child!, 2);
    }
}
