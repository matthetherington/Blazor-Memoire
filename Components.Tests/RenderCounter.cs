using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorMemoire.Components.Tests;

/// <summary>
/// A minimal child component that counts renders and parameter-set calls so tests can
/// assert whether the <see cref="Memo"/> wrapper froze or re-rendered the subtree.
/// </summary>
public sealed class RenderCounter : ComponentBase
{
    [Parameter]
    public string? Text { get; set; }

    [Parameter]
    public int ParentRenderCount { get; set; }

    public int RenderCount { get; private set; }
    public int ParameterSetCount { get; private set; }

    protected override void OnParametersSet()
    {
        ParameterSetCount++;
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        RenderCount++;
        builder.AddContent(0, $"{Text}-{ParentRenderCount}");
    }
}
