namespace NitroType.Tracker.Domain.Tests;

public class DataNormalizationConverterTests
{
    [Fact]
    public void ShouldUseProvidedTimestamp()
    {
        var fixture = new Fixture();
        var seasonMember = fixture.Create<SeasonMember>();

        var team = "KECATS";
        var timestamp = DateTime.UtcNow;
        var result = DataNormalizationConverter.Convert(
            seasonMember, team, timestamp);

        Assert.Equal(timestamp, result.Timestamp);
    }
}
