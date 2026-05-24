# trade.cpp parity · 2026-05-22 (T9.D — per-fn rollup)

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

## Coverage summary

| Bucket | ✅ | ⚠️ | ❌ | Total |
|---|---|---|---|---|
| Trade wire stages | 9 | 0 | 0 | 9 |
| **Totals** | **9** | **0** | **0** | **9** |

100% parity — every public trade.cpp function has a real C# impl
backed by TradeService.

## History

### 2026-05-24 — P2.1 doc-resync close-out (0 stale ⚠️ → ✅; 0 genuine gaps remain)

Verified: doc is at 100% ✅; ⚠️ grep hits are header glyphs only. No-op resync.

### 2026-05-22 — T9.D per-fn rollup

Per-function audit. Baseline: **9 ✅ / 0 ⚠️ / 0 ❌** — 100% parity.
Trade is feature-complete: invite, accept, decline, distance
checks, deal lock (2-stage commit), item/zeny exchange, hack
detection.

### 2026-05-20 — initial audit + wrapper service
- 9 functions covered. Trade engine reused from prior wave.
