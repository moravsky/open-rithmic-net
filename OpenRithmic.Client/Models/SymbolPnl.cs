namespace OpenRithmic;

public sealed record SymbolPnl(
    Account Account,
    string Exchange,
    string Symbol,
    long? Position,
    double? OpenPnl,
    double? ClosedPnl,
    long? BuyQty,
    long? SellQty,
    long? BuyWorkingQty,
    long? SellWorkingQty,
    double? AvgOpenFillPrice,
    DateTimeOffset Timestamp);
