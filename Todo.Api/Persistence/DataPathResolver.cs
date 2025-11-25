using Microsoft.Extensions.Configuration;

namespace Todo.Api.Persistence;

public static class DataPathResolver
{
    public static string ResolveDataPath(IConfiguration configuration)
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

    public static void EnsureWritable(string filePath)
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
            TryDelete(probePath);
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // ignore cleanup failures
        }
    }
}

