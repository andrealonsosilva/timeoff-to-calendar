using System.Text;

namespace FilterIcs;

public static class Program
{
    public const int ExitOk = 0;
    public const int ExitFetch = 1;
    public const int ExitConfig = 2;
    public const int ExitParse = 3;
    public const int ExitAllowlist = 4;

    public static async Task<int> Main(string[] args) => await Run(args);

    /// <summary>Testable entry point. See contracts/cli.md.</summary>
    public static async Task<int> Run(string[] args)
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

        string? sourceUrl = options.SourceUrl
            ?? Environment.GetEnvironmentVariable("SOURCE_ICS_URL");
        if (string.IsNullOrWhiteSpace(sourceUrl))
        {
            Console.Error.WriteLine("error: config: missing source URL (--source-url or SOURCE_ICS_URL)");
            return ExitConfig;
        }

        // 1. Allowlist first so a bad list fails fast, output untouched.
        Allowlist allowlist;
        try
        {
            allowlist = Allowlist.Load(options.AllowlistPath);
        }
        catch (AllowlistException ex)
        {
            Console.Error.WriteLine($"error: allowlist: {ex.Message}");
            return ExitAllowlist;
        }

        // 2. Fetch.
        string raw;
        try
        {
            raw = await Fetch.FetchCalendarAsync(sourceUrl);
        }
        catch (FetchException ex)
        {
            Console.Error.WriteLine($"error: fetch: {ex.Message} ({Redact(sourceUrl)})");
            return ExitFetch;
        }

        // 3. Parse + filter.
        FilterResult result;
        try
        {
            result = Filter.FilterCalendar(raw, allowlist);
        }
        catch (ParseException ex)
        {
            Console.Error.WriteLine($"error: parse: {ex.Message}");
            return ExitParse;
        }

        // 4. Render + atomic write (only on full success).
        Render.AtomicWrite(options.OutputPath, Render.Serialize(result.Calendar));

        int bytes = Encoding.UTF8.GetByteCount(raw);
        Console.WriteLine(
            $"ok: fetched {bytes}B, read {result.Read} events, "
            + $"kept {result.Kept}, dropped {result.Dropped}, allowlist {allowlist.Names.Count} names");
        if (result.UnmatchedNames.Count > 0)
            Console.WriteLine(
                $"warn: allowlist names with no matching events: [{string.Join(", ", result.UnmatchedNames)}]");
        if (options.Verbose)
            Console.WriteLine($"verbose: source={Redact(sourceUrl)} output={options.OutputPath}");

        return ExitOk;
    }

    /// <summary>Return scheme+host only, so the feed token is never logged.</summary>
    public static string Redact(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out Uri? uri)
            ? $"{uri.Scheme}://{uri.Host}"
            : "(url)";
    }

    private sealed record Options(string? SourceUrl, string AllowlistPath, string OutputPath, bool Verbose)
    {
        public static Options Parse(string[] args)
        {
            string? sourceUrl = null;
            string allowlist = "names.json";
            string output = "whos-out.ics";
            bool verbose = false;

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--source-url":
                        sourceUrl = RequireValue(args, ref i);
                        break;
                    case "--allowlist":
                        allowlist = RequireValue(args, ref i);
                        break;
                    case "--output":
                        output = RequireValue(args, ref i);
                        break;
                    case "--verbose":
                        verbose = true;
                        break;
                    default:
                        throw new ArgumentException($"unknown argument: {args[i]}");
                }
            }

            return new Options(sourceUrl, allowlist, output, verbose);
        }

        private static string RequireValue(string[] args, ref int i)
        {
            if (i + 1 >= args.Length)
                throw new ArgumentException($"missing value for {args[i]}");
            return args[++i];
        }
    }
}
