# SCRIPT-02 — Player-state builtins (warp / items / heal / status / exp / job / stats / skills)

> **Epic:** Scripting parity · **Status:** ❌ Not started · **Size:** L · **Player-visible:** yes
> **Depends on:** SCRIPT-07 (var reads for getitembound opts is nice-to-have, not hard) · **Blocks:** SCRIPT-10 (kafra/tool-dealer/job-changer)

## Problem

`ctx.player.*` is the surface a real NPC mutates the clicking player through, and
almost all of the *mutating* methods are no-op `ScriptStub.Call(...)`. A kafra can't
warp you, a tool dealer's "delete all" can't remove items, a healer's "full recovery"
does nothing, a job NPC can't change your class, a stat-reset NPC can't refund points.
Only a thin slice is real: `giveItem` (clean stackable add), `countItem`/`hasItem`,
`hp`/`sp`/`maxHp`/`maxSp` getters+setters. Everything else logs and returns a placeholder.

This is a LARGE ticket — **done-criteria are split by group** so it can be landed in
sub-PRs, but every group must end with zero stubs in its file.

## Current state (C#)

- `PlayerContext.MovementMap.cs:11-26` — `warp`, `savePoint`/`save`, `getSavePoint`,
  `pushPc`, `warpPartner` all `ScriptStub.CallAsync`.
- `PlayerContext.ItemsGiveTakeCount.cs` — `giveItem`/`countItem`/`hasItem` REAL (delegate
  `IInventoryService.GiveItem`); `delItem`, `delItemAtIndex`, `clearItems`, `consumeItem`,
  `giveRentItem`, `giveNamedItem`, `giveRandomGroupItem`, `giveGroupItem`, `countBound`,
  `searchItem`, `getInventory`, `mergeItems`, `identifyAll`, `checkWeight` are stubs.
  `giveItem` ignores `opts` (identify/refine/cards/bound) — the comment says "slice 2+".
- `PlayerContext.HpSpAp.cs:11-32` — `hp`/`sp`/`maxHp`/`maxSp` REAL via `_entity` +
  `MarkDirty`; `ap`/`maxAp` are stubs.
- `PlayerContext.HealVariants.cs` — `heal`, `percentHeal`, `recovery`, `itemHeal` stubs.
- `PlayerContext.ExperienceLeveling.cs` — `giveExp`/`getExp`, `changeBase`/`changeJob` (level),
  base/job exp queries — stubs.
- `PlayerContext.JobClassSex.cs` — `jobChange`, `changeSex`, `class` get/set — stubs.
- `PlayerContext.OptionsStatus.cs` — `setOption`, `option` get/set, scStart/scEnd helpers — stubs.
- `PlayerContext.Stats.cs` — `str/agi/vit/int/dex/luk` setters, `statusUp`, `statpoint`,
  `skillpoint` — stubs (confirm which are getters vs mutating).
- `PlayerContext.Skills.cs` — `addSkill`, `delSkill`, `skillLevel`, `resetSkill` — stubs.
- `PlayerContext.LookAppearance.cs` — appearance refresh (`ZC_SPRITE_CHANGE`) for setOption /
  jobChange / changeSex — stubs.
- `PlayerContext.Reset.cs` — `resetStatus`/`resetSkill`/`resetFeel` — stubs.

Target services (all exist, just need delegation):
`Map.Server/Warps/IWarpService.cs`, `Map.Server/Inventory/IInventoryService.cs` (+ `IItemUseService`),
`Map.Server/Status/IStatusChangeService.cs` (scStart/scEnd), `IExpService`, `IJobChangeService`,
`IPlayerSkillService`, `Map.Server/Status/StatusCalcService` (recalc after stat/job change),
`IPlayerOptionService`/`IPlayerLookService` (appearance), `Map.Server/Persistence/PlayerStateService` (savepoint persist).

## rAthena reference (source of truth)

`pc.cpp` / `status.cpp` / `skill.cpp`.

- `pc.cpp pc_setpos` (warp) and `pc_setsavepoint` (savePoint): savepoint persists
  map/x/y on the char; `pc_setpos` validates target map exists, clears statuses that
  drop on map change, sends `ZC_NPCACK_MAPMOVE`. `warpPartner` resolves the spouse char.
- `pc.cpp pc_delitem` / `pc_additem` — `additem` honors `identify`/`refine`/`card[4]`/
  `bound`/`expire_time`/`option[]` on the `struct item` it builds; `delitem` matches by
  nameid (+ optional identify/refine/card/bound filters), removes `amount`, sends
  `ZC_DELETE_ITEM_FROM_BODY`. `clearItems` = loop delete the whole inventory.
- `status.cpp status_heal` / `status_percent_heal` — `heal(hp,sp,flag)`: add, clamp to max,
  push `ZC_PAR_CHANGE` for SP_HP/SP_SP (flag bit controls the "show damage" effect).
  `percentHeal`: `apply_rate(max, percent)`. `pc_itemheal` applies the item-rate +
  bonus_heal modifiers. Recovery = full HP+SP. **All must flush ZC_PAR after.**
- `status.cpp sc_start` / `status_change_end` — duration ms, val1..val4. Delegate to
  `IStatusChangeService` which already implements the SC machine.
- `pc.cpp pc_gainexp` (giveExp) — splits base/job, applies exp rate, may trigger level-up
  (which itself recalcs status + sends `ZC_PAR` for level/maxhp/etc.). `pc_setbaselevel`/
  `pc_setjoblevel` (changeBase/changeJob-level) set directly + recalc.
- `pc.cpp pc_jobchange` — validate class id, set `status.class_`, recalc skill tree
  (grant/remove class skills), reset to job lv 1, send `ZC_SPRITE_CHANGE` (LOOK_BASE) +
  `clif_changelook`. `pc_changesex` is account-level (goes via char/login) — for the map
  builtin it flags a pending sex-change + disconnect, OR flips `LOOK` for the doram-style
  body. `pc_setoption` toggles `sc.option` bits (cart/falcon/peco/wug/etc.) and refreshes
  look via `clif_changeoption` (`ZC_STATE_CHANGE`).
- `pc.cpp pc_statusup`/`pc_statusup2` — raise a primary stat, charge stat points; recalc.
  `pc_skillup` charges skill points, raises skill lv. `pc_resetstate`/`pc_resetskill`/
  `pc_resetfeel` refund points and reset, then full recalc + skill-tree resend.
- `skill.cpp` / `pc.cpp pc_skill(sd, id, lv, flag)` — addSkill grants a skill at lv
  (flag = permanent/temporary/grant), sends `ZC_ADD_SKILL` / `ZC_SKILLINFO_LIST`.

## Scope — every sub-system that must be touched

Group A — **movement/savepoint**
- [ ] `warp` → `IWarpService` (resolve "Random"/"SavePoint" special map names too); flush.
- [ ] `savePoint`/`save` → set char savepoint fields + persist via `PlayerStateService`;
      `getSavePoint` returns `{map,x,y}`; `pushPc(dir,cells)` via `MovementService` knockback;
      `warpPartner` resolve spouse + warp.

Group B — **items** (honor `opts`: `identify`/`refine`/`cards[4]`/`bound`/`grade`)
- [ ] Extend `giveItem` to read `opts` and build the `InventoryEntity` with those fields.
- [ ] `delItem`/`delItemAtIndex`/`consumeItem`/`clearItems` → `IInventoryService` remove paths
      + `ZC_DELETE_ITEM_FROM_BODY`. `countBound`/`searchItem`/`getInventory`/`checkWeight`/
      `mergeItems`/`identifyAll`/`giveRentItem`/`giveNamedItem`/`giveGroup*` real.

Group C — **HP/SP/heal**
- [ ] `heal`/`percentHeal`/`recovery`/`itemHeal` → `status_heal`-equivalent + ZC_PAR flush.
      Wire `ap`/`maxAp` (trait AP) getters/setters with `SP_AP`/`SP_MAXAP`.

Group D — **status changes**
- [ ] `scStart(type,duration,val1..)`/`scEnd(type)` → `IStatusChangeService`.

Group E — **exp/level**
- [ ] `giveExp`/`changeBase`/`changeJob` → `IExpService` + level-up recalc + ZC_PAR.

Group F — **job/sex/option + appearance**
- [ ] `jobChange` → `IJobChangeService`; `changeSex`; `setOption`/`option` → `IPlayerOptionService`;
      every one ends with the appropriate `clif_changelook`/`ZC_STATE_CHANGE`/`ZC_SPRITE_CHANGE`
      via `IPlayerLookService` (`LookAppearance.cs`).

Group G — **stats/skillpoints/skills**
- [ ] `statusUp`/stat setters/`statpoint`/`skillpoint` → `pc_statusup` path + recalc.
- [ ] `addSkill`/`delSkill`/`skillLevel`/`resetSkill`/`resetStatus` → `IPlayerSkillService` +
      `ZC_ADD_SKILL`/`ZC_SKILLINFO_LIST` resend.

## Done criteria

- **Per group:** zero `ScriptStub.Call` left in that group's `PlayerContext.*.cs` file.
- A: `ctx.player.warp("prontera",155,180)` moves the player; `savePoint(...)` survives relog.
- B: `delItem(501,5)` removes 5 Red Potions and updates the client; `clearItems()` empties
  inventory; `giveItem(1201,1,{refine:7,cards:[4001,0,0,0]})` produces a +7 carded knife.
- C: `percentHeal(100,100)` fully heals and the client HP/SP bars update via ZC_PAR.
- D: `scStart(SC_POISON, 10000)` applies poison; `scEnd(SC_POISON)` clears it.
- E: `giveExp(10000,0)` raises base exp and may level-up with correct ZC_PAR.
- F: `jobChange(JOB_KNIGHT)` changes class, resets job lv, resends skill tree, changes sprite.
- G: `statusUp(bStr)` raises STR and charges a stat point; `addSkill(BASH,10)` grants Bash lv10.

## Test plan

- `Map.Server.Tests/Scripting/PlayerStateBuiltinsTests.cs` — one fixture per group, each
  invoking the corresponding `ctx.player.*` through the engine and asserting the delegated
  service was called with the right args AND the expected ZC_* packet was enqueued.
- Number-exact checks against rAthena for: percentHeal clamp, giveExp level-up threshold,
  statusUp point cost, delItem partial-stack removal.
- Reuse `ScriptHostTests.cs` engine setup + fake `IInventoryService`/`IWarpService` mocks.

## Notes / gotchas

- Mutations must go through `MarkDirty`/`Flush` (the dirty-batch pattern in `PlayerContext.cs`)
  so packets land in dialog order — don't send ZC_PAR ad-hoc mid-script.
- `setOption`/`jobChange`/`changeSex` all need a look refresh — that's the most-forgotten step
  (`LookAppearance.cs`). Treat the appearance push as part of each method's done-criteria.
- `changeSex` in rAthena is account-scoped and forces a reconnect; replicate that (don't fake
  an in-place flip) unless the project already has a map-local sex toggle — verify against
  `JobChangeService` before implementing.
- Keep parity: `delItem` default-matches by nameid only; the card/refine/bound filters apply
  only when `opts` is present.
