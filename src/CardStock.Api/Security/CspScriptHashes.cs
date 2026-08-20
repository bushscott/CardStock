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
