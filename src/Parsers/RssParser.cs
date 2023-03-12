using System.Xml.Linq;
using SlowcialSharing.Data;

namespace SlowcialSharing.Parsers;

internal class RssParser
{
    private Stream _stream;

    internal RssParser(Stream stream) => _stream = stream;

    internal IEnumerable<Item> Items(Site site)
    {
        var element = XElement.Load(_stream);
        var itemElements = element.Descendants("item");
        return itemElements.Select(e =>
            new Item
            {
                SiteId = site.SiteId,
                Site = site,
                Title = (string)e.Element("title"),
                Link = (string)e.Element("link"),
                guid = (string)e.Element("guid"),
                PubDate = DateTimeOffset.Parse((string)e.Element("pubDate")).ToUniversalTime(),
                Author = (string)e.Element("author"),
                Score = null,
            }
        );
    }
}