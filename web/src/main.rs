mod db;

use std::sync::{Arc, Mutex};

use askama::Template;
use axum::{
    Router,
    extract::{Path, Query, State},
    http::StatusCode,
    response::{Html, IntoResponse},
    routing::get,
};
use chrono::{DateTime, NaiveDate, Utc};
use rusqlite::Connection;
use tower_http::services::ServeDir;

type DbPool = Arc<Mutex<Connection>>;

#[derive(Template)]
#[template(path = "index.html")]
struct IndexTemplate {
    title: String,
    sites: Vec<db::Site>,
}

#[derive(Template)]
#[template(path = "summary.html")]
struct SummaryTemplate {
    title: String,
    site: db::Site,
    items: Vec<db::Item>,
    start_time: DateTime<Utc>,
    end_time: DateTime<Utc>,
    start_time_iso: String,
    end_time_iso: String,
    selected_date: Option<String>,
    previous_date: Option<String>,
    next_date: Option<String>,
    last_update_iso: String,
}

#[derive(Template)]
#[template(path = "about.html")]
struct AboutTemplate {
    title: String,
    last_update: DateTime<Utc>,
    next_update: DateTime<Utc>,
    summary_start: DateTime<Utc>,
    summary_end: DateTime<Utc>,
    last_update_iso: String,
    next_update_iso: String,
    summary_start_iso: String,
    summary_end_iso: String,
}

fn iso(dt: &DateTime<Utc>) -> String {
    dt.format("%Y-%m-%dT%H:%M:%S%.3fZ").to_string()
}

fn last_update(now: DateTime<Utc>) -> DateTime<Utc> {
    now.date_naive().and_hms_opt(0, 0, 0).unwrap().and_utc()
}

async fn index(State(pool): State<DbPool>) -> impl IntoResponse {
    let conn = pool.lock().unwrap();
    let sites = db::get_sites(&conn).unwrap_or_default();
    let tmpl = IndexTemplate { title: "Slowcial Sharing".into(), sites };
    Html(tmpl.render().unwrap())
}

#[derive(serde::Deserialize)]
struct SummaryQuery {
    date: Option<String>,
}

async fn summary(
    State(pool): State<DbPool>,
    Path(site_name): Path<String>,
    Query(query): Query<SummaryQuery>,
) -> impl IntoResponse {
    let conn = pool.lock().unwrap();
    let site = match db::get_site_by_name(&conn, &site_name).unwrap() {
        Some(s) => s,
        None => return (StatusCode::NOT_FOUND, Html("Site not found".to_string())),
    };

    let parsed_date: Option<NaiveDate> = query.date.as_ref().and_then(|d| d.parse().ok());

    let (start_time, end_time) = match parsed_date {
        Some(d) => db::date_summary_bounds(d),
        None => db::default_summary_bounds(Utc::now()),
    };

    let items = db::get_summary_items(&conn, site.site_id, &start_time, &end_time).unwrap_or_default();

    let today = Utc::now().date_naive();
    let target_date = parsed_date.unwrap_or(today);
    let prev = target_date - chrono::Duration::days(1);
    let next = target_date + chrono::Duration::days(1);

    let previous_date = if db::has_items_for_date(&conn, site.site_id, prev).unwrap_or(false) {
        Some(prev.format("%Y-%m-%d").to_string())
    } else {
        None
    };
    let next_date = if next <= today && db::has_items_for_date(&conn, site.site_id, next).unwrap_or(false) {
        Some(next.format("%Y-%m-%d").to_string())
    } else {
        None
    };

    let lu = last_update(Utc::now());
    let tmpl = SummaryTemplate {
        title: site.name.clone(),
        site,
        items,
        start_time,
        end_time,
        start_time_iso: iso(&start_time),
        end_time_iso: iso(&end_time),
        selected_date: parsed_date.map(|d| d.format("%Y-%m-%d").to_string()),
        previous_date,
        next_date,
        last_update_iso: iso(&lu),
    };
    (StatusCode::OK, Html(tmpl.render().unwrap()))
}

async fn about() -> impl IntoResponse {
    let lu = last_update(Utc::now());
    let nu = lu + chrono::Duration::days(1);
    let (ss, se) = db::default_summary_bounds(Utc::now());
    let tmpl = AboutTemplate {
        title: "About".into(),
        last_update: lu,
        next_update: nu,
        summary_start: ss,
        summary_end: se,
        last_update_iso: iso(&lu),
        next_update_iso: iso(&nu),
        summary_start_iso: iso(&ss),
        summary_end_iso: iso(&se),
    };
    Html(tmpl.render().unwrap())
}

pub fn build_router(conn: Connection) -> Router {
    let pool: DbPool = Arc::new(Mutex::new(conn));
    Router::new()
        .route("/", get(index))
        .route("/summary/{site_name}", get(summary))
        .route("/about", get(about))
        .nest_service("/static", ServeDir::new("static"))
        .with_state(pool)
}

#[tokio::main]
async fn main() {
    let db_path = std::env::var("DATABASE_PATH").unwrap_or_else(|_| "slowcial.db".to_string());
    let conn = db::init_db(&db_path).expect("Failed to initialize database");
    let app = build_router(conn);

    let addr = "0.0.0.0:3000";
    println!("Listening on {addr}");
    let listener = tokio::net::TcpListener::bind(addr).await.unwrap();
    axum::serve(listener, app).await.unwrap();
}

#[cfg(test)]
mod tests {
    use super::*;
    use chrono::TimeZone;
    use proptest::prelude::*;

    #[test]
    fn test_last_update_never_panics() {
        let cases = vec![
            Utc.with_ymd_and_hms(1970, 1, 1, 0, 0, 0).unwrap(),
            Utc.with_ymd_and_hms(2000, 2, 29, 23, 59, 59).unwrap(),
            Utc.with_ymd_and_hms(9999, 12, 31, 23, 59, 59).unwrap(),
            Utc.with_ymd_and_hms(2024, 6, 15, 12, 30, 45).unwrap(),
        ];
        for now in cases {
            let result = last_update(now);
            assert_eq!(result.time(), chrono::NaiveTime::from_hms_opt(0, 0, 0).unwrap());
        }
    }

    proptest! {
        #[test]
        fn test_last_update_proptest(
            y in 1970i32..9999,
            m in 1u32..=12,
            d in 1u32..=28,
            h in 0u32..=23,
            min in 0u32..=59,
            s in 0u32..=59,
        ) {
            let now = Utc.with_ymd_and_hms(y, m, d, h, min, s).unwrap();
            let result = last_update(now);
            prop_assert_eq!(result.time(), chrono::NaiveTime::from_hms_opt(0, 0, 0).unwrap());
            prop_assert_eq!(result.date_naive(), now.date_naive());
        }
    }
}
