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
