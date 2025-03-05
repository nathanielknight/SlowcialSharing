defmodule Slowcialsharing.SiteItemsParserTest do
  use ExUnit.Case
  alias Slowcialsharing.SiteItemsParser

  test "it parses rss feeds and adds site info" do
    fake_site_id = 3

    {:ok, items} =
      SiteItemsParser.parse_items(
        %{id: fake_site_id},
        Slowcialsharing.RssItemParserTest.rss_feed()
      )

    [item | _] = items
    assert item.changes.site_id == fake_site_id
  end
end
