namespace BlazorMemoire.Components.Tests;

public class MemoFastSequenceTests : MemoTestBase
{
    [Fact]
    public void FastSequencePath_CoversEveryElementTypeItClaimsTo()
    {
        var failures = new List<string>();

        void Verify(string label, (bool Handled, bool Result) actual, bool expected)
        {
            if (!actual.Handled)
            {
                failures.Add($"{label}: not routed to the fast path");
            }
            else if (actual.Result != expected)
            {
                failures.Add($"{label}: expected {expected}, got {actual.Result}");
            }
        }

        void Check<T>(string name, T first, T second)
            where T : IEquatable<T>
        {
            Verify(
                $"{name}[] equal",
                InvokeFastSequenceEqual(new[] { first }, new[] { first }),
                true
            );
            Verify(
                $"{name}[] differing",
                InvokeFastSequenceEqual(new[] { first }, new[] { second }),
                false
            );
            Verify(
                $"List<{name}> equal",
                InvokeFastSequenceEqual(new List<T> { first }, new List<T> { first }),
                true
            );
            Verify(
                $"List<{name}> differing",
                InvokeFastSequenceEqual(new List<T> { first }, new List<T> { second }),
                false
            );
        }

        void CheckNullable<T>(string name, T first, T second)
            where T : struct
        {
            Verify(
                $"{name}?[] equal",
                InvokeFastSequenceEqual(new T?[] { first, null }, new T?[] { first, null }),
                true
            );
            Verify(
                $"{name}?[] differing",
                InvokeFastSequenceEqual(new T?[] { first, null }, new T?[] { second, null }),
                false
            );
            Verify(
                $"List<{name}?> equal",
                InvokeFastSequenceEqual(new List<T?> { first, null }, new List<T?> { first, null }),
                true
            );
            Verify(
                $"List<{name}?> differing",
                InvokeFastSequenceEqual(
                    new List<T?> { first, null },
                    new List<T?> { first, second }
                ),
                false
            );
        }

        Check("string", "a", "b");
        Check("int", 1, 2);
        Check("long", 1L, 2L);
        Check("double", 1.5d, 2.5d);
        Check("float", 1.5f, 2.5f);
        Check("decimal", 1.5m, 2.5m);
        Check("bool", true, false);
        Check("byte", (byte)1, (byte)2);
        Check("Guid", Guid.Empty, Guid.Parse("ba8a58ab-61bd-476e-9b05-2403b51dd2e5"));
        Check("DateTime", new DateTime(2026, 1, 1), new DateTime(2026, 1, 2));
        Check(
            "DateTimeOffset",
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero)
        );
        Check("DateOnly", new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 2));
        Check("TimeOnly", new TimeOnly(9, 0), new TimeOnly(9, 30));
        Check("TimeSpan", TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(2));

        CheckNullable("int", 1, 2);
        CheckNullable("long", 1L, 2L);
        CheckNullable("double", 1.5d, 2.5d);
        CheckNullable("float", 1.5f, 2.5f);
        CheckNullable("decimal", 1.5m, 2.5m);
        CheckNullable("bool", true, false);
        CheckNullable("byte", (byte)1, (byte)2);
        CheckNullable("Guid", Guid.Empty, Guid.Parse("ba8a58ab-61bd-476e-9b05-2403b51dd2e5"));
        CheckNullable("DateTime", new DateTime(2026, 1, 1), new DateTime(2026, 1, 2));
        CheckNullable(
            "DateTimeOffset",
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero)
        );
        CheckNullable("DateOnly", new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 2));
        CheckNullable("TimeOnly", new TimeOnly(9, 0), new TimeOnly(9, 30));
        CheckNullable("TimeSpan", TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(2));

        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }

    [Fact]
    public void FastSequencePath_DeclinesTypesThatNeedRecursion()
    {
        Assert.False(
            InvokeFastSequenceEqual(
                new List<NestedRecord> { new("a", 1) },
                new List<NestedRecord> { new("a", 1) }
            ).Handled
        );
    }

    [Fact]
    public void NullableSequenceDeepComparison_DoesNotAllocate()
    {
        int?[] oldArray = [1, null, 3];
        int?[] newArray = [1, null, 3];
        var oldList = new List<int?> { 1, null, 3 };
        var newList = new List<int?> { 1, null, 3 };

        for (var i = 0; i < 100; i++)
        {
            ValueComparer.ValuesEqual(oldArray, newArray, 0);
            ValueComparer.ValuesEqual(oldList, newList, 0);
        }

        var before = GC.GetAllocatedBytesForCurrentThread();
        var equal = false;

        for (var i = 0; i < 1_000; i++)
        {
            equal =
                ValueComparer.ValuesEqual(oldArray, newArray, 0)
                && ValueComparer.ValuesEqual(oldList, newList, 0);
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.True(equal);
        Assert.Equal(0, allocated);
    }

    [Fact]
    public void FastSequencePath_TakesEnumArraysButNotEnumLists()
    {
        // Enum arrays are assignment-compatible with arrays of their underlying type
        // (ECMA-335 I.8.7.1), so DayOfWeek[] matches `case int[]`.
        var equal = InvokeFastSequenceEqual(new[] { DayOfWeek.Monday }, new[] { DayOfWeek.Monday });
        Assert.True(equal.Handled);
        Assert.True(equal.Result);

        var differing = InvokeFastSequenceEqual(
            new[] { DayOfWeek.Monday },
            new[] { DayOfWeek.Tuesday }
        );
        Assert.True(differing.Handled);
        Assert.False(differing.Result);

        Assert.False(
            InvokeFastSequenceEqual(
                new List<DayOfWeek> { DayOfWeek.Monday },
                new List<DayOfWeek> { DayOfWeek.Monday }
            ).Handled
        );
    }
}
