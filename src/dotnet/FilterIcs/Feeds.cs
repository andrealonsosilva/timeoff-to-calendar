namespace FilterIcs;

public sealed record FeedDefinition(string Basename, string OutputName, string OutputFile, Allowlist Allowlist);

public sealed class RunResult
{
    public int Total { get; init; }
    public List<string> Written { get; } = new();
    public List<(string basename, string reason)> Skipped { get; } = new();
    public List<string> Removed { get; } = new();
    public long SourceBytes { get; init; }
    public Dictionary<string, IReadOnlyList<string>> Unmatched { get; } = new();
}

/// <summary>Multi-feed orchestration: fetch once, filter per file, reconcile outputs.</summary>
public static class Feeds
{
    public static (List<FeedDefinition> feeds, List<(string, string)> errors, List<string> discovered)
        LoadFeeds(string allowlistsDir)
    {
        var feeds = new List<FeedDefinition>();
        var errors = new List<(string, string)>();
        var discovered = new List<string>();

        foreach (string path in Directory.GetFiles(allowlistsDir, "*.json").OrderBy(p => p, StringComparer.Ordinal))
        {
            string basename = Path.GetFileName(path);
            discovered.Add(basename);
            try
            {
                var (fileName, names) = FeedFile.Load(path);
                string outputName = Naming.SanitizeOutputName(fileName);
                Allowlist allowlist = Allowlist.FromNames(names);
                feeds.Add(new FeedDefinition(basename, outputName, Naming.OutputFileName(outputName), allowlist));
            }
            catch (FeedException ex)
            {
                errors.Add((basename, ex.Message));
            }
        }
        return (feeds, errors, discovered);
    }

    public static RunResult Run(string sourceUrl, string allowlistsDir, string outputDir, Func<string, string>? fetcher = null)
    {
        fetcher ??= url => Fetch.FetchCalendarAsync(url).GetAwaiter().GetResult();

        var (feeds, errors, discovered) = LoadFeeds(allowlistsDir);

        // Duplicate output names → every colliding file is an error (FR-007).
        var byOutput = feeds.GroupBy(f => f.OutputFile);
        var valid = new List<FeedDefinition>();
        foreach (var group in byOutput)
        {
            var members = group.ToList();
            if (members.Count > 1)
                foreach (var f in members)
                    errors.Add((f.Basename, $"duplicate output name '{group.Key}'"));
            else
                valid.Add(members[0]);
        }

        // Fetch ONCE; parse-check the source once (throws ParseException on invalid source).
        string raw = fetcher(sourceUrl);
        Filter.FilterCalendar(raw, Allowlist.FromNames(Array.Empty<string>()));

        Directory.CreateDirectory(outputDir);
        string manifestPath = Path.Combine(outputDir, ".feeds.json");
        var old = Manifest.Read(manifestPath);
        var updated = new Dictionary<string, string>();
        var result = new RunResult
        {
            Total = discovered.Count,
            SourceBytes = System.Text.Encoding.UTF8.GetByteCount(raw),
        };

        foreach (var feed in valid)
        {
            FilterResult filtered = Filter.FilterCalendar(raw, feed.Allowlist);
            Render.AtomicWrite(Path.Combine(outputDir, feed.OutputFile), Render.Serialize(filtered.Calendar));
            result.Written.Add(feed.OutputName);
            updated[feed.Basename] = feed.OutputName;
            if (filtered.UnmatchedNames.Count > 0)
                result.Unmatched[feed.OutputName] = filtered.UnmatchedNames;
        }

        // Errored files: record; carry over last-good (keep manifest entry, leave file untouched).
        foreach (var (basename, reason) in errors)
        {
            result.Skipped.Add((basename, reason));
            if (old.ContainsKey(basename) && !updated.ContainsKey(basename))
                updated[basename] = old[basename];
        }

        // Reconcile removals/renames: any previously published output not produced this run is deleted.
        var produced = new HashSet<string>(updated.Values);
        foreach (string oldName in old.Values.Distinct().OrderBy(n => n, StringComparer.Ordinal))
        {
            if (!produced.Contains(oldName))
            {
                string target = Path.Combine(outputDir, Naming.OutputFileName(oldName));
                if (File.Exists(target))
                    File.Delete(target);
                result.Removed.Add(oldName);
            }
        }

        Manifest.Write(manifestPath, updated);
        return result;
    }
}
