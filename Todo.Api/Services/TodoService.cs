using Todo.Api.Contracts;
using Todo.Api.Domain;
using Todo.Api.Persistence;
using Todo.Api.Validation;

using Microsoft.Extensions.Logging;

namespace Todo.Api.Services;

public class TodoService
{
    private readonly ITodoRepository _repo;
    private readonly ILogger<TodoService> _logger;

    public TodoService(ITodoRepository repo, ILogger<TodoService> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task<TodoItem> CreateAsync(CreateTodoRequest req, CancellationToken ct = default)
    {
        _logger.LogInformation("Creating new todo item with title: {Title}", req.Title);
        Validators.ValidateTitle(req.Title);
        var due = Validators.ParseDate(req.DueDate);

        var item = new TodoItem
        {
            Title = req.Title.Trim(),
            Description = req.Description?.Trim(),
            DueDate = due,
            IsCompleted = false,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _repo.AddAsync(item, ct);
        _logger.LogInformation("Created todo item with ID: {Id}", result.Id);
        return result;
    }

    public async Task<PaginatedResponse<TodoItem>> ListAsync(
        bool? isCompleted,
        bool? overdue,
        DateOnly? dueBefore,
        DateOnly? dueAfter,
        TodoSortField? sortBy,
        SortOrder sortOrder,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        _logger.LogDebug("Listing todos - Page: {Page}, PageSize: {PageSize}, IsCompleted: {IsCompleted}, Overdue: {Overdue}",
            page, pageSize, isCompleted, overdue);
        
        Validators.EnsureDateRange(dueAfter, dueBefore);
        Validators.ValidatePagination(page, pageSize);

        var items = await _repo.GetAllAsync(ct);
        var query = items.AsEnumerable();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        if (isCompleted is not null)
        {
            query = query.Where(x => x.IsCompleted == isCompleted.Value);
        }

        if (overdue == true)
        {
            query = query.Where(x =>
                x.DueDate is not null &&
                x.DueDate < today &&
                !x.IsCompleted);
        }

        if (dueBefore is not null)
        {
            query = query.Where(x =>
                x.DueDate is not null &&
                x.DueDate <= dueBefore.Value);
        }

        if (dueAfter is not null)
        {
            query = query.Where(x =>
                x.DueDate is not null &&
                x.DueDate >= dueAfter.Value);
        }

        if (sortBy is not null)
        {
            var descending = sortOrder == SortOrder.Desc;
            query = (sortBy, descending) switch
            {
                (TodoSortField.DueDate, true) => query.OrderByDescending(x => x.DueDate ?? DateOnly.MinValue),
                (TodoSortField.DueDate, false) => query.OrderBy(x => x.DueDate ?? DateOnly.MaxValue),
                (TodoSortField.CreatedAt, true) => query.OrderByDescending(x => x.CreatedAt),
                (TodoSortField.CreatedAt, false) => query.OrderBy(x => x.CreatedAt),
                (TodoSortField.Title, true) => query.OrderByDescending(x => x.Title),
                (TodoSortField.Title, false) => query.OrderBy(x => x.Title),
                _ => query
            };
        }

        var totalCount = query.Count();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        var pagedItems = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        _logger.LogDebug("Returning {Count} items out of {TotalCount} total", pagedItems.Count, totalCount);
        return new PaginatedResponse<TodoItem>(pagedItems, page, pageSize, totalCount, totalPages);
    }

    public Task<TodoItem?> GetAsync(Guid id, CancellationToken ct = default) => _repo.GetByIdAsync(id, ct);

    public async Task<TodoItem?> UpdateAsync(Guid id, UpdateTodoRequest req, CancellationToken ct = default)
    {
        _logger.LogInformation("Updating todo item with ID: {Id}", id);
        var existing = await _repo.GetByIdAsync(id, ct);
        if (existing is null)
        {
            _logger.LogWarning("Todo item with ID {Id} not found for update", id);
            return null;
        }

        var hasChanges = false;

        if (req.Title is not null)
        {
            Validators.ValidateTitle(req.Title);
            existing.Title = req.Title.Trim();
            hasChanges = true;
        }

        if (req.Description is not null)
        {
            existing.Description = req.Description?.Trim();
            hasChanges = true;
        }

        if (req.DueDate is not null)
        {
            existing.DueDate = Validators.ParseDate(req.DueDate);
            hasChanges = true;
        }

        if (!hasChanges)
        {
            throw new ValidationException("request", "Provide at least one field to update.");
        }

        return await _repo.UpdateAsync(existing, ct);
    }

    public async Task<TodoItem?> SetCompletedAsync(Guid id, bool completed, CancellationToken ct = default)
    {
        _logger.LogInformation("Setting todo item {Id} completion status to {Completed}", id, completed);
        var existing = await _repo.GetByIdAsync(id, ct);
        if (existing is null)
        {
            _logger.LogWarning("Todo item with ID {Id} not found for completion status update", id);
            return null;
        }

        existing.IsCompleted = completed;
        return await _repo.UpdateAsync(existing, ct);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("Deleting todo item with ID: {Id}", id);
        var result = await _repo.DeleteAsync(id, ct);
        if (!result)
        {
            _logger.LogWarning("Todo item with ID {Id} not found for deletion", id);
        }
        return result;
    }
}
