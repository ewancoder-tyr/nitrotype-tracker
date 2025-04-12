using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace NitroType.Tracker.Domain;

public sealed class SmartDataRetriever
{
    private static readonly TimeSpan _queryInterval = TimeSpan.FromMinutes(5);
    private readonly Random _random = Random.Shared;
    private readonly ConcurrentDictionary<string, DateTime> _queriedTeams = new();
    private readonly DataRetriever _dataRetriever;
    private readonly RawDataRepository _rawRepo;
    private readonly ILogger<SmartDataRetriever> _logger;

    public SmartDataRetriever(
        RawDataRepository rawRepo,
        DataRetriever dataRetriever,
        ILogger<SmartDataRetriever> logger)
    {
        _rawRepo = rawRepo;
        _dataRetriever = dataRetriever;
        _logger = logger;
    }

    public void RegisterTeam(string teamName)
    {
        _logger.LogInformation("Registered team {Team}", teamName);
        _queriedTeams.TryAdd(teamName, default);
    }

    public void RemoveTeam(string teamName)
    {
        _logger.LogInformation("Removed registration for team {Team}", teamName);
        _queriedTeams.Remove(teamName, out _);
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting endless process of querying NitroType API");
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // For every team.
            foreach (var item in _queriedTeams)
            {
                // Only query each team once per 5 minutes.
                if (DateTime.UtcNow - item.Value < _queryInterval)
                    continue; // Instantly skip any queried teams.

                _logger.LogInformation("Querying {Team} team", item.Key);

                try
                {
                    // Retrieve data for the team.
                    var teamName = item.Key;
                    var data = await _dataRetriever.RetrieveRawDataAsync(teamName)
                        .ConfigureAwait(false);
                    await _rawRepo.SaveAsync(teamName, data)
                        .ConfigureAwait(false);
                    _queriedTeams[item.Key] = DateTime.UtcNow;

                    _logger.LogInformation("Successfully queried {Team} team", item.Key);
                }
                catch (Exception exception)
                {
                    // Do not crash if some data is corrupted.
                    _logger.LogError(exception, "Failed to query and/or save team {Team}", item.Key);
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
