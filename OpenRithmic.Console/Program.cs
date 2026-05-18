using com.omnesys.omne.om;
using com.omnesys.rapi;
using RithmicBalancePnlTradeData;
using RithmicBalancePnlTradeData.Callbacks;

CliOptions opts;
try
{
    opts = CliOptions.Parse(args);
}
catch (ArgumentException ex)
{
    Console.Error.WriteLine(ex.Message);
    Console.Error.WriteLine();
    Console.Error.WriteLine(CliOptions.UsageText);
    return 2;
}

if (opts.Help)
{
    Console.Out.WriteLine(CliOptions.UsageText);
    return 0;
}

if (opts.ListConnections)
{
    foreach (var c in ConnectionRegistry.All)
        Console.Out.WriteLine(c.DisplayName);
    Console.Out.WriteLine($"({ConnectionRegistry.All.Count} connections)");
    return 0;
}

if (string.IsNullOrWhiteSpace(opts.User) || string.IsNullOrWhiteSpace(opts.Password))
{
    Console.Error.WriteLine("--user and --password are required.");
    Console.Error.WriteLine();
    Console.Error.WriteLine(CliOptions.UsageText);
    return 2;
}

var connection = ConnectionRegistry.Find(opts.Connection);
if (connection is null)
{
    Console.Error.WriteLine($"Unknown connection: \"{opts.Connection}\". Try --list-connections.");
    return 2;
}

if (!string.IsNullOrEmpty(opts.CertFile))
    Environment.SetEnvironmentVariable("MML_SSL_CLNT_AUTH_FILE", opts.CertFile);

Console.Out.WriteLine($"Using connection: {connection.DisplayName}");
Console.Out.WriteLine($"  DomainName  : {connection.DomainName}");
Console.Out.WriteLine($"  DmnSrvrAddr : {connection.DmnSrvrAddr}");

var callbacks = new RCallbacksImpl();
var engineParams = new REngineParams
{
    AppName = "RithmicBalancePnlTradeData",
    AppVersion = "1.0.0.0",
    AdmCallbacks = new AdmCallbacksImpl(),
    DomainName = connection.DomainName,
    DmnSrvrAddr = connection.DmnSrvrAddr,
    LicSrvrAddr = connection.LicSrvrAddr,
    LocBrokAddr = connection.LocBrokAddr,
    LoggerAddr = connection.LoggerAddr,
    LogFilePath = "rithmic.log",
};

REngine? engine = null;
using var shutdown = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    shutdown.Cancel();
};

try
{
    engine = new REngine(engineParams);

    var mdCnnctPt = opts.NoMarketData ? string.Empty : connection.MdCnnctPt;

    engine.login(
        callbacks,
        sMdEnvKey: string.Empty,
        sMdUser:   opts.User,
        sMdPassword: opts.Password,
        sMdCnnctPt: mdCnnctPt,
        sTsEnvKey: Constants.DEFAULT_ENVIRONMENT_KEY,
        sTsUser:   opts.User,
        sTsPassword: opts.Password,
        sTsCnnctPt: connection.TsCnnctPt,
        sPnlCnnctPt: connection.PnLCnnctPt,
        sIhEnvKey: string.Empty,
        sIhUser:   string.Empty,
        sIhPassword: string.Empty,
        sIhCnnctPt: string.Empty);

    WaitForLogin(callbacks, opts, shutdown.Token);

    if (shutdown.IsCancellationRequested) return 0;

    Console.Out.WriteLine("Waiting for AccountList...");
    while (!callbacks.GotAccounts && !shutdown.IsCancellationRequested)
        Thread.Sleep(250);
    if (shutdown.IsCancellationRequested) return 0;

    foreach (var account in callbacks.Accounts)
    {
        Console.Out.WriteLine($"Subscribing: {account.FcmId}/{account.IbId}/{account.AccountId}");
        engine.subscribeOrder(account);
        engine.subscribePnl(account);
        engine.replayPnl(account, null);
        engine.replayOpenOrders(account, null);
    }

    Console.Out.WriteLine("Streaming... press Ctrl+C to exit.");
    shutdown.Token.WaitHandle.WaitOne();
}
catch (OMException ex)
{
    Console.Error.WriteLine($"OMException: {ex.Message}");
    return 1;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Exception: {ex}");
    return 1;
}
finally
{
    try
    {
        if (callbacks.LoggedIntoTs || callbacks.LoggedIntoMd || callbacks.LoggedIntoPnL)
            engine?.logout();
    }
    catch { /* best-effort */ }
    try { engine?.shutdown(); } catch { /* best-effort */ }
}

return 0;

static void WaitForLogin(RCallbacksImpl cb, CliOptions opts, CancellationToken token)
{
    Console.Out.WriteLine("Waiting for login completion...");
    while (!token.IsCancellationRequested)
    {
        var mdReady = opts.NoMarketData || cb.LoggedIntoMd;
        if (mdReady && cb.LoggedIntoTs && cb.LoggedIntoPnL)
            return;
        Thread.Sleep(250);
    }
}
