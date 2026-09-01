using BenchmarkDotNet.Running;
using BlazorMemoire.Components.Benchmarks;

BenchmarkSwitcher.FromAssembly(typeof(BenchmarkAnchor).Assembly).Run(args);

namespace BlazorMemoire.Components.Benchmarks
{
    /// <summary>Anchor type for BenchmarkSwitcher's assembly lookup.</summary>
    internal sealed class BenchmarkAnchor;
}
