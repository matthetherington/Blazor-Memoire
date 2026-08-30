using System.Collections;

namespace BlazorMemoire.Components.Tests;

/// <summary>
/// A dictionary that is only an <see cref="IReadOnlyDictionary{TKey, TValue}"/>. Every
/// dictionary in the BCL also implements the non-generic <see cref="IDictionary"/>, which
/// is what the by-key comparison keys off, so a hand-rolled one is the only way to reach
/// the positional fallback.
/// </summary>
public sealed class ReadOnlyLookup(IDictionary<string, int> inner)
    : IReadOnlyDictionary<string, int>
{
    public int this[string key] => inner[key];

    public IEnumerable<string> Keys => inner.Keys;

    public IEnumerable<int> Values => inner.Values;

    public int Count => inner.Count;

    public bool ContainsKey(string key) => inner.ContainsKey(key);

    public bool TryGetValue(string key, out int value) => inner.TryGetValue(key, out value);

    public IEnumerator<KeyValuePair<string, int>> GetEnumerator() => inner.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
