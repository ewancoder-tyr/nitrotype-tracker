using System.Text;
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

    public async ValueTask SaveAsync(IEnumerable<NormalizedPlayerData> data)
    {
        var connection = await _dataSource.OpenConnectionAsync().ConfigureAwait(false);
        var cmd = connection.CreateCommand();

        try
        {
            var valuesBuilder = new StringBuilder();
            var count = 0;

            foreach (var (item, index) in data.Select((x, i) => (x, i)))
            {
                if (index > 0)
                    valuesBuilder.Append(',');

                valuesBuilder.Append($"(@username{index}, @team{index}, @typed{index}, @errors{index}, @name{index}, @racesPlayed{index}, @timestamp{index}, @secs{index})");

                cmd.Parameters.AddWithValue($"@username{index}", item.Username);
                cmd.Parameters.AddWithValue($"@team{index}", item.Team);
                cmd.Parameters.AddWithValue($"@typed{index}", item.Typed);
                cmd.Parameters.AddWithValue($"@errors{index}", item.Errors);
                cmd.Parameters.AddWithValue($"@name{index}", item.Name);
                cmd.Parameters.AddWithValue($"@racesPlayed{index}", item.RacesPlayed);
                cmd.Parameters.AddWithValue($"@timestamp{index}", item.Timestamp);
                cmd.Parameters.AddWithValue($"@secs{index}", item.Secs);
                count++;
            }

            cmd.CommandText = $"""
                INSERT INTO normalized_data
                (username, team, typed, errors, name, races_played, timestamp, secs)
                VALUES {valuesBuilder}
                ON CONFLICT (username, timestamp) DO NOTHING;
                """;

            _logger.LogDebug($"Successfully inserted {count} records");
            await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to save normalized data");
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
