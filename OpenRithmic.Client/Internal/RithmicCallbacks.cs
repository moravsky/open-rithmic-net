using System.Collections.Concurrent;
using com.omnesys.rapi;

namespace OpenRithmic.Internal;

internal sealed class RithmicCallbacks(RithmicSession session) : RCallbacks
{
    private readonly RithmicSession _session = session;
    private readonly ConcurrentDictionary<string, Account> _accounts = new();

    private Account ResolveAccount(AccountInfo info, string? name = null) =>
        _accounts.GetOrAdd(
            $"{info.FcmId}|{info.IbId}|{info.AccountId}",
            _ => new Account(info.FcmId, info.IbId, info.AccountId, name));

    public override void Alert(AlertInfo o)
    {
        var plant = o.ConnectionId switch
        {
            ConnectionId.MarketData    => RithmicPlant.MarketData,
            ConnectionId.TradingSystem => RithmicPlant.TradingSystem,
            ConnectionId.History       => RithmicPlant.History,
            ConnectionId.PnL           => RithmicPlant.PnL,
            ConnectionId.Repository    => RithmicPlant.Repository,
            _                          => RithmicPlant.Other,
        };

        var kind = o.AlertType switch
        {
            AlertType.ConnectionOpened => AlertKind.ConnectionOpened,
            AlertType.ConnectionClosed => AlertKind.ConnectionClosed,
            AlertType.ConnectionBroken => AlertKind.ConnectionBroken,
            AlertType.LoginComplete    => AlertKind.LoginComplete,
            AlertType.LoginFailed      => AlertKind.LoginFailed,
            _                          => AlertKind.Other,
        };

        _session.HandleAlert(new RithmicAlert(plant, kind, o.Message ?? "", o.RpCode));
    }

    public override void AccountList(AccountListInfo o)
    {
        var list = new List<Account>(o.Accounts.Count);
        for (int i = 0; i < o.Accounts.Count; i++)
        {
            var src = o.Accounts[i];
            var account = ResolveAccount(src, src.AccountName);
            list.Add(account);
        }
        _session.HandleAccountList(list);
    }

    public override void PnlReplay(PnlReplayInfo o)
    {
        for (int i = 0; i < o.PnlInfoList.Count; i++)
            DispatchPnl(o.PnlInfoList[i]);
    }

    public override void PnlUpdate(PnlInfo o) => DispatchPnl(o);

    private void DispatchPnl(PnlInfo p)
    {
        var account = ResolveAccount(p.Account);
        var ts = IgnorableExt.ToUtc(p.Ssboe, p.Usecs);

        if (string.IsNullOrEmpty(p.Symbol))
        {
            var summary = new AccountSummary(
                Account: account,
                Currency: null,
                AccountBalance:       p.AccountBalance.AsNullable(),
                CashOnHand:           p.CashOnHand.AsNullable(),
                MarginBalance:        p.MarginBalance.AsNullable(),
                AvailableBuyingPower: p.AvailableBuyingPower.AsNullable(),
                UsedBuyingPower:      p.UsedBuyingPower.AsNullable(),
                ReservedMargin:       p.ReservedMargin.AsNullable(),
                OpenPnl:              p.OpenPnl.AsNullable(),
                ClosedPnl:            p.ClosedPnl.AsNullable(),
                Timestamp:            ts);
            _session.HandleAccountSummary(summary);
        }
        else
        {
            var symbolPnl = new SymbolPnl(
                Account:          account,
                Exchange:         p.Exchange ?? "",
                Symbol:           p.Symbol,
                Position:         p.Position.AsNullable(),
                OpenPnl:          p.OpenPnl.AsNullable(),
                ClosedPnl:        p.ClosedPnl.AsNullable(),
                BuyQty:           p.BuyQty.AsNullable(),
                SellQty:          p.SellQty.AsNullable(),
                BuyWorkingQty:    p.BuyWorkingQty.AsNullable(),
                SellWorkingQty:   p.SellWorkingQty.AsNullable(),
                AvgOpenFillPrice: p.AvgOpenFillPrice.AsNullable(),
                Timestamp:        ts);
            _session.HandleSymbolPnl(symbolPnl);
        }
    }

    public override void FillReport(OrderFillReport o)
    {
        var account = ResolveAccount(o.Account);
        var side = o.BuySellType == Constants.BUY_SELL_TYPE_BUY ? OrderSide.Buy : OrderSide.Sell;
        var ts = IgnorableExt.ToUtc(o.Ssboe, o.Usecs);
        var fill = new Fill(
            Account:   account,
            Exchange:  o.Exchange ?? "",
            Symbol:    o.Symbol ?? "",
            Side:      side,
            Qty:       o.FillSize,
            Price:     o.FillPrice,
            OrderNum:  o.OrderNum,
            UserTag:   o.UserTag,
            Timestamp: ts);
        _session.HandleFill(fill);
    }
}

internal sealed class RithmicAdminCallbacks(RithmicSession session) : AdmCallbacks
{
    private readonly RithmicSession _session = session;

    public override void Alert(AlertInfo o)
    {
        var kind = o.AlertType switch
        {
            AlertType.ConnectionOpened => AlertKind.ConnectionOpened,
            AlertType.ConnectionClosed => AlertKind.ConnectionClosed,
            AlertType.ConnectionBroken => AlertKind.ConnectionBroken,
            AlertType.LoginComplete    => AlertKind.LoginComplete,
            AlertType.LoginFailed      => AlertKind.LoginFailed,
            _                          => AlertKind.Other,
        };
        _session.HandleAlert(new RithmicAlert(RithmicPlant.Admin, kind, o.Message ?? "", o.RpCode));
    }
}
