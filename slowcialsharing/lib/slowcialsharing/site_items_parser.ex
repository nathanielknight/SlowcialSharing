defmodule Slowcialsharing.SiteItemsParser do
  alias Slowcialsharing.{Item, RssFeedParser}

  def parse_items(site, rss_feed) do
    {:ok, items} = RssFeedParser.parse_feed(rss_feed)

    site_items =
      for rss_item <- items do
        item = Map.put(rss_item, :site_id, site.id)
        Item.changeset(%Item{}, item)
      end

    case Enum.all?(site_items, fn item -> item.valid? end) do
      true -> {:ok, site_items}
      false -> {:err, site_items}
    end
  end
end
