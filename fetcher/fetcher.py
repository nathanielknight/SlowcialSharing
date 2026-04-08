"""Fetcher script for SlowcialSharing - fetches RSS feeds and scores."""

import os
import re
import sqlite3
import sys
import xml.etree.ElementTree as ET
from datetime import datetime, timezone, timedelta
from html import unescape

import httpx
from bs4 import BeautifulSoup

USER_AGENT = "SlowcialSharing Scraper"
HEADERS = {"User-Agent": USER_AGENT}


def get_db(path: str | None = None) -> sqlite3.Connection:
    db_path = path or os.environ.get("DATABASE_PATH", "slowcial.db")
    conn = sqlite3.connect(db_path)
    conn.row_factory = sqlite3.Row
    return conn


def init_db(conn: sqlite3.Connection) -> None:
    schema_path = os.path.join(os.path.dirname(__file__), "..", "schema.sql")
    with open(schema_path) as f:
        conn.executescript(f.read())


def parse_rss(xml_text: str) -> list[dict]:
    """Parse RSS XML and return list of item dicts."""
    root = ET.fromstring(xml_text)
    items = []
    for item_el in root.iter("item"):
        title_el = item_el.find("title")
        link_el = item_el.find("link")
        comments_el = item_el.find("comments")
        pubdate_el = item_el.find("pubDate")

        if title_el is None or link_el is None or pubdate_el is None:
            continue

        title = unescape(title_el.text or "")
        link = unescape(link_el.text or "")
        comments_link = unescape(comments_el.text or "") if comments_el is not None else ""
        pub_date = parse_rss_date(pubdate_el.text or "")

        items.append({
            "title": title,
            "link": link,
            "comments_link": comments_link,
            "pub_date": pub_date,
        })
    return items


def parse_rss_date(date_str: str) -> str:
    """Parse RSS date to UTC string format YYYY-MM-DD HH:MM:SS."""
    from email.utils import parsedate_to_datetime
    try:
        dt = parsedate_to_datetime(date_str).astimezone(timezone.utc)
        return dt.strftime("%Y-%m-%d %H:%M:%S")
    except Exception:
        return datetime.now(timezone.utc).strftime("%Y-%m-%d %H:%M:%S")


def fetch_new_items(conn: sqlite3.Connection, client: httpx.Client) -> None:
    """Fetch RSS feeds for all sites and insert new items."""
    sites = conn.execute("SELECT * FROM sites").fetchall()
    for site in sites:
        if not site["rss_url"]:
            continue
        try:
            resp = client.get(site["rss_url"], headers=HEADERS)
            resp.raise_for_status()
        except Exception as e:
            print(f"Error fetching RSS for {site['name']}: {e}", file=sys.stderr)
            continue

        items = parse_rss(resp.text)
        for item in items:
            key = f"{site['name']}:{item['comments_link'] or item['link']}"
            try:
                conn.execute(
                    "INSERT OR IGNORE INTO items (key, title, link, pub_date, comments_link, site_id) "
                    "VALUES (?, ?, ?, ?, ?, ?)",
                    (key, item["title"], item["link"], item["pub_date"],
                     item["comments_link"], site["site_id"]),
                )
            except Exception as e:
                print(f"Error inserting item {key}: {e}", file=sys.stderr)
        conn.commit()
        print(f"Fetched {len(items)} items from {site['name']}")


def fetch_hn_details(url: str, client: httpx.Client) -> tuple[int, int]:
    """Fetch score and comment count from a Hacker News item page."""
    resp = client.get(url, headers=HEADERS)
    resp.raise_for_status()
    soup = BeautifulSoup(resp.text, "lxml")

    score = 0
    score_node = soup.select_one("span.score")
    if score_node:
        match = re.match(r"(\d+)", score_node.get_text())
        if match:
            score = int(match.group(1))

    comments = len(soup.find_all("div", class_=re.compile(r"\bcomment\b", re.I)))
    return score, comments


def fetch_lobsters_details(url: str, client: httpx.Client) -> tuple[int, int]:
    """Fetch score and comment count from a Lobsters item page."""
    resp = client.get(url, headers=HEADERS)
    resp.raise_for_status()
    soup = BeautifulSoup(resp.text, "lxml")

    score = 0
    score_node = soup.select_one("div.story_liner div.voters a.upvoter")
    if score_node:
        try:
            score = int(score_node.get_text().strip())
        except ValueError:
            pass

    comment_divs = soup.find_all("div", class_=re.compile(r"\bcomment\b", re.I))
    comments = max(0, len(comment_divs) - 1)  # subtract comment box
    return score, comments


def fetch_item_details(conn: sqlite3.Connection, client: httpx.Client) -> None:
    """Fetch scores for items older than 24 hours that don't have scores yet."""
    cutoff = (datetime.now(timezone.utc) - timedelta(days=1)).strftime("%Y-%m-%d %H:%M:%S")
    rows = conn.execute(
        "SELECT i.*, s.name as site_name FROM items i "
        "JOIN sites s ON i.site_id = s.site_id "
        "WHERE i.score IS NULL AND i.pub_date < ?",
        (cutoff,),
    ).fetchall()

    print(f"Fetching details for {len(rows)} items")
    for row in rows:
        try:
            if row["site_name"] == "news.ycombinator.com":
                score, comments = fetch_hn_details(row["comments_link"], client)
            elif row["site_name"] == "lobste.rs":
                score, comments = fetch_lobsters_details(row["comments_link"], client)
            else:
                print(f"Unknown site: {row['site_name']}", file=sys.stderr)
                continue

            conn.execute(
                "UPDATE items SET score = ?, comments = ? WHERE key = ?",
                (score, comments, row["key"]),
            )
            conn.commit()
        except Exception as e:
            print(f"Error fetching details for {row['key']}: {e}", file=sys.stderr)


def main() -> None:
    conn = get_db()
    init_db(conn)
    with httpx.Client(timeout=30) as client:
        fetch_new_items(conn, client)
        fetch_item_details(conn, client)
    conn.close()
    print("Done.")


if __name__ == "__main__":
    main()
