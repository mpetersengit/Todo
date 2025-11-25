using System;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Todo.Api.Contracts;
using Xunit;

namespace Todo.Api.Tests;

public sealed class TodoApiEndpointsTests : IDisposable
{
    private readonly TodoApiFactory _factory;
    private readonly HttpClient _client;

    public TodoApiEndpointsTests()
    {
        _factory = new TodoApiFactory();
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task CreateTodo_ReturnsCreatedResponse()
    {
        // Arrange
        var request = new CreateTodoRequest("api task", null, null, null);

        // Act
        var response = await _client.PostAsJsonAsync("/todos", request);
        var payload = await response.Content.ReadFromJsonAsync<TodoResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal("api task", payload!.Title);
        Assert.False(payload.IsCompleted);
    }

    [Fact]
    public async Task ListTodos_WithCompletionFilter_ReturnsMatchingItems()
    {
        // Arrange
        var created = await CreateTodoAsync("complete me");

        // Act
        await _client.PatchAsync($"/todos/{created.Id}/complete", content: null);
        var filtered = await _client.GetFromJsonAsync<PaginatedResponse<TodoResponse>>("/todos?isCompleted=true");

        // Assert
        Assert.NotNull(filtered);
        Assert.Contains(filtered!.Items, t => t.Id == created.Id);
    }

    [Fact]
    public async Task ListTodos_WithPagination_ReturnsPaginatedResponse()
    {
        // Arrange
        for (int i = 0; i < 15; i++)
        {
            await _client.PostAsJsonAsync("/todos", new CreateTodoRequest($"Pagination Task {i}", null, null, null));
        }

        // Act
        var firstPage = await _client.GetFromJsonAsync<PaginatedResponse<TodoResponse>>("/todos?page=1&pageSize=10");
        var secondPage = await _client.GetFromJsonAsync<PaginatedResponse<TodoResponse>>("/todos?page=2&pageSize=10");

        // Assert
        Assert.NotNull(firstPage);
        Assert.True(firstPage!.Items.Count <= 10);
        Assert.True(firstPage.TotalCount >= 15);
        Assert.True(firstPage.TotalPages >= 2);
        Assert.True(firstPage.HasNextPage || firstPage.TotalPages == 1);

        Assert.NotNull(secondPage);
        Assert.True(secondPage!.TotalCount >= 15);
        Assert.True(secondPage.TotalPages >= 2);
        Assert.True(secondPage.HasPreviousPage);

        Assert.Equal(secondPage.TotalCount, firstPage.TotalCount);
        Assert.Equal(secondPage.TotalPages, firstPage.TotalPages);
    }

    [Fact]
    public async Task GetTodo_WithExistingId_ReturnsItem()
    {
        // Arrange
        var created = await CreateTodoAsync("fetch me");

        // Act
        var response = await _client.GetAsync($"/todos/{created.Id}");
        var payload = await response.Content.ReadFromJsonAsync<TodoResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal(created.Id, payload!.Id);
    }

    [Fact]
    public async Task GetTodo_WhenNotFound_Returns404()
    {
        // Arrange
        var missingId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/todos/{missingId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateTodo_WithValidPayload_ReturnsUpdatedItem()
    {
        // Arrange
        var created = await CreateTodoAsync("updatable");
        var request = new UpdateTodoRequest("updated", "desc", "2030-01-01", true);

        // Act
        var response = await _client.PutAsJsonAsync($"/todos/{created.Id}", request);
        var payload = await response.Content.ReadFromJsonAsync<TodoResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal("updated", payload!.Title);
        Assert.Equal("desc", payload.Description);
        Assert.True(payload.IsCompleted);
    }

    [Fact]
    public async Task UpdateTodo_WhenItemMissing_Returns404()
    {
        // Arrange
        var request = new UpdateTodoRequest("missing", null, null, null);

        // Act
        var response = await _client.PutAsJsonAsync($"/todos/{Guid.NewGuid()}", request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CompleteEndpoints_ToggleCompletionState()
    {
        // Arrange
        var created = await CreateTodoAsync("toggle me");

        // Act
        var completeResponse = await _client.PatchAsync($"/todos/{created.Id}/complete", content: null);
        var completed = await completeResponse.Content.ReadFromJsonAsync<TodoResponse>();

        var incompleteResponse = await _client.PatchAsync($"/todos/{created.Id}/incomplete", content: null);
        var incomplete = await incompleteResponse.Content.ReadFromJsonAsync<TodoResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, completeResponse.StatusCode);
        Assert.NotNull(completed);
        Assert.True(completed!.IsCompleted);

        Assert.Equal(HttpStatusCode.OK, incompleteResponse.StatusCode);
        Assert.NotNull(incomplete);
        Assert.False(incomplete!.IsCompleted);
    }

    [Fact]
    public async Task DeleteTodo_WithExistingItem_RemovesItem()
    {
        // Arrange
        var created = await CreateTodoAsync("remove me");

        // Act
        var deleteResponse = await _client.DeleteAsync($"/todos/{created.Id}");
        var fetchResponse = await _client.GetAsync($"/todos/{created.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, fetchResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteTodo_WhenMissing_Returns404()
    {
        // Arrange
        var missingId = Guid.NewGuid();

        // Act
        var response = await _client.DeleteAsync($"/todos/{missingId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task RootEndpoint_RedirectsToSwagger()
    {
        // Arrange
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        var response = await client.GetAsync("/");

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/swagger", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task HealthEndpoints_ReturnHealthyStatuses()
    {
        // Arrange
        // Act
        var health = await _client.GetAsync("/health");
        var ready = await _client.GetAsync("/health/ready");
        var live = await _client.GetAsync("/health/live");

        // Assert
        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
        Assert.Equal(HttpStatusCode.OK, ready.StatusCode);
        Assert.Equal(HttpStatusCode.OK, live.StatusCode);
    }

    private async Task<TodoResponse> CreateTodoAsync(string title)
    {
        var response = await _client.PostAsJsonAsync("/todos", new CreateTodoRequest(title, null, null, null));
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<TodoResponse>();
        return payload!;
    }
}

