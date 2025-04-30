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
    }

    public async ValueTask SaveAsync(string team, string data)
    {
        _logger.LogDebug("Saving the data to the database for {Team} team, data length is {Length}", team, data.Length);
        var connection = await _dataSource.OpenConnectionAsync().ConfigureAwait(false);
        var cmd = connection.CreateCommand();

        try
        {
            cmd.CommandText = """
                INSERT INTO raw_data (team, data, timestamp)
                VALUES (@team, @data, @timestamp);
                """;

            cmd.Parameters.AddWithValue("@team", team);
            cmd.Parameters.AddWithValue("@data", data);
            cmd.Parameters.AddWithValue("@timestamp", DateTime.UtcNow);

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
