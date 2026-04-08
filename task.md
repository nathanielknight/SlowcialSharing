@dotnet-version contains an ASP.Net application that:

- fetches RSS feeds from social link sharing sites (like Hacker News and Lobsters)
- retrieves titles and scores after a cooldown period
- presents the links in a static, time-offset fashion

However, it consumes a great deal of memory.

I'd like to replace it with a version that:

- uses Rust with Axum, Rusqlite, and Askama templates for the web server
- uses a cron job + python script (managed with UV) for the RSS and score fetching

You should have access to `cargo`, `uv`, `dotnet`, and `sqlite`. Please implement the Rust + Python version, using red-green TDD with unit and property-based tests, as well as `hurl` for integration tests.

Commit your work to `git` as you go.
