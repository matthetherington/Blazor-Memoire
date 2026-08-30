using System.Collections;
using System.Collections.Frozen;
using System.Collections.Immutable;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorMemoire.Components;

/// <summary>
/// A wrapper component that freezes its child subtree until explicit <see cref="Keys"/>
/// change. When keys are value-equal to the previous render, <see cref="BuildRenderTree"/>
/// does not run and child components receive no new parameters.
///
/// <list type="bullet">
///   <item><c>Keys = null</c> — no memoisation; renders on every parent render.</item>
///   <item><c>Keys = []</c> — render once, freeze forever.</item>
///   <item><c>Keys = [a, b, c]</c> — re-render only when a key element changes.</item>
/// </list>
///
/// Key elements are compared with deep value equality via <see cref="ValueComparer"/>:
/// collections element-wise, records by value, primitives by value. The keys array is
/// cloned on snapshot to prevent corruption if the caller reuses or mutates it.
/// The snapshot buffer is reused across renders to minimise allocation.
/// </summary>
public sealed class Memo : ComponentBase
{
    /// <summary>
    /// The dependency keys that control when the child subtree re-renders.
    /// When parameters are set, existing keys will be compared to the incoming keys by value.
    /// If there is a change, <see cref="ComponentBase.SetParametersAsync"/> is called and will
    /// eventually re-render the component.
    /// Parameters are re-assigned even when there's no change in value, so they will always hold
    /// the most recent reference.
    /// Null means "always render" (no memoisation).
    /// An empty array means "render once, freeze forever".
    /// </summary>
    [Parameter]
    public IReadOnlyList<object?>? Keys { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private object?[]? _snapshot;
    private bool _hasSnapshot;

    public override Task SetParametersAsync(ParameterView parameters)
    {
        parameters.TryGetValue<IReadOnlyList<object?>>(nameof(Keys), out var keys);

        if (_hasSnapshot && keys is not null && KeysEqual(_snapshot, keys))
        {
            // Keys haven't changed — freeze the subtree. Still assign properties so the
            // component holds the latest instances (e.g. a new ChildContent delegate that
            // we won't invoke until keys change).
            parameters.SetParameterProperties(this);
            return Task.CompletedTask;
        }

        // Clone the keys so a caller reusing or mutating the array can't corrupt the snapshot.
        // The snapshot buffer is reused across renders when the key count is stable.
        _snapshot = keys is null ? null : MaterialiseKeys(keys, _snapshot);
        _hasSnapshot = true;

        return base.SetParametersAsync(parameters);
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.AddContent(0, ChildContent);
    }

    private static bool KeysEqual(object?[]? snapshot, IReadOnlyList<object?> current)
    {
        if (snapshot is null)
        {
            return false; // previous render had null keys (no memoisation)
        }

        if (snapshot.Length != current.Count)
        {
            return false;
        }

        for (var i = 0; i < snapshot.Length; i++)
        {
            if (!ValueComparer.ValuesEqual(snapshot[i], current[i], 0))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Returns a snapshot of <paramref name="keys"/> suitable for later comparison.
    /// The result array is reused from <paramref name="existing"/> when the length matches
    /// to avoid allocation. Lazy <see cref="IEnumerable"/> elements are eagerly evaluated
    /// into arrays; all other elements are copied by reference.
    /// </summary>
    private static object?[] MaterialiseKeys(IReadOnlyList<object?> keys, object?[]? existing)
    {
        var result =
            existing is not null && existing.Length == keys.Count
                ? existing
                : new object?[keys.Count];

        for (var i = 0; i < keys.Count; i++)
        {
            result[i] = keys[i] switch
            {
                // Strings are IEnumerable<char> but already have correct Equals — keep as-is.
                null or string => keys[i],

                // Concrete collections: ICollection covers most standard types; the covariant
                // IReadOnlyCollection<object> catches generic-only types like HashSet<string>.
                ICollection or IReadOnlyCollection<object> => keys[i],

                // Value-type-element collections (e.g. HashSet<int>) that slip through the
                // covariant check — keep by reference so ValueComparer sees the real type.
                IEnumerable when IsKnownCollectionDefinition(keys[i]!) => keys[i],

                // Truly lazy enumerables (yield-return generators, LINQ queries) must be
                // materialised so the snapshot captures their current values — re-enumerating
                // later could produce different results.
                IEnumerable e => e.Cast<object?>().ToArray(),

                // Non-collection values (primitives, records, etc.) — keep by reference.
                _ => keys[i],
            };
        }

        return result;
    }

    /// <summary>
    /// Known generic collection type definitions that implement <c>ICollection&lt;T&gt;</c>
    /// but not the non-generic <see cref="ICollection"/>, and therefore aren't caught by
    /// the covariant <c>IReadOnlyCollection&lt;object&gt;</c> check when the element type
    /// is a value type. Uses a single <see cref="Type.GetGenericTypeDefinition"/> call
    /// rather than iterating all interfaces.
    /// </summary>
    private static readonly FrozenSet<Type> KnownCollectionDefinitions = new[]
    {
        typeof(HashSet<>),
        typeof(FrozenSet<>),
        typeof(ImmutableHashSet<>),
        typeof(ImmutableList<>),
        typeof(ImmutableSortedSet<>),
        typeof(ImmutableQueue<>),
        typeof(ImmutableStack<>),
    }.ToFrozenSet();

    private static bool IsKnownCollectionDefinition(object value)
    {
        var type = value.GetType();
        return type.IsGenericType
            && KnownCollectionDefinitions.Contains(type.GetGenericTypeDefinition());
    }
}
