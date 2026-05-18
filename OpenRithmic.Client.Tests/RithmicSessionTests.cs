namespace OpenRithmic.Client.Tests;

// Covers the parts of RithmicSession that don't depend on REngine: event
// fan-out from internal Handle* sinks. The login/ready state machine inside
// ConnectAsync is out of scope -- it requires a real REngine instance.
public class RithmicSessionTests
{
    [Fact]
    public void HandleAccountList_populates_Accounts_and_raises_AccountsLoaded()
    {
        using var session = new RithmicSession();
        IReadOnlyList<Account>? received = null;
        session.AccountsLoaded += list => received = list;

        var accounts = new[] { new Account("Fcm", "Ib", "A1"), new Account("Fcm", "Ib", "A2") };
        session.HandleAccountList(accounts);

        Assert.Same(accounts, received);
        Assert.Equal(accounts, session.Accounts);
    }

    [Fact]
    public void HandleAlert_raises_Alert_event_with_payload()
    {
        using var session = new RithmicSession();
        RithmicAlert? received = null;
        session.Alert += a => received = a;

        var alert = new RithmicAlert(RithmicPlant.TradingSystem, AlertKind.LoginComplete, "ok", 0);
        session.HandleAlert(alert);

        Assert.Equal(alert, received);
    }

    [Fact]
    public void HandleAccountSummary_raises_AccountSummaryUpdated()
    {
        using var session = new RithmicSession();
        AccountSummary? received = null;
        session.AccountSummaryUpdated += s => received = s;

        var summary = new AccountSummary(
            Account: new Account("Fcm", "Ib", "A1"),
            Currency: "USD",
            AccountBalance: 50_000,
            CashOnHand: 50_000,
            MarginBalance: 0,
            AvailableBuyingPower: 50_000,
            UsedBuyingPower: 0,
            ReservedMargin: 0,
            OpenPnl: 0,
            ClosedPnl: 0,
            Timestamp: DateTimeOffset.UnixEpoch);
        session.HandleAccountSummary(summary);

        Assert.Equal(summary, received);
    }

    [Fact]
    public void HandleSymbolPnl_raises_SymbolPnlUpdated()
    {
        using var session = new RithmicSession();
        SymbolPnl? received = null;
        session.SymbolPnlUpdated += p => received = p;

        var pnl = new SymbolPnl(
            Account: new Account("Fcm", "Ib", "A1"),
            Exchange: "CME",
            Symbol: "ESZ5",
            Position: 1,
            OpenPnl: 12.50,
            ClosedPnl: 0,
            BuyQty: 1,
            SellQty: 0,
            BuyWorkingQty: 0,
            SellWorkingQty: 0,
            AvgOpenFillPrice: 5000.25,
            Timestamp: DateTimeOffset.UnixEpoch);
        session.HandleSymbolPnl(pnl);

        Assert.Equal(pnl, received);
    }

    [Fact]
    public void HandleFill_raises_FillReceived()
    {
        using var session = new RithmicSession();
        Fill? received = null;
        session.FillReceived += f => received = f;

        var fill = new Fill(
            Account: new Account("Fcm", "Ib", "A1"),
            Exchange: "CME",
            Symbol: "ESZ5",
            Side: OrderSide.Buy,
            Qty: 1,
            Price: 5000.25,
            OrderNum: "ORD-1",
            UserTag: "tag",
            Timestamp: DateTimeOffset.UnixEpoch);
        session.HandleFill(fill);

        Assert.Equal(fill, received);
    }

    [Fact]
    public void IsConnected_is_false_before_ConnectAsync_and_after_Dispose()
    {
        var session = new RithmicSession();
        Assert.False(session.IsConnected);
        session.Dispose();
        Assert.False(session.IsConnected);
    }

    [Fact]
    public void Dispose_is_idempotent()
    {
        var session = new RithmicSession();
        session.Dispose();
        session.Dispose();
    }
}
