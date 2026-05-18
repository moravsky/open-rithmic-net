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
        var alert = new RithmicAlert(
            RithmicMappers.MapPlant(o.ConnectionId),
            RithmicMappers.MapAlertKind(o.AlertType),
            o.Message ?? "",
            o.RpCode);
        _session.HandleAlert(alert);
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

        if (RithmicMappers.IsAccountSummaryRow(p.Symbol))
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
        var side = RithmicMappers.MapSide(o.BuySellType);
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
        var alert = new RithmicAlert(
            RithmicPlant.Admin,
            RithmicMappers.MapAlertKind(o.AlertType),
            o.Message ?? "",
            o.RpCode);
        _session.HandleAlert(alert);
    }
}
