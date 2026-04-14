#[tokio::main]
async fn main() {
    let db_path = std::env::var("DATABASE_PATH").unwrap_or_else(|_| "slowcial.db".to_string());
    let conn = slowcial_web::db::init_db(&db_path).expect("Failed to initialize database");
    let app = slowcial_web::build_router(conn);

    let addr = "0.0.0.0:3000";
    println!("Listening on {addr}");
    let listener = tokio::net::TcpListener::bind(addr).await.expect("Couldn't bind 0.0.0.0:3000");
    axum::serve(listener, app).await.expect("Error starting server");
}
