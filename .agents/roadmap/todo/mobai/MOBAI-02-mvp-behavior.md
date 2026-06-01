# MOBAI-02 — MVP-specific AI, drop tier, and boss-HP broadcast

> **Epic:** Mob AI parity · **Status:** ❌ Not started · **Size:** L · **Player-visible:** yes
> **Depends on:** none · **Blocks:** none
> **Related:** FEATURE-01 (mob-death observer) owns the death-side MVP exp/drop/announce wiring; this ticket owns the **AI-side** MVP behavior and cross-references FEATURE-01 for the drop/announce half.

## Problem

The `MobMode.Mvp` bit (`0x0080000`) currently affects **only** the cardfix class
index used by defensive combat math; it has **zero** effect on AI or rewards. A
player fighting an MVP boss sees no MVP skill aggression (bosses cast their kit no
differently from a Poring), gets no MVP item / MVP exp on the killing blow, and
the server never broadcasts the MVP global announce. There is also no boss-HP
broadcast (the floating boss HP bar / `monster_hp_bar` parity), so the client
never shows the boss's remaining HP.

Three concrete gaps:
1. **MVP skill priority** — rAthena gives MVP/boss mobs a wider active radius and
   prioritizes their skill rolls; `MobSkillCastService` treats MVP rows the same
   as any mob and the lazy/hard active-radius gate uses the plain view range.
2. **MVP drop tier + MVP exp** — death drops the normal table only; `MvpExp` and
   `MvpDrops` (already loaded into `MobDbEntry`) are never consumed.
3. **Boss-HP broadcast / MVP announce** — no `clif_mvp_*` / boss-HP ZC packet path.

## Current state (C#)

- `Map.Server/Status/BattleEnums.cs:88 Mvp = 0x0080000` — the mode bit. Its only
  consumers today: `Map.Server/Status/EntityActionGates.cs:79`
  (`isBoss = (src.Stats.Mode & MobMode.Mvp) != 0`) and
  `Map.Server/Status/PlayerMiscServices.cs:133` (a knockback/immune check). Neither
  touches AI or rewards.
- `Map.Server/Mob/MobAiService.cs:278 HasAnyPcInView` — docstring (`:273-276`) calls
  out that boss mobs (`MD_STATUSIMMUNE`) should use a wider active radius
  (`battle_config.boss_active_time`) and flags it as a follow-up; the code uses the
  plain `ChaseRange` for all mobs. The lazy/hard split (`:123`) therefore treats a
  far-away boss as lazy when rAthena keeps it hard for longer.
- `Map.Server/Mob/MobSkillCastService.cs:89 RunPicker` — walks `mob_skill_db` rows
  with no MVP/boss bias; the random-start order and permillage roll are identical
  for MVP and trash mobs. No MVP skill-priority branch.
- `Map.Server/MobDb/MobDbEntry.cs:24 MvpExp`, `:79 MvpDrops` — both loaded
  (`Map.Server/MobDb/MobDb.cs:96,135,184 ExtractMvpDrops`). **Consumed nowhere.**
- `Map.Server/Spawn/MobSpawnService.cs:261 KillMob` — rolls only the normal drop
  table (`RollAndDropLoot`); its docstring (`:298`) explicitly defers MVP drops/exp.
- `Map.Server/Spawn/MobOps/MobOpsService.cs:26 Dead(...) => 0` — empty shell.
- **No ZC MVP / boss-HP packet** exists under `Core.Server/Packets/Out/`
  (`find Core.Server/Packets -iname '*mvp*' -o -iname '*boss*'` → nothing).
- FEATURE-01 (`.agents/roadmap/features/FEATURE-01-mob-death-observer.md`) already
  scopes the death-side MVP exp/drop/announce inside the new `MobDeathObserver`.
  This ticket must **not** duplicate that; it adds the AI-side behavior and the
  MVP **drop-tier selection logic + boss-HP broadcast**, and hands the
  exp/announce emit to the FEATURE-01 observer (or implements it there if FEATURE-01
  is still open — coordinate so there is exactly one MVP death path).

## rAthena reference (source of truth)

Canonical: `rathena/src/map/mob.cpp` (monolithic).

- **Boss type:** `mob.cpp:379 get_bosstype()` → `BOSSTYPE_MVP` when
  `status_has_mode(MD_MVP)`. Used to gate MVP rewards and to ignore kill-steal
  (`mob.cpp:589-590` — MVP and slaves ignore KS).
- **Active radius:** boss/MVP mobs stay in the hard-AI path longer than normal mobs
  (`battle_config.boss_active_time` / `mob_active_time`); the lazy/hard split keys
  off whether the mob was recently spotted and its boss flag. Port: when
  `MD_STATUSIMMUNE` or `MD_MVP`, widen the "has PC in view" window used by the
  lazy/hard split so the boss keeps thinking actively after PCs briefly leave range.
- **Random target:** MVP mobs frequently carry `MD_RANDOMTARGET` (handled by
  MOBAI-03); the skill picker uses `MST_RANDOM` for skill target selection when
  the mob has `MD_RANDOMTARGET` (`mob.cpp:4038`:
  `skill_target = status_has_mode(MD_RANDOMTARGET) ? MST_RANDOM : ms[i]->target`).
- **MVP death block** (`mob.cpp:3118-3210`, inside `mob_dead`), only when
  `mvp_sd && get_bosstype()==BOSSTYPE_MVP`:
  - `clif_mvp_effect(mvp_sd)` — the MVP fireworks effect on the top-damage PC.
  - **MVP exp** (`:3125`): if `md->db->mexp > 0` and map not noexp →
    `clif_mvp_exp(mvp_sd, mexp)` + `pc_gainexp(mvp_sd, mexp)`. With
    `exp_bonus_attacker` and renewal penalty scaling.
  - **MVP drop tier** (`:3148-3210`): from `md->db->mvpitem[MAX_MVP_DROP_TOTAL]`
    pick by rate. `item_drop_mvp_mode==1` → random slot order, else normal order.
    Each `mvpitem[i]` with `nameid>0` rolls `rnd()%10000 < rate` (renewal applies
    `pc_level_penalty_mod(PENALTY_MVP_DROP)`). Winners are **given to the MVP PC**
    (not dropped on the ground like normal loot) via `clif_mvp_item` + additem,
    subject to `MF_NOMVPLOOT`.
  - The MVP global announce ("[name] has killed [boss]") rides on the rare-drop /
    MVP announce path.
- **Boss-HP broadcast:** the client boss HP bar is fed by the monster-HP-bar
  packet (`clif` `ZC_MONSTER_HP_BAR` / the boss-HP variant) on each damage tick
  for `MD_STATUSIMMUNE`/MVP mobs (battle_config `monster_hp_bar`/show_mob_info).
  Port emits the boss-HP ZC to the AOI on HP change for MVP/boss mobs.

## Scope — every sub-system that must be touched

- [ ] **MVP active radius** in `MobAiService`: in `HasAnyPcInView`
      (`MobAiService.cs:278`) widen the search range when
      `(mob.Stats.Mode & (MobMode.Mvp | MobMode.StatusImmune)) != 0` so bosses stay
      in the hard path with a `boss_active`-style radius (plumb a const mirroring
      `battle_config.boss_active_time` semantics, or widen `viewRange` by a fixed
      boss multiplier and document the rAthena knob). Remove the "follow-up" caveat
      in the docstring.
- [ ] **MVP skill priority** in `MobSkillCastService.RunPicker`
      (`MobSkillCastService.cs:89`): for MVP/boss mobs, bias skill selection so MVP
      skill rows are evaluated first (rAthena's effective priority comes from the
      mob_skill_db ordering + the always-active radius; the concrete port is: when
      the mob is MVP, do **not** randomize the start index (`:92 start`) — walk rows
      in declared order so the high-priority boss skills (lower row index) get first
      refusal). Document this as the MVP analogue of the boss-skill ordering.
- [ ] **MST_RANDOM via MD_RANDOMTARGET** in skill target selection: ensure the
      picker passes `MST_RANDOM` when `MobMode.RandomTarget` is set
      (`mob.cpp:4038`). If `MobSkillTargetResolver` already supports `Random`
      (it does — `MobSkillTargetResolver.cs:87`), gate the override on the mode bit.
      (The full `MD_RANDOMTARGET` melee-retarget is MOBAI-03; here it is only the
      skill-target override.)
- [ ] **MVP drop-tier selection** — a new method (in `MobSpawnService.KillMob` MVP
      arm, or the FEATURE-01 `MobDeathObserver`): when
      `mob.DbEntry.MvpExp > 0 || mob.DbEntry.MvpDrops.Count > 0` and a top-damage PC
      exists, select MVP drops by rate from `MvpDrops` (`MobDbEntry.cs:79`) honoring
      normal-vs-random order (`item_drop_mvp_mode`), and **give** the winning item(s)
      to the MVP PC (inventory add, not floor drop), award `MvpExp`, subject to
      `MF_NOMVPLOOT`/noexp mapflags. Resolve the MVP PC as the top cumulative-damage
      contributor from `MobDmgList` (`Map.Server/Combat/MobDmgList.cs`).
- [ ] **Boss-HP broadcast + MVP announce packets**: add the boss/MVP ZC packet
      defs under `Core.Server/Packets/Out/` (boss-HP bar + MVP effect/exp/item +
      global announce) and emit them: boss-HP on MVP/boss damage ticks (hook the
      damage-apply path for `MD_MVP|MD_STATUSIMMUNE` mobs), and the MVP
      effect/announce on the MVP death path. If FEATURE-01 owns the announce emit,
      this ticket supplies the boss-HP-bar packet + emit and leaves a single marked
      call into the FEATURE-01 announce method.
- [ ] **Remove the `MobOpsService.Dead => 0` shell** or fold it into the MVP death
      path (coordinate with FEATURE-01 so there is one death routine).
- [ ] No EF migration (MVP columns already loaded). New packet defs only.

## Done criteria

- An MVP boss kept in view stays in the hard-AI path with a wider active radius
  than a same-stat non-boss mob (observable: it keeps casting/aggroing after PCs
  step just outside a normal mob's view range).
- An MVP boss evaluates its skill rows in declared order (high-priority boss skills
  fire first), unlike a trash mob whose start index is randomized.
- Killing an MVP awards `MvpExp` to the top-damage PC and gives exactly the
  rate-selected `MvpDrops` item(s) to that PC's inventory (not the floor), with
  `MF_NOMVPLOOT`/noexp respected. A non-MVP mob awards none of this.
- The MVP effect + global announce fire once for the killing top-damage PC.
- Damaging a boss/MVP emits the boss-HP-bar packet to the AOI with the correct
  remaining HP; a normal mob does not.
- No `MobOpsService.Dead => 0` shell, no `MvpExp`/`MvpDrops` left unconsumed, no
  log-only no-op in the MVP death path.

## Test plan

- `Map.Server.Tests`:
  - **active radius**: boss vs non-boss at the same distance just past normal view
    → boss takes the hard path (`HasAnyPcInView` true), non-boss takes lazy.
  - **skill order**: MVP mob with rows [A (priority), B] and a seeded RNG → assert A
    is evaluated before B (no random start); non-MVP randomizes.
  - **MST_RANDOM**: mob with `RandomTarget` + a skill row → resolver invoked with
    `Random` target mode.
  - **MVP drops/exp**: kill an MVP with `MvpExp>0` + one `MvpDrops` entry (rate
    forced to pass via seeded RNG) → top-damage PC gains exp + the item in
    inventory; non-MVP mob → neither. `MF_NOMVPLOOT` map → no item.
  - **boss-HP packet**: damage a boss → assert the boss-HP `OutgoingPacket` is
    queued to the AOI; normal mob → not.
- Regression: normal-mob drop/exp tests unchanged; the MVP path must not
  double-award normal exp.
- Manual/live: `@monster <mvp id>`, kill it, confirm MVP fireworks, MVP item in
  bag, global announce, and the boss HP bar updating during the fight.

## Notes / gotchas

- **Single death path.** FEATURE-01 introduces `MobDeathObserver`; the MVP
  exp/drop/announce belongs there. If FEATURE-01 lands first, implement the MVP
  drop-tier + exp logic *inside* the observer's MVP block (it already scopes it).
  If this ticket lands first, put the MVP block in `KillMob` and have FEATURE-01
  move/call it — never two MVP reward paths.
- MVP drops are **given to the PC**, not dropped on the floor — different code path
  from normal loot. Don't route them through `RollAndDropLoot`.
- The MVP PC is the **top cumulative damage** contributor (`mvp_sd`), which can
  differ from the last-hitter (`killer`). Resolve from `MobDmgList`, not from the
  killing-blow source.
- Boss-HP-bar emission is per-damage-tick and can be chatty; gate it on the
  MVP/StatusImmune flag and (optionally) a throttle, mirroring `monster_hp_bar`.
- `MD_RANDOMTARGET` full melee retargeting is MOBAI-03; only the skill-target
  override (`MST_RANDOM`) is in scope here.
