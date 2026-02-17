using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using CozyLedger.Infrastructure.Data;

namespace CozyLedger.Api.Tests;

/// <summary>
/// Test host factory that overrides configuration and database registration for integration tests.
/// </summary>
public sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;
    private readonly string _storageRoot;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestWebApplicationFactory"/> class.
    /// </summary>
    /// <param name="connectionString">Connection string used by test database context.</param>
    /// <param name="storageRoot">Optional root directory for attachment storage.</param>
    public TestWebApplicationFactory(string connectionString, string? storageRoot = null)
    {
        _connectionString = connectionString;
        _storageRoot = storageRoot ?? Path.Combine(Path.GetTempPath(), "cozyledger-tests", Guid.NewGuid().ToString("N"));
    }

    /// <summary>
    /// Configures the test web host with in-memory settings and test database registration.
    /// </summary>
    /// <param name="builder">Web host builder used by the test server.</param>
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
