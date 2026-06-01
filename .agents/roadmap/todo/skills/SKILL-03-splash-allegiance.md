# SKILL-03 — Splash allegiance: slave-mob ownership + PvP / no-friendly-fire mapflags

> **Epic:** Skills · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
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

- [ ] **`MapForeachInRangeService` ctor** — inject `IMapFlagService` and `IEntityRegistry` (already has entities) so `Classify` can read the map's PvP/GvG flags and resolve a slave's master entity.
- [ ] **Slave-master read** — confirm/add a `MasterId` (`EntityId`) on `MobEntity` for summoned slaves. The summon sites (`IMobSpawnService.OnceSpawn` slave path + `NPC_SUMMONSLAVE` plugin) must set it. If the field exists, just read it; if not, add it (transient runtime field, not persisted — slaves aren't saved).
- [ ] **`Classify` rewrite** — port the `battle_check_target` allegiance walk:
  - If `src` or `target` is a slave mob, substitute its master entity and classify from the master's side (recurse once, guard against self-loop).
  - Player↔Player: read the map's PvP/GvG/BG flag + `_noparty`/`_noguild` to decide whether shared party/guild still suppresses enemy status. On non-PvP/non-GvG maps, unaffiliated players are NOT enemies.
  - Player's own slave / same-master slaves classify as `Party`.
  - Mob↔Mob stays `Neutral` unless same master.
- [ ] **Unify with the damage gate** — make `DamageService.CanDamage` and `MapForeachInRangeService.Classify` consult the same allegiance helper (extract a shared `BattleCheckTarget Resolve(src, target, mapFlags)` so the splash filter and the damage gate cannot disagree). At minimum, document and test that a victim that passes the splash filter also passes `CanDamage` for the same map.
- [ ] **Hit-count integrity** — skills that count hits (multi-hit splash, chain) must not have victims silently dropped by a downstream `CanDamage` mismatch. Verify the count reflects the same allegiance decision.
- [ ] **No new packets / IPC / DB.**

## Done criteria

- A player's summoned slave mob is NOT hit by the player's own Storm Gust / splash, and the slave's AoE does not hit the master or the master's party (test).
- On a non-PvP field map, an AoE cast by player A does not splash unaffiliated player B; on a PvP map it does (test, two map-flag fixtures).
- WoE friendly-fire flag (`gvg_noparty` style) makes party members hittable exactly where rAthena enables it (test).
- `Classify` and `CanDamage` agree for the same (src, target, map) — no victim passes the splash filter then gets dropped by the damage gate (test).
- No `TODO` for slave-mob or mapflag handling remains in `MapForeachInRangeService.cs`.

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
