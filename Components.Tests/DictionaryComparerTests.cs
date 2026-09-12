namespace BlazorMemoire.Components.Tests;

public class DictionaryComparerTests : MemoTestBase
{
    private sealed class OrdinalStringComparer : IEqualityComparer<string>
    {
        public bool Equals(string? x, string? y) => string.Equals(x, y, StringComparison.Ordinal);

        public int GetHashCode(string value) => StringComparer.Ordinal.GetHashCode(value);
    }

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
    public void DifferentDictionaryComparers_ReturnFalseInBothDirections()
    {
        var ordinal = new Dictionary<string, int>(StringComparer.Ordinal) { ["a"] = 1 };
        var ignoreCase = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["A"] = 1,
        };

        Assert.Equal((true, false), InvokeFastDictionaryEqual(ordinal, ignoreCase));
        Assert.Equal((true, false), InvokeFastDictionaryEqual(ignoreCase, ordinal));
    }

    [Fact]
    public void SharedCaseInsensitiveComparer_UsesItsKeySemantics()
    {
        var oldDictionary = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["a"] = 1,
        };
        var newDictionary = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["A"] = 1,
        };

        Assert.Equal((true, true), InvokeFastDictionaryEqual(oldDictionary, newDictionary));
    }

    [Fact]
    public void EquivalentButDifferentComparerInstances_ReturnFalse()
    {
        var oldDictionary = new Dictionary<string, int>(new OrdinalStringComparer()) { ["a"] = 1 };
        var newDictionary = new Dictionary<string, int>(new OrdinalStringComparer()) { ["a"] = 1 };

        Assert.Equal((true, false), InvokeFastDictionaryEqual(oldDictionary, newDictionary));
    }

    [Fact]
    public void ObjectDictionariesWithDifferentComparers_ReturnFalse()
    {
        var oldDictionary = new Dictionary<string, object?>(StringComparer.Ordinal) { ["a"] = 1 };
        var newDictionary = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["a"] = 1,
        };

        Assert.Equal((true, false), InvokeFastDictionaryEqual(oldDictionary, newDictionary));
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
