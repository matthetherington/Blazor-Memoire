using BenchmarkDotNet.Attributes;

namespace BlazorMemoire.Components.Benchmarks;

/// <summary>
/// Group A — the benefit of memoisation. Keys never change, so every strategy except the bare
/// baseline should skip the child's render work on each parent re-render. <see cref="Work"/>
/// scales the child's per-render cost; the crossover where memoisation stops paying off is
/// visible as <see cref="Work"/> shrinks toward zero.
/// </summary>
[MemoryDiagnoser]
public class StableKeyBenchmarks
{
    [Params(0, 100, 10_000)]
    public int Work;

    private BenchRenderer _renderer = null!;
    private BenchHost _direct = null!;
    private BenchHost _memoShallow = null!;
    private BenchHost _memoDeep = null!;
    private BenchHost _shouldRender = null!;

    [GlobalSetup]
    public void Setup()
    {
        _renderer = new BenchRenderer();

        _direct = Attach(new BenchHost { Mode = BenchHost.RenderMode.Direct, Work = Work });

        _memoShallow = Attach(
            new BenchHost
            {
                Mode = BenchHost.RenderMode.MemoShallow,
                Work = Work,
                Keys = new object?[] { 0 },
            }
        );

        _memoDeep = Attach(
            new BenchHost
            {
                Mode = BenchHost.RenderMode.MemoDeep,
                Work = Work,
                Keys = new object?[]
                {
                    new List<int> { 1, 2, 3 },
                },
            }
        );

        _shouldRender = Attach(
            new BenchHost
            {
                Mode = BenchHost.RenderMode.ShouldRender,
                Work = Work,
                KeyValue = 0,
            }
        );
    }

    private BenchHost Attach(BenchHost host)
    {
        _renderer.AttachAndRender(host);
        return host;
    }

    [Benchmark(Baseline = true)]
    public void NoMemo() => _renderer.Invoke(_direct.ForceRender);

    [Benchmark]
    public void MemoShallow() => _renderer.Invoke(_memoShallow.ForceRender);

    [Benchmark]
    public void MemoDeep() => _renderer.Invoke(_memoDeep.ForceRender);

    [Benchmark]
    public void ShouldRender() => _renderer.Invoke(_shouldRender.ForceRender);
}
