"""Tests for the fetcher module."""

import sqlite3
import os
from datetime import datetime, timezone, timedelta

import httpx
import pytest
import respx
from hypothesis import given, strategies as st

# Add parent to path
import sys
sys.path.insert(0, os.path.dirname(os.path.dirname(__file__)))

from fetcher import (
    parse_rss,
    parse_rss_date,
    fetch_new_items,
    fetch_hn_details,
    fetch_lobsters_details,
    fetch_item_details,
)

SCHEMA_PATH = os.path.join(os.path.dirname(__file__), "..", "..", "schema.sql")


def make_db() -> sqlite3.Connection:
    conn = sqlite3.connect(":memory:")
    conn.row_factory = sqlite3.Row
    with open(SCHEMA_PATH) as f:
        conn.executescript(f.read())
    return conn


SAMPLE_RSS = """<?xml version="1.0" encoding="UTF-8"?>
<rss version="2.0">
<channel>
<item>
  <title>Test Article &amp; More</title>
  <link>https://example.com/article</link>
  <comments>https://news.ycombinator.com/item?id=123</comments>
  <pubDate>Mon, 01 Jan 2024 12:00:00 +0000</pubDate>
</item>
<item>
  <title>Second Article</title>
  <link>https://example.com/second</link>
  <comments>https://news.ycombinator.com/item?id=456</comments>
  <pubDate>Tue, 02 Jan 2024 12:00:00 +0000</pubDate>
</item>
</channel>
</rss>"""


class TestParseRss:
    def test_parses_items(self):
        items = parse_rss(SAMPLE_RSS)
        assert len(items) == 2

    def test_decodes_html_entities(self):
        items = parse_rss(SAMPLE_RSS)
        assert items[0]["title"] == "Test Article & More"

    def test_extracts_fields(self):
        items = parse_rss(SAMPLE_RSS)
        assert items[0]["link"] == "https://example.com/article"
        assert items[0]["comments_link"] == "https://news.ycombinator.com/item?id=123"

    def test_parses_date(self):
        items = parse_rss(SAMPLE_RSS)
        assert items[0]["pub_date"] == "2024-01-01 12:00:00"

    def test_empty_rss(self):
        xml = '<?xml version="1.0"?><rss><channel></channel></rss>'
        assert parse_rss(xml) == []


class TestParseRssDate:
    def test_standard_date(self):
        result = parse_rss_date("Mon, 01 Jan 2024 12:00:00 +0000")
        assert result == "2024-01-01 12:00:00"

    def test_timezone_conversion(self):
        result = parse_rss_date("Mon, 01 Jan 2024 12:00:00 -0500")
        assert result == "2024-01-01 17:00:00"

    def test_invalid_date_returns_now(self):
        result = parse_rss_date("not a date")
        # Should return something that looks like a date
        assert len(result) == 19


@given(st.text(min_size=1, max_size=100))
def test_parse_rss_date_never_crashes(date_str):
    """Property: parse_rss_date never raises an exception."""
    result = parse_rss_date(date_str)
    assert isinstance(result, str)
    assert len(result) == 19


class TestFetchNewItems:
    @respx.mock
    def test_fetches_and_inserts(self):
        conn = make_db()
        respx.get("https://news.ycombinator.com/rss").mock(
            return_value=httpx.Response(200, text=SAMPLE_RSS)
        )
        respx.get("https://lobste.rs/rss").mock(
            return_value=httpx.Response(200, text=SAMPLE_RSS)
        )
        with httpx.Client() as client:
            fetch_new_items(conn, client)

        count = conn.execute("SELECT COUNT(*) FROM items").fetchone()[0]
        assert count == 4  # 2 items × 2 sites

    @respx.mock
    def test_ignores_duplicates(self):
        conn = make_db()
        respx.get("https://news.ycombinator.com/rss").mock(
            return_value=httpx.Response(200, text=SAMPLE_RSS)
        )
        respx.get("https://lobste.rs/rss").mock(
            return_value=httpx.Response(200, text=SAMPLE_RSS)
        )
        with httpx.Client() as client:
            fetch_new_items(conn, client)
            fetch_new_items(conn, client)

        count = conn.execute("SELECT COUNT(*) FROM items").fetchone()[0]
        assert count == 4

    @respx.mock
    def test_handles_http_error(self):
        conn = make_db()
        respx.get("https://news.ycombinator.com/rss").mock(
            return_value=httpx.Response(500)
        )
        respx.get("https://lobste.rs/rss").mock(
            return_value=httpx.Response(200, text=SAMPLE_RSS)
        )
        with httpx.Client() as client:
            fetch_new_items(conn, client)

        count = conn.execute("SELECT COUNT(*) FROM items").fetchone()[0]
        assert count == 2  # only lobsters items


HN_PAGE = """
<html><body>
<span class="score">42 points</span>
<div class="comment">comment 1</div>
<div class="comment">comment 2</div>
<div class="other">not a comment</div>
</body></html>
"""

LOBSTERS_PAGE = """
<html><body>
<div class="story_liner">
  <div class="voters"><a class="upvoter">15</a></div>
</div>
<div class="comment">comment 1</div>
<div class="comment">comment 2</div>
<div class="comment">comment box</div>
</body></html>
"""


class TestFetchDetails:
    @respx.mock
    def test_hn_details(self):
        respx.get("https://news.ycombinator.com/item?id=123").mock(
            return_value=httpx.Response(200, text=HN_PAGE)
        )
        with httpx.Client() as client:
            score, comments = fetch_hn_details(
                "https://news.ycombinator.com/item?id=123", client
            )
        assert score == 42
        assert comments == 2

    @respx.mock
    def test_lobsters_details(self):
        respx.get("https://lobste.rs/s/test").mock(
            return_value=httpx.Response(200, text=LOBSTERS_PAGE)
        )
        with httpx.Client() as client:
            score, comments = fetch_lobsters_details(
                "https://lobste.rs/s/test", client
            )
        assert score == 15
        assert comments == 2  # 3 divs - 1 for comment box

    @respx.mock
    def test_fetch_item_details_updates_db(self):
        conn = make_db()
        old_date = (datetime.now(timezone.utc) - timedelta(days=2)).strftime(
            "%Y-%m-%d %H:%M:%S"
        )
        conn.execute(
            "INSERT INTO items (key, title, link, pub_date, comments_link, site_id) "
            "VALUES (?, ?, ?, ?, ?, ?)",
            ("hn-123", "Test", "https://example.com", old_date,
             "https://news.ycombinator.com/item?id=123", 1),
        )
        conn.commit()

        respx.get("https://news.ycombinator.com/item?id=123").mock(
            return_value=httpx.Response(200, text=HN_PAGE)
        )
        with httpx.Client() as client:
            fetch_item_details(conn, client)

        row = conn.execute("SELECT score, comments FROM items WHERE key = 'hn-123'").fetchone()
        assert row["score"] == 42
        assert row["comments"] == 2

    @respx.mock
    def test_skips_recent_items(self):
        conn = make_db()
        recent_date = datetime.now(timezone.utc).strftime("%Y-%m-%d %H:%M:%S")
        conn.execute(
            "INSERT INTO items (key, title, link, pub_date, comments_link, site_id) "
            "VALUES (?, ?, ?, ?, ?, ?)",
            ("hn-recent", "Recent", "https://example.com", recent_date,
             "https://news.ycombinator.com/item?id=999", 1),
        )
        conn.commit()

        with httpx.Client() as client:
            fetch_item_details(conn, client)

        row = conn.execute("SELECT score FROM items WHERE key = 'hn-recent'").fetchone()
        assert row["score"] is None


@given(st.integers(min_value=0, max_value=10000))
def test_score_parsing_consistency(score_val):
    """Property: HN score parsing extracts the number correctly from well-formed HTML."""
    html = f'<html><body><span class="score">{score_val} points</span></body></html>'
    from fetcher import fetch_hn_details
    with respx.mock:
        respx.get("http://test").mock(return_value=httpx.Response(200, text=html))
        with httpx.Client() as client:
            parsed_score, _ = fetch_hn_details("http://test", client)
    assert parsed_score == score_val
