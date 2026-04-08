# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Common Commands

### Build and Run
- `dotnet run` - Start the development server (available at http://localhost:5066)
- `dotnet build` - Build the project
- `finch build -t sloshar .` - Build Docker container using Finch
- `dotnet ef migrations add <name>` - Create new database migration
- `dotnet ef database update` - Apply pending migrations

### Deployment
- `scripts/build.sh` - Build production Docker image
- `scripts/deploy.sh` - Deploy to production (if configured)

## Architecture Overview

SlowcialSharing is an ASP.NET Core 9.0 Razor Pages application that aggregates and tracks content from social news sites (Hacker News, Lobsters). The architecture follows a scheduled data collection pattern with background processing.

### Core Components

**Background Processing**: The `ScrapingWorker` hosted service runs every hour to collect new items and update scores. It orchestrates the entire scraping pipeline through the `Scraper` class.

**Data Layer**: Uses Entity Framework Core with SQLite. Models are defined in `Data/Models.cs` with `Item` representing articles and `Site` representing source platforms. Database context is in `Data/ApplicationDbContext.cs`.

**Client Architecture**: Modular scraping clients in `src/Clients/` implement the `IScraper` interface. Each client handles a specific site (HackerNews, Lobsters) with platform-specific parsing logic. All clients share the `RawClient` for HTTP operations.

**Two-Phase Collection**: Items are first collected from RSS feeds, then detailed scores/comments are fetched after a 24-hour delay to allow metrics to stabilize.

**Summary System**: The `SummaryService` (registered as scoped) generates periodic summaries of collected content, accessible via the Summary page.

### Key Files
- `Program.cs` - Application configuration and DI setup
- `src/Scraper.cs` - Main scraping orchestration logic
- `src/ScrapingService.cs` - Background worker service
- `src/Clients/BaseClient.cs` - Shared HTTP client and IScraper interface
- `Data/Models.cs` - Core data models (Item, Site)