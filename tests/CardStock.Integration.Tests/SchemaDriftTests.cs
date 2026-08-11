using System.Security.Cryptography;
using System.Text;

namespace CardStock.Integration.Tests;

public class SchemaDriftTests
{
    /// <summary>
    /// The committed fixture is an unversioned copy of another repo's schema,
    /// and drift is silent. It has precedent: 20260801024826_WidenImageHash
    /// changed a column's type and 20260808022824_RenameShapesToFingerprints
    /// renamed a table and its columns.
    ///
    /// Rather than regenerate the script — which means building the sibling
    /// repo from inside a test run, and deadlocks or crawls against the parent's
    /// MSBuild locks — this fingerprints the sibling's migration sources. Any
    /// change to them fires, which is the right sensitivity: a new crawler
    /// migration always deserves a human deciding whether CardStock reads the
    /// column it touched.
    ///
    /// Runs only where the sibling repo is checked out, so CI and other machines
    /// skip rather than fail.
    /// </summary>
    [SkippableFact]
    public void Committed_scraper_schema_fixture_matches_the_sibling_repo()
    {
        var migrationsDirectory = Path.GetFullPath(Path.Combine(
            MigrationContentTests.RepositoryRoot(),
            "..", "PokemonInvestBatch",
            "src", "PokemonInvestBatch.Infrastructure", "Persistence", "Migrations"));

        Skip.IfNot(Directory.Exists(migrationsDirectory), "../PokemonInvestBatch is not checked out");

        var expected = File.ReadAllText(FingerprintPath()).Trim();
        var actual = Fingerprint(migrationsDirectory);

        Assert.True(
            expected == actual,
            $"""
             The crawler's migrations have changed since the schema fixture was generated.

             This is not a test to silence. Read the sibling's new migration, decide whether
             CardStock reads the column it touched, then regenerate both files:

               cd ../PokemonInvestBatch && dotnet ef migrations script \
                 -p src/PokemonInvestBatch.Infrastructure -s src/PokemonInvestBatch.Infrastructure \
                 -o ../CardStock/tests/CardStock.TestSupport/Fixtures/scraper-schema.sql

             and write the new fingerprint below into Fixtures/scraper-schema.fingerprint:

               expected: {expected}
               actual:   {actual}
             """);
    }

    private static string FingerprintPath() => Path.Combine(
        MigrationContentTests.RepositoryRoot(),
        "tests", "CardStock.TestSupport", "Fixtures", "scraper-schema.fingerprint");

    /// <summary>
    /// A stable hash over every migration source in the sibling, ordered by
    /// name. Line endings are normalized so a checkout on another platform does
    /// not read as drift.
    /// </summary>
    private static string Fingerprint(string migrationsDirectory)
    {
        var builder = new StringBuilder();

        foreach (var file in Directory.GetFiles(migrationsDirectory, "*.cs").OrderBy(f => f, StringComparer.Ordinal))
        {
            builder.Append(Path.GetFileName(file));
            builder.Append('\n');
            builder.Append(File.ReadAllText(file).ReplaceLineEndings("\n"));
            builder.Append('\n');
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()))).ToLowerInvariant();
    }
}
