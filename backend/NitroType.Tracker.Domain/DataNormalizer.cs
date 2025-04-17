using Microsoft.Extensions.Logging;

namespace NitroType.Tracker.Domain;

public sealed class DataNormalizer
{
    private readonly DataProcessor _dataProcessor;
    private readonly NormalizedDataRepository _normalizedRepo;
    private readonly ILogger<DataNormalizer> _logger;

    public DataNormalizer(
        DataProcessor dataProcessor,
        NormalizedDataRepository normalizedRepo,
        ILogger<DataNormalizer> logger)
    {
        _dataProcessor = dataProcessor;
        _normalizedRepo = normalizedRepo;
        _logger = logger;
    }

    public async Task ProcessTeamDataAsync(string team)
    {
        _logger.LogInformation("Starting to process data for team {Team}", team);

        await foreach (var item in _dataProcessor.GetAllEntriesAsync(team).ConfigureAwait(false))
        {
            if (item.Data.Results is null || item.Data.Results.Season is null)
            {
                _logger.LogWarning("Saved raw data was null for {Id} record", item.Id);
                continue;
            }

            foreach (var seasonData in item.Data.Results.Season)
            {
                try
                {
                    if (seasonData.Username is null ||
                        !seasonData.Typed.HasValue ||
                        !seasonData.Errs.HasValue ||
                        !seasonData.RacesPlayed.HasValue ||
                        !seasonData.Secs.HasValue)
                    {
                        _logger.LogWarning("Saved raw data season was null for one of users of {Id} record", item.Id);
                        continue;
                    }

                    var normalizedData = new NormalizedPlayerData
                    {
                        Username = seasonData.Username,
                        Team = item.Team,
                        Typed = seasonData.Typed.Value,
                        Errors = seasonData.Errs.Value,
                        Name = string.IsNullOrWhiteSpace(seasonData.DisplayName)
                            ? seasonData.Username
                            : seasonData.DisplayName,
                        RacesPlayed = seasonData.RacesPlayed.Value,
                        Timestamp = item.Timestamp,
                        Secs = seasonData.Secs.Value
                    };

                    await _normalizedRepo.SaveAsync(normalizedData)
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to process entry for user {Username} in team {Team}",
                        seasonData.Username, item.Team);
                }
            }

            _logger.LogDebug(
                "Finished processing data for team {Team} with id {Id}",
                team, item.Id);
        }

        _logger.LogInformation("Completed processing data for team {Team}", team);
    }
}
