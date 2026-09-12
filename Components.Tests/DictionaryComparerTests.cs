namespace BlazorMemoire.Components.Tests;

public class DictionaryComparerTests : MemoTestBase
{
    [Fact]
    public void SupportedDictionaryShapes_UseFastPath()
    {
        Assert.Equal(
            (true, true),
            InvokeFastDictionaryEqual(
                new Dictionary<string, int>(StringComparer.Ordinal) { ["a"] = 1 },
                new Dictionary<string, int>(StringComparer.Ordinal) { ["a"] = 1 }
            )
        );
        Assert.Equal(
            (true, true),
            InvokeFastDictionaryEqual(
                new Dictionary<int, string> { [1] = "a" },
                new Dictionary<int, string> { [1] = "a" }
            )
        );
        Assert.Equal(
            (true, true),
            InvokeFastDictionaryEqual(
                new Dictionary<int, int> { [1] = 2 },
                new Dictionary<int, int> { [1] = 2 }
            )
        );
        Assert.Equal(
            (true, true),
            InvokeFastDictionaryEqual(
                new Dictionary<string, string>(StringComparer.Ordinal) { ["a"] = "b" },
                new Dictionary<string, string>(StringComparer.Ordinal) { ["a"] = "b" }
            )
        );
        Assert.Equal(
            (true, true),
            InvokeFastDictionaryEqual(
                new Dictionary<string, object?>(StringComparer.Ordinal) { ["a"] = 1 },
                new Dictionary<string, object?>(StringComparer.Ordinal) { ["a"] = 1 }
            )
        );
    }

    [Fact]
    public void DifferentValue_ReturnsFalseFromFastPath()
    {
        var result = InvokeFastDictionaryEqual(
            new Dictionary<string, int>(StringComparer.Ordinal) { ["a"] = 1 },
            new Dictionary<string, int>(StringComparer.Ordinal) { ["a"] = 2 }
        );

        Assert.Equal((true, false), result);
    }

    [Fact]
    public void IncomingDictionaryComparer_IsUsed()
    {
        var result = InvokeFastDictionaryEqual(
            new Dictionary<string, int>(StringComparer.Ordinal) { ["a"] = 1 },
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) { ["A"] = 1 }
        );

        Assert.Equal((true, true), result);
    }

    [Fact]
    public void UnsupportedDictionaryShape_UsesFallback()
    {
        var result = InvokeFastDictionaryEqual(
            new Dictionary<string, decimal>(StringComparer.Ordinal) { ["a"] = 1m },
            new Dictionary<string, decimal>(StringComparer.Ordinal) { ["a"] = 1m }
        );

        Assert.Equal((false, false), result);
    }

    [Fact]
    public void ObjectDictionaryValues_AreComparedRecursively()
    {
        var oldDictionary = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["class"] = new List<string> { "first", "second" },
        };
        var equalDictionary = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["class"] = new List<string> { "first", "second" },
        };
        var changedDictionary = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["class"] = new List<string> { "first", "changed" },
        };

        Assert.True(ValueComparer.ValuesEqual(oldDictionary, equalDictionary, 0));
        Assert.False(ValueComparer.ValuesEqual(oldDictionary, changedDictionary, 0));
    }

    [Fact]
    public void PrimitiveDictionaryDeepComparison_DoesNotAllocate()
    {
        var oldDictionary = Enumerable
            .Range(0, 16)
            .ToDictionary(i => $"k{i}", i => i, StringComparer.Ordinal);
        var newDictionary = Enumerable
            .Range(0, 16)
            .ToDictionary(i => $"k{i}", i => i, StringComparer.Ordinal);

        for (var i = 0; i < 100; i++)
        {
            ValueComparer.ValuesEqual(oldDictionary, newDictionary, 0);
        }

        var before = GC.GetAllocatedBytesForCurrentThread();
        var equal = false;

        for (var i = 0; i < 1_000; i++)
        {
            equal = ValueComparer.ValuesEqual(oldDictionary, newDictionary, 0);
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.True(equal);
        Assert.Equal(0, allocated);
    }

    [Fact]
    public void AttributeDictionaryDeepComparison_DoesNotAllocate()
    {
        var oldDictionary = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["class"] = "button",
            ["tabindex"] = 0,
            ["hidden"] = false,
        };
        var newDictionary = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["class"] = "button",
            ["tabindex"] = 0,
            ["hidden"] = false,
        };

        for (var i = 0; i < 100; i++)
        {
            ValueComparer.ValuesEqual(oldDictionary, newDictionary, 0);
        }

        var before = GC.GetAllocatedBytesForCurrentThread();
        var equal = false;

        for (var i = 0; i < 1_000; i++)
        {
            equal = ValueComparer.ValuesEqual(oldDictionary, newDictionary, 0);
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.True(equal);
        Assert.Equal(0, allocated);
    }
}
