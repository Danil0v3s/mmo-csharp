# MOBAI-03 — Change-target mode bits driven from the hard-AI tick

> **Epic:** Mob AI parity · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** MOBAI-01 (slave-lost-target feeds the same scan) · **Blocks:** none

## Problem

`MobChangeTargetService` correctly ports the `mob_can_changetarget` gate matrix
(per FSM state × `MD_CHANGETARGETMELEE`/`MD_CHANGETARGETCHASE`), but the four
target-switch mode bits are not actually **exercised** from the AI tick beyond the
attacker-driven `TrySetTarget` call in `NotifyAttacked`. Specifically:

- **`MD_CHANGECHASE`** (chase-state retarget scan) — rAthena runs a dedicated
  `mob_ai_sub_hard_changechase` area scan when the mob is RUSH/FOLLOW; the C# tick
  never does this scan, so a chasing mob never opportunistically switches to a
  closer enemy that wandered into melee range.
- **`MD_CHANGETARGETMELEE` / `MD_CHANGETARGETCHASE`** — only consulted reactively
  via `NotifyAttacked → TrySetTarget`; the proactive Berserk/Rush retarget branch
  of `mob_ai_sub_hard` (the `md->attacked_id` arm at `mob.cpp:1785`) is only
  partially mirrored.
- **`MD_RANDOMTARGET`** — after a swing the mob should attack once then pick a new
  random enemy; the C# attack path always continuous-attacks the same target.
- **`MD_TARGETWEAK`** — the active search should skip targets whose level is within
  5 of the mob (`status_get_lv(bl) >= md->level-5`); the C# aggro scan
  (`MobAiService.cs:224-236`) ignores level entirely, so a TARGETWEAK mob aggros
  strong players it should leave alone.

## Current state (C#)

- `Map.Server/Mob/MobChangeTargetService.cs` — `CanChangeTarget` (`:18`) ports the
  FSM×mode matrix (Berserk→ChangeTargetMelee `:33`, Rush→ChangeTargetChase `:36`,
  Follow/Angry/Idle/Walk/Loot→always `:40-44`); `TrySetTarget` (`:51`) gates on
  `CanChangeTarget` only when a target already exists; `RetargetMobsChasing` (`:64`)
  is the KO_GENWAKU sweep. **No** changechase scan, **no** RANDOMTARGET, **no**
  TARGETWEAK level filter.
- `Map.Server/Mob/MobAiService.cs`:
  - aggressive scan (`:212-248`) — closest-PC by Chebyshev within `viewRange`; no
    level filter (TARGETWEAK), no changechase branch, then
    `_attack.StartAttack(mob, closest.Id, continuous: true)` (`:248`) — always
    continuous, so RANDOMTARGET never re-rolls.
  - `NotifyAttacked` (`:377`) calls `_changeTarget.TrySetTarget(mob, attacker)`
    (`:409`) — the reactive retarget, gated by the FSM matrix.
  - The Berserk engaged-target arm (`:178-181`) transitions to Berserk and casts;
    it never re-scans for a closer enemy (changechase).
- `MobMode` bits (`Map.Server/Status/BattleEnums.cs`): `ChangeChase = 0x0400`
  (`:79`), `ChangeTargetMelee = 0x1000` (`:81`), `ChangeTargetChase = 0x2000`
  (`:82`), `TargetWeak = 0x4000` (`:83`), `RandomTarget = 0x8000` (`:84`). All
  loaded by `StatusCalcService.cs:368-373`.
- `MobEntity.SkillState` (`Map.Server/Entities/MobEntity.cs:90`) — drives the FSM
  matrix; transitions set in `MobAiService` (Idle/Berserk) and `MobFsm`.
- Mob level: `mob.DbEntry` carries the mob level; player level via
  `PlayerEntity` status. Needed for the TARGETWEAK comparison.

## rAthena reference (source of truth)

Canonical: `rathena/src/map/mob.cpp` (monolithic).

- **`mob_can_changetarget`** (`mob.cpp:1235-1264`) — the gate matrix C# already
  ports. Note the two Berserk sub-conditions C# intentionally simplifies
  (`MobChangeTargetService.cs:30-32` docstring): the `norm_attacked_id` match and
  the `mob_ai&0x80` distance gate. Keep that simplification but document it.
- **Attacker-driven retarget arm** (`mob.cpp:1785-1852`): when `md->attacked_id`
  set and `MD_CANATTACK`:
  - if `attacked_id == target_id` → rude-attack escalation (MOBAI handled in
    `NotifyAttacked`).
  - else if `abl = attacked_id` and `(!tbl || mob_can_changetarget(md, abl, mode))`
    → if the attacker is a valid reachable enemy, `md->target_id = md->attacked_id`
    (switch), decrement `attacked_count`. This is the proactive switch C# does via
    `TrySetTarget`; verify it matches (gate, then set, then engage next tick).
- **Changechase scan** (`mob.cpp:1881-1887`):
  ```c
  else if (mode&MD_CHANGECHASE && (skillstate==MSS_RUSH || skillstate==MSS_FOLLOW)) {
      search_size = min(view_range, rhw.range);
      map_foreachinallrange(mob_ai_sub_hard_changechase, &md->bl, search_size, ENEMY, md, &tbl);
  }
  ```
  `mob_ai_sub_hard_changechase` (`mob.cpp:1348-1369`): for each enemy in range that
  passes `battle_check_target(BCT_ENEMY)` + `status_check_skilluse`, if
  `battle_check_range(rhw.range)` (in melee reach) → set it as the new target.
  Effect: a chasing mob switches to any enemy already within its melee range.
- **TARGETWEAK** in active search (`mob.cpp:1309-1310`):
  ```c
  if ((mode&MD_TARGETWEAK) && status_get_lv(bl) >= md->level-5)
      return 0;   // skip targets not at least 5 levels weaker
  ```
- **RANDOMTARGET** post-swing (`mob.cpp:1993-2002`): when target in range and
  `MD_RANDOMTARGET`:
  ```c
  unit_attack(&md->bl, tbl->id, 0);           // attack ONCE (continuous=0)
  tbl = battle_getenemy(&md->bl, ENEMY, search_size);  // search_size=min(view,range)
  if (tbl) md->target_id = tbl->id;           // re-aim at a new random enemy
  ```
  Non-RANDOMTARGET path uses `unit_attack(..., 1)` (continuous).

## Scope — every sub-system that must be touched

- [ ] **TARGETWEAK** in `MobAiService` aggressive scan (`:224-236`): when
      `(mode & MobMode.TargetWeak) != 0`, skip any PC whose level
      `>= mobLevel - 5`. Resolve mob level from `mob.DbEntry` and player level from
      `PlayerEntity` status. Mirror `mob.cpp:1309`.
- [ ] **CHANGECHASE branch** in `MobAiService.Tick`: add the `else if` arm after the
      aggressive scan, mirroring `mob.cpp:1881`. When
      `(mode & MobMode.ChangeChase) != 0 && SkillState is Rush or Follow`, run a
      `min(viewRange, attackRange)` enemy scan; for the first enemy already within
      melee range that passes the enemy/skilluse checks, set it as the target.
      Implement the scan as a new `IMobChangeTargetService.TryChangeChase(mob, range)`
      using `IEntityRegistry.ForEachInRange` (so it is unit-testable), returning the
      switched target or null. Honor `CanChangeTarget` (Rush state requires
      `ChangeTargetChase`).
- [ ] **RANDOMTARGET** in the engage/attack path: when
      `(mode & MobMode.RandomTarget) != 0` and the mob is about to attack an
      in-range target, issue a **single** swing (`continuous: false`) instead of
      continuous, then immediately pick a new random enemy within
      `min(viewRange, attackRange)` (a `battle_getenemy` analogue — random enemy in
      range, not nearest) and set `mob.TargetId` to it. Add
      `IMobChangeTargetService.PickRandomEnemy(mob, range)` (or reuse
      `MobSkillTargetResolver.ResolveRandomEnemy` semantics) for the re-aim. This
      lives where `MobAiService` decides continuous vs single attack — at the
      Berserk engage arm (`:178-181`) and the aggressive `StartAttack` (`:248`).
- [ ] **Verify the proactive attacker-switch** (`NotifyAttacked` + `TrySetTarget`)
      matches `mob.cpp:1806-1847`: attacker becomes target only when
      `CanChangeTarget` passes and the attacker is a reachable enemy; ensure the
      `attacked_id` is cleared after the check (rAthena clears it at `:1851`). Add
      the clear if missing (`mob.AttackedId = 0` after the change-target decision).
- [ ] **Document** that the Berserk `norm_attacked_id` + `mob_ai&0x80` sub-gates
      remain the conservative simplification already noted in
      `MobChangeTargetService.cs:30-32` (do not regress that).
- [ ] No EF migration, no packets — pure AI targeting.

## Done criteria

- A `TargetWeak` mob ignores PCs within 5 levels of its own level and only aggros
  weaker PCs; a non-TargetWeak mob aggros regardless of level.
- A `ChangeChase` mob in RUSH/FOLLOW state switches its target to an enemy that
  steps into its melee range mid-chase (and a mob without the bit does not).
- A `RandomTarget` mob swings once at its current target, then re-aims at a
  randomly chosen in-range enemy each cycle (target id observed to change between
  swings when ≥2 enemies are in range); a non-RandomTarget mob keeps hitting the
  same target.
- The attacker-driven proactive switch sets the new target only through the
  `CanChangeTarget` gate and clears `AttackedId` after evaluating.
- Each of the four mode bits independently drives the documented switch; flipping a
  bit off restores the default (no switch) behavior.
- No `// TODO`, no unexercised mode bit, no log-only no-op in the touched paths.

## Test plan

- `Map.Server.Tests` `MobChangeTargetModeTests` (new):
  - **TARGETWEAK**: mob level 50, PC level 46 (within 5) → not aggroed; PC level 44
    → aggroed. Toggle bit off → both aggroed.
  - **CHANGECHASE**: mob RUSH-state chasing PC-A, PC-B steps into melee range →
    `TryChangeChase` switches to PC-B; without the bit, target stays PC-A; without
    RUSH/FOLLOW state, no switch.
  - **RANDOMTARGET**: two enemies in range, seeded RNG → after a swing the mob's
    `TargetId` is re-rolled to the other enemy (single-swing path taken); without
    the bit, continuous attack keeps the same target.
  - **attacker switch**: mob engaged on A, hit by B, `CanChangeTarget` true →
    `TargetId` becomes B and `AttackedId` cleared; gate false → stays on A.
- Regression: existing `MobChangeTargetService` gate-matrix tests and
  `MobAiService` aggro tests still pass.
- Manual/live: spawn a TargetWeak/RandomTarget mob (e.g. an MVP with RandomTarget),
  bring two chars, observe target hopping; bring a high-level char near a
  TargetWeak mob and confirm it is ignored.

## Notes / gotchas

- `battle_getenemy` is a **random** in-range enemy, not the nearest — RANDOMTARGET
  must not reuse the closest-PC aggro scan. `MobSkillTargetResolver.ResolveRandomEnemy`
  (`MobSkillTargetResolver.cs:164`) already implements a random-in-range pick; reuse
  its selection rather than the nearest-scan.
- The changechase branch is an `else if` to the aggressive scan in rAthena — a mob
  that ran the aggressive active-search does **not** also run changechase the same
  tick. Preserve the exclusivity.
- `search_size = min(view_range, rhw.range)` for both changechase and the
  RANDOMTARGET re-aim. Use the mob's attack range, not view range, as the cap.
- TARGETWEAK uses `>= md->level - 5` (skip if target level is at least mob-level
  minus 5) — note the off-by-one direction: targets must be **more than 5 levels
  below** the mob to be picked.
- MOBAI-01's `slaveLostTarget` flag feeds the same aggressive-scan gate; if both
  tickets land, ensure the changechase `else if` sits after the
  `aggressive || slaveLostTarget` arm so the exclusivity still holds.
