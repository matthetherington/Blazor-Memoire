using BenchmarkDotNet.Attributes;
using BlazorMemoire.Components;

namespace BlazorMemoire.Components.Benchmarks;

/// <summary>
/// Group C — the comparison path in isolation, no rendering. Contrasts the default per-key
/// <c>object.Equals</c> (what <c>Memo</c> uses when <c>Deep="false"</c>) with the deep
/// structural <see cref="ValueComparer.ValuesEqual"/> (<c>Deep="true"</c>) across collection
/// shapes and sizes. Inputs are equal-content but distinct instances — the realistic case
/// where the two modes disagree: shallow returns quickly (reference inequality), deep walks
/// the whole structure.
/// </summary>
[MemoryDiagnoser]
public class ComparisonBenchmarks
{
    private sealed record Person(string Name, int Age);

    [Params(4, 64, 1024)]
    public int Size;

    private int[] _arrayA = null!;
    private int[] _arrayB = null!;
    private List<int> _listA = null!;
    private List<int> _listB = null!;
    private List<int?> _nullableListA = null!;
    private List<int?> _nullableListB = null!;
    private List<Person> _recordsA = null!;
    private List<Person> _recordsB = null!;
    private Dictionary<string, int> _dictA = null!;
    private Dictionary<string, int> _dictB = null!;
    private Dictionary<int, int> _intDictA = null!;
    private Dictionary<int, int> _intDictB = null!;
    private Dictionary<string, string> _stringDictA = null!;
    private Dictionary<string, string> _stringDictB = null!;
    private Dictionary<string, object?> _attributeDictA = null!;
    private Dictionary<string, object?> _attributeDictB = null!;
    private Dictionary<string, decimal?> _nullableDictA = null!;
    private Dictionary<string, decimal?> _nullableDictB = null!;
    private HashSet<int> _setA = null!;
    private HashSet<int> _setB = null!;
    private HashSet<Guid?> _nullableSetA = null!;
    private HashSet<Guid?> _nullableSetB = null!;

    [GlobalSetup]
    public void Setup()
    {
        _arrayA = Enumerable.Range(0, Size).ToArray();
        _arrayB = Enumerable.Range(0, Size).ToArray();

        _listA = Enumerable.Range(0, Size).ToList();
        _listB = Enumerable.Range(0, Size).ToList();
        _nullableListA = Enumerable.Range(0, Size).Select(i => (int?)i).ToList();
        _nullableListB = Enumerable.Range(0, Size).Select(i => (int?)i).ToList();

        _recordsA = Enumerable.Range(0, Size).Select(i => new Person($"n{i}", i)).ToList();
        _recordsB = Enumerable.Range(0, Size).Select(i => new Person($"n{i}", i)).ToList();

        _dictA = Enumerable
            .Range(0, Size)
            .ToDictionary(i => $"k{i}", i => i, StringComparer.Ordinal);
        _dictB = Enumerable
            .Range(0, Size)
            .ToDictionary(i => $"k{i}", i => i, StringComparer.Ordinal);

        _intDictA = Enumerable.Range(0, Size).ToDictionary(i => i, i => i);
        _intDictB = Enumerable.Range(0, Size).ToDictionary(i => i, i => i);

        _stringDictA = Enumerable
            .Range(0, Size)
            .ToDictionary(i => $"k{i}", i => $"v{i}", StringComparer.Ordinal);
        _stringDictB = Enumerable
            .Range(0, Size)
            .ToDictionary(i => $"k{i}", i => $"v{i}", StringComparer.Ordinal);

        _attributeDictA = Enumerable
            .Range(0, Size)
            .ToDictionary(i => $"data-{i}", i => (object?)i, StringComparer.Ordinal);
        _attributeDictB = Enumerable
            .Range(0, Size)
            .ToDictionary(i => $"data-{i}", i => (object?)i, StringComparer.Ordinal);

        _nullableDictA = Enumerable
            .Range(0, Size)
            .ToDictionary(i => $"k{i}", i => (decimal?)i, StringComparer.Ordinal);
        _nullableDictB = Enumerable
            .Range(0, Size)
            .ToDictionary(i => $"k{i}", i => (decimal?)i, StringComparer.Ordinal);

        _setA = Enumerable.Range(0, Size).ToHashSet();
        _setB = Enumerable.Range(0, Size).ToHashSet();
        _nullableSetA = Enumerable.Range(0, Size).Select(ToNullableGuid).ToHashSet();
        _nullableSetB = Enumerable.Range(0, Size).Select(ToNullableGuid).ToHashSet();
    }

    // Shallow = the default per-key comparison: whole-object Equals (reference for collections).
    private static bool Shallow(object a, object b) => a.Equals(b);

    [Benchmark(Baseline = true)]
    public bool PrimitiveArray_Shallow() => Shallow(_arrayA, _arrayB);

    [Benchmark]
    public bool PrimitiveArray_Deep() => ValueComparer.ValuesEqual(_arrayA, _arrayB, 0);

    [Benchmark]
    public bool PrimitiveList_Shallow() => Shallow(_listA, _listB);

    [Benchmark]
    public bool PrimitiveList_Deep() => ValueComparer.ValuesEqual(_listA, _listB, 0);

    [Benchmark]
    public bool NullableList_Deep() => ValueComparer.ValuesEqual(_nullableListA, _nullableListB, 0);

    [Benchmark]
    public bool RecordList_Shallow() => Shallow(_recordsA, _recordsB);

    [Benchmark]
    public bool RecordList_Deep() => ValueComparer.ValuesEqual(_recordsA, _recordsB, 0);

    [Benchmark]
    public bool Dictionary_Shallow() => Shallow(_dictA, _dictB);

    [Benchmark]
    public bool Dictionary_Deep() => ValueComparer.ValuesEqual(_dictA, _dictB, 0);

    [Benchmark]
    public bool IntDictionary_Deep() => ValueComparer.ValuesEqual(_intDictA, _intDictB, 0);

    [Benchmark]
    public bool StringDictionary_Deep() => ValueComparer.ValuesEqual(_stringDictA, _stringDictB, 0);

    [Benchmark]
    public bool AttributeDictionary_Deep() =>
        ValueComparer.ValuesEqual(_attributeDictA, _attributeDictB, 0);

    [Benchmark]
    public bool NullableDictionary_Deep() =>
        ValueComparer.ValuesEqual(_nullableDictA, _nullableDictB, 0);

    [Benchmark]
    public bool Set_Shallow() => Shallow(_setA, _setB);

    [Benchmark]
    public bool Set_Deep() => ValueComparer.ValuesEqual(_setA, _setB, 0);

    [Benchmark]
    public bool NullableSet_Deep() => ValueComparer.ValuesEqual(_nullableSetA, _nullableSetB, 0);

    private static Guid? ToNullableGuid(int value) => new Guid(value, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
}
