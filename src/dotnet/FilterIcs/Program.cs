namespace FilterIcs;

public static class Program
{
    public const int ExitOk = 0;
    public const int ExitFetch = 1;
    public const int ExitConfig = 2;

    public static int Main(string[] args) => Run(args);

    /// <summary>Testable entry point. See contracts/cli.md.</summary>
    public static int Run(string[] args)
    {
        Options options;
        try
        {
            options = Options.Parse(args);
        }
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine($"error: config: {ex.Message}");
            return ExitConfig;
        }

        string? sourceUrl = options.SourceUrl ?? Environment.GetEnvironmentVariable("SOURCE_ICS_URL");
        if (string.IsNullOrWhiteSpace(sourceUrl))
        {
            Console.Error.WriteLine("error: config: missing source URL (--source-url or SOURCE_ICS_URL)");
            return ExitConfig;
        }

        if (!Directory.Exists(options.AllowlistsDir))
        {
            Console.Error.WriteLine($"error: config: allowlists dir not found: {options.AllowlistsDir}");
            return ExitConfig;
        }

        RunResult result;
        try
        {
            result = Feeds.Run(sourceUrl, options.AllowlistsDir, options.OutputDir);
        }
        catch (FetchException ex)
        {
            Console.Error.WriteLine($"error: fetch: {ex.Message} ({Redact(sourceUrl)})");
            return ExitFetch;
        }
        catch (ParseException ex)
        {
            Console.Error.WriteLine($"error: source parse: {ex.Message}");
            return ExitFetch;
        }

        foreach (var (basename, reason) in result.Skipped)
            Console.Error.WriteLine($"error: feed {basename}: {reason}");
        foreach (var (outputName, names) in result.Unmatched)
            Console.WriteLine($"warn: {outputName}: allowlist names with no matching events: [{string.Join(", ", names)}]");

        Console.WriteLine(
            $"summary: feeds total={result.Total} written={result.Written.Count} "
            + $"skipped={result.Skipped.Count} removed={result.Removed.Count}, source {result.SourceBytes}B");
        if (options.Verbose)
            Console.WriteLine($"verbose: source={Redact(sourceUrl)} output_dir={options.OutputDir} "
                + $"written=[{string.Join(", ", result.Written)}] removed=[{string.Join(", ", result.Removed)}]");

        return ExitOk;
    }

    /// <summary>Return scheme+host only, so the feed token is never logged.</summary>
    public static string Redact(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) ? $"{uri.Scheme}://{uri.Host}" : "(url)";

    private sealed record Options(string? SourceUrl, string AllowlistsDir, string OutputDir, bool Verbose)
    {
        public static Options Parse(string[] args)
        {
            string? sourceUrl = null;
            string allowlistsDir = "allowlists";
            string outputDir = "public";
            bool verbose = false;

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--source-url": sourceUrl = RequireValue(args, ref i); break;
                    case "--allowlists-dir": allowlistsDir = RequireValue(args, ref i); break;
                    case "--output-dir": outputDir = RequireValue(args, ref i); break;
                    case "--verbose": verbose = true; break;
                    default: throw new ArgumentException($"unknown argument: {args[i]}");
                }
            }
            return new Options(sourceUrl, allowlistsDir, outputDir, verbose);
        }

        private static string RequireValue(string[] args, ref int i)
        {
            if (i + 1 >= args.Length)
                throw new ArgumentException($"missing value for {args[i]}");
            return args[++i];
        }
    }
}
