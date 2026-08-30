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

### Key behaviours

| Keys        | Behaviour                                       |
|-------------|-------------------------------------------------|
| `null`      | No memoisation. Renders on every parent render. |
| `[]`        | Render once, freeze forever.                    |
| `[a, b, c]` | Re-render only when a key element changes.      |

### Deep value equality

Keys are compared using deep value equality, not reference equality:

- **Primitives:** compared by value
- **Records:** compared by value
- **Collections:** (arrays, lists, dictionaries, sets) compared element-wise
- **Nested collections:** compared recursively

```razor
@* These two renders produce the same key values, so the child won't re-render *@
<Memo Keys="@([new List<int> { 1, 2, 3 }])">
    <ChildComponent />
</Memo>
```

### Lazy enumerables

LINQ queries and `yield return` generators are automatically materialised on snapshot so the comparison captures their current values. Concrete collections (`List<T>`, `HashSet<T>`, arrays, etc.) are kept by reference.

## Requirements

- .NET 9.0 or .NET 10.0
- ASP.NET Core (Blazor Server, WebAssembly, or Auto)

## License

[MIT](LICENSE)
