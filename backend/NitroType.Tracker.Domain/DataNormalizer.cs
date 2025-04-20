using Microsoft.Extensions.Logging;

namespace NitroType.Tracker.Domain;

public static class DataNormalizationConverter
{
    public static NormalizedPlayerData Convert(SeasonMember seasonMember, string team, DateTime timestamp)
    {
        if (seasonMember.Username is null)
            throw new ArgumentException("Username is null.");

        if (seasonMember.Typed is null)
            throw new ArgumentException("Typed is null.");

        if (seasonMember.Errs is null)
            throw new ArgumentException("Errs is null.");

        if (seasonMember.RacesPlayed is null)
            throw new ArgumentException("RacesPlayed is null.");

        if (seasonMember.Secs is null)
            throw new ArgumentException("Secs is null.");

        return new NormalizedPlayerData
        {
            Username = seasonMember.Username,
            Team = team,
            Typed = seasonMember.Typed.Value,
            Errors = seasonMember.Errs.Value,
            Name = string.IsNullOrWhiteSpace(seasonMember.DisplayName)
                ? seasonMember.Username
                : seasonMember.DisplayName,
            RacesPlayed = seasonMember.RacesPlayed.Value,
            Timestamp = timestamp,
            Secs = seasonMember.Secs.Value
        };
    }
}

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

    public async Task ProcessTeamDataAsync()
    {
        _logger.LogInformation("Starting to process data");

        await foreach (var item in _dataProcessor.GetNewEntriesAsync().ConfigureAwait(false))
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

                    var normalizedData = DataNormalizationConverter.Convert(seasonData, item.Team, item.Timestamp);

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

            await _normalizedRepo.UpdateLastProcessedIdAsync(item.Id)
                .ConfigureAwait(false);

            _logger.LogDebug(
                "Finished processing data for team {Team} with id {Id}",
                item.Team, item.Id);
        }

        _logger.LogInformation("Completed processing available data");
    }
}
