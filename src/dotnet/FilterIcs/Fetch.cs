namespace FilterIcs;

/// <summary>Raised on network error, timeout, or non-2xx response (exit 1).</summary>
public sealed class FetchException : Exception
{
    public FetchException(string message, Exception? inner = null) : base(message, inner) { }
}

public static class Fetch
{
    /// <summary>Fetch the source feed body as text. Throws <see cref="FetchException"/> on failure.</summary>
    public static async Task<string> FetchCalendarAsync(string url, TimeSpan? timeout = null)
    {
        using var client = new HttpClient { Timeout = timeout ?? TimeSpan.FromSeconds(30) };
        HttpResponseMessage response;
        try
        {
            response = await client.GetAsync(url);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new FetchException($"could not fetch source feed: {ex.Message}", ex);
        }

        if ((int)response.StatusCode / 100 != 2)
            throw new FetchException($"source feed returned HTTP {(int)response.StatusCode}");

        return await response.Content.ReadAsStringAsync();
    }
}
