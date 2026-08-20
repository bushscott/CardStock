# Public Exposure Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Take the deployed LAN-only app public as `https://cardstock.pro` behind a free-tier Cloudflare Tunnel, with the Pi in an Omada DMZ, Postgres loopback-only, HTTPS-only Kestrel on 443, and the D-132 go-public checklist ticked with receipts.

**Architecture:** Code tasks land first (forwarded headers, security headers with startup-computed CSP hashes, host filtering) — each LAN-verifiable before exposure. Ops tasks then follow D-132's A→H order: Cloudflare zone → Pi prep (cert, 443 cutover, Postgres lockdown, unit hardening) → Omada topology + WireGuard → tunnel → edge posture + email DNS → outside-in scans → HSTS strictly last.

**Tech Stack:** .NET 10 / ASP.NET Core minimal API + Blazor WASM · certbot + `python3-certbot-dns-cloudflare` · cloudflared (arm64 apt) · Omada controller (ER605 v2) · Cloudflare free tier · Resend free tier.

**Spec:** `docs/superpowers/specs/2026-08-20-public-exposure-design.md` (rulings in D-132/D-131; checklist in D-132 gets ticked as ops tasks complete).

## Global Constraints

- `net10.0`, `TreatWarningsAsErrors=true`, `Nullable=enable` — a warning is a build failure.
- 4-space C#, file-scoped namespaces, explicit accessibility; CI runs `dotnet format --verify-no-changes` — run `dotnet format` before committing if unsure.
- Test style: xunit, `Sentence_case_with_underscores` names, `TestApp : WebApplicationFactory<Program>` harness with `UseSetting` overrides (see `tests/CardStock.Api.Tests/TestApp.cs`).
- **Schema changes: none.** No migrations, no grants, nothing DB-side except the Postgres lockdown (config, Task 9).
- **Free tier only** on Cloudflare and Resend. No paid feature anywhere.
- **HSTS ordering is load-bearing** (D-129's recorded trap): Task 14 runs strictly after Task 13's SSL Labs pass. Never enable HSTS earlier.
- Ops steps run over ssh as `scott@192.168.0.56` (becomes `192.168.30.56` after Task 10). `sudo` works non-interactively.
- The Pi-only file `/opt/cardstock/api/appsettings.Production.json` is excluded from deploys; always back it up beside itself (`cp x x.bak-YYYYMMDD`) before editing — the existing convention.

---

### Task 1: Forwarded headers — the per-IP cap sees real visitors through the tunnel

**Files:**
- Modify: `src/CardStock.Api/Program.cs` (limiter comment block ~:51–53, pipeline ~:70–72)
- Modify: `tests/CardStock.Api.Tests/TestApp.cs` (new `RemoteIp` hook)
- Test: `tests/CardStock.Api.Tests/ForwardedHeaderTests.cs` (new)

**Interfaces:**
- Consumes: the existing `express-refresh` limiter partitioned on `Connection.RemoteIpAddress` (Program.cs:58–67) and `TestApp.ExpressPerHour`.
- Produces: pipeline guarantee later tasks rely on — `UseForwardedHeaders` runs **before** `UseRateLimiter`, so the limiter and logs see the `CF-Connecting-IP` value from a loopback proxy and ignore it from anyone else. Test hook `TestApp.RemoteIp` (type `System.Net.IPAddress?`).

- [ ] **Step 1: Add the `RemoteIp` hook to TestApp**

TestServer connections have no remote address, so the harness gains a way to fake one. In `TestApp.cs` add the property (beside `ExpressPerHour`), the registration (inside `ConfigureServices`), and the filter class (beside the other private classes). Add `using System.Net;` and `using Microsoft.AspNetCore.Builder;` if not present.

```csharp
/// <summary>Fakes the socket-level client address when set — TestServer
/// connections otherwise have none. Runs before the app's own pipeline,
/// so forwarded-headers trust checks see it as the connection IP.</summary>
public IPAddress? RemoteIp { get; set; }
```

```csharp
// inside ConfigureServices, beside the other conditional registrations:
if (RemoteIp is not null)
{
    services.AddSingleton<IStartupFilter>(new RemoteIpStartupFilter(RemoteIp));
}
```

```csharp
private sealed class RemoteIpStartupFilter(IPAddress ip) : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) =>
        app =>
        {
            app.Use((context, nextMiddleware) =>
            {
                context.Connection.RemoteIpAddress = ip;
                return nextMiddleware();
            });
            next(app);
        };
}
```

- [ ] **Step 2: Write the failing tests**

Create `tests/CardStock.Api.Tests/ForwardedHeaderTests.cs`:

```csharp
using System.Net;

namespace CardStock.Api.Tests;

public class ForwardedHeaderTests
{
    [Fact]
    public async Task Cf_connecting_ip_from_the_loopback_proxy_partitions_the_limiter()
    {
        using var app = new TestApp
        {
            WorkerIntakeHandler = new StubHandler(HttpStatusCode.OK),
            ExpressPerHour = 1,
            RemoteIp = IPAddress.Loopback,
        };
        using var client = app.CreateClient();

        var first = await Post(client, "203.0.113.7");
        var second = await Post(client, "203.0.113.8");
        var third = await Post(client, "203.0.113.7");

        Assert.Equal(HttpStatusCode.OK, first);
        Assert.Equal(HttpStatusCode.OK, second);   // a different visitor gets a fresh bucket
        Assert.Equal(HttpStatusCode.TooManyRequests, third);
    }

    [Fact]
    public async Task A_forged_header_from_a_non_proxy_address_is_ignored()
    {
        using var app = new TestApp
        {
            WorkerIntakeHandler = new StubHandler(HttpStatusCode.OK),
            ExpressPerHour = 1,
            RemoteIp = IPAddress.Parse("192.168.0.99"),
        };
        using var client = app.CreateClient();

        var first = await Post(client, "203.0.113.7");
        var second = await Post(client, "203.0.113.8");

        Assert.Equal(HttpStatusCode.OK, first);
        // Different spoofed headers, same real connection: same bucket.
        Assert.Equal(HttpStatusCode.TooManyRequests, second);
    }

    private static async Task<HttpStatusCode> Post(HttpClient client, string forwardedFor)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/cards/1/refresh");
        request.Headers.TryAddWithoutValidation("CF-Connecting-IP", forwardedFor);
        using var response = await client.SendAsync(request);
        return response.StatusCode;
    }

    private sealed class StubHandler(HttpStatusCode status) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(status));
    }
}
```

- [ ] **Step 3: Run tests to verify they fail**

Run: `dotnet test tests/CardStock.Api.Tests --filter ForwardedHeaderTests -v minimal`
Expected: both FAIL — without the middleware, every request shares one null/faked partition, so `second` in test 1 comes back 429.

- [ ] **Step 4: Implement in Program.cs**

Add `using Microsoft.AspNetCore.HttpOverrides;`. Replace the limiter's stale comment (lines 51–53, "When a Cloudflare tunnel fronts this someday…") with the present-tense truth, and register the options beside the limiter registration:

```csharp
// The tunnel delivers every public request from cloudflared on loopback, with
// the real visitor in CF-Connecting-IP (Cloudflare overwrites client-supplied
// values at its edge). Trust exactly the loopback proxy and nothing else:
// direct LAN/VPN connections arrive from non-proxy addresses and keep their
// own socket IP, and a forged header from one is ignored (D-132 §7).
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor;
    options.ForwardedForHeaderName = "CF-Connecting-IP";
    options.ForwardLimit = 1;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
    options.KnownProxies.Add(IPAddress.Loopback);
    options.KnownProxies.Add(IPAddress.IPv6Loopback);
});
```

In the pipeline, immediately after `var app = builder.Build();` and before `app.UseRateLimiter();`:

```csharp
app.UseForwardedHeaders();
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test tests/CardStock.Api.Tests --filter ForwardedHeaderTests -v minimal`
Expected: 2 PASS.

- [ ] **Step 6: Full suite + commit**

Run: `dotnet test` — expected: green (DB-gated skips are normal). Then:

```bash
git add src/CardStock.Api/Program.cs tests/CardStock.Api.Tests/TestApp.cs tests/CardStock.Api.Tests/ForwardedHeaderTests.cs
git commit -m "api: CF-Connecting-IP forwarded headers — the per-IP cap sees real visitors (D-132 §7)"
```

---

### Task 2: CSP script hashes — computed from index.html, never authored

**Files:**
- Create: `src/CardStock.Api/Security/CspScriptHashes.cs`
- Test: `tests/CardStock.Api.Tests/CspScriptHashesTests.cs` (new)

**Interfaces:**
- Produces: `CardStock.Api.Security.CspScriptHashes.FromHtml(string html)` → `IReadOnlyList<string>` of CSP source tokens like `'sha256-…'`. Task 3 calls it at startup against the deployed `wwwroot/index.html`.

Why this exists: `index.html` carries two inline scripts — the theme-init snippet and the import map that **Blazor's publish step writes with per-build fingerprints**. A hand-authored hash in a header would go stale every publish, in exactly the direction this project forbids. So the header computes its hashes from the deployed file at startup.

- [ ] **Step 1: Write the failing tests**

Create `tests/CardStock.Api.Tests/CspScriptHashesTests.cs`. The two hash vectors were computed with `printf '<body>' | openssl dgst -sha256 -binary | base64` — re-runnable receipts:

```csharp
using CardStock.Api.Security;

namespace CardStock.Api.Tests;

public class CspScriptHashesTests
{
    [Fact]
    public void An_inline_script_hashes_to_the_known_vector()
    {
        var tokens = CspScriptHashes.FromHtml("<script>var x = 1;</script>");

        Assert.Equal(["'sha256-9nfWt3DNT14o+tZCP3YilfLwTrhLI98eqbN689B7ajY='"], tokens);
    }

    [Fact]
    public void Whitespace_inside_the_body_is_preserved_verbatim()
    {
        // CSP hashes the exact bytes between the tags; trimming would break it.
        var tokens = CspScriptHashes.FromHtml("<script>\n  alert(1);\n</script>");

        Assert.Equal(["'sha256-8yUvYAoVPcECP+LCtuQt8Lpqlvatg5ljAKVplA/Yo0M='"], tokens);
    }

    [Fact]
    public void External_and_empty_scripts_produce_no_tokens()
    {
        var html = """
            <script src="_framework/blazor.webassembly.js"></script>
            <script type="importmap"></script>
            """;

        Assert.Empty(CspScriptHashes.FromHtml(html));
    }

    [Fact]
    public void Multiple_inline_scripts_hash_in_document_order()
    {
        var html = "<script>var x = 1;</script><script type=\"importmap\">\n  alert(1);\n</script>";

        var tokens = CspScriptHashes.FromHtml(html);

        Assert.Equal(2, tokens.Count);
        Assert.Equal("'sha256-9nfWt3DNT14o+tZCP3YilfLwTrhLI98eqbN689B7ajY='", tokens[0]);
        Assert.Equal("'sha256-8yUvYAoVPcECP+LCtuQt8Lpqlvatg5ljAKVplA/Yo0M='", tokens[1]);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/CardStock.Api.Tests --filter CspScriptHashesTests -v minimal`
Expected: FAIL — `CspScriptHashes` does not exist (compile error).

- [ ] **Step 3: Implement**

Create `src/CardStock.Api/Security/CspScriptHashes.cs`:

```csharp
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace CardStock.Api.Security;

/// <summary>Computes CSP sha256 source tokens for the inline scripts in a static
/// HTML document. index.html carries two: the theme-init snippet and the import
/// map Blazor's publish writes with per-build fingerprints — so the tokens are
/// computed from the deployed file at startup, never hand-authored (D-132 §7).</summary>
public static partial class CspScriptHashes
{
    [GeneratedRegex("<script(?<attrs>[^>]*)>(?<body>.*?)</script>",
        RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex ScriptElement();

    public static IReadOnlyList<string> FromHtml(string html)
    {
        var tokens = new List<string>();
        foreach (Match match in ScriptElement().Matches(html))
        {
            if (match.Groups["attrs"].Value.Contains("src=", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var body = match.Groups["body"].Value;
            if (body.Length == 0)
            {
                continue;
            }

            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(body));
            tokens.Add($"'sha256-{Convert.ToBase64String(hash)}'");
        }

        return tokens;
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/CardStock.Api.Tests --filter CspScriptHashesTests -v minimal`
Expected: 4 PASS.

- [ ] **Step 5: Commit**

```bash
git add src/CardStock.Api/Security/CspScriptHashes.cs tests/CardStock.Api.Tests/CspScriptHashesTests.cs
git commit -m "api: CSP inline-script hashes computed from index.html, never authored"
```

---

### Task 3: Security headers middleware — CSP, the header quartet, and gated HSTS

**Files:**
- Create: `src/CardStock.Api/Security/SecurityHeaders.cs`
- Modify: `src/CardStock.Api/Program.cs` (options binding + pipeline)
- Modify: `tests/CardStock.Api.Tests/TestApp.cs` (three settings hooks)
- Test: `tests/CardStock.Api.Tests/SecurityHeaderTests.cs` (new)

**Interfaces:**
- Consumes: `CspScriptHashes.FromHtml` (Task 2); `TestApp.RemoteIp` pattern (Task 1) is not needed here.
- Produces: `app.UseSecurityHeaders()` extension; `SecurityHeaderOptions` bound from the `Security` config section with keys `Security:HstsMaxAgeSeconds` (int, **0 = HSTS off — the default and the trap gate**) and `Security:HstsIncludeSubdomains` (bool). Task 8 deploys with 0; Task 14 raises it by config edit only, no redeploy.

- [ ] **Step 1: Add TestApp hooks**

In `TestApp.cs`, beside `ExpressPerHour`:

```csharp
/// <summary>Overrides Security:HstsMaxAgeSeconds (0 = HSTS off, the default).</summary>
public int? HstsMaxAgeSeconds { get; set; }

public bool HstsIncludeSubdomains { get; set; }

/// <summary>When set, becomes the host's webroot — drop an index.html here to
/// exercise the CSP hash computation.</summary>
public string? WebRoot { get; set; }
```

And in `ConfigureWebHost`, beside the existing `UseSetting` calls:

```csharp
if (HstsMaxAgeSeconds is not null)
{
    builder.UseSetting("Security:HstsMaxAgeSeconds", HstsMaxAgeSeconds.Value.ToString());
    builder.UseSetting("Security:HstsIncludeSubdomains", HstsIncludeSubdomains.ToString());
}

if (WebRoot is not null)
{
    builder.UseSetting(WebHostDefaults.WebRootKey, WebRoot);
}
```

- [ ] **Step 2: Write the failing tests**

Create `tests/CardStock.Api.Tests/SecurityHeaderTests.cs`:

```csharp
namespace CardStock.Api.Tests;

public class SecurityHeaderTests
{
    [Fact]
    public async Task Every_response_carries_the_header_set()
    {
        using var app = new TestApp();
        using var client = app.CreateClient();

        var response = await client.GetAsync("/healthz");

        Assert.Equal("DENY", Header(response, "X-Frame-Options"));
        Assert.Equal("nosniff", Header(response, "X-Content-Type-Options"));
        Assert.Equal("strict-origin-when-cross-origin", Header(response, "Referrer-Policy"));
        Assert.Equal("camera=(), microphone=(), geolocation=()", Header(response, "Permissions-Policy"));
        var csp = Header(response, "Content-Security-Policy");
        Assert.Contains("default-src 'self'", csp);
        Assert.Contains("script-src 'self' 'wasm-unsafe-eval'", csp);
        Assert.Contains("style-src 'self' 'unsafe-inline'", csp);
        Assert.Contains("connect-src 'self'", csp);
        Assert.Contains("frame-ancestors 'none'", csp);
    }

    [Fact]
    public async Task Unmatched_paths_get_the_headers_too()
    {
        // The middleware sits above routing and static files, so even a 404
        // (here: the fallback with no webroot index.html) is covered.
        using var app = new TestApp();
        using var client = app.CreateClient();

        var response = await client.GetAsync("/no/such/path");

        Assert.NotNull(Header(response, "Content-Security-Policy"));
    }

    [Fact]
    public async Task Inline_scripts_in_the_webroot_index_are_hashed_into_the_csp()
    {
        var webRoot = Directory.CreateTempSubdirectory("cardstock-csp-tests-").FullName;
        try
        {
            File.WriteAllText(Path.Combine(webRoot, "index.html"), "<script>var x = 1;</script>");
            using var app = new TestApp { WebRoot = webRoot };
            using var client = app.CreateClient();

            var response = await client.GetAsync("/healthz");

            Assert.Contains("'sha256-9nfWt3DNT14o+tZCP3YilfLwTrhLI98eqbN689B7ajY='",
                Header(response, "Content-Security-Policy"));
        }
        finally
        {
            Directory.Delete(webRoot, recursive: true);
        }
    }

    [Fact]
    public async Task Hsts_is_absent_by_default_even_over_https()
    {
        using var app = new TestApp();
        using var client = app.CreateClient(new() { BaseAddress = new Uri("https://localhost") });

        var response = await client.GetAsync("/healthz");

        Assert.Null(Header(response, "Strict-Transport-Security"));
    }

    [Fact]
    public async Task Hsts_appears_over_https_when_configured()
    {
        using var app = new TestApp { HstsMaxAgeSeconds = 86400 };
        using var client = app.CreateClient(new() { BaseAddress = new Uri("https://localhost") });

        var response = await client.GetAsync("/healthz");

        Assert.Equal("max-age=86400", Header(response, "Strict-Transport-Security"));
    }

    [Fact]
    public async Task Hsts_never_appears_on_plain_http()
    {
        using var app = new TestApp { HstsMaxAgeSeconds = 86400 };
        using var client = app.CreateClient();

        var response = await client.GetAsync("/healthz");

        Assert.Null(Header(response, "Strict-Transport-Security"));
    }

    [Fact]
    public async Task The_year_long_hsts_carries_include_subdomains()
    {
        using var app = new TestApp { HstsMaxAgeSeconds = 31536000, HstsIncludeSubdomains = true };
        using var client = app.CreateClient(new() { BaseAddress = new Uri("https://localhost") });

        var response = await client.GetAsync("/healthz");

        Assert.Equal("max-age=31536000; includeSubDomains", Header(response, "Strict-Transport-Security"));
    }

    private static string? Header(HttpResponseMessage response, string name) =>
        response.Headers.TryGetValues(name, out var values) ? string.Join(",", values) : null;
}
```

- [ ] **Step 3: Run tests to verify they fail**

Run: `dotnet test tests/CardStock.Api.Tests --filter SecurityHeaderTests -v minimal`
Expected: FAIL — headers absent (and `WebRoot`/`HstsMaxAgeSeconds` compile only after Step 1).

- [ ] **Step 4: Implement**

Create `src/CardStock.Api/Security/SecurityHeaders.cs`:

```csharp
using Microsoft.Extensions.Options;

namespace CardStock.Api.Security;

public sealed class SecurityHeaderOptions
{
    /// <summary>0 disables HSTS entirely — the deploy-time default. D-129's
    /// recorded trap: never raise this before the trusted cert is live and
    /// verified from outside (D-132 checklist section G owns the ramp).</summary>
    public int HstsMaxAgeSeconds { get; set; }

    public bool HstsIncludeSubdomains { get; set; }
}

public static class SecurityHeaders
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        var env = app.ApplicationServices.GetRequiredService<IWebHostEnvironment>();
        var options = app.ApplicationServices
            .GetRequiredService<IOptions<SecurityHeaderOptions>>().Value;

        var csp = BuildCsp(env);
        var hsts = options.HstsMaxAgeSeconds > 0
            ? $"max-age={options.HstsMaxAgeSeconds}"
              + (options.HstsIncludeSubdomains ? "; includeSubDomains" : "")
            : null;

        return app.Use((context, next) =>
        {
            var headers = context.Response.Headers;
            headers["Content-Security-Policy"] = csp;
            headers["X-Frame-Options"] = "DENY";
            headers["X-Content-Type-Options"] = "nosniff";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
            if (hsts is not null && context.Request.IsHttps)
            {
                headers["Strict-Transport-Security"] = hsts;
            }

            return next();
        });
    }

    private static string BuildCsp(IWebHostEnvironment env)
    {
        // The API project ships no wwwroot of its own; publish overlays the WASM
        // client's. Absent file (dev, tests) → no hash tokens, gracefully.
        var indexPath = Path.Combine(env.WebRootPath ?? "", "index.html");
        IReadOnlyList<string> hashes = File.Exists(indexPath)
            ? CspScriptHashes.FromHtml(File.ReadAllText(indexPath))
            : [];

        var scriptSrc = string.Join(' ',
            new[] { "'self'", "'wasm-unsafe-eval'" }.Concat(hashes));

        return "default-src 'self'; "
            + $"script-src {scriptSrc}; "
            + "style-src 'self' 'unsafe-inline'; "
            + "img-src 'self' data:; "
            + "connect-src 'self'; "
            + "frame-ancestors 'none'; "
            + "base-uri 'self'; "
            + "form-action 'self'";
    }
}
```

In `Program.cs`: add `using CardStock.Api.Security;`, bind the options beside the limiter registration:

```csharp
builder.Services.Configure<SecurityHeaderOptions>(builder.Configuration.GetSection("Security"));
```

and order the pipeline (this is the spec's load-bearing sequence — forwarded headers → host filtering (auto, runs in the host's startup filter) → security headers → rate limiter → app):

```csharp
app.UseForwardedHeaders();
app.UseSecurityHeaders();
app.UseRateLimiter();
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test tests/CardStock.Api.Tests --filter SecurityHeaderTests -v minimal`
Expected: 7 PASS.

- [ ] **Step 6: Full suite + commit**

Run: `dotnet test` — green. Then:

```bash
git add src/CardStock.Api/Security/SecurityHeaders.cs src/CardStock.Api/Program.cs tests/CardStock.Api.Tests/TestApp.cs tests/CardStock.Api.Tests/SecurityHeaderTests.cs
git commit -m "api: security headers — CSP with computed hashes, quartet, config-gated HSTS (D-132 §7)"
```

---

### Task 4: Host filtering — prove `AllowedHosts` refuses unlisted hosts

No production code: `WebApplication.CreateBuilder` registers host filtering by default, reading `AllowedHosts` from config. Dev keeps `"*"` (repo `appsettings.json` unchanged); Production gets `cardstock.pro;localhost;127.0.0.1` in Task 8. This task pins the behavior with a test so the Production value is known-good before it ships.

**Files:**
- Modify: `tests/CardStock.Api.Tests/TestApp.cs` (one hook)
- Test: `tests/CardStock.Api.Tests/HostFilteringTests.cs` (new)

**Interfaces:**
- Produces: `TestApp.AllowedHosts` (string?); the proven Production value Task 8 writes verbatim.

- [ ] **Step 1: Add the TestApp hook**

```csharp
/// <summary>Overrides AllowedHosts — dev/tests default to "*".</summary>
public string? AllowedHosts { get; set; }
```

and in `ConfigureWebHost`:

```csharp
if (AllowedHosts is not null)
{
    builder.UseSetting("AllowedHosts", AllowedHosts);
}
```

- [ ] **Step 2: Write the failing test**

Create `tests/CardStock.Api.Tests/HostFilteringTests.cs`:

```csharp
using System.Net;

namespace CardStock.Api.Tests;

public class HostFilteringTests
{
    [Fact]
    public async Task An_unlisted_host_gets_400_and_a_listed_one_gets_through()
    {
        using var app = new TestApp { AllowedHosts = "cardstock.pro;localhost;127.0.0.1" };
        using var client = app.CreateClient();

        using var evil = new HttpRequestMessage(HttpMethod.Get, "/healthz");
        evil.Headers.Host = "evil.example";
        var refused = await client.SendAsync(evil);

        using var listed = new HttpRequestMessage(HttpMethod.Get, "/healthz");
        listed.Headers.Host = "cardstock.pro";
        var served = await client.SendAsync(listed);

        Assert.Equal(HttpStatusCode.BadRequest, refused.StatusCode);
        Assert.Equal(HttpStatusCode.OK, served.StatusCode);
    }
}
```

- [ ] **Step 3: Run — expect it to PASS immediately**

Run: `dotnet test tests/CardStock.Api.Tests --filter HostFilteringTests -v minimal`
Expected: PASS (the framework middleware already does this — the test exists to prove the config value and catch a future framework/default change). If it FAILS, stop: the assumption that host filtering is auto-registered is wrong for this framework version, and an explicit `AddHostFiltering` + middleware registration must be added to Program.cs — do that, then re-run.

- [ ] **Step 4: Commit**

```bash
git add tests/CardStock.Api.Tests/TestApp.cs tests/CardStock.Api.Tests/HostFilteringTests.cs
git commit -m "api: pin AllowedHosts refusal behavior for the Production host list (D-132 §7)"
```

---

### Task 5: Ops files in the repo — unit hardening, deploy hook, deploy probe

**Files:**
- Modify: `ops/cardstock-api.service`
- Create: `ops/certbot-deploy-hook.sh`
- Modify: `ops/deploy.sh`
- Modify: `ops/README.md` (short new section)

**Interfaces:**
- Produces: the unit file Task 8 installs; the hook script Task 7 installs at `/etc/letsencrypt/renewal-hooks/deploy/cardstock.sh`; TLS files at `/etc/cardstock/tls/{fullchain,privkey}.pem` (root:cardstock 640) that Task 8's Kestrel config reads.

- [ ] **Step 1: Harden the unit for 443**

In `ops/cardstock-api.service`: delete the line `Environment=ASPNETCORE_URLS=http://0.0.0.0:5180` (Kestrel endpoints move to `appsettings.Production.json`; an env URL would fight them), and add below `MemoryMax=2G`:

```ini
# 443 is a privileged port; this grants exactly the low-port bind and nothing
# else, and composes with NoNewPrivileges (D-132 §4).
AmbientCapabilities=CAP_NET_BIND_SERVICE
```

- [ ] **Step 2: Write the certbot deploy hook**

Create `ops/certbot-deploy-hook.sh` (mode 755 in git: `chmod +x`):

```bash
#!/usr/bin/env bash
# Runs as root on successful issue/renew (certbot sets RENEWED_LINEAGE).
# Installed at /etc/letsencrypt/renewal-hooks/deploy/cardstock.sh — see
# ops/README.md. Copies the PEMs where the cardstock user can read them,
# then restarts the unit (Kestrel does not hot-reload cert files).
set -euo pipefail
install -d -m 750 -o root -g cardstock /etc/cardstock/tls
install -m 640 -o root -g cardstock "$RENEWED_LINEAGE/fullchain.pem" /etc/cardstock/tls/fullchain.pem
install -m 640 -o root -g cardstock "$RENEWED_LINEAGE/privkey.pem"  /etc/cardstock/tls/privkey.pem
systemctl restart cardstock-api
```

- [ ] **Step 3: Update the deploy probe for HTTPS**

In `ops/deploy.sh`, replace the last two lines (`curl -sf http://192.168.0.56:5180/healthz/data` and `echo`) with:

```bash
curl -sf --resolve cardstock.pro:443:192.168.0.56 https://cardstock.pro/healthz/data
echo
```

(`--resolve` pins the name to the Pi so the probe is direct, cert-verified, and needs no hosts-file entry. Task 10 changes the two IPs in this file to `192.168.30.56` when the Pi moves.)

- [ ] **Step 4: Document in ops/README.md**

Append a section:

```markdown
## TLS and port 443 (D-132)

Kestrel serves HTTPS-only on 443; endpoints and cert paths live in the Pi-only
appsettings.Production.json (never deployed — see the rsync exclude). Certs:
Let's Encrypt via certbot DNS-01 against Cloudflare (token in
/root/.secrets/certbot/cloudflare.ini, 600). On renew, certbot runs
/etc/letsencrypt/renewal-hooks/deploy/cardstock.sh (source:
ops/certbot-deploy-hook.sh) which copies PEMs to /etc/cardstock/tls
(root:cardstock 640) and restarts the unit. LAN access without Cloudflare:
add "192.168.30.56 cardstock.pro" to /etc/hosts on the dev machine — the
cert genuinely matches, so no warnings. HSTS ramps per D-132 §G only.
```

- [ ] **Step 5: Commit**

```bash
git add ops/cardstock-api.service ops/certbot-deploy-hook.sh ops/deploy.sh ops/README.md
git commit -m "ops: 443 unit capability, certbot deploy hook, HTTPS deploy probe (D-132 §B)"
```

---

### Task 6: Cloudflare zone + nameservers + API token (D-132 §A)

Manual (dashboards). All free tier. Tick D-132 §A boxes as receipts land.

- [ ] **Step 1:** Create the free Cloudflare account (or sign in) → Add a domain → `cardstock.pro` → **Free plan**. Cloudflare imports the parking records — delete every imported DNS record so the zone is empty.
- [ ] **Step 2:** Namecheap → Domain List → `cardstock.pro` → Manage → Nameservers → **Custom DNS** → enter the two nameservers Cloudflare assigned.
- [ ] **Step 3:** Wait for activation (minutes to hours; proceed with Task 7 prerequisites meanwhile). Receipt: `dig NS cardstock.pro +short` returns the two `*.ns.cloudflare.com` names, and the zone shows **Active**.
- [ ] **Step 4:** Cloudflare dashboard → My Profile → API Tokens → Create Token → template **Edit zone DNS** → Zone Resources: *Include → Specific zone → cardstock.pro* → create. Copy the token once.
- [ ] **Step 5:** Store it on the Pi, root-only:

```bash
ssh scott@192.168.0.56 'sudo mkdir -p /root/.secrets/certbot && sudo tee /root/.secrets/certbot/cloudflare.ini > /dev/null && sudo chmod 600 /root/.secrets/certbot/cloudflare.ini' <<'EOF'
dns_cloudflare_api_token = PASTE-TOKEN-HERE
EOF
```

(Paste the real token in place of `PASTE-TOKEN-HERE` when running — never commit it anywhere.)

- [ ] **Step 6:** Tick D-132 §A in `DECISIONS.md`, commit: `git add DECISIONS.md && git commit -m "ledger: D-132 §A ticked — zone live on Cloudflare"`.

---

### Task 7: Certificate issued on the Pi (D-132 §B, first half)

- [ ] **Step 1:** Install: `ssh scott@192.168.0.56 'sudo apt-get update && sudo apt-get install -y certbot python3-certbot-dns-cloudflare'`
- [ ] **Step 2:** Issue (DNS-01; no inbound anything):

```bash
ssh scott@192.168.0.56 'sudo certbot certonly --dns-cloudflare \
  --dns-cloudflare-credentials /root/.secrets/certbot/cloudflare.ini \
  -d cardstock.pro -d www.cardstock.pro \
  --non-interactive --agree-tos -m scbush88@gmail.com'
```

Receipt: `Successfully received certificate`, lineage at `/etc/letsencrypt/live/cardstock.pro/`.

- [ ] **Step 3:** Install the deploy hook from the repo and run it once by hand (the unit restart at the end will fail harmlessly if Task 8 hasn't deployed yet — rerun after Task 8 if so):

```bash
scp ops/certbot-deploy-hook.sh scott@192.168.0.56:/tmp/cardstock-hook.sh
ssh scott@192.168.0.56 'sudo install -m 755 /tmp/cardstock-hook.sh /etc/letsencrypt/renewal-hooks/deploy/cardstock.sh && sudo RENEWED_LINEAGE=/etc/letsencrypt/live/cardstock.pro /etc/letsencrypt/renewal-hooks/deploy/cardstock.sh'
```

Receipt: `ssh scott@192.168.0.56 'sudo ls -l /etc/cardstock/tls'` shows both PEMs, `root cardstock`, mode 640.

- [ ] **Step 4:** Prove renewal works end to end: `ssh scott@192.168.0.56 'sudo certbot renew --dry-run'` → `Congratulations, all simulated renewals succeeded`. Confirm the timer: `systemctl list-timers certbot.timer`.
- [ ] **Step 5:** Tick D-132 §B's cert boxes; commit the ledger edit.

---

### Task 8: The 443 cutover — Production config, unit install, deploy, LAN verify (D-132 §B)

- [ ] **Step 1:** Back up and edit the Pi-only config:

```bash
ssh scott@192.168.0.56 'sudo cp /opt/cardstock/api/appsettings.Production.json /opt/cardstock/api/appsettings.Production.json.bak-20xx-xx-xx && sudo cat /opt/cardstock/api/appsettings.Production.json'
```

Merge these keys into the existing JSON (keep `ConnectionStrings` and everything already there; write with `sudo tee`, then `sudo chown cardstock:cardstock`):

```json
{
  "Kestrel": {
    "Endpoints": {
      "Https": {
        "Url": "https://0.0.0.0:443",
        "Certificate": {
          "Path": "/etc/cardstock/tls/fullchain.pem",
          "KeyPath": "/etc/cardstock/tls/privkey.pem"
        }
      }
    }
  },
  "AllowedHosts": "cardstock.pro;localhost;127.0.0.1",
  "RateLimits": { "ExpressPerHour": 120 },
  "Security": { "HstsMaxAgeSeconds": 0 }
}
```

(120/hour is the D-129 interim tighten — double a hard hour of all-stale browsing, minutes to trip for a script; reverts toward per-account values when accounts land, per D-062. HSTS stays 0 until §G.)

- [ ] **Step 2:** Install the hardened unit and publish/deploy the Task 1–5 code:

```bash
scp ops/cardstock-api.service scott@192.168.0.56:/tmp/cardstock-api.service
ssh scott@192.168.0.56 'sudo install -m 644 /tmp/cardstock-api.service /etc/systemd/system/cardstock-api.service && sudo systemctl daemon-reload'
./ops/publish.sh publish/api
./ops/deploy.sh
```

Expected: deploy.sh's probe prints the `/healthz/data` counts over **https**. If the unit fails to start, read `ssh scott@192.168.0.56 'journalctl -u cardstock-api -n 50'` — the two likely causes are cert file permissions (Task 7 Step 3) and a malformed Production JSON merge.

- [ ] **Step 3:** Hardening receipt: `ssh scott@192.168.0.56 'systemd-analyze security cardstock-api | tail -3'` — record the exposure score in the D-132 tick.
- [ ] **Step 4:** Dev-machine hosts entry (direct, Cloudflare-free access): add `192.168.0.56 cardstock.pro` to `/etc/hosts` (updated in Task 10 to `.30.56`). Receipt: browser loads `https://cardstock.pro` with a valid padlock; `curl -sI https://cardstock.pro/healthz | grep -iE "content-security|x-frame|nosniff"` shows the Task 3 headers; no `Strict-Transport-Security` line yet.
- [ ] **Step 5:** Confirm the plain-HTTP listener is gone: `ssh scott@192.168.0.56 'ss -tlnp | grep -E "5180|:443"'` → 443 present, 5180 absent.
- [ ] **Step 6:** Tick §B's deploy boxes; commit the ledger edit.

---

### Task 9: Postgres lockdown (D-132 §B, last item; ruling in D-131)

- [ ] **Step 1:** Pick a quiet moment (crawler visits are spaced; a 2-second restart between them at worst errors one visit, which the journal records and the scheduler retries).
- [ ] **Step 2:**

```bash
ssh scott@192.168.0.56 'sudo -u postgres psql -c "ALTER SYSTEM SET listen_addresses = '"'"'localhost'"'"';" \
 && sudo cp /etc/postgresql/15/main/pg_hba.conf /etc/postgresql/15/main/pg_hba.conf.bak-20xx-xx-xx \
 && sudo sed -i "/192\.168\.0\.0\/24/d" /etc/postgresql/15/main/pg_hba.conf \
 && sudo systemctl restart postgresql'
```

- [ ] **Step 3:** Receipt: `ssh scott@192.168.0.56 'ss -tlnp | grep 5432'` → `127.0.0.1:5432` and `[::1]:5432` only. The app still answers: `curl -sf --resolve cardstock.pro:443:192.168.0.56 https://cardstock.pro/healthz/data`.
- [ ] **Step 4:** Dev machine `~/.ssh/config` gains (HostName updated in Task 10):

```
Host pi-db
    HostName 192.168.0.56
    User scott
    LocalForward 5433 127.0.0.1:5432
```

Repoint `POKEMON_TEST_DB` to host `127.0.0.1`, port `5433` (same database and `pokemon_tester` credentials). Receipt: `ssh -fN pi-db`, then run the DB-gated test suite — the usual skips become runs and pass.

- [ ] **Step 5:** Tick §B's Postgres box; commit the ledger edit.

---

### Task 10: Omada topology — DMZ VLAN, ACLs, the move, WireGuard, DDNS (D-132 §C)

Controller steps; menu names re-checked against the installed controller version (spec §5's standing flag). Everything here is in the Omada controller UI unless marked.

- [ ] **Step 1 — pre-move sweep:** `ssh scott@192.168.0.56 'ss -tnp | grep -v "127.0.0.1\|::1"'` — record any established LAN-bound flows in the D-132 tick (expected: only your own ssh session).
- [ ] **Step 2 — reservations:** Clients → dev machine → Fixed IP (its current address). (The Pi's reservation lands in Step 4 on the new network.)
- [ ] **Step 3 — DMZ network:** Settings → Wired Networks → LAN → Create New LAN: name `DMZ`, VLAN `30`, gateway/subnet `192.168.30.1/24`, DHCP on with a small range (e.g. `.100–.150`).
- [ ] **Step 4 — gateway ACLs** (Settings → Network Security → ACL → Gateway ACL; create in this exact order — first match wins). If the UI can't express a rule (match-state or VPN-as-source missing on this version), stop and record which; the fallback for rule 4 is spec §5's (VPN sees what LAN sees), and a missing match-state option means rules 1–2 collapse into the controller's "unidirectional" recipe per TP-Link FAQ 3745:

| # | Action | Direction | Source | Destination | Ports | States |
|---|---|---|---|---|---|---|
| 1 | Permit | LAN→LAN | `DMZ` network | `LAN` network | all | Established, Related |
| 2 | Deny | LAN→LAN | `DMZ` network | `LAN` network | all | all |
| 3 | Permit | LAN→LAN | dev machine IP | `192.168.30.56/32` | TCP 22, 443 | all |
| 4 | Permit | LAN→LAN | WG-VPN network/`10.9.0.0/24` | `192.168.30.56/32` | TCP 22, 443 | all |
| 5 | Permit | LAN→LAN | `LAN` network | `192.168.30.56/32` | TCP 443 | all |
| 6 | Deny | LAN→LAN | `LAN` network | `DMZ` network | all | all |

- [ ] **Step 5 — the move:** Devices → the switch → Ports → the Pi's port → Profile: the `DMZ` (VLAN 30) profile. Then Clients → the Pi → Fixed IP `192.168.30.56`. Power-cycle or renew the Pi's lease. Receipt: `ping 192.168.30.56` from the dev machine answers; `ssh scott@192.168.30.56 hostname` works (accept the new known_hosts entry — same key, new address).
- [ ] **Step 6 — update every old-IP reference:** dev `~/.ssh/config` (`pi-db` HostName → `192.168.30.56`), `/etc/hosts` (`192.168.30.56 cardstock.pro`), and in the repo `ops/deploy.sh` (both `192.168.0.56` → `192.168.30.56`); commit: `git add ops/deploy.sh && git commit -m "ops: the Pi moves to the DMZ — deploy targets 192.168.30.56 (D-132 §C)"`.
- [ ] **Step 7 — survival checks (each is a §C receipt):** `./ops/deploy.sh` end to end · `ssh -fN pi-db` + DB-gated tests · `https://cardstock.pro` from the dev machine (hosts entry) and from a phone on wifi (rule 5) · from another LAN device, `ssh 192.168.30.56` **times out** (rule 6) · on the Pi, `curl -s https://www.pricecharting.com > /dev/null && echo out-ok` and the worker journal shows visits continuing · `sudo apt-get update` succeeds.
- [ ] **Step 8 — WireGuard:** Settings → VPN → WireGuard → Create: name `wg-home`, listen port `51820`, local IP `10.9.0.1/24`. Peers → add laptop and phone: each device generates its own keypair (WireGuard app → New tunnel); paste each **public** key, Allowed Address `10.9.0.2/32` / `10.9.0.3/32`. Client config per device: Address = its `/32`, DNS optional, Peer = gateway public key, Endpoint = the Step 9 DDNS name`:51820`, AllowedIPs = `192.168.30.0/24` (add `192.168.0.0/24` on devices that should reach the whole house).
- [ ] **Step 9 — DDNS:** create a free No-IP hostname whose name has no connection to "cardstock" (e.g. a random-word handle). Controller → Settings → Services (or Device Config → DNS on this version) → Dynamic DNS → Create: provider No-IP, the hostname, account credentials, WAN interface. Receipt: status shows a good return code and `dig +short <ddns-name>` returns the home IP.
- [ ] **Step 10 — remote receipt:** phone on cellular (wifi off): WireGuard on → `https://cardstock.pro` loads via the hosts-free public path AND `ssh scott@192.168.30.56` from a terminal app connects; WireGuard off → `ssh` to `192.168.30.56` is unreachable. Tick §C; commit the ledger edit.

---

### Task 11: Tunnel up — the site goes public (D-132 §D)

- [ ] **Step 1:** Zero Trust dashboard → Networks → Tunnels → Create tunnel → Cloudflared → name `cardstock-pi`. Copy the connector install command for **Debian arm64** and run it on the Pi over ssh (it adds Cloudflare's apt repo, installs `cloudflared`, and registers the systemd service with the token).
- [ ] **Step 2:** In the tunnel's Public Hostname tab: hostname `cardstock.pro`, service `https://127.0.0.1:443`, and under Additional application settings → TLS: **Origin Server Name** = `cardstock.pro` (leave "No TLS Verify" **off** — verification is the point).
- [ ] **Step 3:** Zone → SSL/TLS → Overview → mode **Full (strict)**. Edge Certificates → **Always Use HTTPS: on**.
- [ ] **Step 4:** Rules → Redirect Rules → Create: *When* Hostname equals `www.cardstock.pro` → *Then* Static redirect, 301, to `https://cardstock.pro`. (The tunnel hostname step auto-created the apex DNS record; add `www` as a proxied CNAME to `cardstock.pro` if the rule needs the record to exist.)
- [ ] **Step 5:** Receipts: on a phone **off wifi** — `https://cardstock.pro` loads, padlock valid; `http://cardstock.pro` upgrades; `https://www.cardstock.pro` lands on the apex. `ss -tlnp` on the Pi unchanged (no new listeners; cloudflared holds outbound connections only). Tick §D; commit the ledger edit.

---

### Task 12: Edge posture + email DNS + CAA (D-132 §E)

- [ ] **Step 1:** Zone → Security → WAF → Managed rules: **Cloudflare Free Managed Ruleset** on. Security → Bots: **Bot Fight Mode** on (record in the tick that this is the watched toggle).
- [ ] **Step 2:** Security → WAF → Rate limiting rules → Create (the one free slot): *If* URI Path wildcard `/api/v1/cards/*/refresh` → rate 10 requests per 10 seconds per IP → Block for 10 seconds.
- [ ] **Step 3:** DNS → Records: `CAA` at `cardstock.pro`, flag `0`, tag `issue`, value `letsencrypt.org`. **Paired same-day check** (the spec's Universal-SSL subtlety): after it saves, confirm SSL/TLS → Edge Certificates shows the Universal certificate still **Active**, and `https://cardstock.pro` still loads from off-wifi.
- [ ] **Step 4:** Resend (free): create account → Domains → Add `cardstock.pro` → add the DNS records it lists into the Cloudflare zone exactly as issued (DKIM TXT(s); its SPF/MX pair normally lands on the `send` subdomain — **leave the apex records alone**). Wait for Resend to show **Verified**.
- [ ] **Step 5:** Apex email-auth records in the zone: TXT `cardstock.pro` = `v=spf1 -all` · TXT `_dmarc.cardstock.pro` = `v=DMARC1; p=reject` · MX `cardstock.pro` = `.` priority `0` (null MX).
- [ ] **Step 6:** Receipt: from Resend's dashboard send a test email to `scbush88@gmail.com`; in Gmail, Show original → `DKIM: PASS`, `DMARC: PASS`. External check of the records (e.g. MXToolbox SPF/DMARC lookups) shows the strict apex posture. Tick §E; commit the ledger edit.

---

### Task 13: Outside-in verification (D-132 §F)

All from networks that aren't yours (phone hotspot for the CLI probes).

- [ ] **Step 1:** SSL Labs (`ssllabs.com/ssltest`) on `cardstock.pro` → **A** (A+ arrives with HSTS in Task 14). Record the grade.
- [ ] **Step 2:** `securityheaders.com` → **A**. If it flags a header the spec includes, fix before proceeding; if it suggests one the spec deliberately omits, record why in the tick (CSP `unsafe-inline` style-src is expected and reasoned — spec §7).
- [ ] **Step 3:** Origin concealment, from the hotspot: `curl -sv --max-time 10 --resolve cardstock.pro:443:<home-ip> https://cardstock.pro/healthz` → connection **times out** (no route from the internet to the origin). `nmap -Pn <home-ip> -p 22,80,443,5432,51820` → no TCP port answers (51820 is UDP and silent by design).
- [ ] **Step 4:** Cap trip, from the hotspot, using a nonexistent card id so no upstream fetch ever fires (the worker 404s after the limiter counts):

```bash
for i in $(seq 1 15); do curl -s -o /dev/null -w "%{http_code} " -X POST https://cardstock.pro/api/v1/cards/999999999/refresh; done; echo
```

Expected: a run of `404` then `429`s once the edge rule (10/10s) trips. Record the transition point.

- [ ] **Step 5:** Full journey on the phone: browse → a card page → chart renders → ledger pages → a refresh on a stale card completes.
- [ ] **Step 6:** The trackers promise: read `CardStock Mockup/Cardstock Legal.dc.html`'s actual claim (D-037 marked it unread); confirm the page makes only same-origin requests (browser devtools network tab shows no third-party host) and the server config sends nothing third-party. Record what the legal copy actually says for the marketing phase's correction pass.
- [ ] **Step 7:** Tick §F; commit the ledger edit.

---

### Task 14: HSTS ramp — strictly last (D-132 §G)

**Gate:** Tasks 11 and 13 receipts exist (cert live, tunnel serving, SSL Labs A). Do not start this task the same day problems are open anywhere above.

- [ ] **Step 1:** On the Pi, edit `appsettings.Production.json` (backup first, per convention): `"Security": { "HstsMaxAgeSeconds": 86400 }` → `sudo systemctl restart cardstock-api`. Receipt: `curl -sI https://cardstock.pro/healthz | grep -i strict` → `max-age=86400`.
- [ ] **Step 2:** Wait **≥7 days** of normal use with no TLS issues. (Calendar reminder; nothing else in this plan blocks on it.)
- [ ] **Step 3:** Raise to final: `"Security": { "HstsMaxAgeSeconds": 31536000, "HstsIncludeSubdomains": true }` → restart → `curl -sI` shows `max-age=31536000; includeSubDomains`. No `preload` — deliberately (spec §4).
- [ ] **Step 4:** SSL Labs re-scan → **A+**. Tick §G; commit the ledger edit.

---

### Task 15: Closeout (D-132 §H)

- [ ] **Step 1:** In `DECISIONS.md`: mark D-037 largely closed — note what remains and where it lives (the `sales.title` XSS render test rides its screen's phase; the abuse-shape limit moves per-account with D-130). Record the D-129 amendments as executed (443; the static-IP call never made, needed only if direct-443 is ever adopted). Add §H's recorded-risk line verbatim from the checklist.
- [ ] **Step 2:** Update the assistant memory files: `pi-address-and-db-access` (new IP `192.168.30.56`, the `pi-db` tunnel, `https://cardstock.pro`), and the roadmap memory (exposure phase SHIPPED; next: `superpowers:writing-plans` against the banked accounts spec per D-130).
- [ ] **Step 3:** Final commit of the fully-ticked ledger:

```bash
git add DECISIONS.md
git commit -m "ledger: D-132 fully ticked — cardstock.pro is public; the phase closes"
```

- [ ] **Step 4:** Tell the owner the door is open, with the URL and the receipts summary — and that the accounts phase (D-130) is unblocked.
