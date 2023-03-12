using System.Net.Http.Headers;
using SlowcialSharing.Data;

namespace SlowcialSharing.Clients;


internal interface IScraper
{

    public Task<IEnumerable<Item>> FetchNewItems();
    public Task<(int Score, int Comments)> FetchItemDetails(Item item);
}

class RawClient
{
    private static HttpClient _client = ConfiguredHttpClient();

    private static HttpClient ConfiguredHttpClient()
    {
        HttpClient client = new HttpClient();
        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("*/*"));
        client.DefaultRequestHeaders.Add("User-Agent", "SlowcialSharing Scraper");
        return client;
    }

    internal static async Task<Stream> Get(string uri) =>
        await _client.GetStreamAsync(uri);
}
