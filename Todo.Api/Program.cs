using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Todo.Api.Contracts;
using Todo.Api.Persistence;
using Todo.Api.Services;
using Todo.Api.Validation;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables("TODOAPI_");

static string ResolveDataPath(ConfigurationManager configuration)
{
    var explicitPath = configuration.GetValue<string>("Data:Path");
    if (!string.IsNullOrWhiteSpace(explicitPath))
    {
        return explicitPath!;
    }

    var directory = configuration.GetValue<string>("Data:Directory") ?? Path.Combine(AppContext.BaseDirectory, "data");
    if (!Path.IsPathRooted(directory))
    {
        directory = Path.Combine(AppContext.BaseDirectory, directory);
    }

    var fileName = configuration.GetValue<string>("Data:FileName") ?? "todos.json";
    return Path.Combine(directory, fileName);
}

static void EnsureDataDirectoryWritable(string filePath)
{
    var directory = Path.GetDirectoryName(filePath);
    if (string.IsNullOrEmpty(directory))
    {
        throw new InvalidOperationException("Data directory could not be determined.");
    }

    Directory.CreateDirectory(directory);
    var probePath = Path.Combine(directory, $"write-test-{Guid.NewGuid():N}.tmp");
    try
    {
        File.WriteAllText(probePath, "probe");
    }
    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
    {
        throw new InvalidOperationException($"Data directory '{directory}' is not writable.", ex);
    }
    finally
    {
        try
        {
            if (File.Exists(probePath))
            {
                File.Delete(probePath);
            }
        }
        catch
        {
            // ignore cleanup issues
        }
    }
}

var dataPath = ResolveDataPath(builder.Configuration);
EnsureDataDirectoryWritable(dataPath);

builder.Services.AddSingleton(new DataStore(dataPath));
builder.Services.AddSingleton<ITodoRepository, FileTodoRepository>();
builder.Services.AddScoped<TodoService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Todo API",
        Version = "v1",
        Description = "A RESTful API for managing todo items with filtering, sorting, and pagination support."
    });

    // Include XML comments
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

app.UseExceptionHandler(handlerApp =>
{
    handlerApp.Run(async context =>
    {
        var feature = context.Features.Get<IExceptionHandlerFeature>();

        if (feature?.Error is ValidationException validationException)
        {
            var validationProblem = new ValidationProblemDetails
            {
                Title = "Validation failed.",
                Status = StatusCodes.Status400BadRequest
            };
            foreach (var (key, value) in validationException.Errors)
            {
                validationProblem.Errors.Add(key, value);
            }

            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(validationProblem);
            return;
        }

        var problem = new ProblemDetails
        {
            Title = "Unexpected error.",
            Status = StatusCodes.Status500InternalServerError
        };
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(problem);
    });
});

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/", () => Results.Redirect("/swagger", permanent: false));

var todos = app.MapGroup("/todos");

todos.MapPost("", async ([FromBody] CreateTodoRequest req, TodoService svc, CancellationToken ct) =>
    {
        var created = await svc.CreateAsync(req, ct);
        return Results.Created($"/todos/{created.Id}", TodoResponse.From(created));
    })
    .WithName("CreateTodo")
    .WithSummary("Create a new todo item")
    .WithDescription("Creates a new todo item with the provided title, optional description, and optional due date.")
    .WithTags("Todos")
    .Produces<TodoResponse>(StatusCodes.Status201Created)
    .ProducesValidationProblem();

todos.MapGet("", async ([AsParameters] ListTodosQuery query, TodoService svc, CancellationToken ct) =>
    {
        var result = await svc.ListAsync(
            query.IsCompleted,
            query.Overdue,
            query.DueBefore,
            query.DueAfter,
            query.SortBy,
            query.SortDirection,
            query.PageNumber,
            query.PageSizeValue,
            ct);
        var response = new PaginatedResponse<TodoResponse>(
            result.Items.Select(TodoResponse.From).ToList(),
            result.Page,
            result.PageSize,
            result.TotalCount,
            result.TotalPages);
        return Results.Ok(response);
    })
    .WithName("ListTodos")
    .WithSummary("List all todo items")
    .WithDescription("Retrieves a paginated list of todo items with optional filtering and sorting.")
    .WithTags("Todos")
    .Produces<PaginatedResponse<TodoResponse>>(StatusCodes.Status200OK)
    .ProducesValidationProblem();

todos.MapGet("/{id:guid}", async (Guid id, TodoService svc, CancellationToken ct) =>
    {
        var item = await svc.GetAsync(id, ct);
        return item is null ? Results.NotFound() : Results.Ok(TodoResponse.From(item));
    })
    .WithName("GetTodo")
    .WithSummary("Get a todo item by ID")
    .WithDescription("Retrieves a specific todo item by its unique identifier.")
    .WithTags("Todos")
    .Produces<TodoResponse>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status404NotFound);

todos.MapPut("/{id:guid}", async (Guid id, [FromBody] UpdateTodoRequest req, TodoService svc, CancellationToken ct) =>
    {
        var updated = await svc.UpdateAsync(id, req, ct);
        return updated is null ? Results.NotFound() : Results.Ok(TodoResponse.From(updated));
    })
    .WithName("UpdateTodo")
    .WithSummary("Update a todo item")
    .WithDescription("Updates the title, description, or due date of an existing todo item. At least one field must be provided.")
    .WithTags("Todos")
    .Produces<TodoResponse>(StatusCodes.Status200OK)
    .ProducesValidationProblem()
    .ProducesProblem(StatusCodes.Status404NotFound);

todos.MapPatch("/{id:guid}/complete", async (Guid id, TodoService svc, CancellationToken ct) =>
    {
        var updated = await svc.SetCompletedAsync(id, true, ct);
        return updated is null ? Results.NotFound() : Results.Ok(TodoResponse.From(updated));
    })
    .WithName("CompleteTodo")
    .WithSummary("Mark a todo item as completed")
    .WithDescription("Marks a specific todo item as completed by setting its isCompleted flag to true.")
    .WithTags("Todos")
    .Produces<TodoResponse>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status404NotFound);

todos.MapPatch("/{id:guid}/incomplete", async (Guid id, TodoService svc, CancellationToken ct) =>
    {
        var updated = await svc.SetCompletedAsync(id, false, ct);
        return updated is null ? Results.NotFound() : Results.Ok(TodoResponse.From(updated));
    })
    .WithName("IncompleteTodo")
    .WithSummary("Mark a todo item as incomplete")
    .WithDescription("Marks a specific todo item as incomplete by setting its isCompleted flag to false.")
    .WithTags("Todos")
    .Produces<TodoResponse>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status404NotFound);

todos.MapDelete("/{id:guid}", async (Guid id, TodoService svc, CancellationToken ct) =>
    {
        var deleted = await svc.DeleteAsync(id, ct);
        return deleted ? Results.NoContent() : Results.NotFound();
    })
    .WithName("DeleteTodo")
    .WithSummary("Delete a todo item")
    .WithDescription("Permanently deletes a todo item by its unique identifier.")
    .WithTags("Todos")
    .Produces(StatusCodes.Status204NoContent)
    .ProducesProblem(StatusCodes.Status404NotFound);

app.Run();

public partial class Program;
