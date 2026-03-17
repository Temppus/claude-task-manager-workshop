---
name: add-endpoint
description: Scaffold a new REST endpoint with route, handler, validation, and tests. Use when the user wants to add a new API endpoint or CRUD operation.
argument-hint: [HTTP-method] [description of what the endpoint does]
allowed-tools: Read, Write, Glob
---

Scaffold a new REST endpoint: **$ARGUMENTS**

Follow these project patterns exactly:

## Controller
- Add the endpoint to an existing controller in `TaskManager/Controllers/`, or create a new one if it's a new resource
- Use `[ApiController]`, `[Route("api/...")]`, and `[Produces("application/json")]` at class level
- Inject `AppDbContext` via constructor: `public MyController(AppDbContext db) => _db = db;`
- Use attribute routing (`[HttpGet]`, `[HttpPost]`, etc.) with route constraints like `{id:guid}`
- Add `[ProducesResponseType]` attributes for all possible status codes
- Add `/// <summary>` XML doc comments on the method for Swagger

## Model
- Place entity classes in `TaskManager/Models/`
- Use `Guid Id` as primary key
- Use `[Required]` and `[MaxLength]` validation attributes
- Include `DateTime CreatedAt` and `DateTime UpdatedAt` timestamps
- Use nullable `?` for optional fields

## DTOs
- Place in `TaskManager/DTOs/`
- Create separate request and response classes (e.g., `CreateXRequest`, `XResponse`)
- Add `[Required(ErrorMessage = "...")]` validation on request DTOs
- Add a `static XResponse FromEntity(X entity)` factory method on response DTOs
- Enums in responses should be lowercase strings

## DbContext
- Register new entities as `public DbSet<X> Xs => Set<X>();` in `TaskManager/Data/AppDbContext.cs`
- Configure in `OnModelCreating`: primary key, `gen_random_uuid()` default, string conversions for enums, max lengths, timestamp defaults, indexes on foreign keys, and cascade delete for child relationships

## Tests
- Add integration tests in `TaskManager.Tests/` using xUnit and FluentAssertions
- Use `IClassFixture<TaskManagerApiFactory>` and `IAsyncLifetime`
- Name tests `Test_{Action}_{Scenario}_{ExpectedResult}`
- All tests are `async Task` using `HttpClient` from the factory
- Use `JsonSerializerOptions` with `JsonNamingPolicy.SnakeCaseLower`
- Add private helper methods for repeated operations

## JSON
- All request/response JSON uses **snake_case** (handled globally, just use C# PascalCase in code)
