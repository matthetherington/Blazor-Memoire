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
    public async Task Setup()
    {
        _renderer = new BenchRenderer();

        _direct = await Attach(new BenchHost { Mode = BenchHost.RenderMode.Direct, Work = Work })
            .ConfigureAwait(false);
        _memoShallow = await Attach(
                new BenchHost { Mode = BenchHost.RenderMode.MemoShallow, Work = Work }
            )
            .ConfigureAwait(false);
        _memoDeep = await Attach(
                new BenchHost { Mode = BenchHost.RenderMode.MemoDeep, Work = Work }
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
    public Task MemoShallow_ChangingKeys()
    {
        var next = ++_counter;
        return _renderer.InvokeAsync(() =>
        {
            _memoShallow.Keys = new object?[] { next };
            _memoShallow.ForceRender();
        });
    }

    [Benchmark]
    public Task MemoDeep_ChangingKeys()
    {
        var next = ++_counter;
        return _renderer.InvokeAsync(() =>
        {
            _memoDeep.Keys = new object?[]
            {
                new List<int> { next, next + 1, next + 2 },
            };
            _memoDeep.ForceRender();
        });
    }
}
