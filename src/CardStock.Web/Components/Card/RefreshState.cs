namespace CardStock.Web.Components.Card;

/// <summary>
/// card.md §4.2.1 (D-077): the badge slot's four states. <see cref="Fresh"/> and
/// <see cref="Landed"/> both render nothing -- the reserved 28px slot
/// (IdentityHeader.razor's .badge-slot) is what keeps the six price cells from jumping,
/// not visible content in either of those two states.
/// </summary>
public enum RefreshState
{
    /// <summary>Last visited within 24h -- no call ever left the browser.</summary>
    Fresh,

    /// <summary>An express-visit is genuinely in flight. Renders the animated mark.</summary>
    Fetching,

    /// <summary>The refresh returned 200 and the snapshot was refetched in place.</summary>
    Landed,

    /// <summary>The refresh returned anything other than 200. Prices are unchanged --
    /// they were never wrong, only old.</summary>
    Failed,
}
