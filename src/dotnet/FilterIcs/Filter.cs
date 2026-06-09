using Ical.Net;
using Ical.Net.CalendarComponents;

namespace FilterIcs;

/// <summary>Raised when the source body is not a valid iCalendar document (exit 3).</summary>
public sealed class ParseException : Exception
{
    public ParseException(string message, Exception? inner = null) : base(message, inner) { }
}

public sealed class FilterResult
{
    public required Calendar Calendar { get; init; }
    public int Read { get; init; }
    public int Kept { get; init; }
    public int Dropped { get; init; }

    /// <summary>Allowlist names (original spelling) that matched zero events — likely typos.</summary>
    public IReadOnlyList<string> UnmatchedNames { get; init; } = Array.Empty<string>();
}

public static class Filter
{
    /// <summary>
    /// Parse <paramref name="ical"/> and keep only allowlisted people's events. Calendar-level
    /// properties and non-event components are preserved unchanged.
    /// </summary>
    public static FilterResult FilterCalendar(string ical, Allowlist allowlist)
    {
        Calendar? calendar;
        try
        {
            calendar = Calendar.Load(ical);
        }
        catch (Exception ex)
        {
            throw new ParseException($"source is not valid iCalendar: {ex.Message}", ex);
        }

        if (calendar is null)
            throw new ParseException("source is not valid iCalendar: no VCALENDAR found");

        int read = calendar.Events.Count;
        var matched = new HashSet<string>();
        var drop = new List<CalendarEvent>();

        foreach (CalendarEvent ev in calendar.Events)
        {
            string name = Names.Extract(ev.Summary ?? string.Empty);
            if (allowlist.Contains(name))
                matched.Add(Names.Normalize(name));
            else
                drop.Add(ev);
        }

        foreach (CalendarEvent ev in drop)
            calendar.Events.Remove(ev);

        var unmatched = allowlist.Names
            .Where(n => !matched.Contains(Names.Normalize(n)))
            .ToList();

        return new FilterResult
        {
            Calendar = calendar,
            Read = read,
            Kept = read - drop.Count,
            Dropped = drop.Count,
            UnmatchedNames = unmatched,
        };
    }
}
