defmodule Slowcialsharing.RssFeedParser do

  @doc "Parse the items from an RSS feed (as a string)."
  @spec parse_feed(String.t()) :: map()
  def parse_feed(feed) do
    case Floki.parse_document(feed) do
      {:ok, doc} ->
        get_items_from_rss(doc)

      {:error, reason} ->
        {:error, "RSS parsing failed: #{inspect(reason)}"}
    end
  end

  def get_items_from_rss(doc) do
    case Floki.find(doc, "rss") do
      [{"rss", _, _}] ->
        get_items(doc)

      _ ->
        {:error, "Not an rss feed"}
    end
  end

  def get_items(doc) do
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
