using System.Collections.Concurrent;

namespace NitroType.Tracker.Domain;

public sealed class SmartDataRetriever
{
    private static readonly TimeSpan _queryInterval = TimeSpan.FromMinutes(5);
    private readonly Random _random = Random.Shared;
    private readonly ConcurrentDictionary<string, DateTime> _queriedTeams = new();
    private readonly DataRetriever _dataRetriever = new();

    // TODO: Persist to DB.
    private readonly List<RawDataEntry> _data = new();

    public void RegisterTeam(string teamName)
    {
        _queriedTeams.TryAdd(teamName, default);
    }

    public void RemoveTeam(string teamName)
    {
        _queriedTeams.Remove(teamName, out _);
    }

    public async Task RunAsync()
    {
        while (true)
        {
            // For every team.
            foreach (var item in _queriedTeams)
            {
                // Only query each team once per 5 minutes.
                if (DateTime.UtcNow - item.Value < _queryInterval)
                    continue; // Instantly skip any queried teams.

                try
                {
                    // Retrieve data for the team.
                    var teamName = item.Key;
                    var data = await _dataRetriever.RetrieveDataAsync(teamName)
                        .ConfigureAwait(false);
                    _queriedTeams[item.Key] = DateTime.UtcNow;
                    _data.Add(new(teamName, data, DateTime.UtcNow));
                }
                catch (Exception exception)
                {
                    // Do not crash if some data is corrupted.
                    Console.WriteLine($"Error during querying data: {exception}");
                }

                // Wait random time before proceeding to the next team.
                var waitTimeMs = _random.Next(3000, 7000);
                await Task.Delay(TimeSpan.FromMilliseconds(waitTimeMs))
                    .ConfigureAwait(false);
            }

            {
                // Wait random time before proceeding to the next iteration.
                var waitTimeMs = _random.Next(3000, 7000);
                await Task.Delay(TimeSpan.FromMilliseconds(waitTimeMs))
                    .ConfigureAwait(false);
            }
        }
    }
}

public sealed class DataRetriever
{
    public async ValueTask<NitroTypeData> RetrieveDataAsync(string teamName)
    {
        using var client = new HttpClient();

        var response = await client.GetAsync(new Uri($"https://nitrotype.com/api/v2/teams/{teamName}"))
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync()
            .ConfigureAwait(false);

        return JsonSerializer.Deserialize<NitroTypeData>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException("Could not deserialize json.");
    }
}

public sealed record RawDataEntry(string Team, NitroTypeData Data, DateTime Timestamp);

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
