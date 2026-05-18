namespace OpenRithmic;

public enum RithmicPlant { MarketData, TradingSystem, History, PnL, Repository, Admin, Other }

public enum AlertKind
{
    ConnectionOpened,
    ConnectionClosed,
    ConnectionBroken,
    LoginComplete,
    LoginFailed,
    Other,
}

public sealed record RithmicAlert(
    RithmicPlant Plant,
    AlertKind Kind,
    string Message,
    int ResponseCode);
