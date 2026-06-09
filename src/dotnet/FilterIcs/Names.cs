namespace FilterIcs;

/// <summary>Derives the person name from an event SUMMARY.</summary>
public static class Names
{
    /// <summary>
    /// The name is the SUMMARY up to the first " (" (space + open paren), trimmed.
    /// If there is no " (", the whole trimmed SUMMARY is the name.
    /// </summary>
    public static string Extract(string summary)
    {
        int idx = summary.IndexOf(" (", StringComparison.Ordinal);
        string name = idx == -1 ? summary : summary[..idx];
        return name.Trim();
    }

    /// <summary>Normalized form used for matching: trimmed + lower-cased (invariant).</summary>
    public static string Normalize(string name) => name.Trim().ToLowerInvariant();
}
