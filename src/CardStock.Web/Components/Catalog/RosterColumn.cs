using Microsoft.AspNetCore.Components;

namespace CardStock.Web.Components.Catalog;

/// <summary>One terminal-roster column. Deferred ⇒ header carries the ◌ with
/// DeferredTooltip and the column never sorts; Sortable=false ⇒ no pointer, no
/// hover, no dead affordance (D-110 spec §5).</summary>
public sealed record RosterColumn<TRow>(
    string Key,
    string Header,
    int DefaultWidth,
    string Tooltip,
    bool Sortable,
    bool Deferred,
    string? DeferredTooltip,
    RenderFragment<TRow> Cell);
