namespace CardStock.Web.Components.Catalog;

/// <summary>One sort, two surfaces (set.md §6.1): pills, table headers, and the
/// binder grid all read this single instance, so they can never disagree.</summary>
public sealed class SortState(string initialKey)
{
    public string Key { get; private set; } = initialKey;

    public bool Descending { get; private set; } = true;

    /// <summary>Same key flips direction; a new key always starts descending.</summary>
    public void Apply(string key)
    {
        if (Key == key)
        {
            Descending = !Descending;
        }
        else
        {
            Key = key;
            Descending = true;
        }
    }
}

/// <summary>A deferred pill renders disabled with its gate tooltip (D-087's
/// control half — controls disable, statistics get the ◌).</summary>
public sealed record SortPill(
    string Key, string Label, string Tooltip, bool Deferred, string? DeferredTooltip);
