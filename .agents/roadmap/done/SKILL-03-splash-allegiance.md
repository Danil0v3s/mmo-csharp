# SKILL-03 — Splash allegiance: slave-mob ownership + PvP / no-friendly-fire mapflags

> **Epic:** Skills · **Status:** ✅ Done (2026-06-01) · **Size:** M · **Player-visible:** yes
> **Depends on:** none · **Blocks:** SKILL-08 (NPC family), SKILL-12

## Problem

The splash victim classifier (`battle_check_target` port) is incomplete in two ways
that change who gets hit by every AoE:

1. **Slave-mob ownership is unimplemented.** A mob spawned with a player master
   (SC_DEVOTION guardians, summoned slaves, `NPC_SUMMONSLAVE`, homun/merc-adjacent
   summons) should classify as the master's *party* (friendly) — instead the
   classifier always returns `Enemy` for Player↔Mob and `Neutral` for Mob↔Mob. A
   player's own summoned slave currently reads as an enemy and eats the player's AoE
   (and vice-versa), and slaves of the same master don't recognize each other.

2. **PvP / no-friendly-fire mapflags are not consulted.** `Classify` decides
   allegiance purely from party/guild ids. On a PvP map two unaffiliated players
   read as `Enemy` (correct), but on a normal field map they *also* read as `Enemy`
   — so a Storm Gust on a town map would splash other players. Conversely the
   `pvp_noparty` / `gvg_noparty` (friendly-fire-enabled) flags that should make
   party members hittable in WoE are ignored. The classifier comment admits it:
   *"PvP map flags (no-pvp / no-friendly-fire toggles) are not yet consulted."*

The result: AoE friendly-fire is wrong on every map type, and player summons are
hostile to their owner.

## Current state (C#)

- `Map.Server/Skills/Splash/MapForeachInRangeService.cs:62` — `Classify(Entity? src, Entity target)`. Player↔Player uses party/guild only; Player↔Mob and Mob↔Player hardcode `Enemy`; Mob↔Mob hardcodes `Neutral`. No mapflag read, no master/slave read.
- `:75-79` — inline TODO: *"Slave mobs … are TODO — they'd classify as Party when src or target is the slave's master."*
- `:85` — Mob↔Mob TODO: *"Same-master slave-mob handling also TODO."*
- `:10-20` (class doc) — *"PvP map flags … are not yet consulted; that delta lives in `DamageService.CanDamage` for the damage path."* So the splash *target enumeration* and the damage *gate* disagree: a victim can pass the splash filter then be silently dropped by `CanDamage`, wasting the hit and breaking hit-count-dependent skills.
- `ctx.MapFlags` (`IMapFlagService`) is already plumbed into `SkillBehaviorContext` but `MapForeachInRangeService` doesn't receive it.
- `MobEntity` master/owner field: confirm whether one exists; the spawn path (`IMobSpawnService` / `NPC_SUMMONSLAVE` plugins) sets a master id when summoning a slave.

## rAthena reference (source of truth)

- `rathena/src/map/battle.cpp:battle_check_target` (declared `battle.hpp:143`, body in battle.cpp) — the canonical allegiance resolver. Returns `BCT_ENEMY` / `BCT_PARTY` / `BCT_GUILD` / `BCT_SELF` / `BCT_NEUTRAL`. Key branches:
  - **Master substitution:** for a slave mob (`md->master_id`), `battle_check_target` substitutes the master's `block_list` and re-evaluates allegiance from the master's perspective (`battle.cpp` `MD_*` + the `s_bl`/`t_bl` master-walk). A player's slave is friendly to the player and to the player's party.
  - **Mapflag gates:** `map_getmapflag(m, MF_PVP)`, `MF_GVG`, `MF_BATTLEGROUND`, and the `_noparty` / `_noguild` variants decide whether same-party/same-guild members are still `BCT_ENEMY` (friendly-fire on) or remain `BCT_PARTY` (friendly-fire off). On a non-PvP/non-GvG field map, two unaffiliated *players* are NOT mutually attackable (`BCT_NEUTRAL`/`BCT_ALL` without the enemy bit).
  - `BCT_NOENEMY`/`BCT_NOONE` and the state checks (`sc->data[SC_NOEQUIPWEAPON]` etc.) are out of scope for splash filtering — only the allegiance graph + mapflags matter here.
- `rathena/src/map/battle.hpp:61` — `enum e_battle_check_target : uint32` (the `BCT_*` bit values the C# `BattleCheckTarget` mirrors).
- Monolithic-switch caveat: `battle_check_target` is a single function in `battle.cpp` (not split). The mapflag reads are `map_getmapflag`; the slave walk is the `md->master_id` substitution.

## Scope — every sub-system that must be touched

- [x] **`MapForeachInRangeService` ctor** — ✅ injects `IMapFlagService?` + `IMapWorldRegistry?` (optional; DI auto-resolves) so the resolver can read PvP/GvG/BG flags + resolve a slave's master.
- [x] **Slave-master read** — ✅ uses the existing `Entity.MasterId` (`EntityId?`); summon sites already set it (slave-AI follow loop / `MST_MASTER`). No new field needed.
- [x] **`Classify` rewrite** — ✅ extracted to the shared `BattleTargetResolver.Classify` (port of `battle_check_target`): one-hop master substitution (player's slave & same-master siblings → `Party`); Player↔Player reads `Pvp`/`Gvg`/`Battleground` + `PvpNoparty`/`PvpNoguild`/`GvgNoparty` (field-map strangers are `Neutral`, not enemies; GvG guildmates always allies); Mob↔Mob `Neutral`.
- [~] **Unify with the damage gate** — ✅ the splash filter and `CanDamage` now share the allegiance *model* (`BattleTargetResolver`), and the splash filter no longer over-includes (so it never feeds a friendly/neutral victim into the gate). Routing `CanDamage`'s *attack* path through the resolver requires an attack-vs-mechanic-damage split (the heal-flip at `Heal.cs:162` applies damage to allies via `ApplyDamage` and must stay ungated) ➡️ **Moved to SKILL-16**.
- [x] **Hit-count integrity** — ✅ the splash victim set is now the correct allegiance set; offensive masks exclude friendly/neutral victims so the count is accurate (no downstream silent drop for the splash-Enemy set).
- [x] **No new packets / IPC / DB.** ✅ (added 5 `MapFlag` enum members + parser cases only.)

## Done criteria

- ✅ A player's summoned slave is NOT hit by the player's own offensive AoE; the slave's AoE does not hit the master or the master's party (tests: `Slave_FriendlyToMaster`, `Slave_FriendlyToMastersParty`, `SameMasterSlavesAreParty`, `Slave_AttacksWhatMasterWould_WildMob`).
- ✅ On a non-PvP field map an AoE does not splash an unaffiliated player; on a PvP map it does (`FieldMap_SuppressesPlayerSplash` / `PvpMap_EnablesPlayerSplash`).
- ✅ WoE friendly-fire (`gvg_noparty` / `pvp_noparty`) makes party members hittable exactly where the flag enables it (`GvgNoparty_FriendlyFire_MakesPartyHittable`, `PvpMap_PartyMate_NotEnemy_UnlessNoparty`, `GvgMap_GuildMate_AlwaysAlly`).
- [~] `Classify` and `CanDamage` agree — the splash side is correct (never over-includes); full attack-path `CanDamage` routing ➡️ **Moved to SKILL-16**.
- ✅ No `TODO` for slave-mob or mapflag handling remains in `MapForeachInRangeService.cs` (the classifier moved to `BattleTargetResolver`, fully implemented).

## Test plan

- `MapForeachInRangeServiceTests.Slave_FriendlyToMaster` — spawn a mob with `MasterId = player.Id`; assert `Classify(player, slave) == Party` and the splash filter excludes it from the player's offensive mask.
- `...SameMasterSlavesAreParty` — two slaves, same master → `Party`.
- `...PvpMapEnablesPlayerSplash` / `...FieldMapSuppressesPlayerSplash` — same two unaffiliated players, two map fixtures, opposite results.
- `...GvgNopartyFriendlyFire` — party members hittable under the WoE friendly-fire flag.
- `SplashDamageParity` — victim set from `ForEachInSplash` == victim set that `CanDamage` accepts for the same cast.

## Notes / gotchas

- The slave-master walk must guard against cycles (a mis-set `MasterId` pointing at another slave). Resolve at most one hop, then classify against the resolved player/mob.
- Don't break the existing PvE field case: mob vs player on a normal map must stay `Enemy` (that's the common path and most tests assume it). The mapflag branch only changes *Player↔Player*, not Player↔Mob.
- `MapFlag` enum already has `NoPvp` / pvp-adjacent flags (used by `SkillCastService` for `NoSkill`); confirm the exact PvP/GvG/`_noparty` flag names exist on `MapFlag` and add the missing ones rather than string-matching.

## History

- 2026-06-01 · Splash allegiance correctness. Extracted the shared `BattleTargetResolver`
  (port of `battle_check_target`): one-hop summoned-slave master substitution (player's slave
  + sibling slaves → Party) and PvP/GvG/BG-aware Player↔Player (field-map strangers are
  Neutral, not enemies; `pvp_noparty`/`gvg_noparty`/`pvp_noguild` re-enable friendly fire; GvG
  guildmates always allies). `MapForeachInRangeService.Classify` now delegates to it (ctor
  injects `IMapFlagService?`/`IMapWorldRegistry?`). Added 5 `MapFlag` members (Pvp/Battleground/
  PvpNoparty/PvpNoguild/GvgNoparty) + parser cases. Used the existing `Entity.MasterId`.
  `MapForeachInRangeServiceTests` grew to 13 (slaves, field-vs-pvp, gvg_noparty, guild).
  Suite 3677 green. `CanDamage` left on its legacy logic (reverting an over-aggressive merge
  that broke the Akaitsuki heal-flip at Heal.cs:162) — full attack-path routing + the
  attack-vs-mechanic-damage split filed as SKILL-16.
