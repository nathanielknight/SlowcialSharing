use axum::http::{Request, StatusCode};
use http_body_util::BodyExt;
use rusqlite::{Connection, params};
use tower::ServiceExt;

fn test_db_with_items() -> Connection {
    let conn = Connection::open_in_memory().unwrap();
    conn.execute_batch(include_str!("../../schema.sql")).unwrap();

    // Insert items for lobste.rs (site_id=1) with pub_date on 2024-01-14,
    // which falls in the summary window for date=2024-01-15.
    conn.execute(
        "INSERT INTO items (key, title, link, pub_date, comments_link, score, comments, site_id) \
         VALUES (?1, ?2, ?3, ?4, ?5, ?6, ?7, ?8)",
        params![
            "item-1",
            "Rust is Great",
            "https://example.com/rust",
            "2024-01-14 10:00:00",
            "https://lobste.rs/s/item1",
            150,
            42,
            2
        ],
    )
    .unwrap();

    conn.execute(
        "INSERT INTO items (key, title, link, pub_date, comments_link, score, comments, site_id) \
         VALUES (?1, ?2, ?3, ?4, ?5, ?6, ?7, ?8)",
        params![
            "item-2",
            "SQLite Tips",
            "https://example.com/sqlite",
            "2024-01-14 14:00:00",
            "https://lobste.rs/s/item2",
            75,
            10,
            2
        ],
    )
    .unwrap();

    conn
}

async fn get_body(app: axum::Router, uri: &str) -> (StatusCode, String) {
    let response = app
        .oneshot(Request::builder().uri(uri).body(axum::body::Body::empty()).unwrap())
        .await
        .unwrap();
    let status = response.status();
    let bytes = response.into_body().collect().await.unwrap().to_bytes();
    (status, String::from_utf8(bytes.to_vec()).unwrap())
}

#[tokio::test]
async fn test_index_lists_sites() {
    let conn = test_db_with_items();
    let app = slowcial_web::build_router(conn);

    let (status, body) = get_body(app, "/").await;
    assert_eq!(status, StatusCode::OK);
    assert!(body.contains("lobste.rs"), "index should list lobste.rs");
    assert!(
        body.contains("news.ycombinator.com"),
        "index should list news.ycombinator.com"
    );
}

#[tokio::test]
async fn test_summary_renders_items() {
    let conn = test_db_with_items();
    let app = slowcial_web::build_router(conn);

    let (status, body) = get_body(app, "/summary/lobste.rs?date=2024-01-15").await;
    assert_eq!(status, StatusCode::OK);
    assert!(
        body.contains("Rust is Great"),
        "summary should contain item title 'Rust is Great'"
    );
    assert!(
        body.contains("SQLite Tips"),
        "summary should contain item title 'SQLite Tips'"
    );
    assert!(
        body.contains("https://example.com/rust"),
        "summary should contain item link"
    );
    assert!(
        body.contains("150"),
        "summary should contain the score"
    );
}

#[tokio::test]
async fn test_summary_missing_site_returns_404() {
    let conn = test_db_with_items();
    let app = slowcial_web::build_router(conn);

    let (status, _body) = get_body(app, "/summary/nonexistent.example.com").await;
    assert_eq!(status, StatusCode::NOT_FOUND);
}

#[tokio::test]
async fn test_default_view_previous_link_goes_to_earlier_window() {
    // The default view shows the window ending at yesterday's midnight (the
    // same as ?date=<yesterday>), so its "Previous" link must point to the
    // day before yesterday — not to a page showing the identical window.
    let conn = Connection::open_in_memory().unwrap();
    conn.execute_batch(include_str!("../../schema.sql")).unwrap();

    let today = chrono::Utc::now().date_naive();
    let in_default_window = (today - chrono::Duration::days(2)).format("%Y-%m-%d 12:00:00");
    let in_previous_window = (today - chrono::Duration::days(3)).format("%Y-%m-%d 12:00:00");
    for (key, pub_date) in [("cur", in_default_window), ("prev", in_previous_window)] {
        conn.execute(
            "INSERT INTO items (key, title, link, pub_date, comments_link, score, comments, site_id) \
             VALUES (?1, ?2, ?3, ?4, ?5, ?6, ?7, ?8)",
            params![key, "Title", "https://example.com", pub_date.to_string(), "c", 10, 1, 2],
        )
        .unwrap();
    }
    let app = slowcial_web::build_router(conn);

    let (status, body) = get_body(app, "/summary/lobste.rs").await;
    assert_eq!(status, StatusCode::OK);
    let expected_prev = format!("?date={}", (today - chrono::Duration::days(2)).format("%Y-%m-%d"));
    let same_window = format!("?date={}", (today - chrono::Duration::days(1)).format("%Y-%m-%d"));
    assert!(
        body.contains(&expected_prev),
        "previous link should target {expected_prev}"
    );
    assert!(
        !body.contains(&same_window),
        "navigation must not link to {same_window}, which shows the same window as the default view"
    );
}

#[tokio::test]
async fn test_summary_no_items_for_date() {
    let conn = test_db_with_items();
    let app = slowcial_web::build_router(conn);

    let (status, body) = get_body(app, "/summary/lobste.rs?date=2024-06-01").await;
    assert_eq!(status, StatusCode::OK);
    // Should not contain our test items
    assert!(!body.contains("Rust is Great"));
    assert!(!body.contains("SQLite Tips"));
}
