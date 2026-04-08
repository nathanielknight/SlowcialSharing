CREATE TABLE IF NOT EXISTS sites (
    site_id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL UNIQUE,
    homepage_url TEXT NOT NULL,
    rss_url TEXT,
    last_checked TEXT
);

CREATE TABLE IF NOT EXISTS items (
    key TEXT PRIMARY KEY,
    title TEXT NOT NULL,
    link TEXT NOT NULL,
    pub_date TEXT NOT NULL,
    comments_link TEXT NOT NULL,
    score INTEGER,
    comments INTEGER,
    site_id INTEGER NOT NULL REFERENCES sites(site_id)
);

INSERT OR IGNORE INTO sites (name, homepage_url, rss_url) VALUES
    ('news.ycombinator.com', 'https://news.ycombinator.com', 'https://news.ycombinator.com/rss'),
    ('lobste.rs', 'https://lobste.rs', 'https://lobste.rs/rss');
