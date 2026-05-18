using com.omnesys.rapi;

namespace OpenRithmic.Internal;

internal static class RithmicMappers
{
    public static RithmicPlant MapPlant(ConnectionId id) => id switch
    {
        ConnectionId.MarketData    => RithmicPlant.MarketData,
        ConnectionId.TradingSystem => RithmicPlant.TradingSystem,
        ConnectionId.History       => RithmicPlant.History,
        ConnectionId.PnL           => RithmicPlant.PnL,
        ConnectionId.Repository    => RithmicPlant.Repository,
        _                          => RithmicPlant.Other,
    };

    public static AlertKind MapAlertKind(AlertType type) => type switch
    {
        AlertType.ConnectionOpened => AlertKind.ConnectionOpened,
        AlertType.ConnectionClosed => AlertKind.ConnectionClosed,
        AlertType.ConnectionBroken => AlertKind.ConnectionBroken,
        AlertType.LoginComplete    => AlertKind.LoginComplete,
        AlertType.LoginFailed      => AlertKind.LoginFailed,
        _                          => AlertKind.Other,
    };

    public static OrderSide MapSide(string buySellType) =>
        buySellType == Constants.BUY_SELL_TYPE_BUY ? OrderSide.Buy : OrderSide.Sell;

    // PnlInfo carries either a per-account summary row (no symbol) or a
    // per-symbol row. Rithmic emits both on the same callback.
    public static bool IsAccountSummaryRow(string? symbol) =>
        string.IsNullOrEmpty(symbol);
}
