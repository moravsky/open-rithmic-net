# OpenRithmic.NET

A clean, event-driven .NET wrapper around Rithmic's R | API+ that keeps
the plumbing out of your host code.

## Why

- **Hides R | API+ quirks.** E.g. dual-row `PnlInfo` is split into clean account-summary and per-symbol events.
- **Plug-in mode.** `--plugin` attaches to a running R | Trader Pro instead of burning your MD session.
- **Agent-friendly.** Drop the Rithmic .NET SDK at `..\RithmicRef\RApiPlus.NET.13.6.0.0`; point any AI coding agent at the repo and it rips. `AGENTS.md` covers the rest.

## Run it

```powershell
dotnet run --project OpenRithmic.Console -c Release -- `
    --user <user> --password <password>
```

That hits the default `Rithmic Test/Orangeburg` UAT gateway. Pick a different
one with `--connection`, or list all possiblities with:

```powershell
dotnet run --project OpenRithmic.Console -c Release -- --list-connections
```

You should see something like:

```
  Connecting to Rithmic Test/Orangeburg...
  Repository logged in
  TradingSystem logged in
  PnL logged in

Accounts (1):
  - DEMO-12345  [DemoFCM/DEMO]

=============================================
 DEMO-12345  [DemoFCM]
=============================================
  Account Balance       :   $50,000.00
  Cash On Hand          :   $50,000.00
  Margin Balance        :        $0.00
  Available Buying Pwr  :   $50,000.00
  Open P&L              :        $0.00
  Closed P&L            :        $0.00
=============================================

  FILL  BUY   ESZ5     CME   qty=1   px=5,000.2500   14:32:11.412
```

### Sharing Rithmic connection via R | Trader Pro (plug-in mode)

R | Trader Pro software can act as a connection proxy: launch it once with
"Allow Plug-ins" enabled, and any number of plug-in apps can attach
without burning extra Rithmic sessions or kicking each other off.

1. Download and install R | Trader Pro from
   [rithmic.com](https://www.rithmic.com/products/r-trader-pro)
   (version 15.x or newer; earlier builds don't support plug-ins).
2. On its login screen, click **Allow Plug-ins** -- the button turns yellow
   when enabled.
3. Log in to R | Trader Pro using the same Rithmic credentials you
   plan to pass to `--user` / `--password` below.
4. Run OpenRithmic.Console with `--plugin`:

   ```powershell
   dotnet run --project OpenRithmic.Console -c Release -- `
       --user <user> --password <password> --plugin
   ```
5. Run other Rithmic apps in plugin mode. You can launch multiple plug-in apps in parallel; they all share R | Trader Pro's underlying Rithmic connection.


## Layout

| Project                     | What it is                                                          |
| --------------------------- | ------------------------------------------------------------------- |
| `OpenRithmic.Client/`       | Class library (`namespace OpenRithmic`). All `rapiplus.dll` lives here. |
| `OpenRithmic.Console/`      | Thin console host that subscribes to `RithmicSession` events.       |
| `OpenRithmic.Client.Tests/` | xUnit tests for the parser, registry, mappers, and session sinks.   |

## Consume `RithmicSession` from your own host

```csharp
using OpenRithmic;

var connection = ConnectionRegistry.Find("Rithmic Test/Orangeburg")!;

using var session = new RithmicSession();
session.AccountsLoaded        += list => Console.WriteLine($"{list.Count} accounts");
session.AccountSummaryUpdated += s => Console.WriteLine($"{s.Account.AccountId} bal={s.AccountBalance}");
session.SymbolPnlUpdated      += p => Console.WriteLine($"{p.Symbol} pos={p.Position}");
session.FillReceived          += f => Console.WriteLine($"FILL {f.Side} {f.Symbol} @ {f.Price}");

await session.ConnectAsync(connection, user: "u", password: "p");
```

`ConnectAsync` returns after both PnL and Trading System plants are logged
in and accounts are loaded. It has already called `subscribePnl`/`replayPnl`
per account, so events start flowing immediately.

## Requirements

- .NET 10 SDK
- `rapiplus.dll` from Rithmic's `RApiPlus.NET.13.6.0.0/win10/lib_472/`. The
  csproj uses a `RithmicRefRoot` property that defaults to `..\..\RithmicRef`
  -- adjust if your layout differs.

## Security

Connections to Rithmic are TLS-encrypted, and the server's certificate is
verified by the .NET TLS stack against the Windows trust store. You
authenticate with your Rithmic username/password over that encrypted
channel -- same path Rithmic's own `SampleOrder.NET` ships with.

The optional `--cert <path>` flag (which sets `MML_SSL_CLNT_AUTH_FILE`)
adds **client-side** certificate authentication so the server verifies
you cryptographically in addition to username/password. Most paper and
test environments don't need it; some higher-tier live deployments do.
Without it the connection is still encrypted -- you just have one auth
factor instead of two.

## Author

**Petr Moravsky** ([petr@structuredtrading.co](mailto:petr@structuredtrading.co)) -- futures trader and developer.

If OpenRithmic.NET saved you a weekend researching Rithmic API, helped you ship a Rithmic-backed app, or served as a reference for your own development -- star the repo and [leave a tip](https://ko-fi.com/moravsky).

## Disclaimer

OpenRithmic.NET is an unofficial, independent open-source library. It is
not affiliated with, endorsed by, or sponsored by Rithmic, Omnesys, or
any related entity. The Rithmic API and `rapiplus.dll` are separate
products with their own licensing terms; you must obtain them directly
from Rithmic.

OMNE(TM) is a trademark of Omnesys, LLC and Omnesys Technologies, Inc.
The R | API(TM) and R | API+(TM) software is Copyright (C) 2019 by
Rithmic, LLC. Trading Platform by Rithmic(TM) is a trademark of Rithmic,
LLC. The OMNE(TM) software is Copyright (C) 2019 by Omnesys, LLC and
Omnesys Technologies, Inc. All rights reserved.

## Stability

Pre-1.0, subject to change. Public API, project layout, and behavior may
change without notice until things stabilize.
