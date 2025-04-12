namespace NitroType.Tracker.Domain;

public sealed class DataRetriever
{
    private readonly IHttpClientFactory _httpClientFactory;

    public DataRetriever(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async ValueTask<string> RetrieveRawDataAsync(string teamName)
    {
        var uri = new Uri($"https://nitrotype.com/api/v2/teams/{teamName}");

        using var client = _httpClientFactory.CreateClient();
        using var response = await client.GetAsync(uri)
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync()
            .ConfigureAwait(false);

        return json;
    }
}
