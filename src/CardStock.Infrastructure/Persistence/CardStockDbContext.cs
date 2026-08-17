using CardStock.Infrastructure.Persistence.Entities;
using CardStock.Infrastructure.Persistence.ScraperReadModels;
using Microsoft.EntityFrameworkCore;

namespace CardStock.Infrastructure.Persistence;

/// <summary>
/// CardStock owns the <c>cardstock</c> schema. PokemonInvestBatch owns
/// <c>public</c> and is the only writer to it (ADR-0001, D-026). The crawler's
/// tables appear here as views so this context can read them and can never
/// migrate or write them.
/// </summary>
public class CardStockDbContext(DbContextOptions<CardStockDbContext> options) : DbContext(options)
{
    public const string Schema = "cardstock";

    public const string ScraperSchema = "public";

    // CardStock-owned, migrated.
    public DbSet<AppUser> Users => Set<AppUser>();

    public DbSet<UserSession> Sessions => Set<UserSession>();

    // PokemonInvestBatch-owned, view-mapped, never migrated, never written.
    public DbSet<ScraperSet> ScraperSets => Set<ScraperSet>();

    public DbSet<ScraperCard> ScraperCards => Set<ScraperCard>();

    public DbSet<ScraperPriceMonth> ScraperPriceMonths => Set<ScraperPriceMonth>();

    public DbSet<ScraperPopulation> ScraperPopulations => Set<ScraperPopulation>();

    public DbSet<ScraperSale> ScraperSales => Set<ScraperSale>();

    public DbSet<ScraperSpecies> ScraperSpecies => Set<ScraperSpecies>();

    public DbSet<ScraperSpeciesType> ScraperSpeciesTypes => Set<ScraperSpeciesType>();

    public DbSet<ScraperSpeciesEggGroup> ScraperSpeciesEggGroups => Set<ScraperSpeciesEggGroup>();

    public DbSet<ScraperCardSpecies> ScraperCardSpecies => Set<ScraperCardSpecies>();

    public DbSet<ScraperSetDetail> ScraperSetDetails => Set<ScraperSetDetail>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema(Schema);

        ScraperViews.Map(builder);

        builder.Entity<AppUser>(user =>
        {
            // 320 is the practical maximum length of an email address.
            user.Property(u => u.Email).HasMaxLength(320);
            user.HasIndex(u => u.Email).IsUnique();
        });

        builder.Entity<UserSession>(session =>
        {
            session.HasKey(s => s.Id);
            session.Property(s => s.Id).HasMaxLength(64);

            // Written explicitly, per ADR-0001: EF's default for a required
            // relationship is Cascade, and every relationship in this codebase
            // states its intent rather than inheriting one. Cascade is correct
            // here -- deleting an account must take its sessions with it, since
            // deletion is immediate and permanent (D-069).
            session.HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // The worker's session sweep reads this (D-039).
            session.HasIndex(s => s.ExpiresAt);
        });
    }
}
