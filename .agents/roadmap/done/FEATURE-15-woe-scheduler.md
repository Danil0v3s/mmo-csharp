# FEATURE-15 — WoE (Agit) time-of-week scheduler

> **Epic:** Gameplay-WoE · **Status:** ✅ Done (2026-06-03) · **Size:** M · **Player-visible:** yes
> **Depends on:** none · **Blocks:** none
> **Related:** PACKET-* (none required — WoE start/end fire NPC events) · SCRIPT-10 (agit_controller announce)

## Problem

War of Emperium can be started and ended, but **only programmatically or by a
GM** — there is no time-of-week scheduler. `AgitService` exposes
`AgitStart`/`AgitEnd` (and 2.0/TE variants) and `IsActive`, but nothing reads a
WoE schedule and flips them automatically. On a real server WoE runs on a fixed
weekly schedule (e.g. Sat 20:00–22:00); without a scheduler, castle sieges
never happen unless an admin manually triggers them.

## Current state (C#)

- `Map.Server/Agit/AgitService.cs`:
  - `AgitStart`/`AgitEnd` (`:36`/`:45`), `Agit2Start`/`Agit2End` (`:54`/`:63`), `Agit3Start`/`Agit3End` (`:72`/`:81`) — flip a bool, log, and `Fire(AgitEventNames.Start/End*)` to dispatch `OnAgitStart*`/`OnAgitEnd*` NPC events via `INpcOpsService.EventDoAll` (`:97`).
  - `IsAgitActive` / `IsAgit2Active` / `IsAgit3Active` / `IsAnyActive` (`:31`–`:34`).
  - `EndAll` (`:90`).
  - **No schedule, no time source, no per-tick check.** The transitions are only invoked by callers (GM command / script).
- No `woe_schedule` config / data file is read; no scheduler component exists.

## rAthena reference (source of truth)

- `rathena/src/map/guild.cpp` + the WoE schedule:
  - rAthena drives WoE via **scheduled script commands** (`OnClock`/`OnTime` NPC labels in the WoE controller scripts, e.g. `npc/guild/agit_controller.txt`) that call `AgitStart` / `AgitEnd` (and `AgitStart2`/`AgitEnd2`) at fixed day-of-week + time-of-day. The engine itself (`guild.cpp`) exposes `agit_start()`/`agit_end()` (and the 2.0/TE variants) and the `agit_flag`/`agit2_flag` globals — the *schedule* lives in script timers, not C++.
  - `guild.cpp` `guild_agit_start`/`guild_agit_end` broadcast the WoE start/end announce and fire the `OnAgitStart`/`OnAgitEnd` events to all NPCs (the C# `Fire` already mirrors this).
  - Day/time parsing: rAthena `OnClock<HHMM>` and `OnDay...` labels; the config is in the script.

## Scope — every sub-system that must be touched

- [ ] **Schedule config**: a `woe_schedule` config section (appsettings or a data file) of `(woeType ∈ {1.0,2.0,TE}, dayOfWeek, startTime, endTime)` windows. Multiple windows allowed (e.g. two sessions/week).
- [ ] **Scheduler component**: a `WoeScheduler` (or a `Tick(nowUtc)` on `AgitService`) called from the game loop (`MapServerImpl`) that, on a coarse cadence (e.g. once a minute), evaluates the current day-of-week + time-of-day against the schedule and calls `AgitStart`/`AgitEnd` (and 2.0/TE) on the leading edge of each window (start when entering the window, end when leaving). Idempotent — `AgitStart` already returns false if already active, so re-evaluation is safe.
- [ ] **Edge detection**: track the active windows so the scheduler fires `Start` exactly once at window-open and `End` exactly once at window-close (don't re-fire every tick). Handle server-start *inside* an active window (start WoE immediately on boot).
- [ ] **Timezone**: define the schedule timezone (server-local or UTC) explicitly; document which. rAthena uses server-local.
- [ ] **Reload**: a way to reload the schedule (GM `@reloadscript` equivalent) without restart.
- [ ] Keep the existing GM/script `AgitStart`/`AgitEnd` callable (manual override coexists with the scheduler).
- [x] The NPC event fan-out (`OnAgitStart*`/`OnAgitEnd*`) fires via `Fire` (unchanged). **Parity correction:** rAthena `guild_agit_start`/`guild_agit_end` (guild.cpp:2532/2547) do **NOT** broadcast — they only call `npc_event_runall`. The "The War of Emperium has begun" announce is emitted by the **script** (`agit_controller.txt`'s `OnAgitStart` label → `announce`), not the engine. So **no engine broadcast was added** (that would diverge); the announce rides the existing `OnAgitStart` NPC event and appears once the WoE controller NPC is converted (SCRIPT-10).

## Done criteria

- With a configured schedule, WoE 1.0 (and 2.0/TE if scheduled) starts automatically at the window-open day/time and ends at window-close, with no GM action.
- `AgitStart`/`AgitEnd` fire exactly once per window edge (no per-tick re-fire), and the NPC `OnAgitStart`/`OnAgitEnd` events + start/end broadcast fire.
- Server boot inside an active window starts WoE immediately.
- GM/script manual start/end still works alongside the scheduler.
- No scheduler-less `AgitService` that only flips on external call.

## Test plan

- `Map.Server.Tests` (add `WoeSchedulerTests`):
  - a window `(Sat 20:00–22:00)` with an injected clock: tick at 19:59 → inactive; 20:00 → `AgitStart` fired once; 20:30 → still active, no re-fire; 22:00 → `AgitEnd` fired once;
  - boot at 20:30 → WoE active immediately;
  - 2.0/TE windows independent of 1.0;
  - reload picks up a changed schedule.
- Manual/live: configure a near-term window, confirm WoE auto-starts + the announce + castle NPCs react, then auto-ends.

## Event name surface (already present)

`AgitService.Fire` (`:97`) dispatches via `AgitEventNames.Start` / `End` / `Start2` / `End2` / `Start3` / `End3` (`Map.Server/Spawn/NpcOps/`) to `INpcOpsService.EventDoAll`. The scheduler reuses these unchanged — it only decides *when* to call `AgitStart`/`AgitEnd`. No new event names are needed.

## Example schedule shape

```jsonc
// appsettings.json → Server.WoeSchedule (server-local time)
"WoeSchedule": [
  { "Type": "1.0", "Day": "Saturday", "Start": "20:00", "End": "22:00" },
  { "Type": "1.0", "Day": "Sunday",   "Start": "16:00", "End": "18:00" },
  { "Type": "2.0", "Day": "Saturday", "Start": "21:00", "End": "23:00" }
]
```

Type maps to `AgitStart`/`AgitEnd` (1.0), `Agit2Start`/`Agit2End` (2.0), `Agit3Start`/`Agit3End` (TE).

## Notes / gotchas

- rAthena's schedule is *script-driven* (`OnClock`/`OnDay` labels), not engine config. This C# port puts it in config/a scheduler for simplicity — note the divergence in the doc; behavior (auto start/end at fixed weekly times) matches.
- Idempotency is your friend: `AgitStart`/`AgitEnd` already guard double-fire (`return false` if already in that state, `AgitService.cs:38`) — the scheduler just needs correct edge detection so it doesn't spam.
- Use a coarse tick (per-minute) — WoE resolution is minutes, not frames; don't evaluate the schedule every 60 FPS tick. Add a `_nextWoeCheckUtc` gate like the existing `_nextAutosaveUtc`/`_nextKeepAliveUtc` gates in `MapServerImpl`.
- Define the timezone explicitly and document it; off-by-timezone is the classic WoE-scheduler bug.
- Windows that cross midnight (e.g. `23:00–01:00`) and back-to-back windows of different types must be handled — model each window as an explicit `(day, start, end)` and evaluate "is now inside this window" rather than diffing against a single daily boundary.
- Multi-map caveat: WoE state is per map-server process here; if castle maps are sharded across processes the scheduler must run (and agree) on each. For a single map process this is moot — note it for the multi-process case. (The whole project is single-map-process today; this is a property of the architecture, not a WoE defect — no separate ticket.)

## History

- 2026-06-03 — Added the WoE weekly scheduler. New `WoeScheduler` (`IWoeScheduler`) drives
  `IAgitService` from a server-local `Server.WoeSchedule` config (`WoeScheduleEntry` → parsed
  `WoeWindow{Edition,Day,Start,End}`). **Edge-triggered**: fires `AgitStart`/`AgitEnd` (and 2.0/TE)
  exactly at window open/close via a per-edition "was-inside" detector — it does **not** level-enforce
  off-state every tick, so a GM/script manual `@agitstart` between windows survives; a server booting
  inside a window starts immediately. Midnight-crossing windows + independent editions handled.
  `Reload()` re-reads config. Wired into `MapServerImpl` on a coarse 20 s gate (`_nextWoeCheckUtc`,
  `DateTime.Now`). Registered as a singleton in `Program.cs`; `Server.WoeSchedule` added (empty) to
  `appsettings.json`. **Parity note:** rAthena `guild_agit_start` only fires the NPC event (no engine
  broadcast); the "WoE has begun" announce is the `agit_controller` script's `OnAgitStart` → covered by
  SCRIPT-10, not added to the engine. Tests: `WoeSchedulerTests` (7) green; full suite 4394 pass
  (1 fail = pre-existing replay-fixture/INFRA-11). Matched `guild_agit_start`/`guild_agit_end`
  (guild.cpp:2532/2547) + the script-driven `OnClock` schedule model.
