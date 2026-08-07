using Alba;
using ClinicApi.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;
using Testcontainers.RabbitMq;

namespace ClinicApi.Integration.Fixtures;

/// <summary>
/// Shared test fixture using Testcontainers for real MSSQL and RabbitMQ.
/// </summary>
public class AppFixture : IAsyncLifetime
{
    private MsSqlContainer _mssqlContainer = null!;
    private RabbitMqContainer _rabbitMqContainer = null!;

    public IAlbaHost Host { get; private set; } = null!;
    public string MssqlConnectionString => _mssqlContainer.GetConnectionString();
    public string RabbitMqConnectionString => _rabbitMqContainer.GetConnectionString();

    public async Task InitializeAsync()
    {
        _mssqlContainer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword("YourStrong@Passw0rd")
            .Build();

        _rabbitMqContainer = new RabbitMqBuilder("rabbitmq:4-management")
            .Build();

        await Task.WhenAll(
            _mssqlContainer.StartAsync(),
            _rabbitMqContainer.StartAsync());

        // Create the ClinicDb database in the container
        await using var connection = new SqlConnection(MssqlConnectionString + ";TrustServerCertificate=True;");
        await connection.OpenAsync();
        await using var cmd = new SqlCommand("CREATE DATABASE ClinicDb;", connection);
        await cmd.ExecuteNonQueryAsync();
        await connection.CloseAsync();

        // Create EF Core schema before the app host starts
        var clinicConnStr = MssqlConnectionString + ";Database=ClinicDb;TrustServerCertificate=True;";
        var optionsBuilder = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<ClinicDbContext>();
        optionsBuilder.UseSqlServer(clinicConnStr);
        await using var dbContext = new ClinicDbContext(optionsBuilder.Options);
        await dbContext.Database.EnsureCreatedAsync();

        Host = await AlbaHost.For<Program>(builder =>
        {
            builder.UseSetting("ConnectionStrings:ClinicDb", clinicConnStr);
            builder.UseSetting("ConnectionStrings:RabbitMq", RabbitMqConnectionString);
        });
    }

    public async Task DisposeAsync()
    {
        if (Host is not null) await Host.DisposeAsync();
        await _mssqlContainer.DisposeAsync();
        await _rabbitMqContainer.DisposeAsync();
    }
}

[CollectionDefinition("Integration")]
public class IntegrationTestCollection : ICollectionFixture<AppFixture>
{
}
