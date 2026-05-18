using OpenRithmic;
using OpenRithmic.ConsoleApp;

CliOptions opts;
try { opts = CliOptions.Parse(args); }
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

ConsoleRenderer.Status($"Connecting to {connection.DisplayName}...");

using var session = new RithmicSession();
session.Alert                 += ConsoleRenderer.Alert;
session.AccountsLoaded        += ConsoleRenderer.Accounts;
session.AccountSummaryUpdated += ConsoleRenderer.Summary;
session.SymbolPnlUpdated      += ConsoleRenderer.Symbol;
session.FillReceived          += ConsoleRenderer.Fill;

using var shutdown = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; shutdown.Cancel(); };

try
{
    await session.ConnectAsync(
        connection,
        opts.User!,
        opts.Password!,
        new ConnectOptions(
            IncludeMarketData: !opts.NoMarketData,
            PluginMode:        opts.Plugin,
            Timeout:           TimeSpan.FromSeconds(30)),
        shutdown.Token);
}
catch (OperationCanceledException)
{
    return 0;
}
catch (RithmicException ex)
{
    Console.Error.WriteLine($"Connect failed: {ex.Message}");
    return 1;
}

ConsoleRenderer.Status("Streaming. Press Ctrl+C to exit.");
try { await Task.Delay(Timeout.Infinite, shutdown.Token); }
catch (OperationCanceledException) { }

ConsoleRenderer.Goodbye("Disconnecting...");
session.Disconnect();
return 0;
