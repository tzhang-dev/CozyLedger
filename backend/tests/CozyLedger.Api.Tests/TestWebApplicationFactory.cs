using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using CozyLedger.Infrastructure.Data;

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
                ["Jwt:Issuer"] = "CozyLedger",
                ["Jwt:Audience"] = "CozyLedger",
                ["Jwt:Key"] = "dev-secret-key-change-this-is-32bytes",
                ["Jwt:ExpiryMinutes"] = "30",
                ["AttachmentStorage:RootPath"] = _storageRoot
            };

            config.AddInMemoryCollection(overrides);
        });
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options => options.UseNpgsql(_connectionString));
        });

    }
}
