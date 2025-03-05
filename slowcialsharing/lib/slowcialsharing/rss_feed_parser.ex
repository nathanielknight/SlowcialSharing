defmodule Slowcialsharing.RssFeedParser do
  alias Slowcialsharing.Item

  @doc "Parse the items from an RSS feed (as a string)."
  @spec parse_feed(String.t()) :: list(map())
  def parse_feed(feed) do
    case Floki.parse_document(feed) do
      {:ok, doc} ->
        get_items_from_rss(doc)

      {:error, reason} ->
        {:error, "RSS parsing failed: #{inspect(reason)}"}
    end
  end

  # TODO: type
  @doc "Create Items from a site's RSS feed."
  def parse_site_feed(site, rss_feed) do
    {:ok, items} = parse_feed(rss_feed)

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

  defp get_items_from_rss(doc) do
    case Floki.find(doc, "rss") do
      [{"rss", _, _}] ->
        get_items(doc)

      _ ->
        {:error, "Not an rss feed"}
    end
  end

  defp get_items(doc) do
    rss_items = Floki.find(doc, "item")
    items = for rss_item <- rss_items, do: parse_item(rss_item)
    {:ok, items}
  end

  defp parse_item({"item", _attrs, children}) do
    {"link", _, [link]} = find_element(children, "link")
    {"title", _, [title]} = find_element(children, "title")
    {"guid", _, [key]} = find_element(children, "guid")
    {"comments", _, [commentslink]} = find_element(children, "comments")
    {"pubdate", _, [pubdate_src]} = find_element(children, "pubdate")
    parsed = RFC822DateParser.parse_date(pubdate_src)

    {:ok, pubdate} = parsed
    %{link: link, title: title, key: key, commentslink: commentslink, pubdate: pubdate}
  end

  defp find_element(elements, name) do
    elements
    |> Enum.find(fn {n, _, _} -> n == name end)
  end
end
