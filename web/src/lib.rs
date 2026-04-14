pub mod db;

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

fn internal_error(msg: &str) -> (StatusCode, Html<String>) {
    eprintln!("Internal error: {msg}");
    (StatusCode::INTERNAL_SERVER_ERROR, Html("Internal server error".to_string()))
}

fn iso(dt: &DateTime<Utc>) -> String {
    dt.format("%Y-%m-%dT%H:%M:%S%.3fZ").to_string()
}

fn last_update(now: DateTime<Utc>) -> DateTime<Utc> {
    now.date_naive().and_hms_opt(0, 0, 0).unwrap().and_utc()
}

async fn index(State(pool): State<DbPool>) -> impl IntoResponse {
    let conn = match pool.lock() {
        Ok(c) => c,
        Err(e) => return internal_error(&format!("DB lock: {e}")),
    };
    let sites = db::get_sites(&conn).unwrap_or_default();
    let tmpl = IndexTemplate { title: "Slowcial Sharing".into(), sites };
    match tmpl.render() {
        Ok(html) => (StatusCode::OK, Html(html)),
        Err(e) => internal_error(&format!("Template render: {e}")),
    }
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
    let conn = match pool.lock() {
        Ok(c) => c,
        Err(e) => return internal_error(&format!("DB lock: {e}")),
    };
    let site = match db::get_site_by_name(&conn, &site_name) {
        Ok(Some(s)) => s,
        Ok(None) => return (StatusCode::NOT_FOUND, Html("Site not found".to_string())),
        Err(e) => return internal_error(&format!("DB query: {e}")),
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
    match tmpl.render() {
        Ok(html) => (StatusCode::OK, Html(html)),
        Err(e) => internal_error(&format!("Template render: {e}")),
    }
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
    match tmpl.render() {
        Ok(html) => (StatusCode::OK, Html(html)),
        Err(e) => internal_error(&format!("Template render: {e}")),
    }
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
