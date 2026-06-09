using FilterIcs;
using Ical.Net;
using Xunit;

namespace FilterIcs.Tests;

public class FeedsTests
{
    private static HashSet<string> Uids(string icsPath)
    {
        var cal = Calendar.Load(File.ReadAllText(icsPath))!;
        return cal.Events.Select(e => e.Uid!).ToHashSet();
    }

    // ---- T016: one feed per file, fetch once ----

    [Fact]
    public void One_feed_per_file_fetch_once()
    {
        string allow = Fixture.MakeAllowlistsDir(
            ("whos-out.json", "{\"fileName\": \"whos-out\", \"names\": [\"John Doe\", \"Jane Doe\"]}"),
            ("engineering.json", "{\"fileName\": \"engineering\", \"names\": [\"John Doe\"]}"));
        string outDir = Path.Combine(Path.GetTempPath(), "out-" + Path.GetRandomFileName());
        var (fetch, calls) = Fixture.CountingFetcher();

        RunResult result = Feeds.Run("https://example.invalid/feed", allow, outDir, fetch);

        Assert.Equal(1, calls());
        Assert.Equal(new HashSet<string> { "whos-out", "engineering" }, result.Written.ToHashSet());
        Assert.Equal(new HashSet<string> { "evt-john-1", "evt-jane-1" }, Uids(Path.Combine(outDir, "whos-out.ics")));
        Assert.Equal(new HashSet<string> { "evt-john-1" }, Uids(Path.Combine(outDir, "engineering.ics")));

        Directory.Delete(allow, true);
        Directory.Delete(outDir, true);
    }

    [Fact]
    public void FileName_with_ics_extension_not_doubled()
    {
        string allow = Fixture.MakeAllowlistsDir(("team.json", "{\"fileName\": \"team.ics\", \"names\": [\"John Doe\"]}"));
        string outDir = Path.Combine(Path.GetTempPath(), "out-" + Path.GetRandomFileName());
        var (fetch, _) = Fixture.CountingFetcher();

        Feeds.Run("u", allow, outDir, fetch);

        Assert.True(File.Exists(Path.Combine(outDir, "team.ics")));
        Assert.False(File.Exists(Path.Combine(outDir, "team.ics.ics")));
        Directory.Delete(allow, true);
        Directory.Delete(outDir, true);
    }

    // ---- T024: removal ----

    [Fact]
    public void Remove_file_removes_feed()
    {
        string allow = Fixture.MakeAllowlistsDir(
            ("whos-out.json", "{\"fileName\": \"whos-out\", \"names\": [\"John Doe\"]}"),
            ("engineering.json", "{\"fileName\": \"engineering\", \"names\": [\"John Doe\"]}"));
        string outDir = Path.Combine(Path.GetTempPath(), "out-" + Path.GetRandomFileName());
        var (fetch, _) = Fixture.CountingFetcher();

        Feeds.Run("u", allow, outDir, fetch);
        Assert.True(File.Exists(Path.Combine(outDir, "engineering.ics")));

        File.Delete(Path.Combine(allow, "engineering.json"));
        RunResult result = Feeds.Run("u", allow, outDir, fetch);

        Assert.False(File.Exists(Path.Combine(outDir, "engineering.ics")));
        Assert.True(File.Exists(Path.Combine(outDir, "whos-out.ics")));
        Assert.Contains("engineering", result.Removed);

        Directory.Delete(allow, true);
        Directory.Delete(outDir, true);
    }

    // ---- T029: isolation / last-good / safety ----

    [Fact]
    public void Invalid_file_skipped_others_published()
    {
        string allow = Fixture.MakeAllowlistsDir(
            ("whos-out.json", "{\"fileName\": \"whos-out\", \"names\": [\"John Doe\"]}"),
            ("engineering.json", "{\"fileName\": \"engineering\", \"names\": [\"Jane Doe\"]}"),
            ("broken.json", "{ not json"));
        string outDir = Path.Combine(Path.GetTempPath(), "out-" + Path.GetRandomFileName());
        var (fetch, _) = Fixture.CountingFetcher();

        RunResult result = Feeds.Run("u", allow, outDir, fetch);

        Assert.True(File.Exists(Path.Combine(outDir, "whos-out.ics")));
        Assert.True(File.Exists(Path.Combine(outDir, "engineering.ics")));
        Assert.Contains(result.Skipped, s => s.basename == "broken.json");
        Assert.Equal(3, result.Total);

        Directory.Delete(allow, true);
        Directory.Delete(outDir, true);
    }

    [Fact]
    public void Errored_file_preserves_last_good()
    {
        string allow = Fixture.MakeAllowlistsDir(("engineering.json", "{\"fileName\": \"engineering\", \"names\": [\"John Doe\"]}"));
        string outDir = Path.Combine(Path.GetTempPath(), "out-" + Path.GetRandomFileName());
        var (fetch, _) = Fixture.CountingFetcher();

        Feeds.Run("u", allow, outDir, fetch);
        byte[] good = File.ReadAllBytes(Path.Combine(outDir, "engineering.ics"));

        File.WriteAllText(Path.Combine(allow, "engineering.json"), "{ broken");
        RunResult result = Feeds.Run("u", allow, outDir, fetch);

        Assert.Equal(good, File.ReadAllBytes(Path.Combine(outDir, "engineering.ics")));
        Assert.Contains(result.Skipped, s => s.basename == "engineering.json");
        Assert.DoesNotContain("engineering", result.Removed);

        Directory.Delete(allow, true);
        Directory.Delete(outDir, true);
    }

    [Fact]
    public void Duplicate_output_names_both_skipped()
    {
        string allow = Fixture.MakeAllowlistsDir(
            ("a.json", "{\"fileName\": \"team\", \"names\": [\"John Doe\"]}"),
            ("b.json", "{\"fileName\": \"team\", \"names\": [\"Jane Doe\"]}"));
        string outDir = Path.Combine(Path.GetTempPath(), "out-" + Path.GetRandomFileName());
        var (fetch, _) = Fixture.CountingFetcher();

        RunResult result = Feeds.Run("u", allow, outDir, fetch);

        Assert.False(File.Exists(Path.Combine(outDir, "team.ics")));
        Assert.Equal(new HashSet<string> { "a.json", "b.json" }, result.Skipped.Select(s => s.basename).ToHashSet());

        Directory.Delete(allow, true);
        Directory.Delete(outDir, true);
    }

    [Fact]
    public void Path_traversal_rejected()
    {
        string allow = Fixture.MakeAllowlistsDir(("evil.json", "{\"fileName\": \"../evil\", \"names\": [\"John Doe\"]}"));
        string outDir = Path.Combine(Path.GetTempPath(), "out-" + Path.GetRandomFileName());
        var (fetch, _) = Fixture.CountingFetcher();

        RunResult result = Feeds.Run("u", allow, outDir, fetch);

        Assert.Contains(result.Skipped, s => s.basename == "evil.json");
        Assert.False(File.Exists(Path.Combine(Path.GetDirectoryName(outDir)!, "evil.ics")));

        Directory.Delete(allow, true);
        if (Directory.Exists(outDir)) Directory.Delete(outDir, true);
    }
}
