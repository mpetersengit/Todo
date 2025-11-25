using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Todo.Api.Tests;

public class TodoApiFactory : WebApplicationFactory<Program>
{
    private readonly string _tempFile = Path.Combine(Path.GetTempPath(), $"todo-api-tests-{Guid.NewGuid()}.json");

    public TodoApiFactory()
    {
        Environment.SetEnvironmentVariable("TODOAPI_DATA__PATH", _tempFile);
    }

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

        if (disposing)
        {
            Environment.SetEnvironmentVariable("TODOAPI_DATA__PATH", value: null);
            if (File.Exists(_tempFile))
            {
                File.Delete(_tempFile);
            }
        }
    }
}

