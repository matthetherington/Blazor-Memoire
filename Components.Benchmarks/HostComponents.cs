using BlazorMemoire.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorMemoire.Components.Benchmarks;

/// <summary>Consumes computed values so the JIT can't elide the child's render work.</summary>
internal static class Sink
{
    public static long Value;
}

/// <summary>
/// A leaf component that does a configurable amount of work on every render. The amount of
/// work stands in for a real, non-trivial subtree — the whole point of memoisation is to skip
/// this work when keys are unchanged.
/// </summary>
internal sealed class WorkChild : ComponentBase
{
    [Parameter]
    public int Tick { get; set; }

    [Parameter]
    public int Work { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        long sum = 0;
        for (var i = 0; i < Work; i++)
        {
            sum += (long)i * Tick;
        }

        Sink.Value = sum;
        builder.AddContent(0, sum);
    }
}

/// <summary>
/// The <see cref="ShouldRender"/> equivalent of wrapping <see cref="WorkChild"/> in a
/// <c>Memo</c>: it still receives and diffs parameters, but skips its own render (and the
/// work) while its <see cref="Key"/> is unchanged.
/// </summary>
internal sealed class ShouldRenderChild : ComponentBase
{
    private int _lastKey = int.MinValue;

    [Parameter]
    public int Key { get; set; }

    [Parameter]
    public int Tick { get; set; }

    [Parameter]
    public int Work { get; set; }

    protected override bool ShouldRender()
    {
        if (Key == _lastKey)
        {
            return false;
        }

        _lastKey = Key;
        return true;
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        long sum = 0;
        for (var i = 0; i < Work; i++)
        {
            sum += (long)i * Tick;
        }

        Sink.Value = sum;
        builder.AddContent(0, sum);
    }
}

/// <summary>
/// A root host that re-renders on demand and renders one of several equivalent subtrees:
/// a bare child, a child behind <c>Memo</c> (shallow or deep), or a child that guards itself
/// with <see cref="ShouldRenderChild.ShouldRender"/>. Each re-render bumps <c>Tick</c> so an
/// unguarded child always re-renders, isolating the effect of each memoisation strategy.
/// </summary>
internal sealed class BenchHost : ComponentBase
{
    public enum RenderMode
    {
        Direct,
        MemoShallow,
        MemoDeep,
        ShouldRender,
    }

    public RenderMode Mode { get; set; }

    public int Work { get; set; }

    /// <summary>Stable-across-renders for the freeze scenarios; mutated for the change scenarios.</summary>
    public IReadOnlyList<object?>? Keys { get; set; }

    /// <summary>Drives <see cref="ShouldRenderChild"/>; stable to freeze, incremented to force renders.</summary>
    public int KeyValue { get; set; }

    private int _tick;

    public void ForceRender() => StateHasChanged();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        _tick++;

        switch (Mode)
        {
            case RenderMode.Direct:
                BuildWorkChild(builder);
                break;
            case RenderMode.MemoShallow:
                BuildMemo(builder, deep: false);
                break;
            case RenderMode.MemoDeep:
                BuildMemo(builder, deep: true);
                break;
            case RenderMode.ShouldRender:
                BuildShouldRenderChild(builder);
                break;
            default:
                throw new InvalidOperationException($"Unknown mode: {Mode}");
        }
    }

    private void BuildWorkChild(RenderTreeBuilder builder)
    {
        builder.OpenComponent<WorkChild>(0);
        builder.AddAttribute(1, nameof(WorkChild.Tick), _tick);
        builder.AddAttribute(2, nameof(WorkChild.Work), Work);
        builder.CloseComponent();
    }

    private void BuildShouldRenderChild(RenderTreeBuilder builder)
    {
        builder.OpenComponent<ShouldRenderChild>(0);
        builder.AddAttribute(1, nameof(ShouldRenderChild.Key), KeyValue);
        builder.AddAttribute(2, nameof(ShouldRenderChild.Tick), _tick);
        builder.AddAttribute(3, nameof(ShouldRenderChild.Work), Work);
        builder.CloseComponent();
    }

    private void BuildMemo(RenderTreeBuilder builder, bool deep)
    {
        var tick = _tick;
        var work = Work;

        builder.OpenComponent<Memo>(0);
        builder.AddAttribute(1, nameof(Memo.Keys), Keys);
        builder.AddAttribute(2, nameof(Memo.Deep), deep);
        builder.AddAttribute(
            3,
            nameof(Memo.ChildContent),
            (RenderFragment)(
                b =>
                {
                    b.OpenComponent<WorkChild>(0);
                    b.AddAttribute(1, nameof(WorkChild.Tick), tick);
                    b.AddAttribute(2, nameof(WorkChild.Work), work);
                    b.CloseComponent();
                }
            )
        );
        builder.CloseComponent();
    }
}
