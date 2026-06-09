using FilterIcs;
using Xunit;

namespace FilterIcs.Tests;

public class AllowlistObjectTests
{
    [Fact]
    public void Parse_valid_object()
    {
        var (fileName, names) = FeedFile.ParseObject("{\"fileName\": \"engineering\", \"names\": [\"John Doe\", \"Jane Doe\"]}");
        Assert.Equal("engineering", fileName);
        Assert.Equal(new[] { "John Doe", "Jane Doe" }, names);
    }

    [Theory]
    [InlineData("{ not json")]
    [InlineData("[]")]
    [InlineData("{\"names\": [\"x\"]}")]
    [InlineData("{\"fileName\": \"\", \"names\": []}")]
    [InlineData("{\"fileName\": \"x\"}")]
    [InlineData("{\"fileName\": \"x\", \"names\": {}}")]
    public void Parse_invalid_object_throws(string raw) =>
        Assert.Throws<FeedException>(() => FeedFile.ParseObject(raw));

    [Fact]
    public void FromNames_dedupes_and_normalizes()
    {
        Allowlist al = Allowlist.FromNames(new[] { "John Doe", "  john doe  " });
        Assert.Equal(new[] { "John Doe" }, al.Names);
        Assert.True(al.Contains("JOHN DOE"));
        Assert.False(al.Contains("Jane Doe"));
    }

    [Fact]
    public void FromNames_rejects_blank() =>
        Assert.Throws<FeedException>(() => Allowlist.FromNames(new[] { "ok", "  " }));
}
