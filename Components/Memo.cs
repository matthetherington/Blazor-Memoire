using System.Collections;
using System.Collections.Frozen;
using System.Collections.Immutable;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorMemoire.Components;

/// <summary>
/// A wrapper component that freezes its child subtree until explicit <see cref="Keys"/>
/// change. When keys are equal to the previous render, <see cref="BuildRenderTree"/>
/// does not run and child components receive no new parameters.
///
/// <list type="bullet">
///   <item><c>Keys = null</c> — no memoisation; renders on every parent render.</item>
///   <item><c>Keys = []</c> — render once, freeze forever.</item>
///   <item><c>Keys = [a, b, c]</c> — re-render only when a key element changes.</item>
/// </list>
///
/// By default, key elements are compared with a per-element null-safe
/// <see cref="object.Equals(object?)"/>. This means collections such as <c>List&lt;T&gt;</c>
/// are compared by reference, and equal-content-but-distinct instances are treated as changed.
/// Set <see cref="Deep"/> to <c>true</c> to instead use deep structural comparison via
/// <see cref="ValueComparer"/> (collections element-wise, records by value, primitives by value).
///
/// The keys snapshot buffer is reused across renders to minimise allocation. In deep mode,
/// lazy enumerables are materialised on snapshot; in the default mode they are copied by reference.
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

    /// <summary>The child content to render inside the memoisation boundary.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// When <c>false</c> (the default), key elements are compared with a per-element null-safe
    /// <see cref="object.Equals(object?)"/>. Reference-type collections therefore compare by
    /// reference. When <c>true</c>, key elements are compared with deep structural equality via
    /// <see cref="ValueComparer"/> (collections element-wise, records by value), and lazy
    /// enumerables are materialised on snapshot. This value is expected to be constant for the
    /// lifetime of a given <see cref="Memo"/> instance; changing it between renders is treated
    /// as a key change and forces a re-render.
    /// </summary>
    [Parameter]
    public bool Deep { get; set; }

    private object?[]? _snapshot;
    private bool _hasSnapshot;
    private bool _snapshotDeep;

    /// <inheritdoc />
    public override Task SetParametersAsync(ParameterView parameters)
    {
        parameters.TryGetValue<IReadOnlyList<object?>>(nameof(Keys), out var keys);
        parameters.TryGetValue<bool>(nameof(Deep), out var deep);

        if (
            _hasSnapshot
            && keys is not null
            && deep == _snapshotDeep
            && KeysEqual(_snapshot, keys, deep)
        )
        {
            // Keys haven't changed — freeze the subtree. Still assign properties so the
            // component holds the latest instances (e.g. a new ChildContent delegate that
            // we won't invoke until keys change).
            parameters.SetParameterProperties(this);
            return Task.CompletedTask;
        }

        // Snapshot the incoming keys. In deep mode, lazy enumerables are materialised so a
        // caller reusing/mutating a collection can't corrupt the snapshot; in the default mode
        // elements are copied by reference. The snapshot buffer is reused across renders when
        // the key count is stable.
        _snapshot = keys is null ? null : SnapshotKeys(keys, _snapshot, deep);
        _snapshotDeep = deep;
        _hasSnapshot = true;

        return base.SetParametersAsync(parameters);
    }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.AddContent(0, ChildContent);
    }

    private static bool KeysEqual(object?[]? snapshot, IReadOnlyList<object?> current, bool deep)
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
            var equal = deep
                ? ValueComparer.ValuesEqual(snapshot[i], current[i], 0)
                : ShallowEquals(snapshot[i], current[i]);

            if (!equal)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Per-element null-safe equality: two nulls are equal; otherwise defers to the element's
    /// own <see cref="object.Equals(object?)"/>. Used by the default (non-deep) comparison mode.
    /// </summary>
    private static bool ShallowEquals(object? a, object? b) => a is null ? b is null : a.Equals(b);

    /// <summary>
    /// Returns a snapshot of <paramref name="keys"/> suitable for later comparison, reusing
    /// <paramref name="existing"/> when the length matches to avoid allocation. In deep mode
    /// lazy enumerables are materialised (see <see cref="MaterialiseKeys"/>); in the default
    /// mode every element is copied by reference.
    /// </summary>
    private static object?[] SnapshotKeys(
        IReadOnlyList<object?> keys,
        object?[]? existing,
        bool deep
    )
    {
        if (deep)
        {
            return MaterialiseKeys(keys, existing);
        }

        var result =
            existing is not null && existing.Length == keys.Count
                ? existing
                : new object?[keys.Count];

        for (var i = 0; i < keys.Count; i++)
        {
            result[i] = keys[i];
        }

        return result;
    }

    /// <summary>
    /// Returns a snapshot of <paramref name="keys"/> suitable for later deep comparison.
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
