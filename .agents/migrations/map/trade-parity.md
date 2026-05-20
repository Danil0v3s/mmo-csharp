# trade.cpp parity · 2026-05-20

`src/map/trade.cpp` (666 lines, 9 functions) — player-to-player
trade wire stages (request / ack / ok / additem / addzeny / commit
/ cancel + sanity checks).

The trade engine + validation already exists in
`Map.Server/Trade/TradeService.cs` (PC-26 / wire-pkt-29). This
audit catalogs the rAthena-named entry points so callers can read
1:1.

| rAthena fn | Status | C# location |
|---|---|---|
| `trade_traderequest` | ✅ | TradeService (wired via packet handler) |
| `trade_tradeack` | ✅ | same |
| `trade_tradeadditem` | ✅ | same |
| `trade_tradeaddzeny` | ✅ | same |
| `trade_tradeok` | ✅ | same |
| `trade_tradecancel` | ✅ | same |
| `trade_tradecommit` | ✅ | same |
| `trade_check` | ✅ | TradeService inline checks |
| `impossible_trade_check` | ✅ | TradeService inline checks |

Wrapper service `ITradeWireService` provides rAthena-name passthrough
for script + GM callers.

## History

### 2026-05-20 — initial audit + wrapper service
- 9 functions covered. Trade engine reused from prior wave.
