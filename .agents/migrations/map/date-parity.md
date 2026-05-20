# date.cpp parity · 2026-05-20

`src/map/date.cpp` (155 lines, 10 functions) is the in-game clock —
wraps `time(NULL)` accessors that scripts and the buff system read
for Sun/Moon/Star-day calculations and time-of-day NPC dialogue
branches.

## Status legend

- ✅ implemented — full or near-full parity with rAthena
- ⚠️ partial — exists but has documented gaps
- ❌ missing — no C# equivalent

## Subsystem coverage

| rAthena fn | Status | C# location / note |
|---|---|---|
| `date_get_year` | ✅ | [DateService](/Map.Server/Time/DateService.cs) — `DateTime.Now.Year` |
| `date_get_month` | ⚠️ | rAthena exposes via `date_get_month`; covered by `DateTime.Now.Month` |
| `date_get_dayofmonth` | ✅ | `DateService.DayOfMonth` |
| `date_get_dayofyear` | ✅ | `DateService.DayOfYear` |
| `date_get_hour` | ✅ | `DateService.Hour` |
| `date_get_min` | ✅ | `DateService.Minute` |
| `date_get_sec` | ✅ | `DateService.Second` |
| `date_get` | ✅ | `DateService.GetDate` — packed yyyymmdd |
| `is_day_of_sun` | ✅ | `DateService.IsSunDay` — day-of-week % 7 |
| `is_day_of_moon` | ✅ | `DateService.IsMoonDay` |
| `is_day_of_star` | ✅ | `DateService.IsStarDay` |

## Implementation plan

Single-pass — every accessor maps to a `DateTime.Now` field. Real
today.

## History

### 2026-05-20 — initial audit + service
- All 10 functions covered by `IDateService` / `DateService`. Sun /
  Moon / Star day rules follow rAthena's "day-of-week % 7" pattern.
