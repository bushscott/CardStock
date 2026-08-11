# CardStock First Slice Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Stand up the CardStock solution with its own Postgres schema alongside the crawler's, prove a cross-schema read works end to end, and run it on the Pi.

**Architecture:** One `pokemon` database holding two schemas. The crawler keeps `public`; CardStock owns `cardstock` under its own role. CardStock maps the crawler's tables as EF **views** so its migrations can never emit DDL against them, and pins its own migrations history table so the two EF lineages cannot see each other. Nothing auto-migrates — DDL is a deliberate human act from a dev machine, mirroring the crawler exactly.

**Tech Stack:** .NET 10, EF Core 10.0.10, Npgsql 10.0.3, EFCore.NamingConventions 10.0.1, PostgreSQL 15, xUnit 2.9.3, Blazor WebAssembly.

## Global Constraints

Every task's requirements implicitly include this section. Values are copied verbatim from the sibling repo and from `docs/adr/0001-schema-separation-and-migration-ownership.md`.

- **Target framework:** `net10.0`. `Nullable=enable`, `ImplicitUsings=enable`, **`TreatWarningsAsErrors=true`**.
- **DO NOT set `InvariantGlobalization`.** The crawler sets it repo-wide to drop libicu; CardStock formats currency for humans (`docs/screens/screener.md:108`) and copying it changes formatting silently, with no error and no failing test. This is a deliberate divergence (D-070 discussion).
- **Package versions, exactly:** `Microsoft.EntityFrameworkCore` 10.0.10, `Microsoft.EntityFrameworkCore.Design` 10.0.10 (`PrivateAssets=all`), `Microsoft.EntityFrameworkCore.Relational` 10.0.10, `Npgsql.EntityFrameworkCore.PostgreSQL` **10.0.3**, `EFCore.NamingConventions` **10.0.1**.
- **`dotnet-ef` 10.0.10 as a LOCAL tool in `.config/dotnet-tools.json`.** Verified 2026-08-11: the global tool on this machine is **10.0.8**, older than the runtime packages. The crawler's manifest sits at the repo root instead of `.config/`, where `dotnet tool restore` does not look, so it has never used its own pin. Do not copy that mistake.
- **Formatting:** LF endings, UTF-8, final newline, 4-space indent for C#, 2-space for csproj/props/json/yml. File-scoped namespaces at `warning`. Explicit accessibility modifiers at `warning`.
- **PostgreSQL 15.** Verified on the Pi 2026-08-11 as 15.18. Pin CI to `postgres:15`.
- **Naming:** `UseSnakeCaseNamingConvention()` on every context construction, applied through one shared helper so a fourth call site cannot forget it.
- **Nothing calls `Database.Migrate()` or `MigrateAsync()` in `src/`.** Test harnesses may; application code may not.
- **Every relationship writes `.OnDelete(...)` explicitly.** EF's default for a required relationship is Cascade, and a cascade from `users` through `transactions` would destroy the audit log the Binder promises.

## File Structure

```
CardStock/
  .config/dotnet-tools.json          dotnet-ef pinned to 10.0.10, rollForward false
  .editorconfig                      copied verbatim from the sibling
  Directory.Build.props              net10.0 + Nullable + ImplicitUsings + TreatWarningsAsErrors
  CardStock.slnx
  .github/workflows/ci.yml           restore → build → test → format, postgres:15 service
  ops/
    cardstock-postgres-setup.sql     roles, schema, grants — run once by a superuser
    README.md                        the runbook: migrations, deploy, post-migration grants
    cardstock-api.service            systemd unit
  src/
    CardStock.Domain/                references nothing
    CardStock.Application/
    CardStock.Infrastructure/
      Persistence/
        CardStockDbContext.cs        the context; owns Schema constants
        CardStockDbContextOptions.cs the ONE place UseNpgsql + snake_case + history table are set
        CardStockDbContextFactory.cs design-time factory, with the wrong-credential guard
        ScraperViews.cs              all five view mirrors in one file
        Entities/AppUser.cs
        Entities/UserSession.cs
        ScraperReadModels/           ScraperCard, ScraperSet, ScraperPriceMonth,
                                     ScraperPopulation, ScraperSale, IScraperOwned, PriceTier
        Migrations/                  scaffolded; excluded from dotnet format
    CardStock.Api/                   stateless minimal API
    CardStock.Web/                   Blazor WASM — created here, no screens in this slice
    CardStock.Worker/                created here, no jobs in this slice
  tests/
    CardStock.TestSupport/
      CardStockDatabaseTest.cs       throwaway database per test
      Fixtures/scraper-schema.sql    generated from the sibling, committed, drift-guarded
    CardStock.Domain.Tests/
    CardStock.Application.Tests/
    CardStock.Infrastructure.Tests/  the model guards — no database needed
    CardStock.Integration.Tests/     real Postgres
```

**Out of scope for this slice, deliberately:** any screen, the `transactions` table and its foreign key into `public.cards` (that arrives with the Binder), the worker's jobs, and **public network exposure** — D-037 leaves the choice between a DMZ VLAN and a Cloudflare Tunnel unresolved, so Task 6 stops at "running on the Pi, reachable on the LAN."

---

### Task 1: Repository skeleton, tooling, and CI

**Files:**
- Create: `.editorconfig`, `Directory.Build.props`, `.config/dotnet-tools.json`, `CardStock.slnx`, `.github/workflows/ci.yml`
- Create: eleven `.csproj` files under `src/` and `tests/`

**Interfaces:**
- Consumes: nothing
- Produces: a solution that builds clean and a CI pipeline that runs. Project names `CardStock.Domain`, `CardStock.Application`, `CardStock.Infrastructure`, `CardStock.Api`, `CardStock.Web`, `CardStock.Worker`; test projects `CardStock.Domain.Tests`, `CardStock.Application.Tests`, `CardStock.Infrastructure.Tests`, `CardStock.Integration.Tests`, `CardStock.TestSupport`.

- [ ] **Step 1: Create the shared build configuration**

`Directory.Build.props`:

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <!-- Deliberately NOT InvariantGlobalization: the crawler sets it to drop
         libicu on the Pi, but CardStock formats currency for humans and the
         difference is silent. See the plan's Global Constraints. -->
  </PropertyGroup>
</Project>
```

`.editorconfig` (verbatim from the sibling, so the two repos read identically):

```ini
root = true

[*]
charset = utf-8
end_of_line = lf
insert_final_newline = true
indent_style = space
indent_size = 4
trim_trailing_whitespace = true

[*.{csproj,props,targets,sln,json,yml,yaml}]
indent_size = 2

[*.cs]
dotnet_sort_system_directives_first = true
csharp_style_namespace_declarations = file_scoped:warning
csharp_style_var_when_type_is_apparent = true:suggestion
dotnet_style_require_accessibility_modifiers = always:warning
```

`.config/dotnet-tools.json` — note the directory:

```json
{
  "version": 1,
  "isRoot": true,
  "tools": {
    "dotnet-ef": {
      "version": "10.0.10",
      "commands": [
        "dotnet-ef"
      ],
      "rollForward": false
    }
  }
}
```

- [ ] **Step 2: Create the projects**

```bash
cd /Users/scott/RiderProjects/CardStock
dotnet new classlib -o src/CardStock.Domain -n CardStock.Domain
dotnet new classlib -o src/CardStock.Application -n CardStock.Application
dotnet new classlib -o src/CardStock.Infrastructure -n CardStock.Infrastructure
dotnet new web      -o src/CardStock.Api -n CardStock.Api
dotnet new blazorwasm -o src/CardStock.Web -n CardStock.Web
dotnet new worker   -o src/CardStock.Worker -n CardStock.Worker

dotnet new classlib -o tests/CardStock.TestSupport -n CardStock.TestSupport
dotnet new xunit -o tests/CardStock.Domain.Tests -n CardStock.Domain.Tests
dotnet new xunit -o tests/CardStock.Application.Tests -n CardStock.Application.Tests
dotnet new xunit -o tests/CardStock.Infrastructure.Tests -n CardStock.Infrastructure.Tests
dotnet new xunit -o tests/CardStock.Integration.Tests -n CardStock.Integration.Tests
```

Delete every generated `Class1.cs`, `UnitTest1.cs`, and `Worker.cs` stub.

- [ ] **Step 3: Wire the one-directional reference chain**

```bash
dotnet add src/CardStock.Application reference src/CardStock.Domain
dotnet add src/CardStock.Infrastructure reference src/CardStock.Application
dotnet add src/CardStock.Api reference src/CardStock.Infrastructure
dotnet add src/CardStock.Worker reference src/CardStock.Infrastructure

dotnet add tests/CardStock.TestSupport reference src/CardStock.Infrastructure
dotnet add tests/CardStock.Domain.Tests reference src/CardStock.Domain
dotnet add tests/CardStock.Application.Tests reference src/CardStock.Application
dotnet add tests/CardStock.Infrastructure.Tests reference src/CardStock.Infrastructure
dotnet add tests/CardStock.Infrastructure.Tests reference tests/CardStock.TestSupport
dotnet add tests/CardStock.Integration.Tests reference tests/CardStock.TestSupport
```

`CardStock.Domain` must reference nothing. Verify: its `.csproj` has no `ProjectReference`.

- [ ] **Step 4: Add the EF packages to Infrastructure**

```bash
dotnet add src/CardStock.Infrastructure package Microsoft.EntityFrameworkCore --version 10.0.10
dotnet add src/CardStock.Infrastructure package Microsoft.EntityFrameworkCore.Relational --version 10.0.10
dotnet add src/CardStock.Infrastructure package Microsoft.EntityFrameworkCore.Design --version 10.0.10
dotnet add src/CardStock.Infrastructure package Npgsql.EntityFrameworkCore.PostgreSQL --version 10.0.3
dotnet add src/CardStock.Infrastructure package EFCore.NamingConventions --version 10.0.1
```

Then edit `src/CardStock.Infrastructure/CardStock.Infrastructure.csproj` so the Design package carries:

```xml
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.10">
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
  <PrivateAssets>all</PrivateAssets>
</PackageReference>
```

Add to `tests/CardStock.TestSupport`: `xunit.extensibility.core` 2.9.3, and a project reference to Infrastructure (done in Step 3).

- [ ] **Step 5: Create `CardStock.slnx`**

```xml
<Solution>
  <Folder Name="/src/">
    <Project Path="src/CardStock.Api/CardStock.Api.csproj" />
    <Project Path="src/CardStock.Application/CardStock.Application.csproj" />
    <Project Path="src/CardStock.Domain/CardStock.Domain.csproj" />
    <Project Path="src/CardStock.Infrastructure/CardStock.Infrastructure.csproj" />
    <Project Path="src/CardStock.Web/CardStock.Web.csproj" />
    <Project Path="src/CardStock.Worker/CardStock.Worker.csproj" />
  </Folder>
  <Folder Name="/tests/">
    <Project Path="tests/CardStock.Application.Tests/CardStock.Application.Tests.csproj" />
    <Project Path="tests/CardStock.Domain.Tests/CardStock.Domain.Tests.csproj" />
    <Project Path="tests/CardStock.Infrastructure.Tests/CardStock.Infrastructure.Tests.csproj" />
    <Project Path="tests/CardStock.Integration.Tests/CardStock.Integration.Tests.csproj" />
    <Project Path="tests/CardStock.TestSupport/CardStock.TestSupport.csproj" />
  </Folder>
</Solution>
```

- [ ] **Step 6: Verify the build is clean**

Run: `dotnet tool restore && dotnet build CardStock.slnx -c Release`
Expected: PASS, zero warnings. `TreatWarningsAsErrors` means any warning is a failure — fix them now rather than accumulating them.

Run: `dotnet ef --version`
Expected: `10.0.10` (the local tool), **not** 10.0.8. If it prints 10.0.8, `dotnet tool restore` did not run or the manifest is in the wrong directory.

- [ ] **Step 7: Add CI**

`.github/workflows/ci.yml`:

```yaml
name: CI

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]
  workflow_dispatch:

jobs:
  build-and-test:
    name: Build and test
    runs-on: ubuntu-latest

    # Matches the Pi's PostgreSQL 15.18, verified 2026-08-11. A version drift
    # here would pass in CI and fail in production.
    services:
      postgres:
        image: postgres:15
        env:
          POSTGRES_PASSWORD: postgres
        ports:
          - 5432:5432
        options: >-
          --health-cmd pg_isready
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5

    env:
      # Only a template: each test creates and drops a database of its own
      # beside this one.
      CARDSTOCK_TEST_DB: "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres"

    steps:
      - uses: actions/checkout@v4

      - name: Set up .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Cache NuGet packages
        uses: actions/cache@v4
        with:
          path: ~/.nuget/packages
          key: ${{ runner.os }}-nuget-${{ hashFiles('**/*.csproj') }}
          restore-keys: ${{ runner.os }}-nuget-

      # The sibling's CI omits this and therefore never uses its own tool pin.
      - name: Restore tools
        run: dotnet tool restore

      - name: Restore
        run: dotnet restore CardStock.slnx

      - name: Build
        run: dotnet build CardStock.slnx --no-restore -c Release

      - name: Test
        run: dotnet test CardStock.slnx --no-build -c Release

      # EF scaffolds migrations with a byte-order mark the shared .editorconfig
      # rejects, so generated code is excluded rather than hand-edited.
      - name: Verify formatting
        run: dotnet format CardStock.slnx --verify-no-changes --severity error --exclude '**/Migrations/**'
```

- [ ] **Step 8: Verify formatting passes locally**

Run: `dotnet format CardStock.slnx --verify-no-changes --severity error --exclude '**/Migrations/**'`
Expected: PASS with no output.

- [ ] **Step 9: Commit**

```bash
git add -A
git commit -m "Solution skeleton, tooling, and CI

Six source projects in a one-directional chain and five test projects,
mirroring PokemonInvestBatch. dotnet-ef pinned at 10.0.10 in .config/
rather than the repo root, where the sibling's manifest sits and is never
found by 'dotnet tool restore'. InvariantGlobalization deliberately not
copied -- CardStock formats currency for humans."
```

---

### Task 2: The DbContext, the scraper view mirrors, and the model guards

**Files:**
- Create: `src/CardStock.Infrastructure/Persistence/CardStockDbContext.cs`
- Create: `src/CardStock.Infrastructure/Persistence/CardStockDbContextOptions.cs`
- Create: `src/CardStock.Infrastructure/Persistence/ScraperViews.cs`
- Create: `src/CardStock.Infrastructure/Persistence/ScraperReadModels/*.cs`
- Create: `src/CardStock.Infrastructure/Persistence/Entities/AppUser.cs`, `Entities/UserSession.cs`
- Test: `tests/CardStock.Infrastructure.Tests/Persistence/SchemaModelTests.cs`

**Interfaces:**
- Consumes: the project skeleton from Task 1.
- Produces: `CardStockDbContext` with `public const string Schema = "cardstock"` and `public const string ScraperSchema = "public"`; extension method `DbContextOptionsBuilder<CardStockDbContext>.UseCardStock(string connectionString)`; marker interface `IScraperOwned`; entities `AppUser`, `UserSession`; read models `ScraperCard`, `ScraperSet`, `ScraperPriceMonth`, `ScraperPopulation`, `ScraperSale`.

- [ ] **Step 1: Write the failing guard tests**

`tests/CardStock.Infrastructure.Tests/Persistence/SchemaModelTests.cs`:

```csharp
using CardStock.Infrastructure.Persistence;
using CardStock.Infrastructure.Persistence.ScraperReadModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace CardStock.Infrastructure.Tests.Persistence;

/// <summary>
/// The boundary guarantees of ADR-0001, asserted against the compiled EF model
/// with no database. Each one catches a failure that is otherwise silent until
/// it reaches production.
/// </summary>
public class SchemaModelTests
{
    private static DbContextOptions<CardStockDbContext> Options() =>
        new DbContextOptionsBuilder<CardStockDbContext>()
            .UseCardStock("Host=model-only")
            .Options;

    private static IModel Model()
    {
        using var context = new CardStockDbContext(Options());
        return context.Model;
    }

    [Fact]
    public void Nothing_CardStock_migrates_lives_outside_its_own_schema()
    {
        foreach (var entity in Model().GetEntityTypes())
        {
            if (entity.GetTableName() is not null)
            {
                Assert.Equal(CardStockDbContext.Schema, entity.GetSchema());
            }
        }
    }

    [Fact]
    public void Every_scraper_owned_type_is_mapped_to_a_view_and_never_a_table()
    {
        var scraperTypes = Model().GetEntityTypes()
            .Where(e => typeof(IScraperOwned).IsAssignableFrom(e.ClrType))
            .ToList();

        Assert.Equal(5, scraperTypes.Count);

        foreach (var entity in scraperTypes)
        {
            Assert.Null(entity.GetTableName());
            Assert.Equal("public", entity.GetViewSchema());
            Assert.NotNull(entity.GetViewName());
        }
    }

    [Fact]
    public void Migrations_history_table_is_pinned_to_the_cardstock_schema()
    {
        var extension = RelationalOptionsExtension.Extract(Options());

        Assert.Equal("__cardstock_migrations_history", extension.MigrationsHistoryTableName);
        Assert.Equal(CardStockDbContext.Schema, extension.MigrationsHistoryTableSchema);
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/CardStock.Infrastructure.Tests -v minimal`
Expected: FAIL — `CardStockDbContext` and `UseCardStock` do not exist yet.

- [ ] **Step 3: Write the scraper read models**

`src/CardStock.Infrastructure/Persistence/ScraperReadModels/IScraperOwned.cs`:

```csharp
namespace CardStock.Infrastructure.Persistence.ScraperReadModels;

/// <summary>
/// Marks a type owned by PokemonInvestBatch. Every type carrying this is mapped
/// with ToView, never ToTable, so EF can neither migrate it nor write to it.
/// Asserted by SchemaModelTests.
/// </summary>
public interface IScraperOwned;
```

`ScraperReadModels/PriceTier.cs` — CardStock cannot reference the sibling's assembly, so it carries its own copy. **The order is the storage contract**: EF stores this enum as `integer` (verified, `20260728032826_InitialCreate.cs:134`), so a reordering silently remaps every price row.

```csharp
namespace CardStock.Infrastructure.Persistence.ScraperReadModels;

/// <summary>
/// Mirrors PokemonInvestBatch.Domain.Parsing.PriceTier exactly. Stored as
/// integer in price_months.tier. NEVER reorder or insert -- the ordinal is the
/// stored value, and a change here misreads every historical price.
/// </summary>
public enum PriceTier
{
    Ungraded,
    Grade7,
    Grade8,
    Grade9,
    Grade9Half,
    Psa10,
}
```

`ScraperReadModels/ScraperCard.cs` — only the columns CardStock actually reads:

```csharp
namespace CardStock.Infrastructure.Persistence.ScraperReadModels;

/// <summary>Read-only mirror of public.cards. Owned by PokemonInvestBatch.</summary>
public class ScraperCard : IScraperOwned
{
    public long Id { get; init; }

    public long SetId { get; init; }

    public required string Name { get; init; }

    public required string Url { get; init; }

    public string? ImageHash { get; init; }

    public DateTimeOffset? DelistedAt { get; init; }

    public DateTimeOffset? NotACardAt { get; init; }
}
```

`ScraperReadModels/ScraperSet.cs`:

```csharp
namespace CardStock.Infrastructure.Persistence.ScraperReadModels;

/// <summary>Read-only mirror of public.sets. Owned by PokemonInvestBatch.</summary>
public class ScraperSet : IScraperOwned
{
    public long Id { get; init; }

    public required string Slug { get; init; }

    public required string Name { get; init; }
}
```

`ScraperReadModels/ScraperPriceMonth.cs`:

```csharp
namespace CardStock.Infrastructure.Persistence.ScraperReadModels;

/// <summary>
/// Read-only mirror of public.price_months. Change-only append: a row exists
/// only where the value CHANGED, so absence means unchanged, not missing.
/// Composite key (CardId, Tier, Month, ObservedAt).
/// </summary>
public class ScraperPriceMonth : IScraperOwned
{
    public long CardId { get; init; }

    public PriceTier Tier { get; init; }

    public DateOnly Month { get; init; }

    public int PriceCents { get; init; }

    public DateTimeOffset ObservedAt { get; init; }
}
```

`ScraperReadModels/ScraperPopulation.cs`:

```csharp
namespace CardStock.Infrastructure.Persistence.ScraperReadModels;

/// <summary>
/// Read-only mirror of public.populations. Change-only append.
/// Grade is smallint (1..10); Grader is "psa" or "cgc".
/// </summary>
public class ScraperPopulation : IScraperOwned
{
    public long CardId { get; init; }

    public required string Grader { get; init; }

    public short Grade { get; init; }

    public int Population { get; init; }

    public DateTimeOffset ObservedAt { get; init; }
}
```

`ScraperReadModels/ScraperSale.cs`:

```csharp
namespace CardStock.Infrastructure.Persistence.ScraperReadModels;

/// <summary>
/// Read-only mirror of public.sales. Title is stored raw by the crawler and
/// MUST be HTML-encoded at render (D-029). Never pass it through MarkupString.
/// </summary>
public class ScraperSale : IScraperOwned
{
    public long Id { get; init; }

    public long CardId { get; init; }

    public required string Source { get; init; }

    public required string SourceId { get; init; }

    public DateOnly SoldOn { get; init; }

    public required string GradeTier { get; init; }

    public int PriceCents { get; init; }

    public int? ListedPriceCents { get; init; }

    public required string Title { get; init; }

    public DateTimeOffset CapturedAt { get; init; }
}
```

- [ ] **Step 4: Write the view mappings in one file**

`src/CardStock.Infrastructure/Persistence/ScraperViews.cs`:

```csharp
using CardStock.Infrastructure.Persistence.ScraperReadModels;
using Microsoft.EntityFrameworkCore;

namespace CardStock.Infrastructure.Persistence;

/// <summary>
/// Every mapping to a PokemonInvestBatch table, in one place so none can be
/// forgotten piecemeal.
///
/// ToView, not ToTable(..., ExcludeFromMigrations()). Verified 2026-08-11: the
/// ExcludeFromMigrations form still emits cross-schema foreign keys into public
/// when a relationship is configured, and omitting a mapping entirely emits
/// CreateTable(schema: "public") with DropTable(schema: "public") in Down().
/// ToView makes both impossible by construction.
/// </summary>
internal static class ScraperViews
{
    public static void Map(ModelBuilder builder)
    {
        builder.Entity<ScraperSet>(e =>
        {
            e.HasKey(x => x.Id);
            e.ToView("sets", CardStockDbContext.ScraperSchema);
        });

        builder.Entity<ScraperCard>(e =>
        {
            e.HasKey(x => x.Id);
            e.ToView("cards", CardStockDbContext.ScraperSchema);
        });

        // Composite key mirrors PokemonDbContext.cs:52.
        builder.Entity<ScraperPriceMonth>(e =>
        {
            e.HasKey(x => new { x.CardId, x.Tier, x.Month, x.ObservedAt });
            e.ToView("price_months", CardStockDbContext.ScraperSchema);
        });

        // Composite key mirrors PokemonDbContext.cs:58.
        builder.Entity<ScraperPopulation>(e =>
        {
            e.HasKey(x => new { x.CardId, x.Grader, x.Grade, x.ObservedAt });
            e.ToView("populations", CardStockDbContext.ScraperSchema);
        });

        builder.Entity<ScraperSale>(e =>
        {
            e.HasKey(x => x.Id);
            e.ToView("sales", CardStockDbContext.ScraperSchema);
        });
    }
}
```

- [ ] **Step 5: Write the CardStock entities**

`src/CardStock.Infrastructure/Persistence/Entities/AppUser.cs`:

```csharp
namespace CardStock.Infrastructure.Persistence.Entities;

/// <summary>An account. Email is the natural key and is unique, case-folded.</summary>
public class AppUser
{
    public long Id { get; set; }

    public required string Email { get; set; }

    public required string PasswordHash { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Null until the verification link is followed.</summary>
    public DateTimeOffset? EmailVerifiedAt { get; set; }
}
```

`src/CardStock.Infrastructure/Persistence/Entities/UserSession.cs`:

```csharp
namespace CardStock.Infrastructure.Persistence.Entities;

/// <summary>
/// One signed-in session, referenced by the HttpOnly cookie (ADR-0002). The
/// session lives here rather than inside the cookie so that signing out and
/// deleting an account take effect on the next request rather than at expiry.
/// </summary>
public class UserSession
{
    /// <summary>The opaque key carried in the cookie. Generated, never guessable.</summary>
    public required string Id { get; set; }

    public long UserId { get; set; }

    public AppUser? User { get; set; }

    /// <summary>The serialized authentication ticket, as ITicketStore supplies it.</summary>
    public required byte[] Payload { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }
}
```

- [ ] **Step 6: Write the options helper**

`src/CardStock.Infrastructure/Persistence/CardStockDbContextOptions.cs`. This is the **only** place these three settings are applied, so a fourth call site cannot forget one — the sibling repeats `UseSnakeCaseNamingConvention()` at three sites with nothing preventing a fourth from omitting it.

```csharp
using Microsoft.EntityFrameworkCore;

namespace CardStock.Infrastructure.Persistence;

public static class CardStockDbContextOptions
{
    /// <summary>
    /// The single configuration point for every CardStockDbContext, wherever it
    /// is built: API DI, Worker DI, the design-time factory, and the test
    /// harness. The migrations history table override is load-bearing --
    /// HasDefaultSchema alone does NOT relocate it, and without this the
    /// history table lands unqualified on the crawler's own.
    /// </summary>
    public static DbContextOptionsBuilder<CardStockDbContext> UseCardStock(
        this DbContextOptionsBuilder<CardStockDbContext> builder,
        string connectionString) =>
        builder
            .UseNpgsql(connectionString, npgsql => npgsql
                .MigrationsHistoryTable("__cardstock_migrations_history", CardStockDbContext.Schema))
            .UseSnakeCaseNamingConvention();
}
```

- [ ] **Step 7: Write the DbContext**

`src/CardStock.Infrastructure/Persistence/CardStockDbContext.cs`:

```csharp
using CardStock.Infrastructure.Persistence.Entities;
using CardStock.Infrastructure.Persistence.ScraperReadModels;
using Microsoft.EntityFrameworkCore;

namespace CardStock.Infrastructure.Persistence;

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

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema(Schema);

        ScraperViews.Map(builder);

        builder.Entity<AppUser>(user =>
        {
            user.Property(u => u.Email).HasMaxLength(320);
            user.HasIndex(u => u.Email).IsUnique();
        });

        builder.Entity<UserSession>(session =>
        {
            session.HasKey(s => s.Id);
            session.Property(s => s.Id).HasMaxLength(64);
            // Explicit, per the Global Constraints: EF's default here is
            // Cascade, and deleting an account should take its sessions.
            session.HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            session.HasIndex(s => s.ExpiresAt);
        });
    }
}
```

- [ ] **Step 8: Run the guard tests to verify they pass**

Run: `dotnet test tests/CardStock.Infrastructure.Tests -v minimal`
Expected: PASS, 3 tests.

- [ ] **Step 9: Commit**

```bash
git add -A
git commit -m "CardStockDbContext with view-mapped scraper mirrors

Five PokemonInvestBatch tables mapped with ToView so migrations can never
emit DDL against them and EF-level writes throw rather than reaching
Postgres. Migrations history pinned to cardstock.__cardstock_migrations_history,
which HasDefaultSchema alone does not do. Three model guards assert all of
it without a database."
```

---

### Task 3: Postgres roles, schema, and the runbook

**Files:**
- Create: `ops/cardstock-postgres-setup.sql`
- Create: `ops/README.md`

**Interfaces:**
- Consumes: nothing in code.
- Produces: roles `cardstock_owner`, `cardstock_app`, `cardstock_tester`; schema `cardstock` owned by `cardstock_owner`.

- [ ] **Step 1: Write the setup SQL**

`ops/cardstock-postgres-setup.sql`:

```sql
-- CardStock — one-time Postgres setup on the Pi.
--   sudo -u postgres psql -v ON_ERROR_STOP=1 -f cardstock-postgres-setup.sql
--
-- ON_ERROR_STOP is not optional: without it a failed GRANT is silent and the
-- app fails much later with a permission error that looks like a code bug.
--
-- CHANGE THE THREE PASSWORDS BELOW BEFORE RUNNING.
-- Run only after PokemonInvestBatch's own migrations are current.

-- CONNECTION LIMITs are load-bearing, not tidiness: the Pi runs
-- max_connections = 100 (verified 2026-08-11) and three .NET processes at
-- Npgsql's default pool size of 100 each would ask for 300.
CREATE ROLE cardstock_owner  LOGIN PASSWORD 'CHANGE_ME_OWNER'  CONNECTION LIMIT 3;
CREATE ROLE cardstock_app    LOGIN PASSWORD 'CHANGE_ME_APP'    CONNECTION LIMIT 30;
CREATE ROLE cardstock_tester LOGIN PASSWORD 'CHANGE_ME_TEST'   CREATEDB;

\connect pokemon

-- Created by the superuser so cardstock_owner never needs CREATE on the
-- database itself. Safe to re-run: EF's EnsureSchema is a pg_namespace-guarded
-- DO block, not a bare CREATE SCHEMA.
CREATE SCHEMA cardstock AUTHORIZATION cardstock_owner;

-- The runtime role: full DML inside cardstock, never DDL.
GRANT USAGE ON SCHEMA cardstock TO cardstock_app;   -- USAGE only, never CREATE
ALTER DEFAULT PRIVILEGES FOR ROLE cardstock_owner IN SCHEMA cardstock
    GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO cardstock_app;
ALTER DEFAULT PRIVILEGES FOR ROLE cardstock_owner IN SCHEMA cardstock
    GRANT USAGE ON SEQUENCES TO cardstock_app;

-- Read the crawler, and nothing more (D-026, ruled 2026-08-11). ALL TABLES is
-- safe here precisely BECAUSE CardStock's tables are not in this schema.
GRANT USAGE ON SCHEMA public TO cardstock_app;
GRANT SELECT ON ALL TABLES IN SCHEMA public TO cardstock_app;

-- Keeps that read alive across FUTURE crawler migrations. Must be run by a
-- superuser or a member of pokemon_owner. NOTE: this writes into
-- pokemon_owner's pg_default_acl entry alongside the crawler's own, so verify
-- it survives any Pi rebuild:
--   SELECT defaclrole::regrole, defaclacl FROM pg_default_acl;
ALTER DEFAULT PRIVILEGES FOR ROLE pokemon_owner IN SCHEMA public
    GRANT SELECT ON TABLES TO cardstock_app;

-- Documents intent. A no-op today because none of these were ever granted.
REVOKE INSERT, UPDATE, DELETE, TRUNCATE, REFERENCES, TRIGGER
    ON ALL TABLES IN SCHEMA public FROM cardstock_app;

-- The crawler must never read user data. Belt and braces: the crawler's own
-- ALTER DEFAULT PRIVILEGES is scoped IN SCHEMA public and can never fire here.
REVOKE ALL ON SCHEMA cardstock FROM pokemon_app;

-- DO NOT run REVOKE ALL ON DATABASE pokemon FROM PUBLIC. The crawler has no
-- explicit CONNECT grant -- ops/postgres-setup.sql grants only USAGE on the
-- schema -- so it connects on PUBLIC's default. Revoking it stops the crawler
-- and presents as a Postgres outage.

-- DO NOT set search_path on any role. Every statement EF emits is
-- schema-qualified; the crawler's history table is not. Putting cardstock
-- ahead of public would silently relocate it.
```

- [ ] **Step 2: Write the runbook**

`ops/README.md` — the one home for these commands:

````markdown
# CardStock ops

## 1. One-time Postgres setup

Run after PokemonInvestBatch's migrations are current:

```bash
sudo -u postgres psql -v ON_ERROR_STOP=1 -f cardstock-postgres-setup.sql
```

## 2. Migrations

Applied by hand from a dev machine, as the owner role. Nothing auto-migrates —
neither the API nor the Worker calls `Migrate()`, so the two units cannot race
one history table at boot.

```bash
dotnet tool restore

dotnet ef migrations add <Name> \
  -p src/CardStock.Infrastructure -s src/CardStock.Infrastructure \
  -o Persistence/Migrations --context CardStockDbContext

CARDSTOCK_DB="Host=<pi-ip>;Database=pokemon;Username=cardstock_owner;Password=..." \
dotnet ef database update \
  -p src/CardStock.Infrastructure -s src/CardStock.Infrastructure \
  --context CardStockDbContext
```

`--context` is required once a second DbContext exists in the assembly.

### After every `database update`, verify ownership

A wrong credential is the one remaining hole — it would create CardStock's
tables owned by `pokemon_owner`, silently granting `pokemon_app` access to
`cardstock.users`.

```sql
SELECT c.relname, c.relowner::regrole
FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace
WHERE n.nspname = 'cardstock' AND c.relowner <> 'cardstock_owner'::regrole;
-- must return zero rows
```

## 3. Cross-repo migration ordering

- **Additive** crawler changes deploy first.
- **Destructive** ones — renaming or dropping a column CardStock reads —
  require CardStock to deploy first and stop reading it.

"Crawler first, always" is wrong for exactly the migrations that matter.
````

- [ ] **Step 3: Commit**

```bash
git add -A
git commit -m "ops: Postgres roles, schema, and the migration runbook

Roles carry explicit CONNECTION LIMITs because the Pi runs
max_connections = 100 and three .NET processes at Npgsql's default pool
size would ask for 300. Documents the two REVOKEs that must NOT be run,
both of which would stop the crawler."
```

---

### Task 4: The first migration and the integration test harness

**Files:**
- Create: `src/CardStock.Infrastructure/Persistence/CardStockDbContextFactory.cs`
- Create: `tests/CardStock.TestSupport/CardStockDatabaseTest.cs`
- Create: `tests/CardStock.TestSupport/Fixtures/scraper-schema.sql` (generated)
- Create: `tests/CardStock.Integration.Tests/MigrationContentTests.cs`
- Create: `tests/CardStock.Integration.Tests/SchemaDriftTests.cs`
- Create: `src/CardStock.Infrastructure/Persistence/Migrations/*` (scaffolded)

**Interfaces:**
- Consumes: `CardStockDbContext`, `UseCardStock` from Task 2.
- Produces: abstract base class `CardStockDatabaseTest` exposing `protected string ConnectionString`, `protected CardStockDbContext NewContext()`, and `public static bool Available`.

- [ ] **Step 1: Write the design-time factory with its credential guard**

`src/CardStock.Infrastructure/Persistence/CardStockDbContextFactory.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql;

namespace CardStock.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for `dotnet ef`. Generating migrations never connects;
/// applying them uses CARDSTOCK_DB, so the runtime role never holds DDL rights.
/// </summary>
public class CardStockDbContextFactory : IDesignTimeDbContextFactory<CardStockDbContext>
{
    public CardStockDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("CARDSTOCK_DB")
            ?? "Host=localhost;Database=pokemon;Username=cardstock_owner";

        Guard(connectionString);

        var options = new DbContextOptionsBuilder<CardStockDbContext>()
            .UseCardStock(connectionString)
            .Options;

        return new CardStockDbContext(options);
    }

    /// <summary>
    /// A stale POKEMON_DB in the shell would create CardStock's tables owned by
    /// pokemon_owner, which silently grants pokemon_app access to cardstock.users.
    /// Cheaper to refuse than to detect afterwards.
    /// </summary>
    private static void Guard(string connectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        var isTestDatabase = builder.Database?.StartsWith("cardstock_test_", StringComparison.Ordinal) == true;

        if (builder.Username is not "cardstock_owner" && !isTestDatabase)
        {
            throw new InvalidOperationException(
                $"Refusing to migrate as '{builder.Username}'. CARDSTOCK_DB must use cardstock_owner.");
        }
    }
}
```

- [ ] **Step 2: Generate the crawler's schema fixture**

A throwaway test database has no `public.cards`, so the joins that are the whole product cannot be tested without one. Generate the crawler's schema and commit it:

```bash
cd /Users/scott/RiderProjects/PokemonInvestBatch
dotnet tool restore
dotnet ef migrations script \
  -p src/PokemonInvestBatch.Infrastructure \
  -s src/PokemonInvestBatch.Infrastructure \
  -o /Users/scott/RiderProjects/CardStock/tests/CardStock.TestSupport/Fixtures/scraper-schema.sql
```

Then in `tests/CardStock.TestSupport/CardStock.TestSupport.csproj`, make it copy to output:

```xml
<ItemGroup>
  <None Update="Fixtures/scraper-schema.sql" CopyToOutputDirectory="PreserveNewest" />
</ItemGroup>
```

Both test projects that skip when no database is present need the skip attribute:

```bash
dotnet add tests/CardStock.TestSupport package Xunit.SkippableFact --version 1.5.61
dotnet add tests/CardStock.Integration.Tests package Xunit.SkippableFact --version 1.5.61
```

Note that `Skip.If`/`Skip.IfNot` are called from the **derived** test classes, not from
`CardStockDatabaseTest` itself — the same arrangement the sibling uses.

- [ ] **Step 3: Write the database test harness**

`tests/CardStock.TestSupport/CardStockDatabaseTest.cs`:

```csharp
using CardStock.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace CardStock.TestSupport;

/// <summary>
/// A base class for tests that need real PostgreSQL. Each test gets its own
/// database: the crawler's schema is applied first (CardStock reads it and will
/// eventually reference it), then CardStock's own migrations run on top.
///
/// CARDSTOCK_TEST_DB supplies host and credentials. The database it names is
/// only a template and is never written to.
/// </summary>
public abstract class CardStockDatabaseTest : IAsyncLifetime
{
    private string _databaseName = "";

    public static string? Template => Environment.GetEnvironmentVariable("CARDSTOCK_TEST_DB");

    public static bool Available => !string.IsNullOrWhiteSpace(Template);

    protected string ConnectionString { get; private set; } = "";

    public async Task InitializeAsync()
    {
        if (!Available)
        {
            return;
        }

        // A GUID name, so a crashed run leaves an inert orphan rather than
        // corrupting the next run.
        _databaseName = $"cardstock_test_{Guid.NewGuid():N}";
        await using (var admin = new NpgsqlConnection(Maintenance()))
        {
            await admin.OpenAsync();
            await using var create = new NpgsqlCommand($"CREATE DATABASE \"{_databaseName}\"", admin);
            await create.ExecuteNonQueryAsync();
        }

        ConnectionString = new NpgsqlConnectionStringBuilder(Template)
        {
            Database = _databaseName,
        }.ConnectionString;

        await ApplyScraperSchemaAsync();

        await using var db = NewContext();
        await db.Database.MigrateAsync();
    }

    /// <summary>The crawler's tables must exist before CardStock's migrations run.</summary>
    private async Task ApplyScraperSchemaAsync()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "scraper-schema.sql");
        var sql = await File.ReadAllTextAsync(path);

        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    public async Task DisposeAsync()
    {
        if (_databaseName.Length == 0)
        {
            return;
        }

        // PostgreSQL will not drop a database anyone is still attached to, and
        // Npgsql keeps pooled connections open.
        NpgsqlConnection.ClearAllPools();
        await using var admin = new NpgsqlConnection(Maintenance());
        await admin.OpenAsync();

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            if (await TryDropAsync(admin, force: false) || await TryDropAsync(admin, force: true))
            {
                return;
            }

            NpgsqlConnection.ClearAllPools();
            await Task.Delay(TimeSpan.FromMilliseconds(200 * attempt));
        }

        // Out of tries. The name is a GUID, so the orphan is inert and a sweep
        // over cardstock_test_% collects it later. Teardown of a scratch
        // database must never be why a passing test reports failure.
    }

    private async Task<bool> TryDropAsync(NpgsqlConnection admin, bool force)
    {
        try
        {
            await using var drop = new NpgsqlCommand(
                $"DROP DATABASE IF EXISTS \"{_databaseName}\"{(force ? " WITH (FORCE)" : "")}", admin);
            await drop.ExecuteNonQueryAsync();
            return true;
        }
        catch (PostgresException e) when (
            e.SqlState is PostgresErrorCodes.InsufficientPrivilege or PostgresErrorCodes.ObjectInUse)
        {
            return false;
        }
    }

    protected CardStockDbContext NewContext() =>
        new(new DbContextOptionsBuilder<CardStockDbContext>()
            .UseCardStock(ConnectionString)
            .Options);

    /// <summary>CREATE DATABASE cannot run inside the database being created.</summary>
    private static string Maintenance() =>
        new NpgsqlConnectionStringBuilder(Template) { Database = "postgres" }.ConnectionString;
}
```

- [ ] **Step 4: Scaffold the first migration**

```bash
cd /Users/scott/RiderProjects/CardStock
dotnet ef migrations add InitialCreate \
  -p src/CardStock.Infrastructure -s src/CardStock.Infrastructure \
  -o Persistence/Migrations --context CardStockDbContext
```

- [ ] **Step 5: Inspect the generated migration before running it**

Open `src/CardStock.Infrastructure/Persistence/Migrations/*_InitialCreate.cs` and confirm by eye:

- `EnsureSchema(name: "cardstock")` is present.
- `CreateTable` appears exactly twice — `users` and `sessions` — both with `schema: "cardstock"`.
- **The string `"public"` does not appear anywhere in `Up()` or `Down()`.**
- No `CreateTable` or `DropTable` mentions `cards`, `sets`, `price_months`, `populations`, or `sales`.

If any of these fail, the `ToView` mapping is wrong. Stop and fix Task 2 rather than editing the migration.

- [ ] **Step 6: Add the migration format exclusion check**

Create `tests/CardStock.Integration.Tests/MigrationContentTests.cs`:

```csharp
using Xunit;

namespace CardStock.Integration.Tests;

public class MigrationContentTests
{
    /// <summary>
    /// No hand-written migration may reference the crawler's schema. Designer
    /// and snapshot files legitimately record "public" for view mappings, so
    /// they are excluded.
    /// </summary>
    [Fact]
    public void No_migration_references_the_scraper_schema()
    {
        var root = FindRepositoryRoot();
        var migrations = Directory.GetFiles(
            Path.Combine(root, "src", "CardStock.Infrastructure", "Persistence", "Migrations"),
            "*.cs")
            .Where(f => !f.EndsWith(".Designer.cs", StringComparison.Ordinal))
            .Where(f => !f.EndsWith("ModelSnapshot.cs", StringComparison.Ordinal));

        var offenders = migrations
            .Where(f => File.ReadAllText(f).Contains("\"public\"", StringComparison.Ordinal))
            .ToList();

        Assert.Empty(offenders);
    }

    private static string FindRepositoryRoot()
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
```

- [ ] **Step 7: Write the schema drift guard**

The committed fixture is an unversioned copy of another repo's schema, and drift is silent. It has
precedent: `20260801024826_WidenImageHash` changed a column's type and
`20260808022824_RenameShapesToFingerprints` renamed a table and its columns. This test is the only
thing standing between the fixture and quiet rot.

`tests/CardStock.Integration.Tests/SchemaDriftTests.cs`:

```csharp
using System.Diagnostics;
using Xunit;

namespace CardStock.Integration.Tests;

public class SchemaDriftTests
{
    /// <summary>
    /// Regenerates the crawler's schema script and fails if the committed
    /// fixture no longer matches. Runs only where the sibling repo is checked
    /// out, so CI and other machines skip it rather than failing.
    /// </summary>
    [SkippableFact]
    public async Task Committed_scraper_schema_fixture_matches_the_sibling_repo()
    {
        var sibling = Path.GetFullPath(Path.Combine(RepositoryRoot(), "..", "PokemonInvestBatch"));
        Skip.IfNot(Directory.Exists(sibling), "../PokemonInvestBatch is not checked out");

        var regenerated = Path.Combine(Path.GetTempPath(), $"scraper-schema-{Guid.NewGuid():N}.sql");

        var process = Process.Start(new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = sibling,
            ArgumentList =
            {
                "ef", "migrations", "script",
                "-p", "src/PokemonInvestBatch.Infrastructure",
                "-s", "src/PokemonInvestBatch.Infrastructure",
                "-o", regenerated,
            },
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        })!;

        await process.WaitForExitAsync();
        Skip.If(process.ExitCode != 0, "could not regenerate the sibling's schema script");

        var committed = await File.ReadAllTextAsync(
            Path.Combine(RepositoryRoot(), "tests", "CardStock.TestSupport", "Fixtures", "scraper-schema.sql"));
        var current = await File.ReadAllTextAsync(regenerated);
        File.Delete(regenerated);

        Assert.Equal(Normalize(committed), Normalize(current));
    }

    private static string Normalize(string sql) =>
        sql.ReplaceLineEndings("\n").Trim();

    private static string RepositoryRoot()
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
```

**When this test fails**, that is the signal to re-read the crawler's migration, decide whether
CardStock reads the changed column, and regenerate the fixture with the Task 4 Step 2 command. It
is not a test to silence.

- [ ] **Step 8: Run the tests against a real database**

```bash
CARDSTOCK_TEST_DB="Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres" \
  dotnet test CardStock.slnx -v minimal
```

Expected: PASS. If no local Postgres is available, the database tests skip via `Available` and the model guards still run.

- [ ] **Step 9: Commit**

```bash
git add -A
git commit -m "First migration: users and sessions, plus the test harness

Each test builds its own database: the crawler's committed schema fixture
first, then CardStock's migrations on top, because a throwaway database has
no public.cards and the cross-boundary join is the whole product. A test
asserts no hand-written migration mentions the crawler's schema."
```

---

### Task 5: Prove the cross-schema read end to end

**Files:**
- Create: `tests/CardStock.Integration.Tests/ScraperReadTests.cs`
- Modify: `src/CardStock.Api/Program.cs`

**Interfaces:**
- Consumes: `CardStockDbContext`, `CardStockDatabaseTest` from Tasks 2 and 4.
- Produces: a `GET /healthz/data` endpoint returning `{ "cards": <long>, "sets": <long> }`.

This is the task that proves the whole architecture works: two schemas, one query, real grants.

- [ ] **Step 1: Write the failing test**

`tests/CardStock.Integration.Tests/ScraperReadTests.cs`:

```csharp
using CardStock.Infrastructure.Persistence.Entities;
using CardStock.TestSupport;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CardStock.Integration.Tests;

public class ScraperReadTests : CardStockDatabaseTest
{
    [SkippableFact]
    public async Task A_cardstock_row_joins_to_a_scraper_row_in_one_query()
    {
        Skip.IfNot(Available, "CARDSTOCK_TEST_DB is not set");

        await using var db = NewContext();

        // Seed one crawler row directly. CardStock's model cannot write it --
        // ScraperCard is view-mapped -- which is the point.
        await db.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO public.sets (id, slug, name, discovered_at, last_seen_at)
            VALUES (1, 'base-set', 'Base Set', now(), now());
            INSERT INTO public.cards (id, set_id, url, name, first_seen_at, last_seen_at,
                                      any_bucket_at_cap, failure_streak)
            VALUES (42, 1, '/game/pokemon-base-set/charizard-4', 'Charizard', now(), now(), false, 0);
            """);

        db.Users.Add(new AppUser
        {
            Email = "owner@example.com",
            PasswordHash = "not-a-real-hash",
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();

        // The query shape the entire product rests on: CardStock's schema
        // joined to the crawler's in one statement.
        var names = await db.ScraperCards
            .Where(c => c.Id == 42)
            .Select(c => c.Name)
            .ToListAsync();

        Assert.Equal(["Charizard"], names);
    }

    [SkippableFact]
    public async Task Writing_a_scraper_entity_through_EF_is_impossible()
    {
        Skip.IfNot(Available, "CARDSTOCK_TEST_DB is not set");

        await using var db = NewContext();

        db.ScraperSets.Add(new CardStock.Infrastructure.Persistence.ScraperReadModels.ScraperSet
        {
            Id = 999,
            Slug = "should-not-save",
            Name = "Should Not Save",
        });

        // ToView means "not mapped to a table", so EF refuses before Postgres
        // is ever asked. This is the guarantee that outlives any grant change.
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
    }
}
```

`Xunit.SkippableFact` was already added in Task 4 Step 2.

- [ ] **Step 2: Run to verify it fails**

Run: `CARDSTOCK_TEST_DB="Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres" dotnet test tests/CardStock.Integration.Tests -v minimal`
Expected: FAIL — `ScraperReadTests` does not compile yet, or the seeded columns do not match `public.cards`.

- [ ] **Step 3: Make it pass**

No production code should be needed. If `A_cardstock_row_joins_to_a_scraper_row_in_one_query` fails, the fault is in the `ToView` mapping or the fixture, not in the test. If `Writing_a_scraper_entity_through_EF_is_impossible` fails with a Postgres permission error rather than `InvalidOperationException`, the mapping is `ToTable` somewhere — fix Task 2.

- [ ] **Step 4: Run to verify it passes**

Run: `CARDSTOCK_TEST_DB="Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres" dotnet test tests/CardStock.Integration.Tests -v minimal`
Expected: PASS, 3 tests.

- [ ] **Step 5: Add the API endpoint**

`src/CardStock.Api/Program.cs`:

```csharp
using CardStock.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<CardStockDbContext>(options =>
    options.UseCardStock(builder.Configuration.GetConnectionString("CardStock")
        ?? throw new InvalidOperationException("ConnectionStrings:CardStock is not configured.")));

var app = builder.Build();

app.MapGet("/healthz", () => Results.Ok("ok"));

// Proves the deployed app can read the crawler's schema with the grants it
// actually holds -- the one thing a local test cannot confirm.
app.MapGet("/healthz/data", async (CardStockDbContext db) => Results.Ok(new
{
    cards = await db.ScraperCards.LongCountAsync(),
    sets = await db.ScraperSets.LongCountAsync(),
}));

app.Run();
```

Note: **no `Migrate()` call**, per the Global Constraints.

- [ ] **Step 6: Verify the API builds and starts**

Run: `dotnet build src/CardStock.Api -c Release`
Expected: PASS, zero warnings.

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "Prove the cross-schema read end to end

One query joining CardStock's schema to the crawler's, and a test asserting
that writing a view-mapped entity throws InvalidOperationException rather
than reaching Postgres -- the guarantee that survives any future grant
change. /healthz/data exposes the same read for post-deploy verification."
```

---

### Task 6: Run it on the Pi

**Files:**
- Create: `ops/cardstock-api.service`
- Create: `ops/publish.sh`
- Modify: `ops/README.md`

**Interfaces:**
- Consumes: everything above.
- Produces: `cardstock-api` running under systemd on the Pi, answering on the LAN.

**Scope limit:** this task stops at the LAN. Public exposure is blocked on **D-037**, which leaves the choice between a DMZ VLAN and a Cloudflare Tunnel unresolved. Do not open a port on the router as part of this task.

- [ ] **Step 1: Write the publish script**

`ops/publish.sh`:

```bash
#!/usr/bin/env bash
# Publishes the API self-contained for the Pi. No database step, ever --
# migrations are a separate, deliberate act (see README §2).
set -euo pipefail

OUT="${1:-publish/api}"

dotnet publish src/CardStock.Api \
  -c Release \
  -r linux-arm64 \
  --self-contained \
  -o "$OUT"

echo "Published to $OUT"
```

Make it executable: `chmod +x ops/publish.sh`

- [ ] **Step 2: Write the systemd unit**

`ops/cardstock-api.service`:

```ini
[Unit]
Description=CardStock API
After=network-online.target postgresql.service
Wants=network-online.target

[Service]
Type=notify
User=cardstock
WorkingDirectory=/opt/cardstock/api
ExecStart=/opt/cardstock/api/CardStock.Api
Restart=always
RestartSec=10
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://0.0.0.0:5180

# Hardening, per D-037. MemoryMax exists so the web tier cannot starve the
# crawler on a shared 16 GB box.
NoNewPrivileges=true
ProtectSystem=strict
ProtectHome=true
PrivateTmp=true
MemoryMax=2G

[Install]
WantedBy=multi-user.target
```

- [ ] **Step 3: Publish and copy to the Pi**

```bash
./ops/publish.sh publish/api
ssh scott@192.168.0.56 'sudo mkdir -p /opt/cardstock/api && sudo chown scott:scott /opt/cardstock/api'
rsync -az --delete publish/api/ scott@192.168.0.56:/opt/cardstock/api/
```

- [ ] **Step 4: Create the service account and install the unit**

```bash
ssh scott@192.168.0.56 'sudo useradd --system --no-create-home --shell /usr/sbin/nologin cardstock || true'
scp ops/cardstock-api.service scott@192.168.0.56:/tmp/
ssh scott@192.168.0.56 'sudo mv /tmp/cardstock-api.service /etc/systemd/system/ && sudo systemctl daemon-reload'
ssh scott@192.168.0.56 'sudo chown -R cardstock:cardstock /opt/cardstock'
```

- [ ] **Step 5: Configure the connection string**

Create `/opt/cardstock/api/appsettings.Production.json` on the Pi, readable only by the service account. Note `Maximum Pool Size` — the Pi's `max_connections` is 100 and the default pool size is 100.

```json
{
  "ConnectionStrings": {
    "CardStock": "Host=localhost;Database=pokemon;Username=cardstock_app;Password=CHANGE_ME_APP;Maximum Pool Size=20"
  }
}
```

```bash
ssh scott@192.168.0.56 'sudo chown cardstock:cardstock /opt/cardstock/api/appsettings.Production.json && sudo chmod 600 /opt/cardstock/api/appsettings.Production.json'
```

Add `appsettings.Production.json` to `.gitignore`.

- [ ] **Step 6: Start it and verify**

```bash
ssh scott@192.168.0.56 'sudo systemctl enable --now cardstock-api && sleep 3 && systemctl is-active cardstock-api'
```
Expected: `active`

```bash
ssh scott@192.168.0.56 'curl -s localhost:5180/healthz'
```
Expected: `"ok"`

```bash
ssh scott@192.168.0.56 'curl -s localhost:5180/healthz/data'
```
Expected: JSON with non-zero `cards` and `sets` — **this is the moment the architecture is proven.** A running app on the Pi, connected as `cardstock_app`, reading the crawler's schema with real grants.

If it returns a permission error, the `GRANT SELECT ON ALL TABLES IN SCHEMA public` in Task 3 did not run or did not apply.

- [ ] **Step 7: Apply the first migration to the real database**

```bash
CARDSTOCK_DB="Host=192.168.0.56;Database=pokemon;Username=cardstock_owner;Password=CHANGE_ME_OWNER" \
dotnet ef database update \
  -p src/CardStock.Infrastructure -s src/CardStock.Infrastructure \
  --context CardStockDbContext
```

Then run the ownership audit from `ops/README.md` §2 and confirm it returns zero rows.

Also confirm the history table landed in the right place:

```sql
SELECT to_regclass('cardstock.__cardstock_migrations_history');  -- must not be null
SELECT to_regclass('public.__cardstock_migrations_history');     -- must be null
```

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "Deploy the API to the Pi under systemd

Self-contained linux-arm64 publish, mirroring the crawler's deployment.
Hardened unit with MemoryMax so the web tier cannot starve the crawler on a
shared box. Stops at the LAN: public exposure is blocked on D-037, which has
not chosen between a DMZ VLAN and a Cloudflare Tunnel.

/healthz/data returning live card and set counts is the proof that two
schemas, two roles, and view-mapped mirrors work against the real database."
```

---

## What this slice deliberately leaves undone

- **Row-Level Security (D-066).** Agreed in ADR-0002 and needed before any second user exists. It lands with the first user-scoped table that actually holds data; `users` and `sessions` are keyed by the session itself.
- **The cost-lot rule** — `docs/screens/binder.md` §7.4, promoted to blocking by D-067. Nothing in this slice touches it, but the Binder cannot start until it is ruled.
- **Cookie authentication wiring.** The `sessions` table exists; `ITicketStore` and the sign-in endpoints arrive with the Account screen.
- **Public exposure (D-037)** and **transactional email (ADR-0002)** — both need decisions or accounts that do not exist yet.
