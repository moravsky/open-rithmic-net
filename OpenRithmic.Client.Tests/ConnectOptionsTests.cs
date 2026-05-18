namespace OpenRithmic.Client.Tests;

public class ConnectOptionsTests
{
    [Fact]
    public void Defaults_match_documented_behavior()
    {
        var opts = new ConnectOptions();
        Assert.True(opts.IncludeMarketData);
        Assert.False(opts.PluginMode);
        Assert.Null(opts.Timeout);
        Assert.Equal("rithmic.log", opts.LogFilePath);
    }

    [Fact]
    public void PluginMode_can_be_enabled_without_disturbing_other_defaults()
    {
        var opts = new ConnectOptions(PluginMode: true);
        Assert.True(opts.PluginMode);
        Assert.True(opts.IncludeMarketData);
    }

    // The exact endpoints are baked in to the public contract: anyone driving
    // R | Trader Pro as a plug-in host has to point us at these ports, so they
    // must not silently drift in a refactor.
    [Fact]
    public void Plugin_endpoints_match_RTrader_Pro_listener_ports()
    {
        Assert.Equal("127.0.0.1:3010", PluginEndpoints.MarketData);
        Assert.Equal("127.0.0.1:3012", PluginEndpoints.History);
    }
}
