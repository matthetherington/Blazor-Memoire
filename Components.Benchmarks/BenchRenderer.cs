using System.Runtime.ExceptionServices;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace BlazorMemoire.Components.Benchmarks;

/// <summary>
/// A minimal <see cref="Renderer"/> for driving the real Blazor render/diff pipeline inside
/// benchmarks. It runs the same <c>SetParametersAsync</c>/diff machinery that <c>Memo</c>
/// short-circuits, but discards the produced <see cref="RenderBatch"/> instead of serialising
/// HTML, so measurements isolate render work rather than output formatting.
/// </summary>
internal sealed class BenchRenderer : Renderer
{
    public BenchRenderer()
        : base(EmptyServiceProvider(), NullLoggerFactory.Instance) { }

    public override Dispatcher Dispatcher { get; } = Dispatcher.CreateDefault();

    /// <summary>Attaches <paramref name="component"/> as a root and performs its first render.</summary>
    public void AttachAndRender(IComponent component) =>
        Dispatcher
            .InvokeAsync(() =>
            {
                var componentId = AssignRootComponentId(component);
                return RenderRootComponentAsync(componentId);
            })
            .GetAwaiter()
            .GetResult();

    /// <summary>Runs <paramref name="action"/> on the render dispatcher and waits for quiescence.</summary>
    public void Invoke(Action action) => Dispatcher.InvokeAsync(action).GetAwaiter().GetResult();

    protected override void HandleException(Exception exception) =>
        ExceptionDispatchInfo.Capture(exception).Throw();

    // Discard the batch — we pay for the diff, not for output serialisation.
    protected override Task UpdateDisplayAsync(in RenderBatch renderBatch) => Task.CompletedTask;

    private static IServiceProvider EmptyServiceProvider() =>
        new ServiceCollection().BuildServiceProvider();
}
