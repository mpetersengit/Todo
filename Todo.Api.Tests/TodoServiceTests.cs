using Microsoft.Extensions.Logging;
using Moq;
using Todo.Api.Contracts;
using Todo.Api.Domain;
using Todo.Api.Persistence;
using Todo.Api.Services;
using Todo.Api.Validation;
using Xunit;

namespace Todo.Api.Tests;

public class TodoServiceTests
{
    private readonly ILogger<TodoService> _logger = Mock.Of<ILogger<TodoService>>();

    [Fact]
    public async Task CreateAsync_WithValidPayload_ReturnsItem()
    {
        // Arrange
        var repo = new Mock<ITodoRepository>(MockBehavior.Strict);
        repo.Setup(r => r.AddAsync(It.IsAny<TodoItem>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TodoItem item, CancellationToken _) => item);
        var service = new TodoService(repo.Object, _logger);
        var request = new CreateTodoRequest("  ship feature  ", "  finish writing docs ", "2025-01-01", null);

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        repo.Verify(r => r.AddAsync(It.Is<TodoItem>(t =>
            t.Title == "ship feature" &&
            t.Description == "finish writing docs" &&
            t.DueDate == new DateOnly(2025, 1, 1)), It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal("ship feature", result.Title);
        Assert.Equal("finish writing docs", result.Description);
        Assert.Equal(new DateOnly(2025, 1, 1), result.DueDate);
        Assert.False(result.IsCompleted);
        Assert.True(result.CreatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public async Task CreateAsync_WithBlankTitle_ThrowsValidationException()
    {
        // Arrange
        var repo = new Mock<ITodoRepository>(MockBehavior.Strict);
        var service = new TodoService(repo.Object, _logger);
        var request = new CreateTodoRequest("   ", null, null, null);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(request));
        repo.Verify(r => r.AddAsync(It.IsAny<TodoItem>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WithoutChanges_ThrowsValidationException()
    {
        // Arrange
        var existing = new TodoItem { Id = Guid.NewGuid(), Title = "initial" };
        var repo = new Mock<ITodoRepository>(MockBehavior.Strict);
        repo.Setup(r => r.GetByIdAsync(existing.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        var service = new TodoService(repo.Object, _logger);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            service.UpdateAsync(existing.Id, new UpdateTodoRequest(null, null, null, null)));

        repo.Verify(r => r.GetByIdAsync(existing.Id, It.IsAny<CancellationToken>()), Times.Once);
        repo.Verify(r => r.UpdateAsync(It.IsAny<TodoItem>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ListAsync_WithFilters_ReturnsExpectedItems()
    {
        // Arrange
        var overdueItem = new TodoItem
        {
            Title = "done",
            DueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1),
            IsCompleted = false,
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };
        var completedItem = new TodoItem
        {
            Title = "remaining",
            DueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(5),
            IsCompleted = true,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };
        var repo = new Mock<ITodoRepository>(MockBehavior.Strict);
        repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TodoItem> { overdueItem, completedItem });
        var service = new TodoService(repo.Object, _logger);

        // Act
        var overdue = await service.ListAsync(
            isCompleted: false,
            overdue: true,
            dueBefore: null,
            dueAfter: null,
            sortBy: TodoSortField.DueDate,
            sortOrder: SortOrder.Asc,
            page: 1,
            pageSize: 10);

        // Assert
        repo.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.Single(overdue.Items);
        Assert.Equal("done", overdue.Items[0].Title);
        Assert.Equal(1, overdue.TotalCount);
        Assert.Equal(1, overdue.TotalPages);
    }

    [Fact]
    public async Task GetAsync_WhenItemExists_ReturnsItem()
    {
        // Arrange
        var existing = new TodoItem { Id = Guid.NewGuid(), Title = "existing" };
        var repo = new Mock<ITodoRepository>(MockBehavior.Strict);
        repo.Setup(r => r.GetByIdAsync(existing.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        var service = new TodoService(repo.Object, _logger);

        // Act
        var result = await service.GetAsync(existing.Id);

        // Assert
        Assert.Equal(existing, result);
        repo.Verify(r => r.GetByIdAsync(existing.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenItemMissing_ReturnsNull()
    {
        // Arrange
        var repo = new Mock<ITodoRepository>(MockBehavior.Strict);
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TodoItem?)null);
        var service = new TodoService(repo.Object, _logger);

        // Act
        var result = await service.UpdateAsync(Guid.NewGuid(), new UpdateTodoRequest("noop", null, null, null));

        // Assert
        Assert.Null(result);
        repo.Verify(r => r.UpdateAsync(It.IsAny<TodoItem>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SetCompletedAsync_WithExistingItem_UpdatesCompletion()
    {
        // Arrange
        var existing = new TodoItem { Id = Guid.NewGuid(), IsCompleted = false };
        var repo = new Mock<ITodoRepository>(MockBehavior.Strict);
        repo.Setup(r => r.GetByIdAsync(existing.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        repo.Setup(r => r.UpdateAsync(It.IsAny<TodoItem>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TodoItem item, CancellationToken _) => item);
        var service = new TodoService(repo.Object, _logger);

        // Act
        var result = await service.SetCompletedAsync(existing.Id, true);

        // Assert
        Assert.NotNull(result);
        Assert.True(result!.IsCompleted);
        repo.Verify(r => r.UpdateAsync(It.Is<TodoItem>(t => t.IsCompleted), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetCompletedAsync_WhenItemMissing_ReturnsNull()
    {
        // Arrange
        var repo = new Mock<ITodoRepository>(MockBehavior.Strict);
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TodoItem?)null);
        var service = new TodoService(repo.Object, _logger);

        // Act
        var result = await service.SetCompletedAsync(Guid.NewGuid(), true);

        // Assert
        Assert.Null(result);
        repo.Verify(r => r.UpdateAsync(It.IsAny<TodoItem>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WhenItemExists_ReturnsTrue()
    {
        // Arrange
        var repo = new Mock<ITodoRepository>(MockBehavior.Strict);
        repo.Setup(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var service = new TodoService(repo.Object, _logger);

        // Act
        var result = await service.DeleteAsync(Guid.NewGuid());

        // Assert
        Assert.True(result);
        repo.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenItemMissing_ReturnsFalse()
    {
        // Arrange
        var repo = new Mock<ITodoRepository>(MockBehavior.Strict);
        repo.Setup(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var service = new TodoService(repo.Object, _logger);

        // Act
        var result = await service.DeleteAsync(Guid.NewGuid());

        // Assert
        Assert.False(result);
        repo.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

