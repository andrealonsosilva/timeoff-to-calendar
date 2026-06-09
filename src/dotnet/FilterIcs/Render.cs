using Ical.Net;
using Ical.Net.Serialization;

namespace FilterIcs;

public static class Render
{
    public static string Serialize(Calendar calendar) =>
        new CalendarSerializer().SerializeToString(calendar) ?? string.Empty;

    /// <summary>
    /// Write <paramref name="content"/> to <paramref name="path"/> atomically (temp file + move),
    /// so a failure never leaves a partial/empty published feed (FR-011/FR-012).
    /// </summary>
    public static void AtomicWrite(string path, string content)
    {
        string fullPath = Path.GetFullPath(path);
        string directory = Path.GetDirectoryName(fullPath) ?? ".";
        Directory.CreateDirectory(directory);

        string tmp = Path.Combine(directory, Path.GetRandomFileName() + ".tmp");
        try
        {
            File.WriteAllText(tmp, content);
            File.Move(tmp, fullPath, overwrite: true);
        }
        catch
        {
            if (File.Exists(tmp))
                File.Delete(tmp);
            throw;
        }
    }
}
