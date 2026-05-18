namespace OpenRithmic.Client.Tests;

public class ConnectionRegistryTests
{
    [Fact]
    public void All_loads_a_useful_number_of_connections()
    {
        // ~325 connection-params files ship as embedded resources; assert a
        // floor that catches accidental resource-include regressions.
        Assert.True(ConnectionRegistry.All.Count > 200,
            $"Expected >200 embedded connections, got {ConnectionRegistry.All.Count}");
    }

    [Fact]
    public void All_includes_default_Rithmic_Test_Orangeburg_used_for_smoke_tests()
    {
        var match = ConnectionRegistry.All.FirstOrDefault(c =>
            c.SystemName == "Rithmic Test" && c.GatewayName == "Orangeburg");
        Assert.NotNull(match);
        Assert.Equal("rithmic_uat_dmz_domain", match!.DomainName);
    }

    [Fact]
    public void All_is_sorted_by_system_then_gateway_for_deterministic_listings()
    {
        var sorted = ConnectionRegistry.All
            .OrderBy(c => c.SystemName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(c => c.GatewayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.Equal(sorted, ConnectionRegistry.All);
    }

    [Theory]
    [InlineData("Rithmic Test/Orangeburg")]
    [InlineData("rithmic test/orangeburg")]
    [InlineData("RITHMIC TEST / ORANGEBURG")]
    public void Find_matches_case_insensitively_with_trimming(string input)
    {
        var match = ConnectionRegistry.Find(input);
        Assert.NotNull(match);
        Assert.Equal("Rithmic Test", match!.SystemName);
        Assert.Equal("Orangeburg", match.GatewayName);
    }

    [Fact]
    public void Find_returns_null_when_separator_is_missing() =>
        Assert.Null(ConnectionRegistry.Find("Rithmic Test Orangeburg"));

    [Fact]
    public void Find_returns_null_for_unknown_gateway() =>
        Assert.Null(ConnectionRegistry.Find("Rithmic Test/NoSuchGateway"));
}
