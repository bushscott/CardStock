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
