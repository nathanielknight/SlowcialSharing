defmodule Slowcialsharing.SiteItemsParser do
  alias Slowcialsharing.{Site, Item, RssFeedParser}

  def parse_items(site, rss_feed) do
    {:ok, items} = RssFeedParser.parse_feed(rss_feed)
    
  end
end
