using System.Text.RegularExpressions;
using HtmlAgilityPack;
using SlowcialSharing.Data;
using SlowcialSharing.Parsers;

namespace SlowcialSharing.Clients;

internal class HackerNewsClient : IScraper
{
    private Site _site;
    private readonly ILogger _logger;
    public HackerNewsClient(ApplicationDbContext context, ILogger logger)
    {
        _site = context.Sites.Where(s => s.Name == "news.ycombinator.com").First();
        _logger = logger;
    }

    public async Task<IEnumerable<Item>> FetchNewItems()
    {
        if (_site.RssUrl is null)
        {
            _logger.LogError($"No RssUrl for ${_site.Name}");
            return Enumerable.Empty<Item>();
        }
        Stream rssBody = await RawClient.Get(_site.RssUrl);
        var parser = new RssParser(rssBody);
        var rssItems = parser.Items(_site);
        return rssItems.Select(i =>
        new Item
        {
            Key = i.CommentsLink,
            Title = i.Title,
            Link = i.Link,
            CommentsLink = i.CommentsLink,
            PubDate = i.PubDate,
            SiteId = _site.SiteId,
            Site = _site,
        });
    }

    public async Task<(int Score, int Comments)> FetchItemDetails(Item item)
    {
        var web = new HtmlWeb();
        var doc = await Task.Run(() => web.Load(item.CommentsLink));
        var scoreNode = doc.DocumentNode.SelectSingleNode("//span[@class='score']");
        int score, comments = 0;
        if (scoreNode is null)
        {
            _logger.LogWarning($"Missing score node for item: ${item.CommentsLink}");
            score = 0;
        }
        else
        {
            score = int.Parse(scoreNode.InnerText.Split(" ")[0]);
        }
        var commentNodes = doc.DocumentNode.SelectNodes("//div")
            ?.Where(IsCommentNode);
        comments = commentNodes?.Count() ?? 0;
        return (score, comments);
    }

    private bool IsCommentNode(HtmlNode node)
    {
        string classes = node.Attributes["class"]?.Value ?? "";
        string pattern = @"\bcomment\b";
        RegexOptions options = RegexOptions.IgnoreCase;
        return Regex.IsMatch(classes, pattern, options);
    }
}

