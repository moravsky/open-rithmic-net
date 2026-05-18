using System.Globalization;
using OpenRithmic;

namespace OpenRithmic.ConsoleApp;

internal static class ConsoleRenderer
{
    private static readonly object _gate = new();
    private static readonly CultureInfo _us = CultureInfo.GetCultureInfo("en-US");

    public static void Status(string message)
    {
        lock (_gate)
        {
            var prev = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Out.WriteLine($"  {message}");
            Console.ForegroundColor = prev;
        }
    }

    public static void Alert(RithmicAlert a)
    {
        // Suppress the dense connect/login-complete dumps; just render a single status line.
        var verb = a.Kind switch
        {
            AlertKind.ConnectionOpened => "opened",
            AlertKind.ConnectionClosed => "closed",
            AlertKind.ConnectionBroken => "broken",
            AlertKind.LoginComplete    => "logged in",
            AlertKind.LoginFailed      => "login failed",
            _                          => a.Kind.ToString(),
        };
        var msg = $"{a.Plant} {verb}";
        if (!string.IsNullOrWhiteSpace(a.Message) && a.Kind != AlertKind.ConnectionOpened && a.Kind != AlertKind.LoginComplete)
            msg += $" -- {a.Message}";
        Status(msg);
    }

    public static void Accounts(IReadOnlyList<Account> accounts)
    {
        lock (_gate)
        {
            Console.Out.WriteLine();
            Console.Out.WriteLine($"Accounts ({accounts.Count}):");
            foreach (var a in accounts)
                Console.Out.WriteLine($"  - {a.AccountId}  [{a.FcmId}/{a.IbId}]" +
                                      (string.IsNullOrEmpty(a.Name) ? "" : $"  {a.Name}"));
            Console.Out.WriteLine();
        }
    }

    public static void Summary(AccountSummary s)
    {
        lock (_gate)
        {
            var title = $" {s.Account.AccountId}  [{s.Account.FcmId}] ";
            var bar = new string('=', Math.Max(title.Length, 44));
            var prev = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Out.WriteLine(bar);
            Console.Out.WriteLine(title);
            Console.Out.WriteLine(bar);
            Console.ForegroundColor = prev;
            Write("Account Balance",      s.AccountBalance);
            Write("Cash On Hand",         s.CashOnHand);
            Write("Margin Balance",       s.MarginBalance);
            Write("Available Buying Pwr", s.AvailableBuyingPower);
            Write("Used Buying Power",    s.UsedBuyingPower);
            Write("Reserved Margin",      s.ReservedMargin);
            WriteSep();
            Write("Open P&L",             s.OpenPnl);
            Write("Closed P&L",           s.ClosedPnl);
            Console.Out.WriteLine($"  {"as of",-22}  {s.Timestamp.ToLocalTime():HH:mm:ss}");
            Console.Out.WriteLine(bar);
            Console.Out.WriteLine();
        }
    }

    public static void Symbol(SymbolPnl p)
    {
        lock (_gate)
        {
            var line = string.Format(_us,
                "  {0,-8} {1,-6}  pos={2,4}  open={3,12}  closed={4,12}  buy={5,4}  sell={6,4}  workBuy={7,3}  workSell={8,3}",
                p.Symbol,
                p.Exchange,
                p.Position?.ToString() ?? "-",
                Money(p.OpenPnl),
                Money(p.ClosedPnl),
                p.BuyQty?.ToString() ?? "-",
                p.SellQty?.ToString() ?? "-",
                p.BuyWorkingQty?.ToString() ?? "-",
                p.SellWorkingQty?.ToString() ?? "-");
            Console.Out.WriteLine(line);
        }
    }

    public static void Fill(Fill f)
    {
        lock (_gate)
        {
            var prev = Console.ForegroundColor;
            Console.ForegroundColor = f.Side == OrderSide.Buy ? ConsoleColor.Green : ConsoleColor.Red;
            Console.Out.WriteLine(string.Format(_us,
                "  FILL  {0,-4}  {1,-8} {2,-6}  qty={3,3}  px={4,12}  {5}  tag={6}",
                f.Side.ToString().ToUpperInvariant(),
                f.Symbol,
                f.Exchange,
                f.Qty,
                f.Price.ToString("N4", _us),
                f.Timestamp.ToLocalTime().ToString("HH:mm:ss.fff"),
                f.UserTag ?? "-"));
            Console.ForegroundColor = prev;
        }
    }

    public static void Goodbye(string message)
    {
        lock (_gate)
        {
            Console.Out.WriteLine();
            Console.Out.WriteLine(message);
        }
    }

    private static void Write(string label, double? value)
    {
        Console.Out.WriteLine($"  {label,-22}: {Money(value)}");
    }

    private static void WriteSep()
    {
        Console.Out.WriteLine(new string('-', 44));
    }

    private static string Money(double? v) =>
        v is null ? "          -" : v.Value.ToString("C2", _us).PadLeft(11);
}
