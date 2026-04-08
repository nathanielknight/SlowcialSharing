using System.Text.RegularExpressions;
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
        return GetScoreAndComments(doc.DocumentNode);
    }

    public static (int Score, int Comments) GetScoreAndComments(HtmlNode documentNode)
    {
        var scoreNode = documentNode.SelectSingleNode("//div[contains(@class, 'story_liner')]//div[contains(@class, 'voters')]//a[contains(@class, 'upvoter')]");
        int score, comments = 0;
        if (scoreNode is null)
        {
            score = 0;
        }
        else
        {
            score = int.Parse(scoreNode.InnerText);
        }

        var commentNodes = documentNode.SelectNodes("//div")
            ?.Where(IsCommentNode);
        // Subtract 1 because the comment box also matches `div.comment`
        comments = Math.Max(0, (commentNodes?.Count() ?? 0) - 1);
        return (score, comments);
    }

    private static bool IsCommentNode(HtmlNode node)
    {
        string classes = node.Attributes["class"]?.Value ?? "";
        string pattern = @"\bcomment\b";
        RegexOptions options = RegexOptions.IgnoreCase;
        return Regex.IsMatch(classes, pattern, options);
    }
}

