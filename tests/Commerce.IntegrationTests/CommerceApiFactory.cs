using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;

namespace Commerce.IntegrationTests;

public sealed class CommerceApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string ConnectionStringEnvironmentVariableName = "ConnectionStrings__CommerceDatabase";

    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder("postgres:16-alpine")
        .Build();

    private string? _previousConnectionStringValue;

    public async Task InitializeAsync()
    {
        _previousConnectionStringValue = Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariableName);

        try
        {
            await _postgresContainer.StartAsync();

            // ConfigureWebHost config only applies once the real Build() call is intercepted,
            // which is after Program.cs already validates the connection string. An env var
            // set here is visible to CreateBuilder's default sources before that check runs.
            Environment.SetEnvironmentVariable(
                ConnectionStringEnvironmentVariableName,
                _postgresContainer.GetConnectionString());
        }
        catch
        {
            Environment.SetEnvironmentVariable(ConnectionStringEnvironmentVariableName, _previousConnectionStringValue);
            await _postgresContainer.DisposeAsync();
            throw;
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        try
        {
            await base.DisposeAsync();
        }
        finally
        {
            try
            {
                await _postgresContainer.DisposeAsync();
            }
            finally
            {
                Environment.SetEnvironmentVariable(ConnectionStringEnvironmentVariableName, _previousConnectionStringValue);
            }
        }
    }
}
