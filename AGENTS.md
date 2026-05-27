# OpenRithmic Project Guide

## Layout
- `OpenRithmic.Client/` -- class library (`namespace OpenRithmic`) that wraps R | API+ behind a clean event-driven `RithmicSession`. All `rapiplus.dll` references, callback overrides, `Ignorable<T>` unwrapping, and the dual-row PnL trick live here. The 358 connection-params files are embedded resources under `OpenRithmic.RApiConfig.*`.
- `OpenRithmic.Console/` -- thin console app that consumes `OpenRithmic.Client`. CLI parsing in `CliOptions.cs`, formatting in `ConsoleRenderer.cs`, ~80-line `Program.cs`.
- Future siblings: `OpenRithmic.Tui/` and `OpenRithmic.Gui/` will follow the same pattern -- subscribe to `RithmicSession` events, never reference `rapiplus.dll` directly.

The repo directory and solution are both `OpenRithmic.NET`. (If you ever see the directory at `RithmicBalancePnlTradeData/`, the host hasn't been renamed yet -- rename to `OpenRithmic.NET/` to match.)

## Purpose
A Rithmic R | API+ sample that connects to any Rithmic system/gateway and displays:
- Account list and balances
- PnL updates (open/closed/realized/unrealized)
- Trade data (fills and trade prints)

## Build & Run Commands
- **Build (solution)**: `dotnet build OpenRithmic.NET.slnx -c Release`
- **Build (console only)**: `dotnet build OpenRithmic.Console/OpenRithmic.Console.csproj -c Release`
- **Run (Rithmic Test default)**: `dotnet run --project OpenRithmic.Console -c Release -- --user <user> --password <password>`
- **Run (specific gateway)**: `dotnet run --project OpenRithmic.Console -c Release -- --connection "Rithmic Test/Orangeburg" --user <user> --password <password>`
- **List connections**: `dotnet run --project OpenRithmic.Console -c Release -- --list-connections`
- **SSL cert file**: optional. The .NET R | API+ does not require an SSL cert auth file for normal login -- the shipped `SampleOrder.NET` does not set one and works against Rithmic Test. The `--cert <path>` flag exists to set `MML_SSL_CLNT_AUTH_FILE` if some flow needs it later; leave it unset unless Rithmic says otherwise.

## Connection Configuration (multi-gateway)
Every supported `(System, Gateway)` pair ships as an embedded resource that the app loads and parses at startup.

- **In-repo location**: `OpenRithmic.Client/RApiConfig/<System>_<Gateway>_connection_params.txt` (358 files, e.g. `Rithmic-Test_Orangeburg`, `TopstepTrader_Chicago-Area`, `Apex_Europe`, ...).
- **csproj embedding**: include them with `<EmbeddedResource Include="RApiConfig\*.txt" />` so they ship inside the assembly (no external file dependency).
- **Parsing**: each file's header has `System Name : <S>` and `Gateway Name : <G>`; the ".NET" block underneath lists `REngineParams.AdmCnnctPt / DmnSrvrAddr / DomainName / LicSrvrAddr / LocBrokAddr / LoggerAddr` and the `login()` connect points (`sMdCnnctPt`, `sIhCnnctPt`, `sTsCnnctPt`, `sPnLCnnctPt`). Parse those lines into a `RithmicConnection` record.
- **Selection**: a single `--connection "<System>/<Gateway>"` CLI flag picks one. The flag value matches the parsed `System Name` and `Gateway Name` (case-insensitive, trimmed). Provide `--list-connections` to enumerate what's embedded.
- **Default**: `Rithmic Test/Orangeburg` (UAT) when `--connection` is omitted, so first-run smoke tests are safe.

## Environment & Tech Stack
- **SDK**: .NET 10 SDK (latest), SDK-style csproj. Matches the Quantower setup that consumes Rithmic's rapi.dll from modern .NET.
- **Target Framework**: `net10.0` (or `net10.0-windows` if a Windows-only API surface is needed).
- **Architecture**: AnyCPU (rapiplus.dll is AnyCPU managed).
- **Platform**: Windows 11 ARM (running x64 .NET via emulation).
- **Referencing rapiplus.dll**: `rapiplus.dll` ships as a .NET Framework 4.7.2 assembly but is pure managed plumbing/networking, so modern .NET can consume it via the netstandard compatibility shim. Reference it directly with `<Reference Include="rapiplus"><HintPath>...\win10\lib_472\rapiplus.dll</HintPath></Reference>`.
- **Domain**: `com.omnesys.rapi` (REngine, RCallbacks, AdmCallbacks) and `com.omnesys.omne.om`.

## REngineParams / login() field mapping
The parser populates a `RithmicConnection` from the embedded `.txt` and the app maps it onto Rithmic SDK objects as follows:

`REngineParams` (set before constructing `REngine`):
- `DomainName`  <- `REngineParams.DomainName`  (e.g. `rithmic_uat_dmz_domain` for Rithmic Test)
- `DmnSrvrAddr` <- `REngineParams.DmnSrvrAddr` (tilde-separated host:port list)
- `LicSrvrAddr` <- `REngineParams.LicSrvrAddr`
- `LocBrokAddr` <- `REngineParams.LocBrokAddr`
- `LoggerAddr`  <- `REngineParams.LoggerAddr`

Note on `AdmCnnctPt`: the `.txt` files list `REngineParams.AdmCnnctPt : dd_admin_sslc` in their ".NET" block, but the .NET `REngineParams` class does NOT expose an `AdmCnnctPt` property -- only `AdmCallbacks`, `AppName`, `AppVersion`, `DmnSrvrAddr`, `DomainName`, `LicSrvrAddr`, `Listeners`, `LocBrokAddr`, `LogFilePath`, `LoggerAddr`, `UseTraceSource`. The admin connect point is hardcoded inside the .NET wrapper, so the parsed value is captured in `RithmicConnection.AdmCnnctPt` for reference only and is intentionally not assigned to `REngineParams`.

`REngine.login()` connect points used by this sample:
- `sMdCnnctPt`  <- `login_agent_tpc` (set if subscribing market data; not needed for balances/PnL/trades only)
- `sIhCnnctPt`  <- `login_agent_historyc`
- `sTsCnnctPt`  <- `login_agent_opc`    (trading system -- required for accounts, orders, fills)
- `sPnLCnnctPt` <- `login_agent_pnlc`   (PnL feed -- required for PnlUpdate callbacks)

`REngine.loginRepository()` connect point:
- `sCnnctPt`    <- `login_agent_repositoryc`

## R | API+ Callback Surface (relevant to this sample)
Override these on the `RCallbacks` subclass:
- `AccountList(AccountListInfo)`     -- enumerate accounts after login
- `ProductRmsList(ProductRmsListInfo)` -- per-product risk/margin and account balance fields
- `PnlReplay(PnlReplayInfo)`         -- snapshot of current PnLs after `subscribePnL`
- `PnlUpdate(PnlInfo)`               -- streaming PnL updates
- `SodUpdate(SodReport)`             -- start-of-day account state
- `FillReport(OrderFillReport)`      -- own-account fills
- `TradePrint(TradeInfo)`            -- market trade prints (only if MD subscribed)
- `ExecutionReplay(ExecutionReplayInfo)` -- replay of historical fills for an account
- `Alert(AlertInfo)` on both `RCallbacks` and `AdmCallbacks` -- connection/login transitions

Typical startup sequence:
1. `loginRepository` -> wait for `AccountList`
2. `login` with PnL + TS connect points -> wait for `Alert` LoggedIntoTs / LoggedIntoPnL
3. `subscribeOrder(account)` -> receive `FillReport`s
4. `subscribePnL(account)` -> receive `PnlReplay` then `PnlUpdate` stream

## R | Trader Pro Plug-In Mode
Rithmic lets an R | API app attach to a running R | Trader Pro process as a plug-in client. R | Trader Pro then acts as a proxy and multiple plug-in apps can share the underlying Rithmic session without kicking each other off. Use it via `ConnectOptions(PluginMode: true)` (the console exposes `--plugin`). Mechanics:

- Before `new REngine(...)`, set env vars `RAPI_MD_ENCODING=4` and `RAPI_IH_ENCODING=4`. `RithmicSession` does this when `PluginMode` is on.
- Override `sMdCnnctPt` to `127.0.0.1:3010` and (if you also want IH) `sIhCnnctPt` to `127.0.0.1:3012`. We currently only swap MD; IH stays unused in this sample.
- R | Trader Pro must be running with **Allow Plug-ins** enabled (button turns yellow on the login screen) and logged in with the same User ID we pass.

## Plant Capability & Concurrency
Rithmic's "one session per user" enforcement is per-plant, not global:

- **MD plant** -- exclusive. A second login on the same user kicks the first off.
- **TS plant** -- tolerates concurrent logins from the same Rithmic user.
- **PnL plant** -- tolerates concurrent logins from the same Rithmic user.

With MD disabled you still get accounts, balances, fills/status reports, account-level risk/lockouts, and live open/closed PnL. You lose ticks/quotes/L2, TradePrint, BestBid/AskQuote, symbol search, and instrument reference data -- all MD-only.

This gives two patterns for running multiple apps under the same Rithmic user:

1. **Plug-in mode** (`--plugin` / `ConnectOptions(PluginMode: true)`) -- R | Trader Pro proxies the Rithmic session; every plug-in keeps MD. Requires R | Trader Pro running with **Allow Plug-ins** on.
2. **No-MD secondaries** (`--enable-market-data false` / `ConnectOptions(EnableMarketData: false)`) -- secondary apps pass `sMdCnnctPt = string.Empty` to `engine.login()` and just skip the MD plant. No R | Trader Pro needed; secondaries lose MD-only data.

`RithmicSession` already implements (2): when `EnableMarketData` is false it sends an empty MD connect point and excludes the MD plant from the readiness wait.

## Coding Standards
- **Style**: `<LangVersion>latest</LangVersion>` -- use modern C# features (primary constructors, collection expressions, file-scoped namespaces, nullable refs). Enable `<Nullable>enable</Nullable>`.
- **Naming**: PascalCase for methods/properties; prefix private fields with `_`. The Rithmic sample uses `PRI_` prefix and Hungarian (`oInfo`, `sUser`) -- do NOT propagate that style into our code, but match it inside files copied verbatim from the reference.
- **Match local style**: Before adding new code, read nearby code in the same file. New code should be similar to existing code in formatting, naming, log phrasing, and structural choices.
- **Line length**: Aim for under 120 characters per line.
- **Logging**: Print to `Console.Out` for normal output and `Console.Error` for failures/alerts. One line per event, prefixed with the callback name (e.g. `[PnlUpdate] account=... open=... realized=...`).
- **Threading**: R | API+ callbacks fire on internal API threads. Do not block them; copy needed fields and hand off if doing meaningful work. For this sample, formatting and `Console.WriteLine` is fine.

## Reference Source & Dependencies
- **R | API+ reference**: `..\RithmicRef\RApiPlus.NET.13.6.0.0\13.6.0.0`
    - DLL: `win10\lib_472\rapiplus.dll` (the lib_35 build is .NET 3.5; we use lib_472 from modern .NET via compatibility shim).
    - Doxygen HTML docs: `doc\html\annotated.html`
    - Sample sources: `samples\SamplesPlus.NET_src\` (SampleOrder.cs is the most relevant -- accounts, fills, PnL, trade routes).
    - Sample projects (.NET Framework 4.7.2): `samples\SamplesPlus.NET_472\`
- **Standalone Orangeburg sample**: `..\RithmicRef\Rithmic Test_Orangeburg_connection_params.txt` (same format, useful as a single-file reference).
- **SSL cert auth file**: not required for the .NET API in this sample's flow. The C++ docs mention `MML_SSL_CLNT_AUTH_FILE`, but `SampleOrder.NET` (Rithmic's own .NET sample) logs in successfully without it. If a future flow demands one, pass the path via `--cert` and the app will set `MML_SSL_CLNT_AUTH_FILE` before constructing `REngine`.

# Git Guidelines
- Style: Intent-First (Imperative mood, no prefixes like 'fix:' or 'feat:').
- Length: Aim for 50 characters; absolute max 72.
- Format:
    - Subject: A single strong sentence starting with a verb.
    - Body: 1 to 5 bullet points explaining the "Why" and "How."
    - Example: "Print PnlUpdate stream after subscribePnL completes"
- Don't include Co-Authored-By footers in commit messages.
- Always preview the commit message before final execution.

# Text & Encoding Rules
- Strict ASCII Only: Do not use non-keyboard characters in code, comments, or commit messages.
- No Typography Glyphs: Avoid emojis, "smart" quotes, long dashes, and unicode arrows.
- Standard Substitutes: Use standard hyphens (-), double hyphens (--), and ASCII arrows (-> or =>) instead.

# Secrets
- Never commit Rithmic credentials or any SSL cert auth file.
- Pass user/password via command-line args or env vars; do not hardcode them in source.

# Architecture Documentation
- ARCHITECTURE.md contains the architectural layer diagram, key design decisions, and file map.
- Update it when important decisions happen: new patterns, removed features, changed threading models,
  new connection modes, or shifts in how components interact.
- Keep it concise -- one page per major subsystem. The goal is "why we did it this way," not a code walkthrough.
- If a decision is irreversible or affects future maintainers, document it in ARCHITECTURE.md even if AGENTS.md covers the "how."
