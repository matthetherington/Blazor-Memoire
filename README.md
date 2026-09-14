# 🧊BlazorMemoire 🧊

[![MIT License](https://img.shields.io/github/license/matthetherington/Blazor-Memoire?style=for-the-badge&labelColor=143642&color=FE5F55)](https://choosealicense.com/licenses/mit/)

[![NuGet](https://img.shields.io/nuget/v/BlazorMemoire?style=for-the-badge&labelColor=143642&color=FE5F55)](https://www.nuget.org/packages/BlazorMemoire)

[![CI](https://img.shields.io/github/actions/workflow/status/matthetherington/Blazor-Memoire/publish.yml?style=for-the-badge&label=Publish%20to%20NuGet&labelColor=143642&color=FE5F55)](https://github.com/matthetherington/Blazor-Memoire/actions/workflows/publish.yml)

A highly-performant Blazor `<Memo>` component, similar to [`React.memo`](https://react.dev/reference/react/memo),
that stops parent renders propagating through its child subtree until explicit dependency keys change, preventing
unnecessary re-renders and making lifecycle methods fire only when there's been a true change.

## Quick start

BlazorMemoire supports .NET 8, .NET 9, and .NET 10 across Blazor Server, WebAssembly,
and Auto render modes.

```shell
dotnet add package BlazorMemoire
```

Wrap a subtree in `<Memo>` and provide the values that should cause it to update via the `Keys` parameter:

```razor
@using BlazorMemoire.Components

<Memo Keys="@([user.Id, selectedTab])">
    <ExpensiveChildComponent UserId="@user.Id" Tab="@selectedTab" />
</Memo>
```

The child content renders initially, then receives new parameters only when `user.Id` or
`selectedTab` changes.

### Keys

| `Keys` value  | Behaviour                                                         |
|---------------|-------------------------------------------------------------------|
| `null`        | Disable memoisation and render on every parent render.            |
| `[]`          | Render once, then ignore subsequent parent renders.               |
| `[a, b, c]`   | Re-render when the key count changes or any key compares unequal. |

Use the default comparison mode unless you specifically need collection contents to be
compared:

| Mode                     | Behaviour                                                                                    |
|--------------------------|----------------------------------------------------------------------------------------------|
| Default (`Deep="false"`) | Calls `object.Equals` for each key. Most collections therefore compare by reference.         |
| `Deep="true"`            | Compares collection *contents* recursively. Non-collection values still use `object.Equals`. |

## Why memoisation?

Blazor re-renders a child whenever its parent renders, but not always. `ComponentBase` has a built-in optimisation:
if a component's parameters are all primitive, immutable types (`string`, `int`, `bool`, `Guid`, `DateTime`, etc -
[see all](https://github.com/dotnet/aspnetcore/blob/main/src/Components/Components/src/ChangeDetection.cs#L48)) and
none of their values changed, Blazor skips the re-render for you. That's why simple components often feel "free":
the framework is quietly detecting that nothing changed.

That optimisation only covers a limited set of known immutable types, though. As soon as a component takes a complex parameter, for example an
object, a `List<int>`, a `string[]`, a record, or `Action` / `Func<T>`, Blazor can no longer prove it's unchanged, so it plays
it safe and re-renders every time the parent does. This often catches people out because a component that rendered
efficiently for weeks suddenly starts re-rendering on every parent update, and the only thing that changed was adding
a non-primitive parameter. Nothing looks obviously wrong, and there's no warning, the change detection just stopped
applying.

When a subtree does heavy work or makes network calls and database queries in response to parameter changes, or on
render, those redundant renders can really add up, slowing things down for users and increasing system load.

## Why `<Memo>`?

BlazorMemoire's `<Memo>` lets you wrap any subtree and provide a set of dependency keys. The subtree renders once, then
parent-driven updates are suppressed until one or more keys change. While those updates are suppressed, wrapped child
components receive no new parameters from the parent and their parameter lifecycle methods do not run.

- **Skip expensive work:** Freeze subtrees that would otherwise re-run costly logic, queries, or network requests on every parent render.
- **Control from the call site:** Decide when a subtree updates where you use it, not inside the component.
- **Works with any component:** Wrap third-party or shared components you can't (or don't want to) modify.
- **One declaration, whole subtree:** Freeze a component and all its descendants together, without touching their source.
- **Minimal performance overhead, often a substantial gain:** Comparing small dependency keys is cheap, so `<Memo>` only needs to skip a little rendering work to be a net positive.

## Why not `ShouldRender`?

Blazor's built-in `ShouldRender` override lets a component decide internally whether to re-render. This works, but has limitations:

- **`ShouldRender` doesn't prevent parameter diffing.** Even with a `ShouldRender` override, Blazor still calls `SetParametersAsync` then `OnParametersSet`/`OnParametersSetAsync` which can mean redundant execution of expensive network requests or database queries. `<Memo>` short-circuits before that happens so child components receive no new parameters at all when keys haven't changed.
- **Declarative subtree freezing at the call site.** You *can* skip a subtree by returning `false` from a parent's `ShouldRender`, but that couples the decision to the parent and stops the parent re-rendering too. `<Memo>` freezes just the wrapped subtree in one declaration, leaving the surrounding component free to render normally.
- **The component controls its own re-render policy.** If you want the same component to re-render on different conditions in different places, you're stuck as `ShouldRender` is baked into the component itself. `<Memo>` moves that decision to the call site, so the parent chooses when the subtree updates.
- **It requires modifying the component.** Third-party or shared components can't have `ShouldRender` added from the outside. Wrapping them in `<Memo>` gives you render control without touching their source.

`ShouldRender` is still the simpler choice when a component only needs to skip renders based on its own state, and you control its source.

`<Memo>` is useful when you need to control rendering at the point of use instead of in the component, cut out
rendering work for a whole subtree, or can't easily modify the component.

## Choosing a comparison mode

| You want to compare by...                                  | Use                                                      |
|------------------------------------------------------------|----------------------------------------------------------|
| Primitive, string, enum, record, or custom object equality | Default (`Deep="false"`)                                 |
| Identity of a stable collection instance                   | Default (`Deep="false"`)                                 |
| Contents of newly created or immutable collections         | `Deep="true"`                                            |
| An in-place mutation                                       | A separate scalar/version key                            |
| Results of a lazy `IEnumerable`                            | `Deep="true"`, noting the performance implications below |

### Default comparison mode (`Deep="false"`)

The default comparison mode (with `Deep="false"`) performs a null-safe `existing.Equals(incoming)` call for each key,
in order. It does not enumerate collection keys; each key's own `Equals` implementation determines equality.

```razor
@* A new List instance means ChildComponent re-renders on each parent render. *@
<Memo Keys="@([new List<int> { 1, 2, 3 }])">
    <ChildComponent />
</Memo>
```

### Deep comparison mode (`Deep="true"`)

Set `Deep="true"` when collections in `Keys` with equal contents across renders should be considered equal:

```razor
@* The List<int> instance in Keys is different, but its contents are the same across each render *@
@* so ChildComponent renders for the first time and doesn't re-render afterwards *@

<Memo Keys="@([new List<int> { 1, 2, 3 }])" Deep="@true">
    <ChildComponent />
</Memo>
```

> [!IMPORTANT]
> The value of `Deep` is expected to remain constant for a given `<Memo>` instance. Changing it is treated as a key change
> and forces a render.

## Common gotchas

### Keys entirely define when parent updates propagate

This is `<Memo>`'s purpose, but a missing key can easily lead to stale UI. When its parent renders, `<Memo>` *only*
updates its wrapped content if a key has changed. Values used inside the wrapper should therefore have a corresponding
key if changes to them need to reach the child.

```razor
@* Don't do this: selectedTab is passed to the child but omitted from Keys. *@
<Memo Keys="@([user.Id])">
    <ExpensiveChildComponent UserId="@user.Id" Tab="@selectedTab" />
</Memo>
```

Here, changing `selectedTab` alone does not update `ExpensiveChildComponent`. Its `Tab`
parameter remains unchanged until `user.Id` changes and causes the subtree to update.

### Children can still update independently

`<Memo>` only suppresses updates caused by its parent rendering. A child can still re-render
in response to its own state, events, or independently delivered updates such as cascading
values.

### Deep mode does not detect in-place collection mutation

`Deep="true"` compares the contents of collection instances in the previous and current
`Keys` during `SetParametersAsync`. Concrete collections such as `List<T>` are retained by
reference rather than copied, avoiding the memory and GC cost of snapshotting their contents.
Mutating the same list, array, dictionary, or set in place can therefore leave the previous
snapshot pointing at the already-mutated object, and no change will be detected.

Replace collections that are passed into `Keys` when they change:

```csharp
var updatedItems = new List<Item>(_items);
updatedItems.Add(newItem);
_items = updatedItems;
```

Or use an immutable collection, where each change returns a new instance:

```csharp
private ImmutableList<Item> _items = [];

private void AddItem(Item item)
{
    _items = _items.Add(item);
}
```

For nested collections, replace each collection along the changed path. If in-place mutation is
unavoidable, add a scalar version key and increment it after every mutation:

```razor
<Memo Keys="@([_items, _itemsVersion])" Deep="@true">
    <ItemList Items="@_items" />
</Memo>
```

### Memoisation only helps stable subtrees

If the keys change on every parent render, `<Memo>` performs the comparison and then renders
the child anyway. Use it where the subtree is usually stable and expensive enough to justify
the boundary; do not wrap every component by default.

## Default mode detailed behaviour

- Primitives, strings, enums, and records use their existing value semantics.
- Types with custom `Equals` overrides are compared using that.
- Most reference-type collections, including arrays, lists, dictionaries, and sets, compare
  by identity. Distinct instances with equal contents are treated as changed.
- A record containing a collection does not automatically gain structural collection
  equality. Generated record equality uses normal equality semantics for each of its properties.

## Deep mode detailed behaviour

- Ordered collections and other enumerables compare each element in order.
- Nested collections are compared recursively, up to a max depth of 32.
- Collections exceeding the max depth are treated as changed.
- Sets implementing `ISet<T>`, such as `HashSet<T>` and `SortedSet<T>`, compare without
  regard to order.
- Standard dictionaries implementing non-generic `IDictionary`, such as `Dictionary<TKey, TValue>`,
  compare by key/value pairs without regard to enumeration order.
- Custom dictionary types that implement only `IDictionary<TKey, TValue>` or `IReadOnlyDictionary<TKey, TValue>`
  (for example, a generic-only dictionary wrapper or custom read-only lookup) fall back to ordered enumerable
  comparison, so their enumeration order affects equality.
- Non-collection values, including records, are compared with `object.Equals`; properties of
  arbitrary objects (like a plain class) are not traversed unless a custom `Equals` override is present to do this.
- Top-level lazy enumerable keys, such as LINQ queries and `yield return` generators, are
  materialised for the stored snapshot. They are enumerated during later comparisons and may
  be enumerated again when a changed snapshot is stored, so avoid expensive or side-effecting
  queries.

> [!NOTE]
> Dictionary keys are matched with the dictionary's comparer. For the typed fast-path shapes listed in the Performance &
> Benchmarks section, a change in the Dictionary comparer is treated as a key change, even when the current entries are
> identical. This is unlikely to happen in practice, but `<Memo>` errs on the side of caution and prefers an additional
> render to potentially missing an intentional state change.

## Performance & Benchmarks

The repository includes a [BenchmarkDotNet](https://benchmarkdotnet.org/) project
(`Components.Benchmarks`) that drives the real Blazor render pipeline through a minimal
renderer. The numbers below are illustrative (Apple M1 Max, .NET 10). Run them yourself
with `dotnet run -c Release --project Components.Benchmarks -- --filter "*"`.

**Memoisation pays off in proportion to the work it skips.** With stable keys, a `<Memo>`
freezes its child so the child's render work never runs. Measuring a parent re-render where
the child does a varying amount of work:

| Child render cost | No `<Memo>` | With `<Memo>` (stable keys)           |
|-------------------|-------------|---------------------------------------|
| Trivial           | baseline    | ~1.0× (roughly break-even)            |
| Moderate          | baseline    | ~0.75×                                |
| Expensive         | baseline    | ~0.08× (an order of magnitude faster) |

The wrapper adds *negligible* time overhead per-render (nanoseconds, within measurement noise), and allocates 0 bytes
for shallow keys and common deep-comparison collection shapes. Typed fast paths cover arrays, lists, and sets containing
common CLR types and their nullable counterpart when applicable:

| CLR type         |
|------------------|
| `string`         |
| `int`            |
| `long`           |
| `double`         |
| `float`          |
| `decimal`        |
| `bool`           |
| `byte`           |
| `Guid`           |
| `DateTime`       |
| `DateTimeOffset` |
| `DateOnly`       |
| `TimeOnly`       |
| `TimeSpan`       |

String-keyed dictionaries with any of these value types or their nullable counterpart are also covered.
Additional fast paths cover `Dictionary<string, object?>` (the usual shape for captured unmatched Blazor attributes),
`Dictionary<int, string>`, and `Dictionary<int, int>`. Other less common dictionary and collection shapes
use general fallbacks that can allocate due to boxing.

Shallow and deep mode perform almost identically with stable keys, because a frozen child skips its render work in
either mode; the only difference between them is key-comparison cost, which is negligible for simple keys.

**The overhead of `Deep="true"` versus `Deep="false"` is paid on every render** as `<Memo>` must compare the keys to
decide whether to freeze the child, even when they're unchanged. That cost is negligible for common key shapes when
rendering work is skipped, but it's pure overhead when keys change and rendering work happens anyway.
The default per-key `object.Equals` comparison is effectively free either way.

The performance cost of deep comparison scales with the size and shape of the keys. Primitive arrays and lists use a
`Span` fast path, while common dictionary shapes use typed paths that avoid boxing. Supported primitive sets use a
linear typed path; other sets use an order-independent matching pass that can grow quadratically. Less common
dictionary and collection shapes use more expensive general fallbacks. For small keys and collections, as is typical
for parameters, the savings from eliminating render work can easily outweigh the comparison overhead.

In short: prefer the default of `Deep="false"`, and keep keys small when using `Deep="true"`.
Use `<Memo>` in a targeted fashion where it is of most benefit instead of applying it by default.

## License

[MIT License](LICENSE)

Copyright (c) 2026 Matthew Hetherington

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
