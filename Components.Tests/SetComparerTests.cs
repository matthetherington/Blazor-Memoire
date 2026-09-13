namespace BlazorMemoire.Components.Tests;

public class SetComparerTests : MemoTestBase
{
    [Fact]
    public void SupportedSetShapes_UseFastPath()
    {
        void Check<T>(T first, T second)
        {
            Assert.Equal(
                (true, true),
                InvokeFastSetEqual(new HashSet<T> { first }, new HashSet<T> { first })
            );
            Assert.Equal(
                (true, false),
                InvokeFastSetEqual(new HashSet<T> { first }, new HashSet<T> { second })
            );
        }

        void CheckNullable<T>(T first, T second)
            where T : struct
        {
            Check<T?>(first, second);
            Check<T?>(first, null);
        }

        Check("a", "b");
        Check(1, 2);
        Check(1L, 2L);
        Check(1.5d, 2.5d);
        Check(1.5f, 2.5f);
        Check(1.5m, 2.5m);
        Check(true, false);
        Check((byte)1, (byte)2);
        Check(Guid.Empty, Guid.Parse("ba8a58ab-61bd-476e-9b05-2403b51dd2e5"));
        Check(new DateTime(2026, 1, 1), new DateTime(2026, 1, 2));
        Check(
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero)
        );
        Check(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 2));
        Check(new TimeOnly(9, 0), new TimeOnly(9, 30));
        Check(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(2));

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
    }

    [Fact]
    public void NullableSetDeepComparison_DoesNotAllocate()
    {
        var value = Guid.Parse("ba8a58ab-61bd-476e-9b05-2403b51dd2e5");
        var oldSet = new HashSet<Guid?> { value, null };
        var newSet = new HashSet<Guid?> { null, value };

        for (var i = 0; i < 100; i++)
        {
            ValueComparer.ValuesEqual(oldSet, newSet, 0);
        }

        var before = GC.GetAllocatedBytesForCurrentThread();
        var equal = false;

        for (var i = 0; i < 1_000; i++)
        {
            equal = ValueComparer.ValuesEqual(oldSet, newSet, 0);
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.True(equal);
        Assert.Equal(0, allocated);
    }
}
