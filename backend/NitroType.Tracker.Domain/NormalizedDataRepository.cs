using Microsoft.Extensions.Logging;
using Npgsql;

namespace NitroType.Tracker.Domain;

public sealed class NormalizedDataRepository
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<NormalizedDataRepository> _logger;

    public NormalizedDataRepository(
        NpgsqlDataSource dataSource,
        ILogger<NormalizedDataRepository> logger)
    {
        _dataSource = dataSource;
        _logger = logger;

        InitializeDatabaseAsync().GetAwaiter().GetResult();
    }

    private async Task InitializeDatabaseAsync()
    {
        _logger.LogInformation("Initializing normalized data table if needed");

        var connection = await _dataSource.OpenConnectionAsync()
            .ConfigureAwait(false);

        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS ""normalized_data"" (
                ""id"" BIGSERIAL PRIMARY KEY,
                ""username"" VARCHAR(50) NOT NULL,
                ""team"" VARCHAR(50) NOT NULL,
                ""typed"" BIGINT NOT NULL,
                ""errors"" BIGINT NOT NULL,
                ""name"" VARCHAR(100) NOT NULL,
                ""races_played"" INT NOT NULL,
                ""timestamp"" TIMESTAMP NOT NULL,
                ""secs"" BIGINT NOT NULL
            );
            
            CREATE INDEX IF NOT EXISTS idx_normalized_data_team ON normalized_data(team);
            CREATE INDEX IF NOT EXISTS idx_normalized_data_timestamp ON normalized_data(timestamp);
            CREATE INDEX IF NOT EXISTS idx_normalized_data_username ON normalized_data(username);
            CREATE UNIQUE INDEX IF NOT EXISTS idx_normalized_data_username_timestamp 
                ON normalized_data(username, timestamp);";

        try
        {
            await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            _logger.LogInformation("Successfully initialized the normalized data table if was needed");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to initialize the normalized data table");
            throw;
        }
        finally
        {
            await cmd.DisposeAsync().ConfigureAwait(false);
            await connection.DisposeAsync().ConfigureAwait(false);
        }
    }

    public async ValueTask SaveAsync(NormalizedPlayerData data)
    {
        _logger.LogDebug("Saving normalized data for user {Username} in team {Team}", data.Username, data.Team);

        var connection = await _dataSource.OpenConnectionAsync().ConfigureAwait(false);
        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO ""normalized_data"" 
            (""username"", ""team"", ""typed"", ""errors"", ""name"", ""races_played"", ""timestamp"", ""secs"")
            VALUES (@username, @team, @typed, @errors, @name, @racesPlayed, @timestamp, @secs)
            ON CONFLICT (username, timestamp) DO NOTHING;";

        cmd.Parameters.AddWithValue("@username", data.Username);
        cmd.Parameters.AddWithValue("@team", data.Team);
        cmd.Parameters.AddWithValue("@typed", data.Typed);
        cmd.Parameters.AddWithValue("@errors", data.Errors);
        cmd.Parameters.AddWithValue("@name", data.Name);
        cmd.Parameters.AddWithValue("@racesPlayed", data.RacesPlayed);
        cmd.Parameters.AddWithValue("@timestamp", data.Timestamp);
        cmd.Parameters.AddWithValue("@secs", data.Secs);

        try
        {
            await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            _logger.LogDebug("Successfully saved normalized data for user {Username}", data.Username);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to save normalized data for user {Username}", data.Username);
            throw;
        }
        finally
        {
            await cmd.DisposeAsync().ConfigureAwait(false);
            await connection.DisposeAsync().ConfigureAwait(false);
        }
    }
}
