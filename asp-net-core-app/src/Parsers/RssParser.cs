using System.Web;
using System.Xml.Linq;
using HtmlAgilityPack;
using SlowcialSharing.Data;

namespace SlowcialSharing.Parsers;

internal record RssItem(
    string? Title,
    string? Link,
    string? CommentsLink,
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

        string? DecodedAttribute(XElement e, string name) =>
            HttpUtility.HtmlDecode((string)e.Element(name));

        DateTimeOffset DateTime(XElement e) =>
            DateTimeOffset.Parse((string)e.Element("pubDate")).ToUniversalTime();

        return itemElements.Select(e =>
            new RssItem(
                Title: DecodedAttribute(e, "title"),
                Link: DecodedAttribute(e, "link"),
                CommentsLink: DecodedAttribute(e, "comments"),
                PubDate: DateTime(e)
            )
        );
    }
}