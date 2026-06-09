using FilterIcs;

namespace FilterIcs.Tests;

internal static class Fixture
{
    public static string SourcePath =>
        Path.Combine(AppContext.BaseDirectory, "fixtures", "source.ics");

    public static string SourceText => File.ReadAllText(SourcePath);

    public static Allowlist AllowOf(params string[] names) => Allowlist.FromNames(names);

    /// <summary>A temp allowlists dir with the given (basename, json) entries; caller deletes.</summary>
    public static string MakeAllowlistsDir(params (string name, string json)[] files)
    {
        string dir = Path.Combine(Path.GetTempPath(), "al-" + Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        foreach (var (name, json) in files)
            File.WriteAllText(Path.Combine(dir, name), json);
        return dir;
    }

    /// <summary>A fetcher that always returns the fixture and counts calls.</summary>
    public static (Func<string, string> fetch, Func<int> calls) CountingFetcher()
    {
        int n = 0;
        return (_ => { n++; return SourceText; }, () => n);
    }
}
