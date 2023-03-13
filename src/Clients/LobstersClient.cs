using HtmlAgilityPack;
using SlowcialSharing.Data;
using SlowcialSharing.Parsers;

namespace SlowcialSharing.Clients;

internal class LobstersClient : IScraper
{
    private Site _site;
    private readonly ILogger _logger;
    public LobstersClient(ApplicationDbContext context, ILogger logger)
    {
        _site = context.Sites.Where(s => s.Name == "lobste.rs").First();
        _logger = logger;
    }

    public async Task<IEnumerable<Item>> FetchNewItems()
    {
        Stream rssBody = await RawClient.Get(_site.Url);
        var parser = new RssParser(rssBody);
        return parser.Items(_site);
    }

    public async Task<(int Score, int Comments)> FetchItemDetails(Item item)
    {
        var web = new HtmlWeb();
        var doc = await Task.Run(() => web.Load(item.guid));
        var scoreNode = doc.DocumentNode.SelectSingleNode("//div[@class='score']");
        int score, comments = 0;
        if (scoreNode is null)
        {
            _logger.LogWarning($"Missing scorenode for item: ${item.guid}");
            score = 0;
        }
        else
        {
            score = int.Parse(scoreNode.InnerText);
        }
        return (score, comments);
    }
}

