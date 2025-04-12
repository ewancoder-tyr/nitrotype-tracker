using System.Diagnostics;
using NitroType.Tracker.Domain;
using Npgsql;
using Tyr.Framework;

var builder = WebApplication.CreateBuilder(args);
var isDebug = false;
#if DEBUG
isDebug = true;
#endif

var config = TyrHostConfiguration.Default(
    builder.Configuration,
    "FoulTalk",
    isDebug: isDebug);

await builder.ConfigureTyrApplicationBuilderAsync(config);

builder.Services.AddHttpClient();
builder.Services.AddSingleton<DataRetriever>();
builder.Services.AddSingleton<RawDataRepository>();
builder.Services.AddSingleton<SmartDataRetriever>();
builder.Services.AddSingleton<DataProcessor>();

builder.Services.AddSingleton<NpgsqlDataSource>(provider =>
{
    var connectionString = provider.GetRequiredService<IConfiguration>()["DbConnectionString"]
                           ?? throw new InvalidOperationException("DB connection string is not set.");

    var dbBuilder = new NpgsqlDataSourceBuilder(connectionString);
    return dbBuilder.Build();
});

var app = builder.Build();

using var cts = new CancellationTokenSource();

app.ConfigureTyrApplication(config);

var retriever = app.Services.GetRequiredService<SmartDataRetriever>();
retriever.RegisterTeam("KECATS");
retriever.RegisterTeam("SSH");
var task = retriever.RunAsync(cts.Token);

var processor = app.Services.GetRequiredService<DataProcessor>();
var logger = app.Services.GetRequiredService<ILogger<Program>>();

app.MapGet("/api/statistics/{team}", async (string team) =>
{
    var sw = new Stopwatch();
    sw.Start();
    team = team.ToUpperInvariant();
    var processed = new List<PlayerInfo>();
    await foreach (var item in processor.GetAllEntriesAsync(team))
    {
        if (item.Data.Results is null || item.Data.Results.Season is null)
            continue;

        try
        {
            processed.AddRange(item.Data.Results.Season.Select(s => new PlayerInfo
            {
                Username = s.Username!,
                Team = item.Team,
                Typed = s.Typed!.Value,
                Errors = s.Errs!.Value,
                Name = s.DisplayName ?? s.Username!,
                RacesPlayed = s.RacesPlayed!.Value,
                Timestamp = item.Timestamp,
                Secs = s.Secs!.Value
            }));
        }
        catch
        {
            continue;
        }
    }

    //var period = TimeSpan.FromDays(1);
    var querySince = new DateTime(2025, 4, 10); // Start of the league season.
    var users = processed.GroupBy(x => x.Username).ToDictionary(x => x.Key, x => x.OrderBy(a => a.Timestamp).ToList());

    var periodStatses = new List<PlayerInfo>();
    foreach (var username in users.Keys)
    {
        var user = users[username];
        var now = user.LastOrDefault()!;
        var previous = user.FirstOrDefault(x => x.Timestamp > querySince);
        var forDiff = user.FirstOrDefault(x => x.Timestamp <= DateTime.UtcNow - TimeSpan.FromDays(1))
            ?? user.FirstOrDefault();
        var periodStats = now - previous!;
        periodStats.AccuracyDiff = now.Accuracy == 0 ? 0 : now.Accuracy - forDiff!.Accuracy;
        periodStats.AverageSpeedDiff = now.AverageSpeed == 0 ? 0 : now.AverageSpeed - forDiff!.AverageSpeed;
        periodStats.RacesPlayedDiff = now.RacesPlayed - forDiff!.RacesPlayed;
        periodStatses.Add(periodStats);
    }

    sw.Stop();
    logger.LogInformation("Gathered data for team {Team}, took {Seconds} seconds", team, sw.Elapsed.TotalSeconds);
    return periodStatses;
});

await app.RunAsync();

public sealed class PlayerInfo
{
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
    // ReSharper disable once ArrangeRedundantParentheses
    public decimal AverageSpeed => Secs == 0 ? 0 : (60m / 5) * Typed / Secs;

    public decimal AccuracyDiff { get; set; }
    public decimal AverageSpeedDiff { get; set; }
    public decimal RacesPlayedDiff { get; set; }

    public string TimeSpent
    {
        get
        {
            var time = TimeSpan.FromSeconds(Secs);
            var parts = new List<string>();
            if (time.Days > 0)
                parts.Add($"{time.Days} day{(time.Days > 1 ? "s" : "")}");
            if (time.Hours > 0)
                parts.Add($"{time.Hours} hour{(time.Hours > 1 ? "s" : "")}");
            if (time.Minutes > 0)
                parts.Add($"{time.Minutes} minute{(time.Minutes > 1 ? "s" : "")}");
            if (time.Seconds > 0)
                parts.Add($"{time.Seconds} second{(time.Minutes > 1 ? "s" : "")}");

            return string.Join(" ", parts);
        }
    }

    public static PlayerInfo operator -(PlayerInfo one, PlayerInfo two)
    {
        if (one.Username != two.Username)
            throw new InvalidOperationException("Cannot subtract different users.");

        return new PlayerInfo
        {
            Username = one.Username,
            Name = one.Name,
            Team = one.Team,
            Timestamp = one.Timestamp,
            Typed = one.Typed - two.Typed,
            Secs = one.Secs - two.Secs,
            Errors = one.Errors - two.Errors,
            RacesPlayed = one.RacesPlayed - two.RacesPlayed
        };
    }
}
