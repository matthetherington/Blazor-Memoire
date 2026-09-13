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
        void CheckValue<T>(T first, T second)
        {
            Assert.Equal(
                (true, true),
                InvokeFastDictionaryEqual(
                    new Dictionary<string, T>(StringComparer.Ordinal) { ["a"] = first },
                    new Dictionary<string, T>(StringComparer.Ordinal) { ["a"] = first }
                )
            );
            Assert.Equal(
                (true, false),
                InvokeFastDictionaryEqual(
                    new Dictionary<string, T>(StringComparer.Ordinal) { ["a"] = first },
                    new Dictionary<string, T>(StringComparer.Ordinal) { ["a"] = second }
                )
            );
        }

        void CheckNullable<T>(T first, T second)
            where T : struct
        {
            CheckValue<T?>(first, second);
            CheckValue<T?>(first, null);
        }

        CheckValue("a", "b");
        CheckValue(1, 2);
        CheckValue(1L, 2L);
        CheckValue(1.5d, 2.5d);
        CheckValue(1.5f, 2.5f);
        CheckValue(1.5m, 2.5m);
        CheckValue(true, false);
        CheckValue((byte)1, (byte)2);
        CheckValue(Guid.Empty, Guid.Parse("ba8a58ab-61bd-476e-9b05-2403b51dd2e5"));
        CheckValue(new DateTime(2026, 1, 1), new DateTime(2026, 1, 2));
        CheckValue(
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero)
        );
        CheckValue(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 2));
        CheckValue(new TimeOnly(9, 0), new TimeOnly(9, 30));
        CheckValue(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(2));

        CheckNullable(1, 2);
        CheckNullable(1L, 2L);
        CheckNullable(1.5d, 2.5d);
        CheckNullable(1.5f, 2.5f);
        CheckNullable(1.5m, 2.5m);
        CheckNullable(true, false);
        CheckNullable((byte)1, (byte)2);
        CheckNullable(Guid.Empty, Guid.Parse("ba8a58ab-61bd-476e-9b05-2403b51dd2e5"));
        CheckNullable(new DateTime(2026, 1, 1), new DateTime(2026, 1, 2));
        CheckNullable(
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero)
        );
        CheckNullable(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 2));
        CheckNullable(new TimeOnly(9, 0), new TimeOnly(9, 30));
        CheckNullable(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(2));

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
            new Dictionary<string, NestedRecord>(StringComparer.Ordinal)
            {
                ["a"] = new NestedRecord("a", 1),
            },
            new Dictionary<string, NestedRecord>(StringComparer.Ordinal)
            {
                ["a"] = new NestedRecord("a", 1),
            }
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
    public void NullableDictionaryDeepComparison_DoesNotAllocate()
    {
        var oldDictionary = new Dictionary<string, decimal?>(StringComparer.Ordinal)
        {
            ["a"] = 1.5m,
            ["b"] = null,
        };
        var newDictionary = new Dictionary<string, decimal?>(StringComparer.Ordinal)
        {
            ["a"] = 1.5m,
            ["b"] = null,
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
