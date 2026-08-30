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

        Check("string", "a", "b");
        Check("int", 1, 2);
        Check("long", 1L, 2L);
        Check("double", 1.5d, 2.5d);
        Check("float", 1.5f, 2.5f);
        Check("decimal", 1.5m, 2.5m);
        Check("bool", true, false);
        Check("byte", (byte)1, (byte)2);
        Check("Guid", Guid.Empty, Guid.Parse("6f9619ff-8b86-d011-b42d-00c04fc964ff"));
        Check("DateTime", new DateTime(2024, 1, 1), new DateTime(2024, 1, 2));
        Check(
            "DateTimeOffset",
            new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2024, 1, 2, 0, 0, 0, TimeSpan.Zero)
        );
        Check("DateOnly", new DateOnly(2024, 1, 1), new DateOnly(2024, 1, 2));
        Check("TimeOnly", new TimeOnly(9, 0), new TimeOnly(9, 30));
        Check("TimeSpan", TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(2));

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

        // Nullables have no IEquatable<T>, so they fall through to the general path.
        Assert.False(InvokeFastSequenceEqual(new int?[] { 1 }, new int?[] { 1 }).Handled);
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
