# Architecture

## Purpose

OpenRithmic is a thin Rithmic R | API+ client that connects to any Rithmic
system/gateway and displays account balances, PnL updates, and trade fills in
the console. The core library (`OpenRithmic.Client`) wraps the SDK behind a
clean event-driven `RithmicSession`, making it consumable by any UI tier
(console, TUI, GUI) without direct API references.

## Layer Diagram

```
+--------------------------------------------------------------+
|  ConsoleRenderer (ConsoleApp)                                |  Presentation
|  - Formatted output for alerts, accounts, summaries, fills   |
|  - Dual-row PnL reconciliation (latest summary + symbols)    |
+--------------------------------------------------------------+
          | subscribes to events
          v
+--------------------------------------------------------------+
|  RithmicSession (OpenRithmic)                                |  Session layer
|  - ConnectAsync / Disconnect / Dispose                       |
|  - Event bus: Alert, AccountsLoaded, Summary, Symbol, Fill   |
|  - Readiness gate: TS + PnL plants logged in, accounts in    |
|  - Post-login subscribe/replay per account                   |
+--------------------------------------------------------------+
          | delegates to
          v
+--------------------------------------------------------------+
|  RithmicCallbacks / RithmicAdminCallbacks (Internal)         |  Callback bridge
|  - Override RCallbacks / AdmCallbacks                        |
|  - Map SDK types -> domain models                            |
|  - Resolve accounts via ConcurrentDictionary                 |
|  - Dispatch to session handler methods                       |
+--------------------------------------------------------------+
          | maps via
          v
+--------------------------------------------------------------+
|  RithmicMappers (Internal)                                   |  Type mappers
|  - ConnectionId -> RithmicPlant (switch)                     |
|  - AlertType -> AlertKind (switch)                           |
|  - BuySellType string -> OrderSide                           |
|  - IsAccountSummaryRow (empty symbol heuristic)              |
+--------------------------------------------------------------+
          | creates
          v
+--------------------------------------------------------------+
|  Domain Models (Account, AccountSummary, SymbolPnl, Fill,    |  Domain
|  RithmicAlert, RithmicConnection)                            |
|  - Immutable records / sealed classes                        |
|  - No SDK dependencies                                       |
+--------------------------------------------------------------+
          | calls
          v
+--------------------------------------------------------------+
|  rapiplus.dll (com.omnesys.rapi + com.omnesys.omne)          |  Rithmic SDK
|  - REngine, RCallbacks, AdmCallbacks, REngineParams        |
|  - .NET Framework 4.7.2 assembly consumed via compat shim    |
+--------------------------------------------------------------+
```

## Key Design Decisions

### Event-driven RithmicSession

`RithmicSession` is the single public API surface. It exposes events
(`Alert`, `AccountsLoaded`, `AccountSummaryUpdated`, `SymbolPnlUpdated`,
`FillReceived`) that consumers subscribe to. The internal `RithmicCallbacks`
class overrides the SDK callback interface and dispatches to `session.Handle*`
methods. This decouples the SDK's callback model from a .NET event model.

### Readiness gate via plant login tracking

`ConnectAsync` returns only when the session is fully ready. Readiness requires:
- Account list received
- Trading System plant logged in
- PnL plant logged in
- Market Data plant logged in (unless `EnableMarketData` is false)

A `TaskCompletionSource` is completed once all conditions are met, gated by a
lock. This lets callers `await` a fully-connected session rather than polling.

### Dual-row PnL trick

Rithmic emits PnL data on a single callback surface (`PnlUpdate`) that carries
two row types distinguished by whether `Symbol` is empty:
- Empty symbol = account-level summary (balance, margin, open/closed PnL)
- Non-empty symbol = per-symbol PnL (position, working qty, open/closed PnL)

`RithmicCallbacks.DispatchPnl` checks `IsAccountSummaryRow` and routes to
either `AccountSummary` or `SymbolPnl` handlers. `ConsoleRenderer` keeps the
latest summary per account and a dictionary of symbol PnLs, reprinting the
full account view on every event to avoid stale or partial displays.

### Embedded connection parameters

358 connection-param files ship as embedded resources (`OpenRithmic.RApiConfig.*`).
`ConnectionRegistry` loads them lazily via `Lazy<T>`, parses each file with
regex, and returns a `RithmicConnection` record. This eliminates external file
dependencies and lets the app support any Rithmic system/gateway out of the box.

### Plugin mode and no-MD secondary patterns

Two patterns allow multiple apps under the same Rithmic user:

1. **Plugin mode** (`--plugin`): Routes MD through R | Trader Pro's local
   listeners (127.0.0.1:3010). Requires R | Trader Pro running with "Allow
   Plug-ins" enabled.
2. **No-MD secondary** (`--enable-market-data false`): Sends empty `sMdCnnctPt`
   to `engine.login()` and skips the MD plant in the readiness gate. No R |
   Trader Pro needed; secondaries lose MD-only data.

`RithmicSession.ConnectAsync` implements both via `ConnectOptions`.

### Ignorable<T> unwrapping

The Rithmic SDK uses `Ignorable<double>`, `Ignorable<int>`, etc. to represent
potentially-absent numeric values. `IgnorableExt` provides `AsNullable()`
extension methods that unwrap these to `double?` / `int?` / `long?`, making
the domain models nullable-aware without SDK type leakage.

## File Map

| File | Responsibility |
|------|---------------|
| `RithmicSession.cs` | Session lifecycle, event bus, readiness gate, connect/disconnect |
| `RithmicConnection.cs` | Immutable record for parsed connection parameters |
| `ConnectionRegistry.cs` | Lazy loading + regex parsing of embedded .txt files |
| `Internal/RithmicCallbacks.cs` | RCallbacks + AdmCallbacks overrides -> session handlers |
| `Internal/RithmicMappers.cs` | SDK enum/string -> domain enum conversions |
| `Internal/IgnorableExt.cs` | Ignorable<T> unwrapping, timestamp conversion |
| `Models/*.cs` | Domain models (Account, AccountSummary, SymbolPnl, Fill, RithmicAlert) |
| `Console/Program.cs` | ~80-line CLI orchestrator |
| `Console/CliOptions.cs` | Manual CLI argument parsing |
| `Console/ConsoleRenderer.cs` | Formatted output, dual-row PnL reconciliation |

## Threading Model

- SDK callbacks (`RCallbacks`, `AdmCallbacks`) fire on internal API threads.
  Handlers copy needed fields and dispatch to `RithmicSession.Handle*` methods.
- `RithmicSession.HandleAlert` and `HandleAccountList` use a lock (`_gate`)
  to coordinate plant tracking and the readiness gate.
- `ConsoleRenderer` handlers are invoked from callback threads; they acquire
  a lock (`_gate`) before writing to `Console.Out` to prevent interleaved output.
- `ConnectAsync` awaits the readiness TCS on a background thread; the caller
  can pass a `CancellationToken` for timeout or cancellation.
