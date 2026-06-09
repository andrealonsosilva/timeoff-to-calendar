using System.Text.Json;

namespace FilterIcs;

/// <summary>Raised when names.json is missing, not JSON, or schema-invalid (exit 4).</summary>
public sealed class AllowlistException : Exception
{
    public AllowlistException(string message, Exception? inner = null) : base(message, inner) { }
}

/// <summary>The people to keep, loaded from a flat JSON array of name strings.</summary>
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

    public static Allowlist Load(string path)
    {
        string text;
        try
        {
            text = File.ReadAllText(path);
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            throw new AllowlistException($"allowlist file not found: {path}", ex);
        }
        catch (IOException ex)
        {
            throw new AllowlistException($"cannot read allowlist file {path}: {ex.Message}", ex);
        }

        JsonElement root;
        try
        {
            using JsonDocument doc = JsonDocument.Parse(text);
            root = doc.RootElement.Clone();
        }
        catch (JsonException ex)
        {
            throw new AllowlistException($"allowlist is not valid JSON: {ex.Message}", ex);
        }

        if (root.ValueKind != JsonValueKind.Array)
            throw new AllowlistException("allowlist must be a JSON array of name strings");

        var names = new List<string>();
        var seen = new HashSet<string>();
        foreach (JsonElement item in root.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String)
                throw new AllowlistException("allowlist entries must be non-empty strings");
            string value = item.GetString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(value))
                throw new AllowlistException("allowlist entries must be non-empty strings");

            string trimmed = value.Trim();
            string key = FilterIcs.Names.Normalize(trimmed);
            if (seen.Add(key))
                names.Add(trimmed);
        }

        return new Allowlist(names, seen);
    }
}
