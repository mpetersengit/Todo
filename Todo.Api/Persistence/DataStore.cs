using System.Text.Json;
using Todo.Api.Domain;

namespace Todo.Api.Persistence;

public class DataStore
{
    private readonly string _filePath;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly SemaphoreSlim _mutex = new(1, 1);
    private List<TodoItem> _cache = new();

    public DataStore(string filePath)
    {
        _filePath = filePath;
        var directory = Path.GetDirectoryName(_filePath) ?? throw new InvalidOperationException("Data file path is invalid.");
        Directory.CreateDirectory(directory);
        LoadExistingData();
    }

    public async Task<IReadOnlyList<TodoItem>> ReadAllAsync(CancellationToken ct = default)
    {
        await _mutex.WaitAsync(ct);
        try
        {
            return _cache.Select(Clone).ToList();
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task<TodoItem?> ReadByIdAsync(Guid id, CancellationToken ct = default)
    {
        await _mutex.WaitAsync(ct);
        try
        {
            var item = _cache.FirstOrDefault(x => x.Id == id);
            return item is null ? null : Clone(item);
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task<TodoItem> AddAsync(TodoItem item, CancellationToken ct = default)
    {
        await _mutex.WaitAsync(ct);
        try
        {
            var stored = Clone(item);
            _cache.Add(stored);
            await PersistAsync(ct);
            return Clone(stored);
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task<TodoItem?> UpdateAsync(TodoItem item, CancellationToken ct = default)
    {
        await _mutex.WaitAsync(ct);
        try
        {
            var idx = _cache.FindIndex(x => x.Id == item.Id);
            if (idx < 0)
            {
                return null;
            }

            var stored = Clone(item);
            _cache[idx] = stored;
            await PersistAsync(ct);
            return Clone(stored);
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task<bool> RemoveAsync(Guid id, CancellationToken ct = default)
    {
        await _mutex.WaitAsync(ct);
        try
        {
            var removed = _cache.RemoveAll(x => x.Id == id) > 0;
            if (removed)
            {
                await PersistAsync(ct);
            }
            return removed;
        }
        finally
        {
            _mutex.Release();
        }
    }

    private void LoadExistingData()
    {
        if (!File.Exists(_filePath))
        {
            PersistSync();
            return;
        }

        try
        {
            var json = File.ReadAllText(_filePath);
            var data = JsonSerializer.Deserialize<List<TodoItem>>(json, _jsonOptions);
            _cache = data ?? new List<TodoItem>();
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Failed to parse persisted data at '{_filePath}'.", ex);
        }
        catch (IOException ex)
        {
            throw new InvalidOperationException($"Unable to read data file '{_filePath}'.", ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new InvalidOperationException($"Access to data file '{_filePath}' was denied.", ex);
        }
    }

    private async Task PersistAsync(CancellationToken ct)
    {
        var directory = Path.GetDirectoryName(_filePath)!;
        var tempFile = Path.Combine(directory, $"{Path.GetFileName(_filePath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            var json = JsonSerializer.Serialize(_cache, _jsonOptions);
            await File.WriteAllTextAsync(tempFile, json, ct);
            File.Copy(tempFile, _filePath, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new IOException($"Failed to persist data to '{_filePath}'.", ex);
        }
        finally
        {
            TryDelete(tempFile);
        }
    }

    private void PersistSync()
    {
        var directory = Path.GetDirectoryName(_filePath)!;
        var tempFile = Path.Combine(directory, $"{Path.GetFileName(_filePath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            var json = JsonSerializer.Serialize(_cache, _jsonOptions);
            File.WriteAllText(tempFile, json);
            File.Copy(tempFile, _filePath, overwrite: true);
        }
        finally
        {
            TryDelete(tempFile);
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
            // Ignore cleanup failures to avoid masking primary errors.
        }
    }

    private static TodoItem Clone(TodoItem item) => new()
    {
        Id = item.Id,
        Title = item.Title,
        Description = item.Description,
        DueDate = item.DueDate,
        IsCompleted = item.IsCompleted,
        CreatedAt = item.CreatedAt
    };
}
