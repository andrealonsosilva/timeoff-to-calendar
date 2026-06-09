using FilterIcs;

namespace FilterIcs.Tests;

internal static class Fixture
{
    public static string SourcePath =>
        Path.Combine(AppContext.BaseDirectory, "fixtures", "source.ics");

    public static string SourceText => File.ReadAllText(SourcePath);

    public static Allowlist AllowOf(params string[] names)
    {
        string json = "[" + string.Join(",", names.Select(n => $"\"{n}\"")) + "]";
        string tmp = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".json");
        File.WriteAllText(tmp, json);
        try
        {
            return Allowlist.Load(tmp);
        }
        finally
        {
            File.Delete(tmp);
        }
    }
}
