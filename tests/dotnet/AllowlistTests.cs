using System.Threading.Tasks;
using FilterIcs;
using Xunit;

namespace FilterIcs.Tests;

public class AllowlistTests
{
    private static string WriteTemp(string content)
    {
        string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".json");
        File.WriteAllText(path, content);
        return path;
    }

    // ---- T030: behavior ----

    [Fact]
    public void Loads_names_and_matches_case_insensitively()
    {
        Allowlist al = Allowlist.Load(WriteTemp("[\"Pedro Fernandes\", \"Thiago Bessa\"]"));
        Assert.Equal(new[] { "Pedro Fernandes", "Thiago Bessa" }, al.Names);
        Assert.True(al.Contains("pedro fernandes"));
        Assert.False(al.Contains("Maria Silva"));
    }

    [Fact]
    public void Empty_array_is_valid()
    {
        Allowlist al = Allowlist.Load(WriteTemp("[]"));
        Assert.Empty(al.Names);
    }

    [Fact]
    public void Duplicates_are_deduped()
    {
        Allowlist al = Allowlist.Load(WriteTemp("[\"Pedro Fernandes\", \"  pedro fernandes  \"]"));
        Assert.Equal(new[] { "Pedro Fernandes" }, al.Names);
    }

    // ---- T028: safety ----

    [Fact]
    public void Missing_file_throws()
    {
        string missing = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".json");
        Assert.Throws<AllowlistException>(() => Allowlist.Load(missing));
    }

    [Fact]
    public void Invalid_json_throws()
    {
        Assert.Throws<AllowlistException>(() => Allowlist.Load(WriteTemp("{ not json")));
    }

    [Fact]
    public void Non_array_throws()
    {
        Assert.Throws<AllowlistException>(() => Allowlist.Load(WriteTemp("{\"name\":\"x\"}")));
    }

    [Fact]
    public void Empty_string_entry_throws()
    {
        Assert.Throws<AllowlistException>(() => Allowlist.Load(WriteTemp("[\"ok\", \"  \"]")));
    }

    [Fact]
    public async Task Cli_bad_allowlist_exits_4_and_leaves_output_untouched()
    {
        string output = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".ics");
        File.WriteAllText(output, "PREVIOUS GOOD FEED");
        string bad = WriteTemp("{ not json");

        int code = await Program.Run(new[]
        {
            "--source-url", "https://example.invalid/feed",
            "--allowlist", bad,
            "--output", output,
        });

        Assert.Equal(Program.ExitAllowlist, code);
        Assert.Equal("PREVIOUS GOOD FEED", File.ReadAllText(output));
        File.Delete(output);
    }

    [Fact]
    public async Task Cli_missing_source_url_exits_2()
    {
        Environment.SetEnvironmentVariable("SOURCE_ICS_URL", null);
        string names = WriteTemp("[]");
        int code = await Program.Run(new[] { "--allowlist", names, "--output",
            Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".ics") });
        Assert.Equal(Program.ExitConfig, code);
    }

    [Fact]
    public void Redact_strips_token()
    {
        Assert.Equal("https://example.com", Program.Redact("https://example.com/feed?token=SECRET"));
        Assert.DoesNotContain("SECRET", Program.Redact("https://example.com/feed?token=SECRET"));
    }
}
