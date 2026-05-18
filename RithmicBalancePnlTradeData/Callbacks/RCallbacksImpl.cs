using System.Text;
using com.omnesys.rapi;

namespace RithmicBalancePnlTradeData.Callbacks;

internal sealed class RCallbacksImpl : RCallbacks
{
    private readonly List<AccountInfo> _accounts = new();
    private readonly object _gate = new();

    private volatile bool _loggedIntoMd;
    private volatile bool _loggedIntoTs;
    private volatile bool _loggedIntoPnL;
    private volatile bool _loggedIntoIh;
    private volatile bool _gotAccounts;

    public bool LoggedIntoMd => _loggedIntoMd;
    public bool LoggedIntoTs => _loggedIntoTs;
    public bool LoggedIntoPnL => _loggedIntoPnL;
    public bool LoggedIntoIh => _loggedIntoIh;
    public bool GotAccounts => _gotAccounts;

    public IReadOnlyList<AccountInfo> Accounts
    {
        get { lock (_gate) return _accounts.ToArray(); }
    }

    public override void Alert(AlertInfo oInfo)
    {
        var sb = new StringBuilder();
        sb.Append("[Alert] ");
        oInfo.Dump(sb);
        Console.Out.Write(sb);

        if (oInfo.AlertType == AlertType.LoginComplete)
        {
            switch (oInfo.ConnectionId)
            {
                case ConnectionId.MarketData:    _loggedIntoMd = true;  break;
                case ConnectionId.TradingSystem: _loggedIntoTs = true;  break;
                case ConnectionId.PnL:           _loggedIntoPnL = true; break;
                case ConnectionId.History:       _loggedIntoIh = true;  break;
            }
        }
    }

    public override void AccountList(AccountListInfo oInfo)
    {
        var sb = new StringBuilder();
        sb.Append("[AccountList] ");
        oInfo.Dump(sb);
        sb.Append('\n');
        Console.Out.Write(sb);

        lock (_gate)
        {
            _accounts.Clear();
            for (int i = 0; i < oInfo.Accounts.Count; i++)
            {
                var src = oInfo.Accounts[i];
                _accounts.Add(new AccountInfo(src.FcmId, src.IbId, src.AccountId));
            }
        }
        _gotAccounts = true;
    }

    public override void ProductRmsList(ProductRmsListInfo oInfo)
    {
        var sb = new StringBuilder();
        sb.Append("[ProductRmsList] ");
        oInfo.Dump(sb);
        sb.Append('\n');
        Console.Out.Write(sb);
    }

    public override void PnlReplay(PnlReplayInfo oInfo)
    {
        var account = $"{oInfo.Account.FcmId}/{oInfo.Account.IbId}/{oInfo.Account.AccountId}";
        for (int i = 0; i < oInfo.PnlInfoList.Count; i++)
            PrintPnl("PnlReplay", account, oInfo.PnlInfoList[i]);
    }

    public override void PnlUpdate(PnlInfo oInfo)
    {
        var account = $"{oInfo.Account.FcmId}/{oInfo.Account.IbId}/{oInfo.Account.AccountId}";
        PrintPnl("PnlUpdate", account, oInfo);
    }

    private static void PrintPnl(string tag, string account, PnlInfo p)
    {
        if (string.IsNullOrEmpty(p.Symbol))
            PrintAccountSummary(tag, account, p);
        else
            PrintSymbolPnl(tag, account, p);
    }

    private static void PrintAccountSummary(string tag, string account, PnlInfo p)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"[{tag}] Account Summary: {account}");
        sb.AppendLine($"  AccountBalance       : {Fmt(p.AccountBalance)}");
        sb.AppendLine($"  CashOnHand           : {Fmt(p.CashOnHand)}");
        sb.AppendLine($"  MarginBalance        : {Fmt(p.MarginBalance)}");
        sb.AppendLine($"  AvailableBuyingPower : {Fmt(p.AvailableBuyingPower)}");
        sb.AppendLine($"  UsedBuyingPower      : {Fmt(p.UsedBuyingPower)}");
        sb.AppendLine($"  ReservedMargin       : {Fmt(p.ReservedMargin)}");
        sb.AppendLine($"  OpenPnL              : {Fmt(p.OpenPnl)}");
        sb.AppendLine($"  ClosedPnL            : {Fmt(p.ClosedPnl)}");
        Console.Out.Write(sb);
    }

    private static void PrintSymbolPnl(string tag, string account, PnlInfo p)
    {
        Console.Out.WriteLine(
            $"[{tag}] {account} {p.Exchange}:{p.Symbol} " +
            $"pos={Fmt(p.Position)} openPnL={Fmt(p.OpenPnl)} closedPnL={Fmt(p.ClosedPnl)} " +
            $"buyQty={Fmt(p.BuyQty)} sellQty={Fmt(p.SellQty)} " +
            $"workBuy={Fmt(p.BuyWorkingQty)} workSell={Fmt(p.SellWorkingQty)} " +
            $"avgOpen={Fmt(p.AvgOpenFillPrice)}");
    }

    private static string Fmt(Ignorable<double> v) =>
        v.Use ? v.Value.ToString("0.########") : "-";

    private static string Fmt(Ignorable<int> v) =>
        v.Use ? v.Value.ToString() : "-";

    private static string Fmt(Ignorable<long> v) =>
        v.Use ? v.Value.ToString() : "-";

    public override void SodUpdate(SodReport oReport)
    {
        var sb = new StringBuilder();
        sb.Append("[SodUpdate] ");
        oReport.Dump(sb);
        sb.Append('\n');
        Console.Out.Write(sb);
    }

    public override void FillReport(OrderFillReport oReport)
    {
        var sb = new StringBuilder();
        sb.Append("[FillReport] ");
        oReport.Dump(sb);
        sb.Append('\n');
        Console.Out.Write(sb);
    }

    public override void ExecutionReplay(ExecutionReplayInfo oInfo)
    {
        var sb = new StringBuilder();
        sb.Append("[ExecutionReplay] ");
        oInfo.Dump(sb);
        sb.Append('\n');
        Console.Out.Write(sb);
    }

    public override void TradePrint(TradeInfo oInfo)
    {
        var sb = new StringBuilder();
        sb.Append("[TradePrint] ");
        oInfo.Dump(sb);
        sb.Append('\n');
        Console.Out.Write(sb);
    }
}
