using System.Text.RegularExpressions;

namespace FilterIcs;

/// <summary>Output-name sanitization for feed files (contracts/publishing.md G4).</summary>
public static partial class Naming
{
    [GeneratedRegex("^[A-Za-z0-9._-]+$")]
    private static partial Regex SafeSegment();

    /// <summary>
    /// Return a safe base name (no extension). Strips a trailing ".ics" once, then validates a
    /// single safe path segment (no separators, no '.'/'..', no leading dot). Throws on unsafe/empty.
    /// </summary>
    public static string SanitizeOutputName(string fileName)
    {
        string name = fileName.Trim();
        if (name.EndsWith(".ics", StringComparison.OrdinalIgnoreCase))
            name = name[..^4];

        if (string.IsNullOrEmpty(name)
            || name is "." or ".."
            || name.Contains('/')
            || name.Contains('\\')
            || name.StartsWith('.')
            || !SafeSegment().IsMatch(name))
        {
            throw new FeedException($"unsafe or empty fileName: '{fileName}'");
        }
        return name;
    }

    public static string OutputFileName(string outputName) => outputName + ".ics";
}
