using System.Xml.Linq;
using SlowcialSharing.Data;

namespace SlowcialSharing.Parsers;

internal record RssItem(
    string Title,
    string Link,
    string CommentsLink,
    DateTimeOffset PubDate
);

internal class RssParser
{
    private Stream _stream;

    internal RssParser(Stream stream) => _stream = stream;

    internal IEnumerable<RssItem> Items(Site site)
    {
        var element = XElement.Load(_stream);
        var itemElements = element.Descendants("item");
        return itemElements.Select(e =>
            new RssItem(
                Title: (string)e.Element("title"),
                Link: (string)e.Element("link"),
                CommentsLink: (string)e.Element("comments"),
                PubDate: DateTimeOffset.Parse((string)e.Element("pubDate")).ToUniversalTime()
            )
        );
    }
}