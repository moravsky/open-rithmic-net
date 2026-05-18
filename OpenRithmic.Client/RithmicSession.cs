using com.omnesys.omne.om;
using com.omnesys.rapi;
using OpenRithmic.Internal;

namespace OpenRithmic;

public sealed record ConnectOptions(
    bool IncludeMarketData = true,
    TimeSpan? Timeout = null,
    string? LogFilePath = "rithmic.log");

public sealed class RithmicSession : IDisposable
{
    private readonly string _appName;
    private readonly string _appVersion;
    private readonly object _gate = new();

    private REngine? _engine;
    private RithmicCallbacks? _callbacks;
    private TaskCompletionSource? _readyTcs;
    private ConnectOptions _options = new();
    private readonly HashSet<RithmicPlant> _loggedInPlants = new();
    private bool _accountsReceived;
    private bool _disposed;

    public IReadOnlyList<Account> Accounts { get; private set; } = Array.Empty<Account>();
    public bool IsConnected => _engine is not null && !_disposed;

    public event Action<RithmicAlert>? Alert;
    public event Action<IReadOnlyList<Account>>? AccountsLoaded;
    public event Action<AccountSummary>? AccountSummaryUpdated;
    public event Action<SymbolPnl>? SymbolPnlUpdated;
    public event Action<Fill>? FillReceived;

    public RithmicSession(string appName = "OpenRithmic", string appVersion = "1.0.0.0")
    {
        _appName = appName;
        _appVersion = appVersion;
    }

    public async Task ConnectAsync(
        RithmicConnection connection,
        string user,
        string password,
        ConnectOptions? options = null,
        CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_engine is not null)
            throw new InvalidOperationException("Already connected.");

        _options = options ?? new ConnectOptions();
        _readyTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _callbacks = new RithmicCallbacks(this);

        var engineParams = new REngineParams
        {
            AppName      = _appName,
            AppVersion   = _appVersion,
            AdmCallbacks = new RithmicAdminCallbacks(this),
            DomainName   = connection.DomainName,
            DmnSrvrAddr  = connection.DmnSrvrAddr,
            LicSrvrAddr  = connection.LicSrvrAddr,
            LocBrokAddr  = connection.LocBrokAddr,
            LoggerAddr   = connection.LoggerAddr,
            LogFilePath  = _options.LogFilePath ?? string.Empty,
        };

        try
        {
            _engine = new REngine(engineParams);

            var mdCnnctPt = _options.IncludeMarketData ? connection.MdCnnctPt : string.Empty;

            _engine.login(
                _callbacks,
                sMdEnvKey:    string.Empty,
                sMdUser:      user,
                sMdPassword:  password,
                sMdCnnctPt:   mdCnnctPt,
                sTsEnvKey:    Constants.DEFAULT_ENVIRONMENT_KEY,
                sTsUser:      user,
                sTsPassword:  password,
                sTsCnnctPt:   connection.TsCnnctPt,
                sPnlCnnctPt:  connection.PnLCnnctPt,
                sIhEnvKey:    string.Empty,
                sIhUser:      string.Empty,
                sIhPassword:  string.Empty,
                sIhCnnctPt:   string.Empty);
        }
        catch (OMException ex)
        {
            Cleanup();
            throw new RithmicException(ex.Message, ex);
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        if (_options.Timeout is { } t)
            timeoutCts.CancelAfter(t);
        await using (timeoutCts.Token.Register(() => _readyTcs?.TrySetCanceled(timeoutCts.Token)))
        {
            try { await _readyTcs.Task.ConfigureAwait(false); }
            catch
            {
                Cleanup();
                throw;
            }
        }

        // Subscribe + replay per account so balance/orders show up immediately.
        foreach (var account in Accounts)
        {
            var info = new AccountInfo(account.FcmId, account.IbId, account.AccountId);
            _engine!.subscribeOrder(info);
            _engine!.subscribePnl(info);
            _engine!.replayPnl(info, null);
            _engine!.replayOpenOrders(info, null);
        }
    }

    public void Disconnect()
    {
        if (_disposed || _engine is null) return;
        try { _engine.logout(); } catch { /* best-effort */ }
        try { _engine.shutdown(); } catch { /* best-effort */ }
        Cleanup();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Disconnect();
    }

    private void Cleanup()
    {
        _engine = null;
        _callbacks = null;
        _readyTcs = null;
        lock (_gate)
        {
            _loggedInPlants.Clear();
            _accountsReceived = false;
        }
    }

    // ---- Internal event sink (invoked from callbacks) ----

    internal void HandleAlert(RithmicAlert alert)
    {
        Alert?.Invoke(alert);
        if (alert.Kind == AlertKind.LoginComplete)
            lock (_gate)
            {
                _loggedInPlants.Add(alert.Plant);
                TryCompleteReady_NoLock();
            }
        else if (alert.Kind == AlertKind.LoginFailed)
            _readyTcs?.TrySetException(
                new RithmicException($"Login failed on {alert.Plant}: {alert.Message} (rp={alert.ResponseCode})"));
    }

    internal void HandleAccountList(IReadOnlyList<Account> accounts)
    {
        Accounts = accounts;
        AccountsLoaded?.Invoke(accounts);
        lock (_gate)
        {
            _accountsReceived = true;
            TryCompleteReady_NoLock();
        }
    }

    internal void HandleAccountSummary(AccountSummary s) => AccountSummaryUpdated?.Invoke(s);
    internal void HandleSymbolPnl(SymbolPnl p)           => SymbolPnlUpdated?.Invoke(p);
    internal void HandleFill(Fill f)                     => FillReceived?.Invoke(f);

    private void TryCompleteReady_NoLock()
    {
        if (!_accountsReceived) return;
        if (!_loggedInPlants.Contains(RithmicPlant.TradingSystem)) return;
        if (!_loggedInPlants.Contains(RithmicPlant.PnL)) return;
        if (_options.IncludeMarketData && !_loggedInPlants.Contains(RithmicPlant.MarketData)) return;
        _readyTcs?.TrySetResult();
    }
}

public sealed class RithmicException : Exception
{
    public RithmicException(string message) : base(message) { }
    public RithmicException(string message, Exception inner) : base(message, inner) { }
}
