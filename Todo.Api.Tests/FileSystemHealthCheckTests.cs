using Microsoft.Extensions.Diagnostics.HealthChecks;
using Todo.Api.HealthChecks;
using Xunit;

namespace Todo.Api.Tests;

public class FileSystemHealthCheckTests : IDisposable
{
    private readonly string _tempPath = Path.Combine(Path.GetTempPath(), $"todo-health-{Guid.NewGuid():N}", "todos.json");

    [Fact]
    public async Task CheckHealthAsync_WithWritableDirectory_ReturnsHealthy()
    {
        // Arrange
        var check = new FileSystemHealthCheck(_tempPath);

        // Act
        var result = await check.CheckHealthAsync(new HealthCheckContext());

        // Assert
        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_WithInvalidDirectory_ReturnsUnhealthy()
    {
        // Arrange
        var check = new FileSystemHealthCheck("todos.json");

        // Act
        var result = await check.CheckHealthAsync(new HealthCheckContext());

        // Assert
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Contains("invalid", result.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenDataFileLocked_ReturnsDegraded()
    {
        // Arrange
        var directory = Path.GetDirectoryName(_tempPath)!;
        Directory.CreateDirectory(directory);
        File.WriteAllText(_tempPath, "[]");
        await using var lockStream = new FileStream(_tempPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        var check = new FileSystemHealthCheck(_tempPath);

        // Act
        var result = await check.CheckHealthAsync(new HealthCheckContext());

        // Assert
        Assert.Equal(HealthStatus.Degraded, result.Status);
    }

    public void Dispose()
    {
        try
        {
            var root = Path.GetDirectoryName(_tempPath);
            if (!string.IsNullOrEmpty(root) && Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
        catch
        {
            // best effort cleanup
        }
    }
}

