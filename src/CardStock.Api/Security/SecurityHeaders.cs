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
