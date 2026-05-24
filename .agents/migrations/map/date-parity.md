# date.cpp parity · 2026-05-22 (T9.H — per-fn rollup)

`src/map/date.cpp` (155 lines, 10 functions) — in-game clock —
wraps `time(NULL)` accessors that scripts and the buff system read
for Sun / Moon / Star-day calculations and time-of-day NPC dialogue
branches.

Canonical entry points: [IDateService](/Map.Server/Time/IDateService.cs) /
[DateService](/Map.Server/Time/DateService.cs).

## Status legend

- ✅ implemented — full or near-full parity with rAthena
- ⚠️ partial — exists but has documented gaps
- ❌ missing — no C# equivalent

## Per-function coverage

| rAthena fn | Status | C# location / note |
|---|---|---|
| `date_get_year` | ✅ | `DateService.Year` — `DateTime.Now.Year` |
| `date_get_month` | ✅ | `DateService.Month` — `DateTime.Now.Month` |
| `date_get_dayofmonth` | ✅ | `DateService.DayOfMonth` |
| `date_get_dayofyear` | ✅ | `DateService.DayOfYear` |
| `date_get_hour` | ✅ | `DateService.Hour` |
| `date_get_min` | ✅ | `DateService.Minute` |
| `date_get_sec` | ✅ | `DateService.Second` |
| `date_get` | ✅ | `DateService.GetDate` — packed yyyymmdd |
| `is_day_of_sun` | ✅ | `DateService.IsSunDay` — day-of-week % 7 |
| `is_day_of_moon` | ✅ | `DateService.IsMoonDay` |
| `is_day_of_star` | ✅ | `DateService.IsStarDay` |

## Coverage summary

| Bucket | ✅ | ⚠️ | ❌ | Total |
|---|---|---|---|---|
| Clock accessors | 11 | 0 | 0 | 11 |
| **Totals** | **11** | **0** | **0** | **11** |

100% parity. Every accessor reads `DateTime.Now` on call, ensuring
the live host clock is always visible to scripts. Sun / Moon / Star
day rules follow rAthena's day-of-week % 7 semantics.

## History

### 2026-05-24 — P2.1 doc-resync close-out (0 stale ⚠️ → ✅; 0 genuine gaps remain)

Verified: doc is at 100% ✅; ⚠️ grep hits are in the legend only. No-op resync.

### 2026-05-22 — T9.H per-fn rollup

Per-function audit. Baseline: **11 ✅ / 0 ⚠️ / 0 ❌** — 100%
parity. (Doc previously had a per-fn table without a Coverage
summary block; this commit adds the rollup format.)

### 2026-05-20 — initial audit + service
- All 10 functions covered by `IDateService` / `DateService`. Sun /
  Moon / Star day rules follow rAthena's "day-of-week % 7" pattern.
