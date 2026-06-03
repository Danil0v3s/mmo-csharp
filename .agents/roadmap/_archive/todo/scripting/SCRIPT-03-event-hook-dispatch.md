# SCRIPT-03 — Event-hook dispatch (OnInit / OnTouch / OnTimer / OnClock / PC lifecycle / donpcevent)

> **Epic:** Scripting parity · **Status:** ❌ Not started · **Size:** L · **Player-visible:** yes
> **Depends on:** none · **Blocks:** SCRIPT-10 (touch warps, timer NPCs), SCRIPT-01 (shares loop-timer plumbing)

## Problem

Event hooks are **parsed but never fired.** `registerNpc({ onInit, onTouch, onTimer,
onClock, onPCLogin, onPCDeath, onPCKill, onNPCKill })` captures every closure into
`NpcHooks`, and the docstring on `NpcHooks` literally says "Phase 2 wires the dispatcher
that actually invokes them" — that wiring never happened. Only `OnClick` is dispatched
(by `ContactNpcHandler` → `DialogDispatcher.StartOnClick`). Consequences for players:
- An NPC with `onInit` never initializes (no waiting-room creation, no initial state).
- `onTouch` warp-portals / trap NPCs do nothing when walked onto.
- `onTimer`/`onClock` clocks (spawn cycles, MVP timers, shop refresh) never tick.
- `onPCLogin`/`onPCDeath`/`onPCKill`/`onNPCKill` (login rewards, death penalties scripted,
  kill counters, MVP-kill announcers) never run.

## Current state (C#)

- `Map.Server/Scripting/Records/NpcHooks.cs:12-32` — record holds `OnClick`, `OnTouch`,
  `OnInit`, `OnTimer` (dict<int,handle>), `OnClock` (dict<string,handle>), `OnPCLogin`,
  `OnPCDeath`, `OnPCKill`, `OnNPCKill`. `Any` reports which are set. **No dispatcher reads
  any field except `OnClick`.**
- `Map.Server/Scripting/Dialog/DialogDispatcher.cs` — only `StartOnClick` + the resume
  methods. No `Fire(hook, ...)` for non-click hooks.
- `Map.Server/Scripting/NpcRegistry.cs` — holds the registered NPCs + their hooks. This is
  the iteration source for OnInit-at-boot and the lookup for named donpcevent.
- `Map.Server/Movement/MovementService.cs` — moves players cell-by-cell and (verify) calls
  `IWarpService`/`WarpDispatcher` on cell entry. **No `onTouch` NPC trigger** is fired here.
- `Map.Server/Warps/WarpService.cs` — the existing cell-entry trigger to mirror for onTouch
  area checks (touch areas are NPC `trigger` rectangles, like warps).
- `Map.Server/MapServerImpl.cs` — owns the 60fps tick loop; no per-NPC timer service runs on it.
- PC lifecycle: connect flow (login), `Combat`/death path, mob-kill path — none invoke the
  PC hooks. Find the death handler in `Map.Server/Status`/`Combat` and the mob-death in
  `Map.Server/Mob`.

## rAthena reference (source of truth)

`npc.cpp`.

- `npc.cpp npc_event(sd, eventname, ...)` — fires a named `NPCName::OnLabel` event with a
  player attached (`st->rid`). `donpcevent`/`doevent` (`script.cpp:11549/11566`) resolve the
  event by `"NpcName::OnLabel"` and run it (donpcevent = no rid; doevent = current rid).
- `npc.cpp npc_touch_areanpc` / `npc_touchnext_areanpc` — on every player move, scan NPCs whose
  trigger area (`bl.x±xs, bl.y±ys`) contains the new cell; the first matching NPC with an
  `OnTouch` label fires it with the player attached. `OnTouch_` (no rid) is the no-player variant.
  Mirror the cell-entry hook that warps already use.
- `npc.cpp npc_timerevent` / `npc_timerevent_start/stop` — per-NPC timer with a tick counter;
  `OnTimer<ms>` labels fire when the NPC's elapsed timer crosses `<ms>`. `initnpctimer`/
  `startnpctimer`/`stopnpctimer`/`addtimercount` (`script.cpp:11603-11740`) control it. The
  timer advances on the map tick.
- `npc.cpp npc_event_do_clock` — wall-clock: labels `OnMinuteNN`, `OnHourNN`, `OnClockHHMM`,
  `OnDayDDDD`, `OnSunday..OnSaturday` fire when local time matches. Evaluated once per minute.
- PC lifecycle global events (`npc.cpp`): `OnPCLoginEvent` (all NPCs with that label fire on
  login), `OnPCDieEvent`, `OnPCKillEvent`, `OnNPCKillEvent`, `OnPCLoadMapEvent` (map-flagged).
- **Reentrancy:** rAthena runs each script on its own state; a player already in a dialog
  must not have a touch/timer event clobber their dialog state. Guard: if `session.Dialog != null`,
  defer/skip player-attached touch & PC-event fires for that player.

## Scope — every sub-system that must be touched

- [ ] **New `EventDispatcher`** (`Map.Server/Scripting/Dialog/EventDispatcher.cs` or a new
      `Events/` folder) with: `FireOnInitAll()`, `FireOnTouch(session, npc)`,
      `FireNamed(npcName, label, session?)` (for donpcevent/doevent), `FirePcEvent(label, session)`
      (broadcasts to all NPCs carrying that label), `FireOnTimer(npc, ms)`. Each builds a
      `DialogContext` (player may be null for no-rid hooks) and invokes the `ScriptHandle`
      exactly like `StartOnClick` does (Invoke + fault continuation).
- [ ] **OnInit at boot** — after `ScriptHost` finishes registration and `NpcRegistry` is
      populated, iterate all NPCs and fire `OnInit` (no player). Hook into the map-server
      startup sequence in `MapServerImpl`/`Program.cs` after spawn.
- [ ] **OnTouch on cell entry** — in `MovementService` (the same place warps trigger), after
      committing the player's new cell, query NPCs whose trigger area contains it and call
      `EventDispatcher.FireOnTouch`. Need NPC trigger-area fields (`trigger`/`xs`/`ys`) on the
      registration record — add to `NpcRegistration`/`NpcEntity` if absent.
- [ ] **Per-NPC timer service** — `NpcTimerService` advancing on the 60fps loop (tick from
      `MapServerImpl`). Tracks per-NPC elapsed ms + active flag; fires the matching `OnTimer<ms>`
      handle as the counter crosses each registered key. Back the `initnpctimer`/`startnpctimer`/
      `stopnpctimer`/`addtimercount`/`getnpctimer`/`setnpctimer` builtins (these live on
      `ctx.npc` — see `NpcInfo.*` stubs).
- [ ] **OnClock service** — a once-per-minute check (driven off the same tick or a wall-clock
      timer) that fires `OnMinuteNN`/`OnHourNN`/`OnClockHHMM`/`OnDayDDDD`/`OnSunday..` labels.
- [ ] **PC lifecycle wiring** — call `FirePcEvent("OnPCLoginEvent", session)` from the connect
      flow; `OnPCDieEvent` from the death handler; `OnPCKillEvent`/`OnNPCKillEvent` from the
      kill paths (PvP kill / mob kill). Pass the right rid.
- [ ] **donpcevent/doevent builtins** — `ctx.world.doNpcEvent("Name::OnX")` / `ctx.doEvent(...)`
      (verify exact JS names in `WorldContext`/`DialogContext`) → `EventDispatcher.FireNamed`.
- [ ] **Reentrancy guard** — skip player-attached fires when `session.Dialog != null`; OnInit/
      OnClock/no-rid timer fires always run (no player).

## Done criteria

- An NPC with `onInit` runs exactly once at boot (assert a side effect, e.g. it set a mapreg).
- Walking onto an `onTouch` area fires the hook once per entry (not every cell within), with
  the moving player attached.
- `onTimer: { 1000: fn }` fires ~1 s after `startnpctimer`; `stopnpctimer` halts it;
  `addtimercount` shifts the counter.
- `onClock: { "OnMinute00": fn }` fires at the top of the minute.
- Logging in fires every NPC's `onPCLogin`; dying fires `onPCDeath`; killing a mob fires
  `onNPCKill`; PvP kill fires `onPCKill`.
- A player mid-dialog does NOT get their dialog state corrupted by a touch/timer fire.
- **`NpcHooks` docstring "Phase 2 wires the dispatcher" is removed and every field is consumed.**

## Test plan

- `Map.Server.Tests/Scripting/EventDispatchTests.cs`: register an NPC with each hook via a TS
  fixture; assert OnInit fired at boot; simulate a move into the trigger area → OnTouch fired
  once; advance a fake clock/tick → OnTimer fired at the key boundary; fire `FirePcEvent` →
  all NPCs with the label ran; assert reentrancy guard skips touch while a dialog is open.
- Pin "once per entry" for OnTouch (move out and back → fires twice; move within → no refire).

## Notes / gotchas

- `OnTouch` area semantics differ from warps: warps fill the whole rectangle as warpable cells;
  touch-NPCs fire only on the *first* cell crossing into the area — mirror `npc_touchnext_areanpc`
  bookkeeping (track which area the player currently occupies).
- All fires must reuse `StartOnClick`'s fault-handling continuation so a script throw is logged
  and doesn't poison the loop.
- No-rid hooks (`OnInit`, `OnClock`, `OnTimer` without a triggering player) build a
  `DialogContext` with `player == null`; the suspending dialog primitives are invalid there —
  rAthena treats `mes` with no rid as a no-op. Guard or document.
- The per-NPC timer and SCRIPT-01's progressbar/sleep timers should share one loop-timer
  abstraction; land whichever first and reuse.
