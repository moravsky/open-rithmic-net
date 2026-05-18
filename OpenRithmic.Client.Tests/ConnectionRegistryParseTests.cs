namespace OpenRithmic.Client.Tests;

public class ConnectionRegistryParseTests
{
    private const string SampleParams = """
        System Name  : Rithmic Test
        Gateway Name : Orangeburg
        File Version : 17.88.0.0

        For C++ Rithmic APIs :
        ======================

        REngineParams.sAdmCnnctPt : dd_admin_sslc

        For .NET Rithmic APIs :
        =======================

        REngineParams :

           * REngineParams.AdmCnnctPt  : dd_admin_sslc
           * REngineParams.DmnSrvrAddr : rituz00100.00.rithmic.com:65000~rituz00100.00.rithmic.net:65000
           * REngineParams.DomainName  : rithmic_uat_dmz_domain
           * REngineParams.LicSrvrAddr : rituz00100.00.rithmic.com:56000
           * REngineParams.LocBrokAddr : rituz00100.00.rithmic.com:64100
           * REngineParams.LoggerAddr  : rituz00100.00.rithmic.com:45454

        REngine::loginRepository() Params :

           * sCnnctPt : login_agent_repositoryc


        REngine::login() Params :

           * sMdCnnctPt  : login_agent_tpc
           * sIhCnnctPt  : login_agent_historyc
           * sTsCnnctPt  : login_agent_opc
           * sPnLCnnctPt : login_agent_pnlc
        """;

    [Fact]
    public void TryParse_extracts_all_required_fields()
    {
        Assert.True(ConnectionRegistry.TryParse(SampleParams, out var c));

        Assert.Equal("Rithmic Test", c.SystemName);
        Assert.Equal("Orangeburg", c.GatewayName);
        Assert.Equal("Rithmic Test/Orangeburg", c.DisplayName);
        Assert.Equal("dd_admin_sslc", c.AdmCnnctPt);
        Assert.Equal("rithmic_uat_dmz_domain", c.DomainName);
        Assert.Equal("rituz00100.00.rithmic.com:65000~rituz00100.00.rithmic.net:65000", c.DmnSrvrAddr);
        Assert.Equal("rituz00100.00.rithmic.com:56000", c.LicSrvrAddr);
        Assert.Equal("rituz00100.00.rithmic.com:64100", c.LocBrokAddr);
        Assert.Equal("rituz00100.00.rithmic.com:45454", c.LoggerAddr);
        Assert.Equal("login_agent_repositoryc", c.RepositoryCnnctPt);
        Assert.Equal("login_agent_tpc", c.MdCnnctPt);
        Assert.Equal("login_agent_historyc", c.IhCnnctPt);
        Assert.Equal("login_agent_opc", c.TsCnnctPt);
        Assert.Equal("login_agent_pnlc", c.PnLCnnctPt);
    }

    [Fact]
    public void TryParse_picks_first_sMdCnnctPt_line_when_aggregated_alt_is_present()
    {
        // Real files list two `sMdCnnctPt` lines (one regular, one aggregated).
        // We commit to the first one and ignore the alt.
        var text = SampleParams.Replace(
            "* sMdCnnctPt  : login_agent_tpc",
            """
            * sMdCnnctPt  : login_agent_tpc
                         - or -
               * sMdCnnctPt  : login_agent_tp_aggc (for aggregated market data)
            """);

        Assert.True(ConnectionRegistry.TryParse(text, out var c));
        Assert.Equal("login_agent_tpc", c.MdCnnctPt);
    }

    [Fact]
    public void TryParse_returns_false_when_header_is_missing()
    {
        var text = SampleParams.Replace("System Name  : Rithmic Test", "Something Else : x");
        Assert.False(ConnectionRegistry.TryParse(text, out _));
    }

    [Fact]
    public void TryParse_returns_false_when_dotnet_block_is_missing()
    {
        var text = SampleParams.Replace("For .NET Rithmic APIs :", "For Java Rithmic APIs :");
        Assert.False(ConnectionRegistry.TryParse(text, out _));
    }

    [Fact]
    public void TryParse_returns_false_when_a_login_connect_point_is_missing()
    {
        var text = SampleParams.Replace("* sTsCnnctPt  : login_agent_opc", "");
        Assert.False(ConnectionRegistry.TryParse(text, out _));
    }

    [Fact]
    public void TryParse_returns_false_when_repository_section_is_missing()
    {
        var text = SampleParams.Replace("REngine::loginRepository() Params :", "REngine::other() Params :");
        Assert.False(ConnectionRegistry.TryParse(text, out _));
    }
}
