using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace CozyLedger.Api.Tests;

public sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    public TestWebApplicationFactory(string connectionString)
    {
        _connectionString = connectionString;
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
                ["Jwt:ExpiryMinutes"] = "30"
            };

            config.AddInMemoryCollection(overrides);
        });

    }
}
