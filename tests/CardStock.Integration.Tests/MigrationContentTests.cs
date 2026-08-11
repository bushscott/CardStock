namespace CardStock.Integration.Tests;

public class MigrationContentTests
{
    /// <summary>
    /// No hand-written migration may reference the crawler's schema. Designer
    /// and snapshot files legitimately record "public" for the view mappings,
    /// so they are excluded — scoping this wrong makes the test pass always.
    ///
    /// This is the last line of defence behind the ToView mapping: if a mapping
    /// ever regresses to ToTable, the scaffolder emits CreateTable and, in
    /// Down(), DropTable against tables holding data nobody can rebuild.
    /// </summary>
    [Fact]
    public void No_migration_references_the_scraper_schema()
    {
        var migrations = Directory.GetFiles(
                Path.Combine(RepositoryRoot(), "src", "CardStock.Infrastructure", "Persistence", "Migrations"),
                "*.cs")
            .Where(f => !f.EndsWith(".Designer.cs", StringComparison.Ordinal))
            .Where(f => !f.EndsWith("ModelSnapshot.cs", StringComparison.Ordinal))
            .ToList();

        // A misresolved path would make this vacuously green.
        Assert.NotEmpty(migrations);

        var offenders = migrations
            .Where(f => File.ReadAllText(f).Contains("\"public\"", StringComparison.Ordinal))
            .Select(Path.GetFileName)
            .ToList();

        Assert.Empty(offenders);
    }

    internal static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "CardStock.slnx")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return directory!.FullName;
    }
}
