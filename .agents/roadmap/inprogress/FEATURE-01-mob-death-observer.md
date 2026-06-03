# FEATURE-01 — Mob-death observer hub

> **Epic:** Gameplay-Observers · **Status:** 🚧 In progress · **Size:** L · **Player-visible:** yes
> **Depends on:** none · **Blocks:** FEATURE-03, FEATURE-04, FEATURE-07
> **Related:** PACKET-* (client packets for kill-count UI / MVP announce are a separate epic)

## Problem

When a mob dies, rAthena's `mob_dead` fans the event out to many subsystems:
quest objective counters, achievement triggers, the pet-catch roll, and MVP
reward/announce. In the C# port the mob-death path does **none** of this. A
player can kill the exact mob a quest asks for and the quest counter never
moves; MVP bosses drop like trash mobs with no MVP item / no broadcast; a
hunting-net catch attempt that lands the killing blow silently fails. This is
the single missing hook that FEATURE-03/04/07 all key off — without it those
services have no trigger source.

## Current state (C#)

- `Map.Server/Combat/DamageService.cs:468 HandleDeath(Entity target, Entity? source)` — the live death entry. For a `MobEntity` it awards exp (last-hit / party-share) then calls `_mobSpawn.KillMob(mob.Id, source as PlayerEntity)`. It calls **no** quest / achievement / pet-catch / MVP logic.
- `Map.Server/Spawn/MobSpawnService.cs:261 KillMob(EntityId, PlayerEntity?)` — rolls the normal drop table (`RollAndDropLoot`), vanishes the mob, schedules respawn. Its docstring (line 298) explicitly defers: *"MVP-only drops, exp distribution, party share, and rate modifiers all live with the broader combat work."* No MVP item roll, no MVP exp, no `clif_mvp_*`.
- `Map.Server/Spawn/MobOps/MobOpsService.cs:26 Dead(MobEntity mob, Entity? killer, byte type) => 0;` — a literal shell that returns 0; nothing calls it.
- `Map.Server/Quest/QuestService.cs:38 UpdateObjective(...) { }` — empty; never invoked on kill.
- `Map.Server/Achievement/AchievementService.cs:36 UpdateObjective(...) { }` and `:42 MobExists(int) => false;` — empty/false; never invoked on kill.
- `Map.Server/Pet/PetOps/PetOpsService.cs:341 CatchProcessEnd(...)` — only clears `master.PetCatchTargetClass = -1` and logs; comment says *"the success path is handled by the mob-death observer"* — that observer does not exist.
- `Map.Server/Combat/DamageService.cs:200` already records `MobDmgList` per-hit damage, so the contributor set needed for objective attribution exists on the mob.

## rAthena reference (source of truth)

- `rathena/src/map/mob.cpp` `mob_dead(...)` — canonical dispatch order:
  1. Build the damage-log contributor set; resolve the *killer* (mvp_sd / second_sd / third_sd) and the *last-damage* PC.
  2. EXP / job-exp distribution + party share (already ported in C# DamageService).
  3. **Drop table** roll (normal + card + treasure) — already ported.
  4. **MVP block** (`if (md->db->mexp > 0 ...)`): pick MVP item via `mvp_drop` rates, give MVP exp to `mvp_sd`, `clif_mvp_item` / `clif_mvp_exp` / `clif_mvp_effect`, and broadcast `ZC_BROADCAST` ("MVP" global announce).
  5. **Quest**: for each contributing PC, `quest_update_objective(sd, &md->db->status)` → `pc_show_questinfo`. Mob-kill objectives match by `mob_id` (or `mob_id == 0` "any mob").
  6. **Achievement**: `achievement_update_objective(sd, AG_BATTLE, 1, md->mob_id)` and `AG_TAMING` where relevant.
  7. **Pet catch**: if a contributing PC has `sd->catch_target_class == md->mob_id` (or `PET_CATCH_UNIVERSAL`), roll catch (`pet_catch_process2`).
- `rathena/src/map/pet.cpp` `pet_catch_process2(map_session_data*, int target_id)` — catch rate from `pet_db` `CaptureRate` (scaled /10000), success → `intif_create_pet(...)`; failure → fail clif.
- Mob-kill quest match: `rathena/src/map/quest.cpp` `quest_update_objective` walks `sd->quest_log[]`, for each active (state==Q_ACTIVE) quest's `mob[]` objective array increments `count[i]` when the killed mob_id matches and count < target.

## Scope — every sub-system that must be touched

- [ ] **New service** `Map.Server/Mob/MobDeathObserver.cs` + `IMobDeathObserver` — single method `OnMobDead(MobEntity mob, PlayerEntity? killer, IReadOnlyList<MobDmgEntry> dmgLog)`. Registered in `Program.cs` DI. Dispatch order mirrors rAthena step 4→7 (drops/exp already happen upstream).
- [ ] Resolve the **contributor PC set** from `mob.DmgList` (see `Map.Server/Combat/MobDmgList.cs`) — distinct PCs who dealt damage, plus the last-hitter `killer`. Used for quest + achievement + catch fan-out.
- [ ] Wire the observer into `DamageService.HandleDeath` (`Map.Server/Combat/DamageService.cs:472` MobEntity arm): call `_mobDeath.OnMobDead(mob, source as PlayerEntity, mob.DmgList.Snapshot())` **before** `_mobSpawn.KillMob` (rAthena runs quest/achievement before the unit is freed). Verify the only other `KillMob` caller (`MobSpawnService.cs:236`, GM/scripted kill with `lastHitter: null`) routes through the observer too, or document why scripted kills skip it (rAthena `status_kill` with `MOB_FORCE` does not credit).
- [ ] **MVP block** in `MobSpawnService.KillMob` (or the observer): when `mob.DbEntry.MvpExp > 0` / `mob.DbEntry.MvpDrops.Count > 0`, pick one MVP drop by rate, drop/give it to the MVP PC, award MVP exp, emit `ZC_MVP_GETTING_ITEM` / `ZC_MVP_GETTING_SPECIAL_EXP` / `ZC_MVP` effect, and a server-wide announce. Confirm `MobDbEntity` exposes MVP columns; **add fields + DB loader mapping if missing** (check `Map.Server/MobDb/` against `mob_db.yml` `MvpExp` / `MvpDrops`).
- [ ] **Quest** — implement `QuestService.UpdateObjective(PlayerEntity, int mobId, ...)` (see FEATURE-03 for the full body): for each active quest with a matching mob objective, bump the count, emit `ZC_HUNTING_QUEST_INFO`/`ZC_UPDATE_MISSION_HUNT`, and flip state to complete when all objectives met. Observer calls it per contributor.
- [ ] **Achievement** — implement `AchievementService.UpdateObjective(pc, AG_BATTLE, 1, mobId)` + `MobExists(mobId)` (FEATURE-04). Observer calls it per contributor.
- [ ] **Pet catch** — implement the real roll in `PetOpsService.CatchProcessEnd` (FEATURE-07): when `master.PetCatchTargetClass == mob.ClassId` (or universal sentinel), read `pet_db.CaptureRate`, roll `rng(10000) < rate`, success → `IntifService.PetCreate(...)`, failure → fail clif. Observer invokes this for the killer (rAthena only the catcher, not all contributors).
- [ ] **Client-visible packets**: MVP item/exp/effect + global announce (ZC_MVP*), quest hunt counter (ZC_UPDATE_MISSION_HUNT), achievement update (ZC_ACH_UPDATE). The actual ZC_* emit wiring is shared with PACKET-* — if those packet classes don't exist yet, the observer must still perform the **state mutation** (counts/rewards/catch) and leave a single clearly-marked call into the (PACKET-*-owned) clif method, not a no-op.

## Done criteria

- Killing a mob whose `mob_id` matches an active quest objective increments that quest's count by exactly 1 per kill, for every PC that contributed damage, and the quest auto-completes when the last objective hits its target.
- Killing a mob fires `AchievementService.UpdateObjective(AG_BATTLE,1,mob_id)` for each contributor; a battle-type achievement with `MobID:` matching advances.
- Killing the catch target while a catch is armed rolls catch at the `pet_db.CaptureRate`-derived probability and, on success, dispatches `IntifService.PetCreate` (egg row created char-side); on failure the catch marker is cleared and a fail notice path is invoked.
- Killing an MVP mob (mexp>0) awards MVP exp + one MVP drop to the MVP PC and triggers the MVP announce/effect emit point.
- `MobOpsService.Dead` no longer returns a bare `0` shell **or** is removed if the observer fully supersedes it — no dead stub left.
- No `// TODO`, no "for now we clear", no log-only no-op in `HandleDeath`, `KillMob`, `CatchProcessEnd` for the death path.

## Test plan

- `Map.Server.Tests` (add): `MobDeathObserverTests` —
  - quest objective increments only for matching mob_id and only for PCs in the dmg list;
  - achievement `UpdateObjective` called once per contributor with `(AG_BATTLE,1,mob_id)`;
  - catch success path calls `IntifService.PetCreate` exactly once when armed + roll forced to pass (inject seeded RNG);
  - MVP path awards mvp exp + one mvp drop and does not for a non-MVP mob.
- Regression: existing exp/drop tests for `KillMob` still pass (observer must not double-award exp/drops).
- Manual/live: with a real client, accept a hunting quest, kill the target, confirm the client mission counter ticks; kill an MVP and confirm the global announce.

## Dispatch order (the contract this hub owns)

```
HandleDeath(mob, source):           // DamageService.cs:472
  [exp/party-share already awarded here — leave as-is]
  observer.OnMobDead(mob, killer, mob.DmgList.Snapshot()):
    1. contributors = distinct PCs in dmglog ∪ {killer}
    2. for each contributor: QuestService.UpdateObjective(pc, mob.ClassId)
    3. for each contributor: AchievementService.UpdateObjective(pc, AG_BATTLE, 1, mob.ClassId)
    4. if killer armed a catch for mob.ClassId: PetOpsService.CatchProcessEnd(killer, mob.ClassId)
    5. if mob.DbEntry.MvpExp > 0: MVP block (mvp drop + mvp exp + announce/effect)
  _mobSpawn.KillMob(mob.Id, killer)  // drops + vanish + respawn (already real)
```

Steps 2–5 must run **before** `KillMob` frees the entity + dmglog.

## Notes / gotchas

- Order matters: run quest/achievement/catch **while the mob entity (and its dmglog) is still alive**, i.e. before `KillMob` removes it. The current `HandleDeath` calls `KillMob` last (`DamageService.cs:498`), so insert the observer call just above it.
- Don't double-count: exp/party-share is already awarded in `HandleDeath`; the observer must NOT re-award exp. Keep exp where it is, move only quest/ach/catch/MVP into the observer.
- The dmglog contributor set must be **distinct PCs**, and party/guild expansion for quest credit follows rAthena (only the killing party's members in range get quest credit — verify against `mob_dead`'s `pt`/`tmpsd[]` loop).
- Scripted/GM kills (`KillMob(id)` with null lastHitter) must not crash the observer (null killer, possibly empty dmglog).
