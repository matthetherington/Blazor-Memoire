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
    public async Task Setup()
    {
        _renderer = new BenchRenderer();

        _direct = await Attach(new BenchHost { Mode = BenchHost.RenderMode.Direct, Work = Work })
            .ConfigureAwait(false);

        _memoShallow = await Attach(
                new BenchHost
                {
                    Mode = BenchHost.RenderMode.MemoShallow,
                    Work = Work,
                    Keys = new object?[] { 0 },
                }
            )
            .ConfigureAwait(false);

        _memoDeep = await Attach(
                new BenchHost
                {
                    Mode = BenchHost.RenderMode.MemoDeep,
                    Work = Work,
                    Keys = new object?[]
                    {
                        new List<int> { 1, 2, 3 },
                    },
                }
            )
            .ConfigureAwait(false);

        _shouldRender = await Attach(
                new BenchHost
                {
                    Mode = BenchHost.RenderMode.ShouldRender,
                    Work = Work,
                    KeyValue = 0,
                }
            )
            .ConfigureAwait(false);
    }

    // The continuation here only returns the host and never touches renderer state,
    // so ConfigureAwait(false) is safe and avoids capturing context.
    private async Task<BenchHost> Attach(BenchHost host)
    {
        await _renderer.AttachAndRenderAsync(host).ConfigureAwait(false);
        return host;
    }

    [Benchmark(Baseline = true)]
    public Task NoMemo() => _renderer.InvokeAsync(_direct.ForceRender);

    [Benchmark]
    public Task MemoShallow() => _renderer.InvokeAsync(_memoShallow.ForceRender);

    [Benchmark]
    public Task MemoDeep() => _renderer.InvokeAsync(_memoDeep.ForceRender);

    [Benchmark]
    public Task ShouldRender() => _renderer.InvokeAsync(_shouldRender.ForceRender);
}
