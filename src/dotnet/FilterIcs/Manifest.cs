using System.Text.Json;

namespace FilterIcs;

/// <summary>Feed manifest (public/.feeds.json): basename -> output name.</summary>
public static class Manifest
{
    public static Dictionary<string, string> Read(string path)
    {
        if (!File.Exists(path))
            return new Dictionary<string, string>();
        try
        {
            string text = File.ReadAllText(path);
            var map = JsonSerializer.Deserialize<Dictionary<string, string>>(text);
            return map ?? new Dictionary<string, string>();
        }
        catch (Exception ex) when (ex is JsonException or IOException)
        {
            return new Dictionary<string, string>();
        }
    }

    public static void Write(string path, IDictionary<string, string> map)
    {
        var sorted = new SortedDictionary<string, string>(new Dictionary<string, string>(map), StringComparer.Ordinal);
        string json = JsonSerializer.Serialize(sorted, new JsonSerializerOptions { WriteIndented = true }) + "\n";
        Render.AtomicWrite(path, json);
    }
}
