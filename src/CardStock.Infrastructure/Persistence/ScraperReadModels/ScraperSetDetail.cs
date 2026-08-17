namespace CardStock.Infrastructure.Persistence.ScraperReadModels;

/// <summary>Read-only mirror of public.set_details. One row per set, always;
/// MatchStatus 0 = Matched (code/date/series written), 1 = Pending (all null).</summary>
public class ScraperSetDetail : IScraperOwned
{
    public long SetId { get; init; }

    public short MatchStatus { get; init; }

    /// <summary>TCGdex id verbatim ("swsh7") — display formatting is CardStock's job.</summary>
    public string? Code { get; init; }

    public DateOnly? ReleasedOn { get; init; }

    public string? Series { get; init; }

    public string? Era { get; init; }
}
