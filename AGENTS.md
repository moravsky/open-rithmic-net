# RithmicBalancePnlTradeData Project Guide

## Purpose
A Rithmic R | API+ sample that connects to any Rithmic system/gateway and displays:
- Account list and balances
- PnL updates (open/closed/realized/unrealized)
- Trade data (fills and trade prints)

## Build & Run Commands
- **Build**: `dotnet build RithmicBalancePnlTradeData/RithmicBalancePnlTradeData.csproj -c Release`
- **Run (Rithmic Test default)**: `dotnet run --project RithmicBalancePnlTradeData -c Release -- --user <user> --password <password>`
- **Run (specific gateway)**: `dotnet run --project RithmicBalancePnlTradeData -c Release -- --connection "Rithmic Test/Orangeburg" --user <user> --password <password>`
- **List connections**: `dotnet run --project RithmicBalancePnlTradeData -c Release -- --list-connections`
- **SSL cert file**: optional. The .NET R | API+ does not require an SSL cert auth file for normal login -- the shipped `SampleOrder.NET` does not set one and works against Rithmic Test. The `--cert <path>` flag exists to set `MML_SSL_CLNT_AUTH_FILE` if some flow needs it later; leave it unset unless Rithmic says otherwise.

## Connection Configuration (multi-gateway)
Every supported `(System, Gateway)` pair ships as an embedded resource that the app loads and parses at startup.

- **In-repo location**: `RithmicBalancePnlTradeData/RApiConfig/<System>_<Gateway>_connection_params.txt` (325 files, e.g. `Rithmic-Test_Orangeburg`, `TopstepTrader_Chicago-Area`, `Apex_Europe`, ...).
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
