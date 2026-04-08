use chrono::{DateTime, NaiveDate, Utc};
use rusqlite::{Connection, params};

#[derive(Debug, Clone)]
pub struct Site {
    pub site_id: i64,
    pub name: String,
    pub homepage_url: String,
    pub rss_url: Option<String>,
}

#[derive(Debug, Clone)]
pub struct Item {
    pub key: String,
    pub title: String,
    pub link: String,
    pub pub_date: DateTime<Utc>,
    pub comments_link: String,
    pub score: Option<i64>,
    pub comments: Option<i64>,
    pub site_id: i64,
}

impl Item {
    pub fn domain(&self) -> String {
        url::Url::parse(&self.link)
            .map(|u| format!("http://{}", u.host_str().unwrap_or("")))
            .unwrap_or_default()
    }

    pub fn pub_date_iso(&self) -> String {
        self.pub_date.format("%Y-%m-%dT%H:%M:%S%.3fZ").to_string()
    }
}

pub fn init_db(path: &str) -> rusqlite::Result<Connection> {
    let conn = Connection::open(path)?;
    conn.execute_batch(include_str!("../../schema.sql"))?;
    Ok(conn)
}

pub fn get_sites(conn: &Connection) -> rusqlite::Result<Vec<Site>> {
    let mut stmt = conn.prepare("SELECT site_id, name, homepage_url, rss_url FROM sites ORDER BY name")?;
    let sites = stmt.query_map([], |row| {
        Ok(Site {
            site_id: row.get(0)?,
            name: row.get(1)?,
            homepage_url: row.get(2)?,
            rss_url: row.get(3)?,
        })
    })?.collect::<Result<Vec<_>, _>>()?;
    Ok(sites)
}

pub fn get_site_by_name(conn: &Connection, name: &str) -> rusqlite::Result<Option<Site>> {
    let mut stmt = conn.prepare("SELECT site_id, name, homepage_url, rss_url FROM sites WHERE name = ?1")?;
    let mut rows = stmt.query_map(params![name], |row| {
        Ok(Site {
            site_id: row.get(0)?,
            name: row.get(1)?,
            homepage_url: row.get(2)?,
            rss_url: row.get(3)?,
        })
    })?;
    match rows.next() {
        Some(Ok(site)) => Ok(Some(site)),
        Some(Err(e)) => Err(e),
        None => Ok(None),
    }
}

/// Returns (start_time, end_time) for the default summary view.
/// Default: items from 2 days ago to 1 day ago (UTC midnight boundaries).
pub fn default_summary_bounds() -> (DateTime<Utc>, DateTime<Utc>) {
    let now = Utc::now();
    let today_midnight = now.date_naive().and_hms_opt(0, 0, 0).unwrap().and_utc();
    let end_time = today_midnight - chrono::Duration::days(1);
    let start_time = end_time - chrono::Duration::days(1);
    (start_time, end_time)
}

/// Returns (start_time, end_time) for a specific date.
pub fn date_summary_bounds(date: NaiveDate) -> (DateTime<Utc>, DateTime<Utc>) {
    let end_time = date.and_hms_opt(0, 0, 0).unwrap().and_utc();
    let start_time = end_time - chrono::Duration::days(1);
    (start_time, end_time)
}

pub fn get_summary_items(
    conn: &Connection,
    site_id: i64,
    start_time: &DateTime<Utc>,
    end_time: &DateTime<Utc>,
) -> rusqlite::Result<Vec<Item>> {
    let start_str = start_time.format("%Y-%m-%d %H:%M:%S").to_string();
    let end_str = end_time.format("%Y-%m-%d %H:%M:%S").to_string();
    let mut stmt = conn.prepare(
        "SELECT key, title, link, pub_date, comments_link, score, comments, site_id \
         FROM items \
         WHERE site_id = ?1 AND score IS NOT NULL AND pub_date >= ?2 AND pub_date < ?3 \
         ORDER BY score DESC, pub_date DESC"
    )?;
    let items = stmt.query_map(params![site_id, start_str, end_str], |row| {
        let pub_date_str: String = row.get(3)?;
        let pub_date = chrono::NaiveDateTime::parse_from_str(&pub_date_str, "%Y-%m-%d %H:%M:%S")
            .map(|ndt| ndt.and_utc())
            .unwrap_or_else(|_| Utc::now());
        Ok(Item {
            key: row.get(0)?,
            title: row.get(1)?,
            link: row.get(2)?,
            pub_date,
            comments_link: row.get(4)?,
            score: row.get(5)?,
            comments: row.get(6)?,
            site_id: row.get(7)?,
        })
    })?.collect::<Result<Vec<_>, _>>()?;
    Ok(items)
}

pub fn has_items_for_date(conn: &Connection, site_id: i64, date: NaiveDate) -> rusqlite::Result<bool> {
    let (start, end) = date_summary_bounds(date);
    let start_str = start.format("%Y-%m-%d %H:%M:%S").to_string();
    let end_str = end.format("%Y-%m-%d %H:%M:%S").to_string();
    let count: i64 = conn.query_row(
        "SELECT COUNT(*) FROM items WHERE site_id = ?1 AND pub_date >= ?2 AND pub_date < ?3",
        params![site_id, start_str, end_str],
        |row| row.get(0),
    )?;
    Ok(count > 0)
}

#[cfg(test)]
mod tests {
    use super::*;

    fn test_db() -> Connection {
        let conn = Connection::open_in_memory().unwrap();
        conn.execute_batch(include_str!("../../schema.sql")).unwrap();
        conn
    }

    #[test]
    fn test_get_sites() {
        let conn = test_db();
        let sites = get_sites(&conn).unwrap();
        assert_eq!(sites.len(), 2);
        assert_eq!(sites[0].name, "lobste.rs");
        assert_eq!(sites[1].name, "news.ycombinator.com");
    }

    #[test]
    fn test_get_site_by_name() {
        let conn = test_db();
        let site = get_site_by_name(&conn, "lobste.rs").unwrap();
        assert!(site.is_some());
        assert_eq!(site.unwrap().name, "lobste.rs");

        let missing = get_site_by_name(&conn, "nonexistent").unwrap();
        assert!(missing.is_none());
    }

    #[test]
    fn test_default_summary_bounds() {
        let (start, end) = default_summary_bounds();
        assert_eq!(end - start, chrono::Duration::days(1));
        assert!(end < Utc::now());
    }

    #[test]
    fn test_date_summary_bounds() {
        let date = NaiveDate::from_ymd_opt(2024, 1, 15).unwrap();
        let (start, end) = date_summary_bounds(date);
        assert_eq!(end.format("%Y-%m-%d").to_string(), "2024-01-15");
        assert_eq!(start.format("%Y-%m-%d").to_string(), "2024-01-14");
    }

    #[test]
    fn test_get_summary_items_empty() {
        let conn = test_db();
        let (start, end) = default_summary_bounds();
        let items = get_summary_items(&conn, 1, &start, &end).unwrap();
        assert!(items.is_empty());
    }

    #[test]
    fn test_insert_and_retrieve_items() {
        let conn = test_db();
        conn.execute(
            "INSERT INTO items (key, title, link, pub_date, comments_link, score, comments, site_id) \
             VALUES (?1, ?2, ?3, ?4, ?5, ?6, ?7, ?8)",
            params![
                "test-key", "Test Title", "https://example.com", "2024-01-14 12:00:00",
                "https://lobste.rs/s/test", 42, 10, 1
            ],
        ).unwrap();

        let date = NaiveDate::from_ymd_opt(2024, 1, 15).unwrap();
        let (start, end) = date_summary_bounds(date);
        let items = get_summary_items(&conn, 1, &start, &end).unwrap();
        assert_eq!(items.len(), 1);
        assert_eq!(items[0].title, "Test Title");
        assert_eq!(items[0].score, Some(42));
    }

    #[test]
    fn test_items_without_score_excluded() {
        let conn = test_db();
        conn.execute(
            "INSERT INTO items (key, title, link, pub_date, comments_link, score, comments, site_id) \
             VALUES (?1, ?2, ?3, ?4, ?5, NULL, NULL, ?6)",
            params![
                "no-score", "No Score", "https://example.com", "2024-01-14 12:00:00",
                "https://lobste.rs/s/test", 1
            ],
        ).unwrap();

        let date = NaiveDate::from_ymd_opt(2024, 1, 15).unwrap();
        let (start, end) = date_summary_bounds(date);
        let items = get_summary_items(&conn, 1, &start, &end).unwrap();
        assert!(items.is_empty());
    }

    #[test]
    fn test_item_domain() {
        let item = Item {
            key: "k".into(), title: "t".into(), link: "https://example.com/path".into(),
            pub_date: Utc::now(), comments_link: "c".into(),
            score: Some(1), comments: Some(0), site_id: 1,
        };
        assert_eq!(item.domain(), "http://example.com");
    }

    use proptest::prelude::*;

    proptest! {
        #[test]
        fn test_date_bounds_always_one_day(y in 2020i32..2030, m in 1u32..=12, d in 1u32..=28) {
            let date = NaiveDate::from_ymd_opt(y, m, d).unwrap();
            let (start, end) = date_summary_bounds(date);
            prop_assert_eq!((end - start).num_seconds(), 86400);
            prop_assert_eq!(end.format("%Y-%m-%d").to_string(), date.format("%Y-%m-%d").to_string());
        }

        #[test]
        fn test_item_domain_never_panics(link in "[a-z]{3,10}://[a-z]{1,10}\\.[a-z]{2,4}/[a-z]*") {
            let item = Item {
                key: "k".into(), title: "t".into(), link,
                pub_date: Utc::now(), comments_link: "c".into(),
                score: Some(1), comments: Some(0), site_id: 1,
            };
            let _ = item.domain(); // should not panic
        }
    }

    #[test]
    fn test_has_items_for_date() {
        let conn = test_db();
        let date = NaiveDate::from_ymd_opt(2024, 1, 15).unwrap();
        assert!(!has_items_for_date(&conn, 1, date).unwrap());

        conn.execute(
            "INSERT INTO items (key, title, link, pub_date, comments_link, score, comments, site_id) \
             VALUES (?1, ?2, ?3, ?4, ?5, ?6, ?7, ?8)",
            params!["k", "t", "https://example.com", "2024-01-14 12:00:00", "c", 1, 0, 1],
        ).unwrap();
        assert!(has_items_for_date(&conn, 1, date).unwrap());
    }
}
