using System.IO;
using Todo.Api.Domain;
using Todo.Api.Persistence;
using Xunit;

namespace Todo.Api.Tests;

public class DataStoreTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), $"todo-store-tests-{Guid.NewGuid():N}");

    [Fact]
    public void Ctor_WithMalformedJson_Throws()
    {
        Directory.CreateDirectory(_tempDirectory);
        var filePath = Path.Combine(_tempDirectory, "todos.json");
        File.WriteAllText(filePath, "{ not valid json");

        var ex = Assert.Throws<InvalidOperationException>(() => new DataStore(filePath));
        Assert.Contains("Failed to parse persisted data", ex.Message);
    }

    [Fact]
    public async Task Persist_WhenFileLocked_ThrowsIOException()
    {
        Directory.CreateDirectory(_tempDirectory);
        var filePath = Path.Combine(_tempDirectory, "todos.json");
        File.WriteAllText(filePath, "[]");

        var store = new DataStore(filePath);
        await using var handle = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

        var item = new TodoItem { Title = "locked" };

        await Assert.ThrowsAsync<IOException>(() => store.AddAsync(item));
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, recursive: true);
            }
        }
        catch
        {
            // best effort cleanup
        }
    }
}

