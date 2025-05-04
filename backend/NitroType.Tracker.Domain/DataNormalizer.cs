using Microsoft.Extensions.Logging;
using StackExchange.Redis;

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
    private readonly IConnectionMultiplexer _redis;

    public DataNormalizer(
        DataProcessor dataProcessor,
        NormalizedDataRepository normalizedRepo,
        ILogger<DataNormalizer> logger,
        IConnectionMultiplexer redis)
    {
        _dataProcessor = dataProcessor;
        _normalizedRepo = normalizedRepo;
        _logger = logger;
        _redis = redis;
    }

    public async Task ProcessTeamDataAsync()
    {
        var db = _redis.GetDatabase(1);
        var lockKey = "tnt_normalization_lock";
        var lockValue = "locked";
        var acquired = await db.LockTakeAsync(lockKey, lockValue, TimeSpan.FromMinutes(10))
            .ConfigureAwait(false);

        if (!acquired)
        {
            _logger.LogInformation("Could not acquire lock for normalization. Skipping.");
            return;
        }

        var startedAt = DateTime.UtcNow;

        try
        {
            _logger.LogInformation("Starting to process data");

            await foreach (var item in _dataProcessor.GetNewEntriesAsync().ConfigureAwait(false))
            {
                if (DateTime.UtcNow - startedAt > TimeSpan.FromMinutes(9))
                {
                    _logger.LogWarning("Normalization took more than 9 minutes, quitting from this process to avoid duplication.");
                    break;
                }

                if (item.Data.Results is null || item.Data.Results.Season is null)
                {
                    _logger.LogWarning("Saved raw data was null for {Id} record", item.Id);
                    continue;
                }

                var values = item.Data.Results.Season
                    .Where(seasonData =>
                        seasonData.Username is not null
                        && seasonData.Typed.HasValue
                        && seasonData.Errs.HasValue
                        && seasonData.RacesPlayed.HasValue
                        && seasonData.Secs.HasValue)
                    .Select(seasonData => DataNormalizationConverter.Convert(seasonData, item.Team, item.Timestamp));

                try
                {
                    await _normalizedRepo.SaveAsync(values)
                        .ConfigureAwait(false);

                    await _normalizedRepo.UpdateLastProcessedIdAsync(item.Id)
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to process entries for team {Team}, or to update last processed ID", item.Team);
                }

                _logger.LogDebug(
                    "Finished processing data for team {Team} with id {Id}",
                    item.Team, item.Id);
            }

            _logger.LogInformation("Completed processing available data");
        }
        finally
        {
            await db.LockReleaseAsync(lockKey, lockValue)
                .ConfigureAwait(false);
        }
    }
}
