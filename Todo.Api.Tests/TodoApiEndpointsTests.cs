using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Todo.Api.Contracts;
using Xunit;
using Microsoft.Extensions.Configuration;

namespace Todo.Api.Tests;

public class TodoApiEndpointsTests : IClassFixture<TodoApiFactory>
{
    private readonly HttpClient _client;

    public TodoApiEndpointsTests(TodoApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateTodo_ReturnsCreatedResponse()
    {
        var response = await _client.PostAsJsonAsync("/todos", new CreateTodoRequest("api task", null, null));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<TodoResponse>();
        Assert.NotNull(payload);
        Assert.Equal("api task", payload!.Title);
        Assert.False(payload.IsCompleted);
    }

    [Fact]
    public async Task ListTodos_WithCompletionFilter_ReturnsMatchingItems()
    {
        var created = await _client.PostAsJsonAsync("/todos", new CreateTodoRequest("complete me", null, null));
        var todo = await created.Content.ReadFromJsonAsync<TodoResponse>();
        Assert.NotNull(todo);

        await _client.PatchAsync($"/todos/{todo!.Id}/complete", content: null);

        var filtered = await _client.GetFromJsonAsync<PaginatedResponse<TodoResponse>>("/todos?isCompleted=true");

        Assert.NotNull(filtered);
        Assert.Contains(filtered!.Items, t => t.Id == todo.Id);
    }

    [Fact]
    public async Task ListTodos_WithPagination_ReturnsPaginatedResponse()
    {
        // Create multiple todos
        for (int i = 0; i < 15; i++)
        {
            await _client.PostAsJsonAsync("/todos", new CreateTodoRequest($"Pagination Task {i}", null, null));
        }

        var firstPage = await _client.GetFromJsonAsync<PaginatedResponse<TodoResponse>>("/todos?page=1&pageSize=10");
        var secondPage = await _client.GetFromJsonAsync<PaginatedResponse<TodoResponse>>("/todos?page=2&pageSize=10");

        Assert.NotNull(firstPage);
        Assert.True(firstPage!.Items.Count <= 10);
        Assert.True(firstPage.TotalCount >= 15);
        Assert.True(firstPage.TotalPages >= 2);
        Assert.True(firstPage.HasNextPage || firstPage.TotalPages == 1);

        Assert.NotNull(secondPage);
        Assert.True(secondPage!.TotalCount >= 15);
        Assert.True(secondPage.TotalPages >= 2);
        Assert.True(secondPage.HasPreviousPage);
        
        // Verify pagination metadata is correct
        Assert.Equal(secondPage.TotalCount, firstPage.TotalCount);
        Assert.Equal(secondPage.TotalPages, firstPage.TotalPages);
    }
}

public class TodoApiFactory : WebApplicationFactory<Program>
{
    private readonly string _tempFile = Path.Combine(Path.GetTempPath(), $"todo-api-tests-{Guid.NewGuid()}.json");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            var settings = new Dictionary<string, string?>
            {
                ["Data:Path"] = _tempFile
            };
            config.AddInMemoryCollection(settings);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && File.Exists(_tempFile))
        {
            File.Delete(_tempFile);
        }
    }
}

