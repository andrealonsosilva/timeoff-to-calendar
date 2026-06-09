using FilterIcs;
using Xunit;

namespace FilterIcs.Tests;

public class FilterTests
{
    private static HashSet<string> Uids(FilterResult result) =>
        result.Calendar.Events.Select(e => e.Uid!).ToHashSet();

    [Fact]
    public void Keeps_only_allowlisted_people()
    {
        FilterResult result = Filter.FilterCalendar(
            Fixture.SourceText, Fixture.AllowOf("Pedro Fernandes", "Thiago Bessa"));

        Assert.Equal(4, result.Read);
        Assert.Equal(2, result.Kept);
        Assert.Equal(2, result.Dropped);
        Assert.Equal(new HashSet<string> { "evt-pedro-1", "evt-thiago-1" }, Uids(result));
    }

    [Fact]
    public void Matching_is_case_and_whitespace_insensitive()
    {
        FilterResult result = Filter.FilterCalendar(
            Fixture.SourceText, Fixture.AllowOf("  pedro fernandes  "));
        Assert.Equal(new HashSet<string> { "evt-pedro-1" }, Uids(result));
    }

    [Fact]
    public void Empty_allowlist_yields_no_events()
    {
        FilterResult result = Filter.FilterCalendar(Fixture.SourceText, Fixture.AllowOf());
        Assert.Equal(0, result.Kept);
        Assert.Empty(result.Calendar.Events);
    }

    [Fact]
    public void Calendar_name_is_preserved()
    {
        FilterResult result = Filter.FilterCalendar(
            Fixture.SourceText, Fixture.AllowOf("Pedro Fernandes"));
        string serialized = Render.Serialize(result.Calendar);
        Assert.Contains("Quem está fora", serialized);
    }

    [Fact]
    public void Kept_event_preserves_summary_and_dates()
    {
        FilterResult result = Filter.FilterCalendar(
            Fixture.SourceText, Fixture.AllowOf("Pedro Fernandes"));
        var ev = Assert.Single(result.Calendar.Events);
        Assert.Equal("Pedro Fernandes (Folga - 11 dias)", ev.Summary);
        Assert.Equal(2026, ev.Start!.Year);
        Assert.Equal(5, ev.Start!.Month);
        Assert.Equal(18, ev.Start!.Day);
    }

    [Fact]
    public void Unmatched_names_are_reported()
    {
        FilterResult result = Filter.FilterCalendar(
            Fixture.SourceText, Fixture.AllowOf("Pedro Fernandes", "Nobody Here"));
        Assert.Equal(new[] { "Nobody Here" }, result.UnmatchedNames);
    }
}
