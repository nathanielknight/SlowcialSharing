using HtmlAgilityPack;
using SlowcialSharing.Data;
using SlowcialSharing.Parsers;

namespace SlowcialSharing.Clients;

internal class LobstersClient : IScraper
{
    private ApplicationDbContext _context;
    private Site _site;
    LobstersClient(ApplicationDbContext context)
    {
        _site = context.Sites.Where(s => s.Name == "lobste.rs").First();
        _context = context;
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
        var doc = web.Load(item.guid);
        var scoreNode = doc.DocumentNode.SelectSingleNode("//div[@class='score']");
        return (int.Parse(scoreNode.InnerText), 0);

    }



}

