using FilterIcs;
using Xunit;

namespace FilterIcs.Tests;

public class NamingTests
{
    [Theory]
    [InlineData("whos-out", "whos-out")]
    [InlineData("engineering", "engineering")]
    [InlineData("team.ics", "team")]
    [InlineData("  spaced  ", "spaced")]
    [InlineData("team_v2", "team_v2")]
    public void Sanitize_ok(string raw, string expected) =>
        Assert.Equal(expected, Naming.SanitizeOutputName(raw));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("../evil")]
    [InlineData("a/b")]
    [InlineData("a\\b")]
    [InlineData("..")]
    [InlineData(".hidden")]
    [InlineData("bad name")]
    public void Sanitize_rejects_unsafe(string raw) =>
        Assert.Throws<FeedException>(() => Naming.SanitizeOutputName(raw));

    [Fact]
    public void Output_file_name() => Assert.Equal("whos-out.ics", Naming.OutputFileName("whos-out"));
}
