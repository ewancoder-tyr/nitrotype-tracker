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
        _logger.LogInformation("Initializing normalized data tables if needed");

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

        CREATE TABLE IF NOT EXISTS ""processing_state"" (
            ""id"" INT PRIMARY KEY DEFAULT 1,  -- We'll only ever have one row
            ""last_processed_id"" BIGINT NOT NULL DEFAULT 0,
            ""last_updated"" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
        );
        
        -- Insert initial record if doesn't exist
        INSERT INTO ""processing_state"" (""id"", ""last_processed_id"")
        VALUES (1, 0)
        ON CONFLICT (id) DO NOTHING;

        CREATE INDEX IF NOT EXISTS idx_normalized_data_team ON normalized_data(team);
        CREATE INDEX IF NOT EXISTS idx_normalized_data_timestamp ON normalized_data(timestamp);
        CREATE INDEX IF NOT EXISTS idx_normalized_data_username ON normalized_data(username);
        CREATE UNIQUE INDEX IF NOT EXISTS idx_normalized_data_username_timestamp 
            ON normalized_data(username, timestamp);";

        try
        {
            await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            _logger.LogInformation("Database initialization completed successfully");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to initialize database");
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
        //_logger.LogTrace("Saving normalized data for user {Username} in team {Team}", data.Username, data.Team);

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
            //_logger.LogTrace("Successfully saved normalized data for user {Username}", data.Username);
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

    public async Task<long> GetLastProcessedIdAsync()
    {
        var connection = await _dataSource.OpenConnectionAsync().ConfigureAwait(false);
        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT last_processed_id FROM processing_state WHERE id = 1;";

        try
        {
            var lastId = await cmd.ExecuteScalarAsync().ConfigureAwait(false);
            return (long)lastId;
        }
        finally
        {
            await cmd.DisposeAsync().ConfigureAwait(false);
            await connection.DisposeAsync().ConfigureAwait(false);
        }
    }

    public async Task UpdateLastProcessedIdAsync(long newId)
    {
        var connection = await _dataSource.OpenConnectionAsync().ConfigureAwait(false);
        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
        UPDATE processing_state 
        SET last_processed_id = @newId, 
            last_updated = CURRENT_TIMESTAMP 
        WHERE id = 1;";

        cmd.Parameters.AddWithValue("@newId", newId);

        try
        {
            await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            _logger.LogDebug("Updated last processed ID to {NewId}", newId);
        }
        finally
        {
            await cmd.DisposeAsync().ConfigureAwait(false);
            await connection.DisposeAsync().ConfigureAwait(false);
        }
    }

    public async Task<List<NormalizedPlayerData>> GetTeamStatsAsync(string team, DateTime startDate)
    {
        var connection = await _dataSource.OpenConnectionAsync().ConfigureAwait(false);
        var cmd = connection.CreateCommand();

        cmd.CommandText = @"
        WITH LatestStats AS (
            SELECT DISTINCT ON (username) 
                username, team, typed, errors, name, races_played, timestamp, secs
            FROM normalized_data
            WHERE team = @team
            ORDER BY username, timestamp DESC
        ),
        StartingStats AS (
            SELECT DISTINCT ON (username) 
                username, typed, errors, races_played, secs
            FROM normalized_data
            WHERE team = @team 
                AND timestamp >= @startDate
            ORDER BY username, timestamp ASC
        ),
        DayAgoStats AS (
            SELECT DISTINCT ON (username) 
                username, races_played as races_played_day_ago
            FROM normalized_data 
            WHERE team = @team 
                AND timestamp >= NOW() - INTERVAL '24 hours'
            ORDER BY username, timestamp ASC
        )
        SELECT 
            l.username,
            l.team,
            COALESCE(l.typed - s.typed, l.typed) as typed,
            COALESCE(l.errors - s.errors, l.errors) as errors,
            l.name,
            COALESCE(l.races_played - s.races_played, l.races_played) as races_played,
            l.timestamp,
            COALESCE(l.secs - s.secs, l.secs) as secs,
            COALESCE(l.races_played - d.races_played_day_ago, 0) as races_played_diff
        FROM LatestStats l
        LEFT JOIN StartingStats s ON l.username = s.username
        LEFT JOIN DayAgoStats d ON l.username = d.username
        ORDER BY typed DESC";

        cmd.Parameters.AddWithValue("@team", team.ToUpperInvariant());
        cmd.Parameters.AddWithValue("@startDate", startDate);

        try
        {
            var results = new List<NormalizedPlayerData>();
            var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);

            try
            {
                while (await reader.ReadAsync().ConfigureAwait(false))
                {
                    results.Add(new NormalizedPlayerData
                    {
                        Username = reader.GetString(0),
                        Team = reader.GetString(1),
                        Typed = reader.GetInt64(2),
                        Errors = reader.GetInt64(3),
                        Name = reader.GetString(4),
                        RacesPlayed = reader.GetInt32(5),
                        Timestamp = reader.GetDateTime(6),
                        Secs = reader.GetInt64(7),
                        RacesPlayedDiff = reader.GetInt32(8)
                    });
                }

                return results;
            }
            finally
            {
                await reader.DisposeAsync().ConfigureAwait(false);
            }
        }
        finally
        {
            await cmd.DisposeAsync().ConfigureAwait(false);
            await connection.DisposeAsync().ConfigureAwait(false);
        }
    }
}
