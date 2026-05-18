namespace OpenRithmic;

public enum OrderSide { Buy, Sell }

public sealed record Fill(
    Account Account,
    string Exchange,
    string Symbol,
    OrderSide Side,
    long Qty,
    double Price,
    string? OrderNum,
    string? UserTag,
    DateTimeOffset Timestamp);
