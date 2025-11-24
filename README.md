# Todo API

A minimal ASP.NET Core Web API that manages to-do items with file-backed persistence. The project demonstrates production-minded practices: layered architecture, validation, persistence abstractions, filtering/sorting, and automated tests.

## Getting Started

### Prerequisites

- [.NET SDK 9/10 preview](https://dotnet.microsoft.com/en-us/download) (the project targets `net10.0`).

### Run the API

```bash
dotnet run --project Todo.Api
```

The application hosts Swagger UI at `http://localhost:5000/swagger` (and `https://localhost:7000`).

### Configuration

The API stores tasks in a JSON file. Configure via `appsettings.json`, environment variables, or command-line args:

| Setting | Description | Default |
| --- | --- | --- |
| `Data:Path` | Absolute path to the JSON file | `null` (computed from directory + file) |
| `Data:Directory` | Directory under the app base path | `data` |
| `Data:FileName` | File name within the directory | `todos.json` |

Example override:

```bash
Data__Directory=C:\tmp\data dotnet run --project Todo.Api
# or
TODOAPI_Data__Path=/tmp/custom-todos.json dotnet run --project Todo.Api
```

### API Overview

| Verb | Route | Description |
| --- | --- | --- |
| `POST` | `/todos` | Create a to-do item |
| `GET` | `/todos` | List with optional filtering, sorting, and pagination (see query parameters below) |
| `GET` | `/todos/{id}` | View details |
| `PUT` | `/todos/{id}` | Update title/description/due date |
| `PATCH` | `/todos/{id}/complete` | Mark complete |
| `PATCH` | `/todos/{id}/incomplete` | Mark incomplete |
| `DELETE` | `/todos/{id}` | Remove item |

**List Todos Query Parameters:**
- `isCompleted` (bool?): Filter by completion status
- `overdue` (bool?): Show only overdue items (due date in past and not completed)
- `dueBefore` (DateOnly?): Filter items with due date on or before this date (YYYY-MM-DD)
- `dueAfter` (DateOnly?): Filter items with due date on or after this date (YYYY-MM-DD)
- `sortBy` (TodoSortField?): Sort by `Title`, `DueDate`, or `CreatedAt`
- `order` (SortOrder?): Sort order - `Asc` or `Desc` (defaults to `Asc`)
- `page` (int?): Page number (1-based, defaults to 1)
- `pageSize` (int?): Items per page (defaults to 10, maximum 100)

The list endpoint returns a paginated response with metadata including total count, total pages, and navigation flags.

Validation errors are returned as RFC 7807 problem responses.

### Tests

```bash
dotnet test Todo.Api.Tests/Todo.Api.Tests.csproj
```

Tests cover core service behaviors and API surface (via `WebApplicationFactory`) to ensure routing + JSON contracts remain intact.

### Docker

Build and run the API in a container:

```bash
docker build -t todo-api .
docker run --rm -p 8080:8080 -v $(pwd)/data:/app/data todo-api
```

The container listens on port `8080` by default (override with `-e ASPNETCORE_URLS=http://+:5000`). Mounting `data/` keeps the JSON store on the host. Windows PowerShell users can map the volume via `-v ${PWD}/data:/app/data`.

## Design Notes

- **Contracts first:** Minimal API endpoints reuse strongly typed request/response records, ensuring the Swagger document aligns with actual payloads.
- **Validation layer:** Centralized in `Validators` with a custom `ValidationException` surfaced as `ValidationProblemDetails`, enabling consistent client feedback.
- **Persistence boundary:** `ITodoRepository` hides file I/O details. `DataStore` maintains a cached list protected by a `SemaphoreSlim`, validates file accessibility at startup, and writes via atomic temp-file swaps to stay consistent under concurrent requests or sudden interruptions.
- **Filtering, sorting & pagination:** The service layer owns query logic (completion, overdue, date range, sorting on title/due date/creation time, and pagination) and is exposed via query parameters. Pagination defaults to 10 items per page with a maximum of 100.
- **Testing strategy:** Unit tests use an in-memory repository to isolate domain logic, while integration tests boot the full host with a temporary data file to execute HTTP scenarios end-to-end.

## Security

Authentication and authorization are intentionally omitted to keep the sample focused on CRUD behavior. To secure the API add a middleware such as ASP.NET Core authentication handlers (e.g., JWT bearer or API keys), require `[Authorize]` on the endpoints, and configure HTTPS-only listeners inside `Program.cs`.

## Assumptions & Trade-offs

- File-based persistence is sufficient for the assignment scope; replacing it with a database would only require a new `ITodoRepository` implementation.
- Authentication/authorization is out of scope; all endpoints are public (see Security section for adding authentication).
- Pagination is implemented with a maximum page size of 100 to prevent excessive data transfer.

