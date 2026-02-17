using CozyLedger.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace CozyLedger.Api.Tests;

/// <summary>
/// Manages a PostgreSQL container lifecycle for integration tests.
/// </summary>
public sealed class PostgresContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("cozyledger_test")
        .WithUsername("cozy")
        .WithPassword("cozy")
        .Build();

    /// <summary>
    /// Gets the connection string exposed by the running test container.
    /// </summary>
    public string ConnectionString => _container.GetConnectionString();

    /// <summary>
    /// Starts the PostgreSQL container before tests run.
    /// </summary>
    /// <returns>A task that completes when the container is ready.</returns>
    public Task InitializeAsync() => _container.StartAsync();

    /// <summary>
    /// Disposes the PostgreSQL container after tests complete.
    /// </summary>
    /// <returns>A task that completes when container resources are released.</returns>
    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}

/// <summary>
/// Verifies database connectivity and schema creation for the EF context.
/// </summary>
public class DbConnectionTests : IClassFixture<PostgresContainerFixture>
{
    private readonly string _connectionString;

    /// <summary>
    /// Initializes a new instance of the <see cref="DbConnectionTests"/> class.
    /// </summary>
    /// <param name="fixture">Shared PostgreSQL container fixture.</param>
    public DbConnectionTests(PostgresContainerFixture fixture)
    {
        _connectionString = fixture.ConnectionString;
    }

    /// <summary>
    /// Ensures the database schema can be created and the connection is usable.
    /// </summary>
    /// <returns>A task that completes when assertions are validated.</returns>
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
