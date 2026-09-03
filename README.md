# 🧊BlazorMemoire 🧊

[![MIT License](https://img.shields.io/github/license/matthetherington/Blazor-Memoire?style=for-the-badge&labelColor=143642&color=FE5F55)](https://choosealicense.com/licenses/mit/)

[![NuGet](https://img.shields.io/nuget/v/BlazorMemoire?style=for-the-badge&labelColor=143642&color=FE5F55)](https://www.nuget.org/packages/BlazorMemoire)

[![CI](https://img.shields.io/github/actions/workflow/status/matthetherington/Blazor-Memoire/publish.yml?style=for-the-badge&label=Publish%20to%20NuGet&labelColor=143642&color=FE5F55)](https://github.com/matthetherington/Blazor-Memoire/actions/workflows/publish.yml)

A Blazor `<Memo>` component, similar to React's [`useMemo`](https://react.dev/reference/react/useMemo#skipping-re-rendering-of-components) 
that freezes its child subtree until explicit dependency keys change, preventing unnecessary re-renders and making 
lifecycle methods fire only when there's been a true change.

## Why memoisation?

Blazor re-renders a child whenever its parent renders, but not always. `ComponentBase` has a built-in optimisation: 
if a component's parameters are all primitive, immutable types (`string`, `int`, `bool`, `Guid`, `DateTime`, etc - 
[see all](https://github.com/dotnet/aspnetcore/blob/main/src/Components/Components/src/ChangeDetection.cs#L48)) and 
none of their values changed, Blazor skips the re-render for you. That's why simple components often feel "free":
the framework is quietly detecting that nothing changed.

That optimisation only covers primitives though. As soon as a component takes a complex parameter, for example an 
object, a `List<int>`, a `string[]`, a record, or `Action` / `Func<T>`, Blazor can no longer prove it's unchanged, so it plays 
it safe and re-renders every time the parent does. This often catches people out because a component that rendered 
efficiently for weeks suddenly starts re-rendering on every parent update, and the only thing that changed was adding 
a non-primitive parameter. Nothing looks obviously wrong, and there's no warning, the change detection just stopped 
applying.

When a subtree does heavy work or makes network calls and database queries on render, or in response to parameter 
changes, those redundant renders can really add up, slowing things down for users and increasing system load.

## Why BlazorMemoire?

`<Memo>` lets you wrap any subtree and provide a set of dependency keys. The subtree renders once, then stays frozen 
until one or more keys change. No new parameters flow in, no lifecycle methods fire, and nothing downstream re-renders 
unless you've declared that it should.

- **Skip expensive work.** Freeze subtrees that would otherwise re-run costly logic, queries, or network requests on every parent render.
- **Control from the call site.** Decide when a subtree updates where you use it, not inside the component.
- **Works with any component.** Wrap third-party or shared components you can't (or don't want to) modify.
- **One declaration, whole subtree.** Freeze a component and all its descendants together, without touching their source.
- **Minimal performance overhead, often a substantial gain.** `<Memo>` is fast, and consumes negligible amounts of memory in nearly all cases. The performance cost of comparing the dependency keys is low, which means `<Memo>` only needs to skips a little rendering work to be a net positive. When used where it will skip a lot of rendering work - the savings can outweigh the cost many times over. 

## Why not `ShouldRender`?

Blazor's built-in `ShouldRender` override lets a component decide internally whether to re-render. This works, but has limitations:

- **`ShouldRender` doesn't prevent parameter diffing.** Even with a `ShouldRender` override, Blazor still calls `SetParametersAsync` then `OnParametersSet`/`OnParametersSetAsync` which can mean redundant execution of expensive network requests or database queries. `<Memo>` short-circuits before that happens so child components receive no new parameters at all when keys haven't changed.
- **Declarative subtree freezing at the call site.** You *can* skip a subtree by returning `false` from a parent's `ShouldRender`, but that couples the decision to the parent and stops the parent re-rendering too. `<Memo>` freezes just the wrapped subtree in one declaration, leaving the surrounding component free to render normally.
- **The component controls its own re-render policy.** If you want the same component to re-render on different conditions in different places, you're stuck as `ShouldRender` is baked into the component itself. `<Memo>` moves that decision to the call site, so the parent chooses when the subtree updates.
- **It requires modifying the component.** Third-party or shared components can't have `ShouldRender` added from the outside. Wrapping them in `<Memo>` gives you render control without touching their source.

`ShouldRender` is still the simpler choice when a component only needs to skip renders based on its own state, and you control its source.

`<Memo>` earns its place when you need to control rendering at the point of use instead of in the component, cut out 
rendering work for a whole subtree, or can't easily modify the component.

## Installation

```shell
dotnet add package BlazorMemoire
```

## Usage

Wrap any subtree in a `<Memo>` component and provide dependency keys. The child content only re-renders when a key value changes.

```razor
@using BlazorMemoire.Components

<Memo Keys="@([user.Id, selectedTab])">
    <ExpensiveChildComponent User="user" Tab="selectedTab" />
</Memo>
```

### Keys behaviour

| Keys / setting | Behaviour                                                                                                                                               |
|----------------|---------------------------------------------------------------------------------------------------------------------------------------------------------|
| `null`         | No memoisation. Renders on every parent render.                                                                                                         |
| `[]`           | Render once, freeze forever.                                                                                                                            |
| `[a, b, c]`    | Re-render only when a key element changes.                                                                                                              |
| `Deep="false"` | **Default.** Each existing key element is compared with the incoming key element via its own `object.Equals` (reference equality for most collections). |
| `Deep="true"`  | Opt-in to deep structural comparison of key elements (collections, nesting).                                                                            |

### Comparison modes

#### Shallow comparison (default with `Deep="false"`)

By default (with `Deep="false"`), each incoming key element is compared against the corresponding element from the previous 
render's snapshot, position by position.

The comparison is a null-safe call to the existing element's `object.Equals` method (i.e. `existing.Equals(incoming)`).
two nulls are equal and otherwise the stored element's own equality decides. This is allocation-free and involves no reflection.

- **Primitives, strings, records, enums, `DateTime`, `TimeOnly`, etc.:** compared by value, because
  those types override `Equals` to compare by value.
- **Reference-type collections (`List<T>`, `Dictionary<K,V>`, arrays, `HashSet<T>`, etc.):**
  compared **by reference**. Two distinct instances with identical contents are treated
  as *changed*.

```razor
@* Default mode: a fresh List each render is a new reference, so the child DOES re-render. *@
<Memo Keys="@([new List<int> { 1, 2, 3 }])">
    <ChildComponent />
</Memo>

@* To freeze on content in default/shallow mode, reuse the same instance across renders: *@
<Memo Keys="@([_stableList])">
    <ChildComponent />
</Memo>
```

> [!NOTE]
> Wrapping a collection in a `record` does **not** give you content comparison.
> A record's generated `Equals` compares each field with `EqualityComparer<T>.Default`, and
> for a `List<T>` field that is reference equality, so two records holding equal-content but
> distinct lists are still unequal. Use `Deep="true"` (or a type that implements structural
> `Equals` itself) when you need content comparison.

### Deep structural equality (opt-in with `Deep="true"`)

Set `Deep="true"` to compare key elements by deep value equality instead:

- **Collections** (arrays, lists, dictionaries, sets) are compared element-wise
- **Dictionaries** are compared by key/value pairs; **sets** compared unordered
- **Nested collections** compared recursively (up to a depth limit of 32, which falls back to always rendering if exceeded)
- **Lazy enumerables** (LINQ queries, `yield return` generators) are materialised when the
  snapshot is created, so the comparison captures their current values

```razor
@* Deep mode: equal-content lists compare equal, so the child does NOT re-render. *@
<Memo Keys="@([new List<int> { 1, 2, 3 }])" Deep="true">
    <ChildComponent />
</Memo>
```

`Deep` is expected to be constant for a given `<Memo>` instance; changing it between renders
is treated as a key change and forces a re-render.

### Choosing a mode

| You want to compare keys by…                                  | Use                                        |
|---------------------------------------------------------------|--------------------------------------------|
| Reference identity of a collection you already keep stable    | Default (`Deep="false"`) + reuse instances |
| Value of primitives, strings, records, enums                  | Default (`Deep="false"`)                   |
| Content of collections / nested structures you rebuild often  | `Deep="true"`                              |
| Content of lazy `IEnumerable` snapshots                       | `Deep="true"`                              |

### Modes

- **Default mode** is the most performant option with a single O(n) pass over the keys, calling each
  element's `object.Equals` method. No reflection, no boxing beyond what the `object?` keys already carry,
  and the snapshot reuses its backing buffer across renders (references copied, nothing
  enumerated or materialised). Prefer this mode with stable references or primitive keys on
  hot render paths.
- **Deep mode** trades work for convenience, but is still highly performant. It walks collections 
  element-wise, matches dictionaries by key and sets in an order-insensitive way, and recurses into nested 
  enumerables/collections with a max depth of 32. Lazy enumerables are **fully enumerated and allocated** into arrays 
  on snapshot so their values are stable (this is the main extra cost) so avoid `Deep="true"` with `IEnumerable` keys 
  for very large or expensive-to-enumerate objects unless a simpler memo key for it cannot be derived. Primitive-element 
  arrays and lists still take an internal `Span` fast-path.

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

The wrapper adds *negligible* time overhead per-render (nanoseconds, within measurement noise), and allocates 0 bytes, 
with dictionaries being the sole exception due to boxing.

A trivial child has almost no work to skip, so the net performance gain is roughly zero; the benefit grows quickly 
as the wrapped subtree gets more expensive, however.

Shallow and deep mode perform almost identically with stable keys, because a frozen child skips its render work in 
either mode; the only difference between them is key-comparison cost, which is negligible for simple keys.

**When keys change every render, `<Memo>` can't help**. It pays for the key comparison and
then re-renders anyway, costing roughly 2× the un-memoised baseline, so reserve `<Memo>` for
subtrees that are actually stable most of the time.

**The overhead of `Deep="true"` versus `Deep="false"` is paid on every render** as `<Memo>` must compare the keys to 
decide whether to freeze the child, even when they're unchanged. That cost is negligible when rendering work is skipped, 
but it's pure overhead when keys change and rendering work happens anyway. The default per-key `object.Equals` 
comparison is effectively free either way.

The performance cost of deep comparison scales with the size and shape of the keys: primitive arrays and lists stay in the 
nanoseconds via a `Span` fast-path, records and sets grow linearly. Dictionaries are the most expensive case, but 
for a small set of keys and dictionaries with a small number of items, as is typically the case for parameters, the 
savings by eliminating work easily outweigh the performance overhead. 

In short: prefer the default of `Deep="false"`, and keep keys small when using `Deep="true"`. 
Use `<Memo>` in a targeted fashion where it is of most benefit instead of applying it by default.

## Requirements

- .NET 9.0 or .NET 10.0
- ASP.NET Core (Blazor Server, WebAssembly, or Auto)

## License

[MIT](LICENSE)
