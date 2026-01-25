using CozyLedger.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace CozyLedger.Api.Tests;

public sealed class PostgresContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("cozyledger_test")
        .WithUsername("cozy")
        .WithPassword("cozy")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}

public class DbConnectionTests : IClassFixture<PostgresContainerFixture>
{
    private readonly string _connectionString;

    public DbConnectionTests(PostgresContainerFixture fixture)
    {
        _connectionString = fixture.ConnectionString;
    }

    [Fact]
    public async Task Can_connect_and_create_schema()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_connectionString)
            .Options;

        await using var context = new AppDbContext(options);
        var created = await context.Database.EnsureCreatedAsync();
        var canConnect = await context.Database.CanConnectAsync();

        created.Should().BeTrue();
        canConnect.Should().BeTrue();
    }
}
