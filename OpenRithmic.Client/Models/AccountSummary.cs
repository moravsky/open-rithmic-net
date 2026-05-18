namespace OpenRithmic;

public sealed record AccountSummary(
    Account Account,
    string? Currency,
    double? AccountBalance,
    double? CashOnHand,
    double? MarginBalance,
    double? AvailableBuyingPower,
    double? UsedBuyingPower,
    double? ReservedMargin,
    double? OpenPnl,
    double? ClosedPnl,
    DateTimeOffset Timestamp);
