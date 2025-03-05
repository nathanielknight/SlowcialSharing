defmodule Slowcialsharing.RssItemParserTest do
  alias Slowcialsharing.RssFeedParser
  use ExUnit.Case

  @rss_feed """
  <?xml version="1.0" encoding="UTF-8"?>
  <rss version="2.0" xmlns:atom="http://www.w3.org/2005/Atom">
    <title>lobste.rs</title>
    <title>Lobsters</title>
    <link>https://lobste.rs/</link>
    <atom:link href="https://lobste.rs/rss" rel="self"/>
    <description></description>
    <pubDate>Mon, 03 Mar 2025 16:14:12 -0600</pubDate>
    <ttl>120</ttl>
  <item>
    <title>Use Firefox in 2025</title>
    <link>https://alexn.org/blog/2025/03/03/use-firefox/</link>
    <guid>https://lobste.rs/s/np4prk</guid>
    <author>alexn.org by alexelcu</author>
    <pubDate>Mon, 03 Mar 2025 16:14:12 -0600</pubDate>
    <comments>https://lobste.rs/s/np4prk/use_firefox_2025</comments>
    <description>&lt;p&gt;&lt;a href="https://lobste.rs/s/np4prk/use_firefox_2025"&gt;Comments&lt;/a&gt;&lt;/p&gt;</description>
    <category>browsers</category>
    <category>android</category>
    <category>privacy</category>
    <category>web</category>
  </item>
  <item>
    <title>Apple's Software Quality Crisis: When Premium Hardware Meets Subpar Software</title>
    <link>https://www.eliseomartelli.it/blog/2025-03-02-apple-quality</link>
    <guid>https://lobste.rs/s/epok9v</guid>
    <author>eliseomartelli.it by eliseomartelli</author>
    <pubDate>Mon, 03 Mar 2025 02:10:07 -0600</pubDate>
    <comments>https://lobste.rs/s/epok9v/apple_s_software_quality_crisis_when</comments>
    <description>&lt;p&gt;&lt;a href="https://lobste.rs/s/epok9v/apple_s_software_quality_crisis_when"&gt;Comments&lt;/a&gt;&lt;/p&gt;</description>
    <category>ios</category>
    <category>rant</category>
  </item>
  </channel>
  </rss>
  """

  def rss_feed() do
    @rss_feed
  end

  test "reports nonsense input" do
    {:error, _} = RssFeedParser.parse_feed("")
  end

  test "parses rss feeds" do
    assert {:ok, items} = RssFeedParser.parse_feed(@rss_feed)

    assert items == [
             %{
               commentslink: "https://lobste.rs/s/np4prk/use_firefox_2025",
               key: "https://lobste.rs/s/np4prk",
               link: "https://alexn.org/blog/2025/03/03/use-firefox/",
               title: "Use Firefox in 2025",
               pubdate: ~U[2025-03-03 22:14:12Z]
             },
             %{
               commentslink: "https://lobste.rs/s/epok9v/apple_s_software_quality_crisis_when",
               key: "https://lobste.rs/s/epok9v",
               link: "https://www.eliseomartelli.it/blog/2025-03-02-apple-quality",
               title:
                 "Apple's Software Quality Crisis: When Premium Hardware Meets Subpar Software",
               pubdate: ~U[2025-03-03 08:10:07Z]
             }
           ]
  end
end
