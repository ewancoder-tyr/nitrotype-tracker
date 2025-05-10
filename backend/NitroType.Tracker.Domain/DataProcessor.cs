using Npgsql;

namespace NitroType.Tracker.Domain;

public sealed class DataProcessor
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly NormalizedDataRepository _normalizedDataRepository;

    public DataProcessor(
        NpgsqlDataSource dataSource,
        NormalizedDataRepository normalizedDataRepository)
    {
        _dataSource = dataSource;
        _normalizedDataRepository = normalizedDataRepository;
    }

    public async IAsyncEnumerable<RawTeamEntry> GetNewEntriesAsync(string? team = null)
    {
        var lastProcessedId = await _normalizedDataRepository.GetLastProcessedIdAsync().ConfigureAwait(false);

        var connection = await _dataSource.OpenConnectionAsync().ConfigureAwait(false);
        var cmd = connection.CreateCommand();

        try
        {
            cmd.CommandText = team is null
                ? "SELECT data, timestamp, id, team FROM raw_data WHERE id > @lastId ORDER BY id;"
                : "SELECT data, timestamp, id, team FROM raw_data where team = @team AND id > @lastId ORDER BY id;";

            cmd.Parameters.AddWithValue("@lastId", lastProcessedId);
            if (team is not null)
                cmd.Parameters.AddWithValue("@team", team);

            var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                var json = reader.GetString(0);
                var timestamp = reader.GetDateTime(1);
                var id = reader.GetInt64(2);
                team = reader.GetString(3);

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
    //Stat[]? Stats,
    //SeasonMember[]? Season,
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
