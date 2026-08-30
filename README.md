# BlazorMemoire

A Blazor memoisation component that freezes its child subtree until explicit dependency keys change, preventing unnecessary re-renders.

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

| Keys | Behaviour |
|------|-----------|
| `null` | No memoisation -- renders on every parent render |
| `[]` | Render once, freeze forever |
| `[a, b, c]` | Re-render only when a key element changes |

### Deep value equality

Keys are compared using deep value equality, not reference equality:

- **Primitives** -- compared by value
- **Records** -- compared by value
- **Collections** (arrays, lists, dictionaries, sets) -- compared element-wise
- **Nested collections** -- compared recursively

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
