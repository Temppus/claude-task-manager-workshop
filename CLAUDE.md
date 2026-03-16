# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run

```bash
# Start PostgreSQL (required for local development)
docker compose up -d

# Build
dotnet build

# Run the API (http://localhost:5000, Swagger at /swagger)
cd TaskManager && dotnet run

# Run all tests (spins up a disposable PostgreSQL via Testcontainers - requires Docker)
dotnet test

# Run a single test by name
dotnet test --filter "Test_CreateTask_ReturnsCreatedWithCorrectFields"

# Run tests matching a pattern
dotnet test --filter "StatusTransition"
```

## Database Migrations

EF Core migrations auto-apply on startup (`db.Database.Migrate()` in Program.cs). This also runs during tests against the Testcontainers PostgreSQL instance.

```bash
# Create a new migration after model changes
cd TaskManager && dotnet ef migrations add <MigrationName>

# Generate SQL script instead of auto-applying
dotnet ef migrations script
```

The `DesignTimeDbContextFactory` provides a fallback connection string so `dotnet ef` commands work without a running app.

## Architecture

Single ASP.NET Core 8.0 Web API project (`TaskManager/`) with a companion integration test project (`TaskManager.Tests/`).

**API layer**: One controller (`TasksController`) handles all endpoints under `/api/tasks`. JSON serialization uses **snake_case** naming policy (configured in Program.cs), so C# PascalCase properties map to snake_case in HTTP request/response bodies.

**Data layer**: EF Core with Npgsql for PostgreSQL. Enums (Priority, Status) are stored as strings in the database. Connection string resolves from `DATABASE_URL` env var first, then falls back to `ConnectionStrings:DefaultConnection` in appsettings.json.

**Business rules**: Status transitions are strictly enforced: `todo → in_progress → done` (no skipping, no going backwards). Due dates must be in the future at creation time. Overdue count in stats excludes done tasks.

**Testing**: Integration tests use `WebApplicationFactory<Program>` with Testcontainers (`TaskManagerApiFactory`). Each test class gets a fresh PostgreSQL container that receives all migrations automatically. Tests use FluentAssertions and follow `Test_{Description}` naming.
