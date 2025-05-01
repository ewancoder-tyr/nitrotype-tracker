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
    }

    public async ValueTask SaveAsync(IList<NormalizedPlayerData> data)
    {
        var connection = await _dataSource.OpenConnectionAsync().ConfigureAwait(false);
        var cmd = connection.CreateCommand();

        try
        {
            var values = string.Join(",", Enumerable.Range(0, data.Count)
                .Select(i => $"(@username{i}, @team{i}, @typed{i}, @errors{i}, @name{i}, @racesPlayed{i}, @timestamp{i}, @secs{i})"));

            cmd.CommandText = $"""
                INSERT INTO normalized_data
                (username, team, typed, errors, name, races_played, timestamp, secs)
                VALUES {values}
                ON CONFLICT (username, timestamp) DO NOTHING;
                """;

            for (var i = 0; i < data.Count; i++)
            {
                cmd.Parameters.AddWithValue($"@username{i}", data[i].Username);
                cmd.Parameters.AddWithValue($"@team{i}", data[i].Team);
                cmd.Parameters.AddWithValue($"@typed{i}", data[i].Typed);
                cmd.Parameters.AddWithValue($"@errors{i}", data[i].Errors);
                cmd.Parameters.AddWithValue($"@name{i}", data[i].Name);
                cmd.Parameters.AddWithValue($"@racesPlayed{i}", data[i].RacesPlayed);
                cmd.Parameters.AddWithValue($"@timestamp{i}", data[i].Timestamp);
                cmd.Parameters.AddWithValue($"@secs{i}", data[i].Secs);
            }

            _logger.LogDebug($"Successfully inserted {data.Count} records");
            await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to save normalized data for {Count} users", data.Count);
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

        try
        {
            cmd.CommandText = "SELECT last_processed_id FROM processing_state WHERE id = 1;";
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

        try
        {
            cmd.CommandText = """
                UPDATE processing_state 
                SET last_processed_id = @newId, 
                    last_updated = CURRENT_TIMESTAMP 
                WHERE id = 1;
                """;

            cmd.Parameters.AddWithValue("@newId", newId);

            await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            _logger.LogDebug("Updated last processed ID to {NewId}", newId);
        }
        finally
        {
            await cmd.DisposeAsync().ConfigureAwait(false);
            await connection.DisposeAsync().ConfigureAwait(false);
        }
    }

    // TODO: Review this logic.
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
            COALESCE(l.races_played - d.races_played_day_ago, 0) as races_played_diff,
            COALESCE(s.typed, 0) as starting_typed
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
                    if (reader.GetInt32(9) == 0)
                        continue; // Do not show players who did not type yet in this period.

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

                return results.ToList();
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
