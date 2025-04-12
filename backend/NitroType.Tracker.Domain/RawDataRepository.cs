using Microsoft.Extensions.Logging;
using Npgsql;

namespace NitroType.Tracker.Domain;

public sealed class RawDataRepository
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<RawDataRepository> _logger;

    public RawDataRepository(
        NpgsqlDataSource dataSource,
        ILogger<RawDataRepository> logger)
    {
        _dataSource = dataSource;
        _logger = logger;

        _ = InitializeDatabaseAsync();
    }

    private async Task InitializeDatabaseAsync()
    {
        _logger.LogInformation("Initializing database if needed");

        // TODO: Create the database itself if not created. Or use migrations.
        var connection = await _dataSource.OpenConnectionAsync()
            .ConfigureAwait(false);

        var cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE IF NOT EXISTS \"raw_data\" (\"team\" VARCHAR(50), \"data\" VARCHAR, \"timestamp\" timestamp);";

        var cmd2 = connection.CreateCommand();
        cmd2.CommandText = "ALTER TABLE \"raw_data\" ADD COLUMN IF NOT EXISTS \"id\" BIGSERIAL PRIMARY KEY;";

        try
        {
            await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            await cmd2.ExecuteNonQueryAsync().ConfigureAwait(false);
            _logger.LogInformation("Successfully initialized the database if was needed");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to initialize the database");
            throw;
        }
        finally
        {
            await cmd2.DisposeAsync().ConfigureAwait(false);
            await cmd.DisposeAsync().ConfigureAwait(false);
            await connection.DisposeAsync().ConfigureAwait(false);
        }
    }

    public async ValueTask SaveAsync(string team, string data)
    {
        _logger.LogDebug("Saving the data to the database for {Team} team, data length is {Length}", team, data.Length);
        var connection = await _dataSource.OpenConnectionAsync().ConfigureAwait(false);
        var cmd = connection.CreateCommand();
        cmd.CommandText = "INSERT INTO \"raw_data\" (\"team\", \"data\", \"timestamp\") VALUES (@team, @data, @timestamp);";
        cmd.Parameters.AddWithValue("@team", team);
        cmd.Parameters.AddWithValue("@data", data);
        cmd.Parameters.AddWithValue("@timestamp", DateTime.UtcNow);

        try
        {
            await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            _logger.LogDebug("Successfully saved the data for team {Team}", team);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to save the data for team {Team}", team);
            throw;
        }
        finally
        {
            await cmd.DisposeAsync().ConfigureAwait(false);
            await connection.DisposeAsync().ConfigureAwait(false);
        }
    }
}
