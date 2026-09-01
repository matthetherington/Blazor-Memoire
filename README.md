# BlazorMemoire

[![NuGet](https://img.shields.io/nuget/v/BlazorMemoire)](https://www.nuget.org/packages/BlazorMemoire)
[![CI](https://github.com/matthetherington/Blazor-Memoire/actions/workflows/publish.yml/badge.svg)](https://github.com/matthetherington/Blazor-Memoire/actions/workflows/publish.yml)

A Blazor memoisation component, similar to React's [`useMemo`](https://react.dev/reference/react/useMemo#skipping-re-rendering-of-components) that freezes its child subtree until explicit dependency keys change, preventing unnecessary re-renders.

## Why not `ShouldRender`?

Blazor's built-in `ShouldRender` override lets a component decide internally whether to re-render. This works, but has limitations:

- **The component controls its own re-render policy.** If you want the same component to re-render on different conditions in different places, you're stuck as `ShouldRender` is baked into the component itself. `<Memo>` moves that decision to the call site, so the parent chooses when the subtree updates.
- **`ShouldRender` doesn't prevent parameter diffing.** Even when `ShouldRender` returns `false`, Blazor still calls `SetParametersAsync` and diffs every parameter on every parent render. `<Memo>` short-circuits before that happens so child components receive no new parameters at all when keys haven't changed.
- **It requires modifying the component.** Third-party or shared components can't have `ShouldRender` added from the outside. Wrapping them in `<Memo>` gives you render control without touching their source.
- **Subtree-level control.** `ShouldRender` applies to a single component. `<Memo>` freezes an entire subtree (the wrapped component and all its descendants) in one declaration.

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

#### Shallow comparison (default with `Deep="true"`)

By default (with `Deep="false"`), each incoming key element is compared against the corresponding element from the previous 
render's snapshot, position by position.

The comparison is a null-safe call to the existing element's `object.Equals` method — i.e. `existing.Equals(incoming)`.
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

> **Note:** wrapping a collection in a `record` does **not** give you content comparison.
> A record's generated `Equals` compares each field with `EqualityComparer<T>.Default`, and
> for a `List<T>` field that is reference equality — so two records holding equal-content but
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

### Performance

- **Default mode** is the most performant option with a single O(n) pass over the keys, calling each
  element's `object.Equals` method. No reflection, no boxing beyond what the `object?` keys already carry,
  and the snapshot reuses its backing buffer across renders (references copied, nothing
  enumerated or materialised). Prefer this mode with stable references or primitive keys on
  hot render paths.
- **Deep mode** trades work for convenience, but is still highly performant. It walks collections 
  element-wise, matches dictionaries by key and sets in an order-insensitive way, and recurses into nested 
  enumerables/collections with a max depth of 32. Lazy enumerables are **fully enumerated and allocated** into arrays on snapshot so their
  values are stable — this is the main extra cost, so avoid `Deep="true"` with `IEnumerable` keys for very large or
  expensive-to-enumerate objects unless a simpler memo key for it cannot be derived. Primitive-element arrays
  and lists still take an internal `Span` fast-path.

## Requirements

- .NET 9.0 or .NET 10.0
- ASP.NET Core (Blazor Server, WebAssembly, or Auto)

## License

[MIT](LICENSE)
