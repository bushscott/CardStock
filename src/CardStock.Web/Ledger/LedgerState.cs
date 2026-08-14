using CardStock.Application.Cards;

namespace CardStock.Web.Ledger;

/// <summary>
/// The 19-value grade vocabulary and its filter-chip partition (card.md §3.7, R-3/R-4). This
/// governs only the ledger's grade bucket, its filter chips, and the sort rank -- never the
/// six-tier price strip or chart series (R-1/R-2).
/// </summary>
public static class LedgerVocabulary
{
    /// <summary>The 19 labels in RANK order, low→high — also the sort rank (card.md R-14).
    /// Index 0 is the DB label "Ungraded", displayed as "Raw" everywhere.</summary>
    public static readonly IReadOnlyList<string> Buckets =
        ["Ungraded", "Grade 1", "Grade 2", "Grade 3", "Grade 4", "Grade 5", "Grade 6",
         "Grade 7", "Grade 8", "Grade 9", "Grade 9.5", "PSA 10", "CGC 10", "CGC 10 Prist.",
         "TAG 10", "ACE 10", "SGC 10", "BGS 10", "BGS 10 Black"];

    public static string Display(string dbLabel) => dbLabel == "Ungraded" ? "Raw" : dbLabel;

    // Filter controls (card.md §3.7): 7 direct chips + 2 groups partition all 19 exactly once.
    public static readonly IReadOnlyList<string> DirectChips =
        ["PSA 10", "Grade 9.5", "Grade 9", "Grade 8", "Grade 7", "Ungraded"]; // rendered "Raw" last
    public static readonly IReadOnlyList<string> OtherTens =
        ["CGC 10", "CGC 10 Prist.", "TAG 10", "ACE 10", "SGC 10", "BGS 10", "BGS 10 Black"];
    public static readonly IReadOnlyList<string> GradesOneToSix =
        ["Grade 6", "Grade 5", "Grade 4", "Grade 3", "Grade 2", "Grade 1"];

    /// <summary>O(1) rank lookup for the bucket comparator. Unknown labels rank lowest (-1)
    /// rather than throwing -- row data is expected to always be a Buckets member, but a sort
    /// key must never crash the page over a data surprise.</summary>
    private static readonly IReadOnlyDictionary<string, int> Rank =
        Buckets.Select((b, i) => (b, i)).ToDictionary(x => x.b, x => x.i);

    public static int RankOf(string dbLabel) => Rank.GetValueOrDefault(dbLabel, -1);
}

/// <summary>
/// Sales-ledger filter and sort state (card.md §4.4-§4.6/§5.5), pure and side-effect-free so it
/// is exhaustively unit-testable without a component host.
/// </summary>
public sealed class LedgerState
{
    /// <summary>DB labels currently selected; empty means "All" (R-16).</summary>
    public HashSet<string> Selected { get; } = [];

    public string SortKey { get; private set; } = "date";
    public bool Descending { get; private set; } = true;

    /// <summary>D-090: current zero-based page over the filtered/sorted set. Any change
    /// to filters or sort snaps back to the first page — the set under it changed.</summary>
    public int Page { get; private set; }

    public const int PageSize = 25;   // owner-tuned 2026-08-13, down from 50

    public void Toggle(string dbLabel)
    {
        if (!Selected.Remove(dbLabel))
        {
            Selected.Add(dbLabel);
        }

        Page = 0;
    }

    public void ClearAll()
    {
        Selected.Clear();
        Page = 0;
    }

    /// <summary>New key -> descending. Same key clicked again -> flip (R-13, desc-first).</summary>
    public void Sort(string key)
    {
        if (SortKey == key)
        {
            Descending = !Descending;
        }
        else
        {
            SortKey = key;
            Descending = true;
        }

        Page = 0;
    }

    public static int PageCount(int total) => Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));

    public void NextPage(int total)
    {
        if (Page < PageCount(total) - 1)
        {
            Page++;
        }
    }

    public void PrevPage()
    {
        if (Page > 0)
        {
            Page--;
        }
    }

    /// <summary>The current page's window of an already filtered/sorted list.</summary>
    public IReadOnlyList<SaleDto> Slice(IReadOnlyList<SaleDto> sorted) =>
        [.. sorted.Skip(Page * PageSize).Take(PageSize)];

    /// <summary>
    /// Filters (OR across <see cref="Selected"/>, empty = all, R-16) then sorts by
    /// <see cref="SortKey"/>/<see cref="Descending"/>. Ties fall to <c>SoldOn</c> and inherit the
    /// active direction (R-15); if that is still tied, the original row order is the final,
    /// direction-independent tiebreak, so the comparator is total and never returns 0.
    /// </summary>
    public IReadOnlyList<SaleDto> Apply(IReadOnlyList<SaleDto> rows)
    {
        var filtered = Selected.Count == 0
            ? rows
            : rows.Where(r => Selected.Contains(r.GradeTier)).ToList();

        var indexed = filtered.Select((row, index) => (row, index)).ToList();
        indexed.Sort(Compare);
        return indexed.Select(x => x.row).ToList();
    }

    private int Compare((SaleDto row, int index) a, (SaleDto row, int index) b)
    {
        var result = CompareByKeyThenDate(a.row, b.row);
        return result != 0 ? result : a.index.CompareTo(b.index);
    }

    private int CompareByKeyThenDate(SaleDto a, SaleDto b)
    {
        var primary = CompareByKey(a, b);
        if (primary != 0)
        {
            return Descending ? -primary : primary;
        }

        var dateTie = a.SoldOn.CompareTo(b.SoldOn);
        return Descending ? -dateTie : dateTie;
    }

    private int CompareByKey(SaleDto a, SaleDto b) => SortKey switch
    {
        "date" => a.SoldOn.CompareTo(b.SoldOn),
        "bucket" => LedgerVocabulary.RankOf(a.GradeTier).CompareTo(LedgerVocabulary.RankOf(b.GradeTier)),
        "price" => a.PriceCents.CompareTo(b.PriceCents),
        "src" => string.CompareOrdinal(a.Source, b.Source),
        _ => string.CompareOrdinal(a.Title, b.Title), // "anything else (title) -> string" (card.md §4.5)
    };
}
