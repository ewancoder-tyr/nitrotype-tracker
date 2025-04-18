namespace NitroType.Tracker.Domain;

public sealed class NormalizedPlayerData
{
    public long Id { get; set; }
    public required string Username { get; set; }
    public required string Team { get; set; }
    public required long Typed { get; set; }
    public required long Errors { get; set; }
    public required string Name { get; set; }
    public required int RacesPlayed { get; set; }
    public required DateTime Timestamp { get; set; }
    public required long Secs { get; set; }

    public decimal Accuracy => Typed == 0 ? 0 : 100m * (Typed - Errors) / Typed;
    public decimal AverageTextLength => RacesPlayed == 0 ? 0 : (decimal)Typed / RacesPlayed;
    public decimal AverageSpeed => Secs == 0 ? 0 : (60m / 5) * Typed / Secs;
}
