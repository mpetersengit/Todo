using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Todo.Api.HealthChecks;

public class FileSystemHealthCheck : IHealthCheck
{
    private readonly string _dataPath;

    public FileSystemHealthCheck(string dataPath)
    {
        _dataPath = dataPath;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var directory = Path.GetDirectoryName(_dataPath);
            if (string.IsNullOrEmpty(directory))
            {
                return Task.FromResult(HealthCheckResult.Unhealthy("Data directory path is invalid"));
            }

            if (!Directory.Exists(directory))
            {
                try
                {
                    Directory.CreateDirectory(directory);
                }
                catch (Exception ex)
                {
                    return Task.FromResult(HealthCheckResult.Unhealthy($"Cannot create data directory: {ex.Message}"));
                }
            }

            // Test write access
            var testFile = Path.Combine(directory, $"health-check-{Guid.NewGuid():N}.tmp");
            try
            {
                File.WriteAllText(testFile, "health check");
                File.Delete(testFile);
            }
            catch (Exception ex)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy($"Data directory is not writable: {ex.Message}"));
            }

            // Check if data file exists and is readable (if it exists)
            if (File.Exists(_dataPath))
            {
                try
                {
                    File.ReadAllText(_dataPath);
                }
                catch (Exception ex)
                {
                    return Task.FromResult(HealthCheckResult.Degraded($"Data file exists but is not readable: {ex.Message}"));
                }
            }

            return Task.FromResult(HealthCheckResult.Healthy("File system is accessible and writable"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy($"File system health check failed: {ex.Message}", ex));
        }
    }
}

