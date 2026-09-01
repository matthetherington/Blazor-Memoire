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
    private List<Person> _recordsA = null!;
    private List<Person> _recordsB = null!;
    private Dictionary<string, int> _dictA = null!;
    private Dictionary<string, int> _dictB = null!;
    private HashSet<int> _setA = null!;
    private HashSet<int> _setB = null!;

    [GlobalSetup]
    public void Setup()
    {
        _arrayA = Enumerable.Range(0, Size).ToArray();
        _arrayB = Enumerable.Range(0, Size).ToArray();

        _listA = Enumerable.Range(0, Size).ToList();
        _listB = Enumerable.Range(0, Size).ToList();

        _recordsA = Enumerable.Range(0, Size).Select(i => new Person($"n{i}", i)).ToList();
        _recordsB = Enumerable.Range(0, Size).Select(i => new Person($"n{i}", i)).ToList();

        _dictA = Enumerable
            .Range(0, Size)
            .ToDictionary(i => $"k{i}", i => i, StringComparer.Ordinal);
        _dictB = Enumerable
            .Range(0, Size)
            .ToDictionary(i => $"k{i}", i => i, StringComparer.Ordinal);

        _setA = Enumerable.Range(0, Size).ToHashSet();
        _setB = Enumerable.Range(0, Size).ToHashSet();
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
    public bool RecordList_Shallow() => Shallow(_recordsA, _recordsB);

    [Benchmark]
    public bool RecordList_Deep() => ValueComparer.ValuesEqual(_recordsA, _recordsB, 0);

    [Benchmark]
    public bool Dictionary_Shallow() => Shallow(_dictA, _dictB);

    [Benchmark]
    public bool Dictionary_Deep() => ValueComparer.ValuesEqual(_dictA, _dictB, 0);

    [Benchmark]
    public bool Set_Shallow() => Shallow(_setA, _setB);

    [Benchmark]
    public bool Set_Deep() => ValueComparer.ValuesEqual(_setA, _setB, 0);
}
