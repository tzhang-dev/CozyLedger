using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace CozyLedger.Api.Tests;

public sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;
    private readonly string _storageRoot;

    public TestWebApplicationFactory(string connectionString, string? storageRoot = null)
    {
        _connectionString = connectionString;
        _storageRoot = storageRoot ?? Path.Combine(Path.GetTempPath(), "cozyledger-tests", Guid.NewGuid().ToString("N"));
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            var overrides = new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = _connectionString,
                ["Jwt:Issuer"] = "CozyLedger.Test",
                ["Jwt:Audience"] = "CozyLedger.Test",
                ["Jwt:Key"] = "test-key-for-cozyledger-32bytes",
                ["Jwt:ExpiryMinutes"] = "30",
                ["AttachmentStorage:RootPath"] = _storageRoot
            };

            config.AddInMemoryCollection(overrides);
        });

    }
}
