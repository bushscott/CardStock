using CardStock.Application.Cards;
using CardStock.Web.Ledger;

namespace CardStock.Web.Tests;

// card.md §3.7/§4.4-§4.6/§5.5, R-4/R-13/R-14/R-15/R-16 -- pure state logic, exhaustive per the
// task-18 brief: partition, rank sort, desc-first/flip, tie-break-inherits-direction, OR filter.
public class LedgerStateTests
{
    private static SaleDto Sale(string date, string bucket, int priceCents, string src = "ebay",
        string title = "listing", int? listedCents = null) =>
        new(DateOnly.Parse(date), bucket, priceCents, listedCents, src, title);

    // ---- LedgerVocabulary ----

    [Fact]
    public void Partition_covers_all_19_buckets_exactly_once()
    {
        var partition = LedgerVocabulary.DirectChips
            .Concat(LedgerVocabulary.OtherTens)
            .Concat(LedgerVocabulary.GradesOneToSix)
            .ToList();

        Assert.Equal(19, partition.Count);
        Assert.Equal(19, partition.Distinct().Count());
        Assert.Equal(LedgerVocabulary.Buckets.OrderBy(b => b).ToList(),
            partition.OrderBy(b => b).ToList());
        Assert.Equal(19, LedgerVocabulary.Buckets.Count);
    }

    [Fact]
    public void Grade_nine_point_five_ranks_between_grade_nine_and_psa_ten()
    {
        var g9 = LedgerVocabulary.RankOf("Grade 9");
        var g95 = LedgerVocabulary.RankOf("Grade 9.5");
        var psa10 = LedgerVocabulary.RankOf("PSA 10");

        Assert.True(g9 < g95);
        Assert.True(g95 < psa10);
    }

    [Fact]
    public void Display_maps_the_db_label_ungraded_to_raw()
    {
        Assert.Equal("Raw", LedgerVocabulary.Display("Ungraded"));
    }

    [Theory]
    [InlineData("Grade 1")]
    [InlineData("PSA 10")]
    [InlineData("BGS 10 Black")]
    public void Display_is_the_identity_for_every_other_label(string label)
    {
        Assert.Equal(label, LedgerVocabulary.Display(label));
    }

    // ---- Toggle / ClearAll ----

    [Fact]
    public void Toggle_adds_an_unselected_bucket_and_removes_a_selected_one()
    {
        var state = new LedgerState();

        state.Toggle("PSA 10");
        Assert.Contains("PSA 10", state.Selected);

        state.Toggle("PSA 10");
        Assert.DoesNotContain("PSA 10", state.Selected);
    }

    [Fact]
    public void ClearAll_empties_the_selection()
    {
        var state = new LedgerState();
        state.Toggle("PSA 10");
        state.Toggle("Grade 9");

        state.ClearAll();

        Assert.Empty(state.Selected);
    }

    // ---- Sort: desc-first, flip, default ----

    [Fact]
    public void Default_sort_is_date_descending()
    {
        var state = new LedgerState();
        Assert.Equal("date", state.SortKey);
        Assert.True(state.Descending);
    }

    [Fact]
    public void A_new_column_always_sorts_descending_first()
    {
        var state = new LedgerState();
        state.Sort("date"); // flips date -> asc
        Assert.False(state.Descending);

        state.Sort("price"); // new key -> desc, regardless of prior direction
        Assert.Equal("price", state.SortKey);
        Assert.True(state.Descending);
    }

    [Fact]
    public void The_same_column_flips_direction_each_click()
    {
        var state = new LedgerState();
        state.Sort("price");
        Assert.True(state.Descending);

        state.Sort("price");
        Assert.False(state.Descending);

        state.Sort("price");
        Assert.True(state.Descending);
    }

    // ---- Apply: filter OR, empty = all ----

    [Fact]
    public void Empty_selection_returns_every_row()
    {
        var state = new LedgerState();
        var rows = new[] { Sale("2026-08-01", "PSA 10", 100), Sale("2026-08-02", "Grade 9", 200) };

        Assert.Equal(2, state.Apply(rows).Count);
    }

    [Fact]
    public void Selecting_several_buckets_filters_with_or_semantics()
    {
        var state = new LedgerState();
        state.Toggle("PSA 10");
        state.Toggle("Grade 7");
        var rows = new[]
        {
            Sale("2026-08-01", "PSA 10", 100),
            Sale("2026-08-02", "Grade 9", 200),
            Sale("2026-08-03", "Grade 7", 300),
        };

        var filtered = state.Apply(rows);

        Assert.Equal(2, filtered.Count);
        Assert.All(filtered, r => Assert.True(r.GradeTier is "PSA 10" or "Grade 7"));
    }

    [Fact]
    public void Deselecting_the_last_bucket_returns_to_the_all_state()
    {
        var state = new LedgerState();
        state.Toggle("PSA 10");
        state.Toggle("PSA 10"); // back off
        var rows = new[] { Sale("2026-08-01", "PSA 10", 100), Sale("2026-08-02", "Grade 9", 200) };

        Assert.Equal(2, state.Apply(rows).Count);
    }

    // ---- Apply: comparators ----

    [Fact]
    public void Date_sort_orders_by_sold_on()
    {
        var state = new LedgerState(); // default date/desc
        var rows = new[] { Sale("2026-08-01", "PSA 10", 100), Sale("2026-08-03", "PSA 10", 100), Sale("2026-08-02", "PSA 10", 100) };

        var ordered = state.Apply(rows).Select(r => r.SoldOn.ToString("yyyy-MM-dd")).ToList();

        Assert.Equal(["2026-08-03", "2026-08-02", "2026-08-01"], ordered);
    }

    [Fact]
    public void Bucket_sort_orders_by_rank_not_alphabetically()
    {
        var state = new LedgerState();
        state.Sort("bucket"); // desc
        var rows = new[]
        {
            Sale("2026-08-01", "Grade 9", 100),
            Sale("2026-08-01", "Grade 9.5", 100),
            Sale("2026-08-01", "PSA 10", 100),
        };

        var ordered = state.Apply(rows).Select(r => r.GradeTier).ToList();

        // Descending rank: PSA 10 (18th) > Grade 9.5 (10th) > Grade 9 (9th). Alphabetically
        // "Grade 9" < "Grade 9.5" < "PSA 10" would give a different, wrong order.
        Assert.Equal(["PSA 10", "Grade 9.5", "Grade 9"], ordered);
    }

    [Fact]
    public void Price_sort_is_numeric_on_price_cents()
    {
        var state = new LedgerState();
        state.Sort("price"); // desc
        var rows = new[] { Sale("2026-08-01", "PSA 10", 100), Sale("2026-08-01", "PSA 10", 30000), Sale("2026-08-01", "PSA 10", 500) };

        var ordered = state.Apply(rows).Select(r => r.PriceCents).ToList();

        Assert.Equal([30000, 500, 100], ordered);
    }

    [Fact]
    public void Source_sort_is_ordinal_string_comparison()
    {
        var state = new LedgerState();
        state.Sort("src"); // desc
        var rows = new[] { Sale("2026-08-01", "PSA 10", 100, "ebay"), Sale("2026-08-01", "PSA 10", 100, "pwcc"), Sale("2026-08-01", "PSA 10", 100, "goldin") };

        var ordered = state.Apply(rows).Select(r => r.Source).ToList();

        Assert.Equal(["pwcc", "goldin", "ebay"], ordered);
    }

    [Fact]
    public void Title_sort_is_ordinal_string_comparison()
    {
        var state = new LedgerState();
        state.Sort("title"); // desc
        var rows = new[]
        {
            Sale("2026-08-01", "PSA 10", 100, title: "Alpha"),
            Sale("2026-08-01", "PSA 10", 100, title: "beta"),
            Sale("2026-08-01", "PSA 10", 100, title: "Gamma"),
        };

        var ordered = state.Apply(rows).Select(r => r.Title).ToList();

        // Ordinal, case-sensitive: uppercase sorts before lowercase, so "beta" (lowercase) is
        // the ordinal maximum -- first under descending.
        Assert.Equal(["beta", "Gamma", "Alpha"], ordered);
    }

    // ---- Apply: tie-break falls to date and inherits direction ----

    [Fact]
    public void Ties_on_the_primary_key_fall_to_date_and_inherit_ascending_direction()
    {
        var state = new LedgerState();
        state.Sort("price"); // desc
        state.Sort("price"); // flip -> asc
        var older = Sale("2026-07-01", "PSA 10", 500);
        var newer = Sale("2026-08-01", "PSA 10", 500);

        var ordered = state.Apply([newer, older]);

        Assert.Equal(older, ordered[0]); // asc -> oldest first
        Assert.Equal(newer, ordered[1]);
    }

    [Fact]
    public void Ties_on_the_primary_key_fall_to_date_and_inherit_descending_direction()
    {
        var state = new LedgerState();
        state.Sort("price"); // desc (default direction for a new key)
        var older = Sale("2026-07-01", "PSA 10", 500);
        var newer = Sale("2026-08-01", "PSA 10", 500);

        var ordered = state.Apply([older, newer]);

        Assert.Equal(newer, ordered[0]); // desc -> newest first
        Assert.Equal(older, ordered[1]);
    }

    [Fact]
    public void The_comparator_never_returns_zero_full_ties_fall_back_to_original_index()
    {
        var state = new LedgerState(); // date/desc
        var a = Sale("2026-08-01", "PSA 10", 500, "ebay", "same");
        var b = Sale("2026-08-01", "PSA 10", 500, "ebay", "same");
        var c = Sale("2026-08-01", "PSA 10", 500, "ebay", "same");

        var ordered = state.Apply([a, b, c]);

        // Fully tied rows (same date too) keep their original relative order regardless of
        // direction -- the sort is total and deterministic without reordering identical rows.
        Assert.Same(a, ordered[0]);
        Assert.Same(b, ordered[1]);
        Assert.Same(c, ordered[2]);
    }

    // ---- D-090: client-side paging over the filtered/sorted set ----

    [Fact]
    public void Paging_slices_twenty_five_rows_and_clamps_at_both_ends()
    {
        var state = new LedgerState();
        var rows = Enumerable.Range(0, 120)
            .Select(i => Sale("2026-08-01", "PSA 10", i))
            .ToList();

        Assert.Equal(5, LedgerState.PageCount(120));
        Assert.Equal(1, LedgerState.PageCount(0));      // an empty set is one empty page

        Assert.Equal(25, state.Slice(rows).Count);
        Assert.Equal(0, state.Slice(rows)[0].PriceCents);

        state.PrevPage();                                // already first -> no-op
        Assert.Equal(0, state.Page);

        state.NextPage(120);
        Assert.Equal(25, state.Slice(rows)[0].PriceCents);

        state.NextPage(120);
        state.NextPage(120);
        state.NextPage(120);
        Assert.Equal(20, state.Slice(rows).Count);       // the 120-row tail page

        state.NextPage(120);                             // already last -> no-op
        Assert.Equal(4, state.Page);
    }

    [Fact]
    public void Filter_sort_and_clear_changes_reset_to_the_first_page()
    {
        var state = new LedgerState();

        state.NextPage(120);
        state.Toggle("PSA 10");
        Assert.Equal(0, state.Page);

        state.NextPage(120);
        state.Sort("price");
        Assert.Equal(0, state.Page);

        state.NextPage(120);
        state.ClearAll();
        Assert.Equal(0, state.Page);
    }
}
