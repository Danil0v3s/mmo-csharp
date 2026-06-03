# SCRIPT-08 — Effect / clif / NPC-control / timer builtins (specialeffect, announce, viewpoint, enablenpc, movenpc, addtimer, …)

> **Epic:** Scripting parity · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** SCRIPT-03 (per-player addtimer shares the loop-timer plumbing) · **Blocks:** SCRIPT-10 (town/quest NPCs use effects + enablenpc/disablenpc)

## Problem

The "send a packet to the client / control an NPC's visibility & position" builtins are almost
all no-op stubs. These are mechanical ZC packet sends and small NPC-state toggles, but without
them NPCs can't play a cast effect, announce a broadcast, drop a map waypoint, hide/show a gate
NPC, move/redirect an NPC, or run a per-player countdown. This is high-leverage because the
fixes are small and nearly every real script uses several of them.

## Current state (C#)

- `Map.Server/Scripting/Dialog/PlayerContext.DisplayEffects.cs` — `specialEffect`, `miscEffect`,
  emotion/effect helpers — stubs.
- `Map.Server/Scripting/Dialog/WorldContext.AnnounceFamily.cs` — `announce`, `mapAnnounce`,
  `areaAnnounce` — stubs. `WorldContext.SoundBgm.cs` — `playBgm`/`playBgmAll`/`soundEffect`/
  `soundEffectAll` — stubs.
- `Map.Server/Scripting/Dialog/NpcInfo.DisplayMovement.cs` — `enable`/`disable` (enablenpc/
  disablenpc), `move` (movenpc), `setDisplay` (setnpcdisplay), `setDir` (setnpcdir), `emotion`,
  `viewpoint`, `showScript` — stubs. `NpcInfo.Misc.cs` — timer ctl (`initTimer`/`startTimer`/
  `stopTimer`/`setTimer`/`getTimer`/`attachTimer`) — stubs.
- `Map.Server/Scripting/Dialog/PlayerContext.Misc.cs` — `addTimer`/`delTimer`/`addTimerCount`
  (per-player event timers) — stubs.
- `Core.Server/Packets/Out/ZC/` — has `ZC_NOTIFY_EFFECT2`, `ZC_BROADCAST2`, `ZC_QUEST_NOTIFY_EFFECT`.
  **Missing:** `ZC_NOTIFY_EFFECT3`/`ZC_SPECIAL_EFFECT` (0x01f3 / 0x0283), `ZC_BROADCAST` (0x009a),
  `ZC_LOCAL_BROADCAST`/area broadcast, `ZC_COMPASS` (viewpoint, 0x0144), `ZC_SHOWSCRIPT`
  (0x08b3 / 0x0b8d), `ZC_PLAY_NPC_BGM` (0x07fe / 0x0a91), `ZC_SOUND` (0x01d3), `ZC_EMOTION`
  (0x00c0), `ZC_NPCSPRITE_CHANGE`/`ZC_CHANGE_NPC_*` for setnpcdisplay. Add the ones absent.
- NPC state: hide/show needs the visibility service (`Map.Server/Visibility/`) + the entity's
  `Hidden`/`option` flag; movenpc needs `Map.Server/Movement` for the NPC bl.

## rAthena reference (source of truth)

`script.cpp` + `clif.cpp` + `npc.cpp`.

- `script.cpp:15547 BUILDIN(specialeffect)` → `clif_specialeffect(bl, type, target)` →
  `ZC_NOTIFY_EFFECT2/3`. `:15151 misceffect` → `clif_misceffect` (effect over the NPC).
- `script.cpp:6932 BUILDIN(viewpoint)` → `clif_viewpoint(sd, npc_id, type, x, y, id, color)` →
  `ZC_COMPASS` (0x0144): drops/removes a minimap waypoint.
- `script.cpp:24270 BUILDIN(showscript)` → `clif_showscript(bl, message, send_target)` →
  `ZC_SHOWSCRIPT`: floating text over a unit.
- `script.cpp:11908 announce` / `:11979 mapannounce` / `:12001 areaannounce` →
  `clif_broadcast`/`clif_broadcast2` → `ZC_BROADCAST`/`ZC_BROADCAST2` with flag bits
  (`BC_ALL`/`BC_MAP`/`BC_AREA`/`BC_SELF`, color, font). `mapannounce` targets one map;
  `areaannounce` a rectangle.
- `script.cpp:15177-15259 playBGM/playBGMall/soundeffect/soundeffectall` → `ZC_PLAY_NPC_BGM` /
  `ZC_SOUND`. `:13769 emotion` → `clif_emotion(bl, type)` → `ZC_EMOTION`.
- `script.cpp:12290 enablenpc`/disablenpc → `npc_enable(name, flag)`: toggles the NPC's view/
  touch/click state and sends show/hide (`clif_clearunit`/spawn). `hideonnpc`/`hideoffnpc` are
  the visual-only variants.
- `script.cpp:16097 movenpc` → `npc_movenpc(nd, x, y)`: walk/teleport the NPC bl to a cell.
- `script.cpp:17791 setnpcdisplay` → change an NPC's display name and/or sprite (`ZC_NPCSPRITE_CHANGE`
  / name update). `setnpcdir` → set facing + resend.
- `script.cpp:11603 addtimer`/`:11626 deltimer`/`:11642 addtimercount` → **per-player** event
  timers (`pc_addeventtimer`): fire `NpcName::OnLabel` for that player after N ms. Distinct from
  the per-NPC `initnpctimer` family (SCRIPT-03). `:11661-11740 initnpctimer/startnpctimer/
  stopnpctimer` are the per-NPC ones — back them via SCRIPT-03's `NpcTimerService`.

## Scope — every sub-system that must be touched

- [ ] **Add the missing ZC packets** (Out/ZC): special-effect, broadcast (009a) + variants,
      compass/viewpoint (0144), showscript, npc bgm, sound, emotion (00c0), npc-sprite/name change,
      compass-remove. Register var-length sizes in `appsettings.packets.json` for the string ones
      (broadcast, showscript, sound).
- [ ] **`PlayerContext.DisplayEffects.cs`** — `specialEffect`/`miscEffect`/emotion → enqueue the
      effect packet (target self or broadcast to the area per the rAthena send_target).
- [ ] **`WorldContext.AnnounceFamily.cs` + `SoundBgm.cs`** — `announce`(BC_ALL), `mapAnnounce`
      (one map), `areaAnnounce` (rect), `playBgm*`/`soundEffect*`. Honor color/font flag args.
- [ ] **`NpcInfo.DisplayMovement.cs`** — `enable`/`disable` → toggle NPC visible/clickable +
      spawn/clear via `Map.Server/Visibility`; `move` → `npc_movenpc` via Movement; `setDisplay`/
      `setDir` → mutate + resend sprite/name/dir; `viewpoint`/`showScript`/`emotion` → packets.
- [ ] **Per-player timers** (`PlayerContext.Misc.cs`) — `addTimer(ms, "Npc::OnLabel")` schedules
      a fire on the loop for that player; `delTimer`/`addTimerCount` adjust. Reuse the loop-timer
      service from SCRIPT-03 (or the `DialogTimerService` from SCRIPT-01) and dispatch via
      `EventDispatcher.FireNamed` with the player attached.
- [ ] **Per-NPC timer ctl** (`NpcInfo.Misc.cs`) — `initTimer`/`startTimer`/`stopTimer`/`setTimer`/
      `getTimer` → SCRIPT-03's `NpcTimerService` (cross-dep; if SCRIPT-03 not landed, this ticket
      may stub-free only the per-player ones and leave the per-NPC ones to SCRIPT-03 — note it).

## Done criteria

- `ctx.player.specialEffect(EF_HEAL)` plays the heal effect on the player; `miscEffect` over the NPC.
- `ctx.world.announce("Server restart in 5m", BC_ALL)` shows the yellow broadcast to everyone;
  `mapAnnounce` only on the current map; `areaAnnounce` only in the rectangle.
- `ctx.npc.viewpoint(1, x, y, id, color)` drops a minimap waypoint; type 2 removes it.
- `ctx.npc.disable()` hides+unclickables the NPC for all viewers; `enable()` restores it.
- `ctx.npc.move(x, y)` relocates the NPC; `setDisplay("New Name", sprite)` updates clients.
- `ctx.player.addTimer(5000, "Quiz::OnTimeout")` fires that label for the player after 5 s;
  `delTimer` cancels it.
- **No `ScriptStub.Call` left in the files listed above** (per-NPC timer methods may defer to
  SCRIPT-03 if explicitly cross-referenced).

## Test plan

- `Map.Server.Tests/Scripting/EffectClifNpcBuiltinsTests.cs`: for each builtin, assert the right
  ZC packet was enqueued with the right fields (effect id, broadcast text+flag, compass coords,
  emotion id). For enable/disable assert the NPC's visible/clickable state flipped and a
  spawn/clear packet went to area viewers.
- addTimer test: schedule via injectable clock, advance, assert `FireNamed` invoked with the
  player and the label; delTimer cancels before fire.

## Notes / gotchas

- send_target matters: `specialeffect` defaults to AREA (everyone who can see the unit), not just
  the clicking player — mirror rAthena's target arg.
- Broadcast flag bits pack color + font-type into the high bits — get the bit layout right or the
  client renders the wrong color / drops the message.
- `addtimer` (per-player) and `initnpctimer` (per-NPC) are **different timers** with different
  semantics; don't collapse them. Per-player timers move with the player and survive map changes
  until fired or deleted.
- movenpc should respect cell walkability if the project models NPC pathing; teleport is acceptable
  parity for a static NPC.
