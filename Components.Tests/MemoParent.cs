using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorMemoire.Components.Tests;

/// <summary>
/// A parent component that renders <c>&lt;Memo Keys="@Keys"&gt;&lt;RenderCounter /&gt;&lt;/Memo&gt;</c>.
/// Simulates the render tree Razor would generate. Captures a reference to the child so
/// tests can inspect its render/parameter counts.
/// </summary>
/// <summary>
/// A parent component that renders <c>&lt;Memo Keys="@Keys"&gt;&lt;RenderCounter /&gt;&lt;/Memo&gt;</c>.
/// Simulates the render tree Razor would generate. Captures a reference to the child so
/// tests can inspect its render/parameter counts.
///
/// <see cref="_parentRenderCount"/> is passed to the child as <see cref="RenderCounter.ParentRenderCount"/>
/// so that every time Memo allows a re-render, the child receives a changed parameter and
/// Blazor's diff engine propagates it (Blazor skips <c>SetParametersAsync</c> on a child
/// component when all its attribute frames are reference-equal to the previous render).
/// </summary>
internal sealed class MemoParent : ComponentBase
{
    private int _parentRenderCount;

    [Parameter]
    public object?[]? Keys { get; set; }

    [Parameter]
    public string? ChildText { get; set; }

    public RenderCounter? Child { get; private set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        _parentRenderCount++;

        builder.OpenComponent<Memo>(0);
        builder.AddAttribute(1, nameof(Memo.Keys), Keys);
        builder.AddAttribute(
            2,
            nameof(Memo.ChildContent),
            (RenderFragment)(
                b =>
                {
                    b.OpenComponent<RenderCounter>(0);
                    b.AddAttribute(1, nameof(RenderCounter.Text), ChildText);
                    b.AddAttribute(2, nameof(RenderCounter.ParentRenderCount), _parentRenderCount);
                    b.AddComponentReferenceCapture(3, o => Child = (RenderCounter)o);
                    b.CloseComponent();
                }
            )
        );
        builder.CloseComponent();
    }
}
