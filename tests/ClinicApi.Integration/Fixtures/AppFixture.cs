using Alba;
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

        Host = await AlbaHost.For<Program>(builder =>
        {
            builder.ConfigureServices((context, services) =>
            {
                context.Configuration["ConnectionStrings:ClinicDb"] = MssqlConnectionString + ";Database=ClinicDb;TrustServerCertificate=True;";
                context.Configuration["ConnectionStrings:RabbitMq"] = RabbitMqConnectionString;
            });
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
