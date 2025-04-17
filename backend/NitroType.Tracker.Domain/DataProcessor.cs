using Npgsql;

namespace NitroType.Tracker.Domain;

public sealed class DataProcessor
{
    private readonly NpgsqlDataSource _dataSource;

    public DataProcessor(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async IAsyncEnumerable<RawTeamEntry> GetAllEntriesAsync(string team)
    {
        // TODO: Create the database/table if not created. Or use migrations.
        var connection = await _dataSource.OpenConnectionAsync().ConfigureAwait(false);
        var cmd = new NpgsqlCommand("SELECT data, timestamp, id FROM raw_data where team = @team;");
        cmd.Connection = connection;
        cmd.Parameters.AddWithValue("@team", team);

        try
        {
            var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                var json = reader.GetString(0);
                var timestamp = reader.GetDateTime(1);
                var id = reader.GetInt64(2);

                var data = JsonSerializer.Deserialize<NitroTypeData>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                           ?? throw new InvalidOperationException("Could not deserialize the json.");

                yield return new(team, data, timestamp, id);
            }
        }
        finally
        {
            await cmd.DisposeAsync().ConfigureAwait(false);
            await connection.DisposeAsync().ConfigureAwait(false);
        }
    }
}

public sealed record RawTeamEntry(string Team, NitroTypeData Data, DateTime Timestamp, long Id);

// Root object
public record NitroTypeData(
    string? Status,
    Results? Results
);

// Results object containing info and members
public record Results(
    TeamInfo? Info,
    Member[]? Members,
    Stat[]? Stats,
    SeasonMember[]? Season,
    object? Leaderboard, // null in sample, keeping as object
    bool? OnThisTeam
);

// Team information
public record TeamInfo(
    int? TeamId,
    int? UserId,
    string? Tag,
    string? TagColor,
    string? Name,
    int? MinLevel,
    int? MinRaces,
    int? MinSpeed,
    int? AutoRemove,
    string? OtherRequirements,
    int? Members,
    int? ActivePercent,
    int? Searchable,
    string? Enrollment,
    int? LeagueId,
    int? LeagueTier,
    int? HighestLeagueTier,
    int? ProfileViews,
    long? LastActivity,
    long? LastModified,
    long? CreatedStamp,
    string? Username,
    string? DisplayName
);

// Team member information
public record Member(
    int? UserId,
    int? TeamId,
    int? RacesPlayed,
    int? AvgSpeed,
    long? LastLogin,
    int? Played,
    int? Secs,
    int? Typed,
    int? Errs,
    long? JoinStamp,
    long? LastActivity,
    string? Role,
    string? Username,
    string? DisplayName,
    string? Membership,
    string? Title,
    int? CarId,
    int? CarHueAngle,
    string? Status,
    int? HighestSpeed
);

// Statistics
public record Stat(
    string? Board,
    string? Typed,
    string? Secs,
    string? Played,
    string? Errs,
    long? Stamp
);

// Season member information (extends member data with points)
public record SeasonMember(
    int? UserId,
    int? RacesPlayed,
    int? AvgSpeed,
    long? LastLogin,
    int? Played,
    int? Secs,
    int? Typed,
    int? Errs,
    int? Points,
    long? LastActivity,
    string? Role,
    long? JoinStamp,
    string? Username,
    string? DisplayName,
    string? Membership,
    string? Title,
    int? CarId,
    int? CarHueAngle,
    string? Status,
    int? HighestSpeed
);
