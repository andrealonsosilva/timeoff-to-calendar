using System.Text.Json;

namespace FilterIcs;

/// <summary>A per-feed problem (bad JSON, missing fileName, bad names). The feed is skipped.</summary>
public sealed class FeedException : Exception
{
    public FeedException(string message, Exception? inner = null) : base(message, inner) { }
}

/// <summary>An immutable allowlist used for matching.</summary>
public sealed class Allowlist
{
    private readonly HashSet<string> _normalized;

    public IReadOnlyList<string> Names { get; }

    private Allowlist(IReadOnlyList<string> names, HashSet<string> normalized)
    {
        Names = names;
        _normalized = normalized;
    }

    public bool Contains(string name) => _normalized.Contains(Names_Normalize(name));

    private static string Names_Normalize(string name) => FilterIcs.Names.Normalize(name);

    /// <summary>Build from a list of names (de-duped, trimmed). Throws on blank entries.</summary>
    public static Allowlist FromNames(IEnumerable<string> names)
    {
        var ordered = new List<string>();
        var seen = new HashSet<string>();
        foreach (string item in names)
        {
            if (string.IsNullOrWhiteSpace(item))
                throw new FeedException("'names' entries must be non-empty strings");
            string trimmed = item.Trim();
            string key = FilterIcs.Names.Normalize(trimmed);
            if (seen.Add(key))
                ordered.Add(trimmed);
        }
        return new Allowlist(ordered, seen);
    }
}

/// <summary>Parses one allowlist file object: { "fileName": ..., "names": [...] }.</summary>
public static class FeedFile
{
    public static (string fileName, IReadOnlyList<string> names) ParseObject(string raw)
    {
        JsonElement root;
        try
        {
            using JsonDocument doc = JsonDocument.Parse(raw);
            root = doc.RootElement.Clone();
        }
        catch (JsonException ex)
        {
            throw new FeedException($"not valid JSON: {ex.Message}", ex);
        }

        if (root.ValueKind != JsonValueKind.Object)
            throw new FeedException("allowlist file must be a JSON object");

        if (!root.TryGetProperty("fileName", out JsonElement fn)
            || fn.ValueKind != JsonValueKind.String
            || string.IsNullOrWhiteSpace(fn.GetString()))
        {
            throw new FeedException("missing or empty 'fileName'");
        }

        if (!root.TryGetProperty("names", out JsonElement namesEl)
            || namesEl.ValueKind != JsonValueKind.Array)
        {
            throw new FeedException("'names' must be an array");
        }

        var names = new List<string>();
        foreach (JsonElement item in namesEl.EnumerateArray())
            names.Add(item.ValueKind == JsonValueKind.String ? item.GetString() ?? "" : "");

        return (fn.GetString()!, names);
    }

    public static (string fileName, IReadOnlyList<string> names) Load(string path)
    {
        string raw;
        try
        {
            raw = File.ReadAllText(path);
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException or IOException)
        {
            throw new FeedException($"cannot read {path}: {ex.Message}", ex);
        }
        return ParseObject(raw);
    }
}
