using com.omnesys.rapi;
using OpenRithmic.Internal;

namespace OpenRithmic.Client.Tests;

public class RithmicMappersTests
{
    [Theory]
    [InlineData(ConnectionId.MarketData,    RithmicPlant.MarketData)]
    [InlineData(ConnectionId.TradingSystem, RithmicPlant.TradingSystem)]
    [InlineData(ConnectionId.History,       RithmicPlant.History)]
    [InlineData(ConnectionId.PnL,           RithmicPlant.PnL)]
    [InlineData(ConnectionId.Repository,    RithmicPlant.Repository)]
    public void MapPlant_known_connection_ids(ConnectionId id, RithmicPlant expected) =>
        Assert.Equal(expected, RithmicMappers.MapPlant(id));

    [Fact]
    public void MapPlant_unknown_connection_id_falls_back_to_Other() =>
        Assert.Equal(RithmicPlant.Other, RithmicMappers.MapPlant((ConnectionId)9999));

    [Theory]
    [InlineData(AlertType.ConnectionOpened, AlertKind.ConnectionOpened)]
    [InlineData(AlertType.ConnectionClosed, AlertKind.ConnectionClosed)]
    [InlineData(AlertType.ConnectionBroken, AlertKind.ConnectionBroken)]
    [InlineData(AlertType.LoginComplete,    AlertKind.LoginComplete)]
    [InlineData(AlertType.LoginFailed,      AlertKind.LoginFailed)]
    public void MapAlertKind_known_alert_types(AlertType type, AlertKind expected) =>
        Assert.Equal(expected, RithmicMappers.MapAlertKind(type));

    [Fact]
    public void MapAlertKind_unknown_alert_type_falls_back_to_Other() =>
        Assert.Equal(AlertKind.Other, RithmicMappers.MapAlertKind((AlertType)9999));

    [Fact]
    public void MapSide_returns_Buy_for_buy_constant() =>
        Assert.Equal(OrderSide.Buy, RithmicMappers.MapSide(Constants.BUY_SELL_TYPE_BUY));

    [Fact]
    public void MapSide_returns_Sell_for_sell_constant() =>
        Assert.Equal(OrderSide.Sell, RithmicMappers.MapSide(Constants.BUY_SELL_TYPE_SELL));

    [Theory]
    [InlineData(null,  true)]
    [InlineData("",    true)]
    [InlineData("ESZ5", false)]
    public void IsAccountSummaryRow_treats_missing_symbol_as_summary(string? symbol, bool expected) =>
        Assert.Equal(expected, RithmicMappers.IsAccountSummaryRow(symbol));
}
