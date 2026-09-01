using BenchmarkDotNet.Attributes;

namespace BlazorMemoire.Components.Benchmarks;

/// <summary>
/// Group B — the honest cost when memoisation can't help. Keys change on every parent
/// re-render, so <c>Memo</c> pays for the key comparison and then re-renders the child anyway.
/// Compared against the bare baseline, this isolates the per-render overhead a stable-key
/// win has to earn back. <see cref="Work"/> is kept small so the comparison overhead — not
/// the child work — dominates.
/// </summary>
[MemoryDiagnoser]
public class ChangingKeyBenchmarks
{
    [Params(0, 100)]
    public int Work;

    private BenchRenderer _renderer = null!;
    private BenchHost _direct = null!;
    private BenchHost _memoShallow = null!;
    private BenchHost _memoDeep = null!;
    private int _counter;

    [GlobalSetup]
    public void Setup()
    {
        _renderer = new BenchRenderer();

        _direct = Attach(new BenchHost { Mode = BenchHost.RenderMode.Direct, Work = Work });
        _memoShallow = Attach(
            new BenchHost { Mode = BenchHost.RenderMode.MemoShallow, Work = Work }
        );
        _memoDeep = Attach(new BenchHost { Mode = BenchHost.RenderMode.MemoDeep, Work = Work });
    }

    private BenchHost Attach(BenchHost host)
    {
        _renderer.AttachAndRender(host);
        return host;
    }

    [Benchmark(Baseline = true)]
    public void NoMemo() => _renderer.Invoke(_direct.ForceRender);

    [Benchmark]
    public void MemoShallow_ChangingKeys()
    {
        var next = ++_counter;
        _renderer.Invoke(() =>
        {
            _memoShallow.Keys = new object?[] { next };
            _memoShallow.ForceRender();
        });
    }

    [Benchmark]
    public void MemoDeep_ChangingKeys()
    {
        var next = ++_counter;
        _renderer.Invoke(() =>
        {
            _memoDeep.Keys = new object?[]
            {
                new List<int> { next, next + 1, next + 2 },
            };
            _memoDeep.ForceRender();
        });
    }
}
