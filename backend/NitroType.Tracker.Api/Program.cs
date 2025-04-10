using NitroType.Tracker.Domain;
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

var app = builder.Build();

app.ConfigureTyrApplication(config);

var retriever = new SmartDataRetriever();
retriever.RegisterTeam("KECATS");
retriever.RegisterTeam("SSH");
await retriever.RunAsync();

await app.RunAsync();
