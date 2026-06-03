# INFRA-04 — Sage AutoSpell (SA_AUTOSPELL) SC attach + on-hit proc

> **Epic:** Infra parity · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** none · **Blocks:** none

## Problem

The Sage skill **AutoSpell** (`SA_AUTOSPELL`) — which grants a temporary buff that
randomly auto-casts a chosen bolt (Cold/Fire/Lightning Bolt, Soul Strike, etc.) on the
caster's normal melee/ranged hits — does **nothing**. `AutoSpell` only logs and returns
true: no `SC_AUTOSPELL` status is attached, and the on-hit chain never rolls or casts the
granted spell. A Sage casting AutoSpell sees the cast "succeed" but gets zero procs.

Note: the **card-driven** `bonus3 bAutoSpell` (item autospell) already works through
`ScriptedBonusHost` → `IPlayerBonusService.AddAutobonus(OnHit, ...)`. This ticket is the
**skill** SA_AUTOSPELL, which uses the `SC_AUTOSPELL` status mechanism, not the autobonus
script path.

## Current state (C#)

- `Map.Server/Skills/SkillSideEffectService.cs:52-59` — `AutoSpell(Entity caster, ushort
  grantedSkillId)`: logs "deferred per PARITY-REMAINING" and `return true`. No
  `_sc.Start(... StatusType.Autospell ...)`, no proc hook.
- `SkillSideEffectService` ctor (`:25-37`) already injects `IStatusChangeService? _sc`,
  `IMapSessionRegistry? _sessions`, and `Random _rng` — everything needed to attach the
  SC and roll the proc is already in scope.
- `Map.Server/Status/StatusType.cs:101` — `Autospell, // SC_AUTOSPELL` enum value exists.
- `Map.Server/Status/IPlayerBonusService.cs:60` — `AutobonusTrigger.OnHit` (used by the
  *card* autospell at `ScriptedBonusHost.cs:119-126`). The on-hit chain that fires
  autobonus already exists (`ExecuteAutobonus`); the SC_AUTOSPELL proc must hook the same
  attack pipeline.
- The on-hit attack chain (where `skill_attack` / `pc_autospell` would run) — locate the
  C# equivalent (battle / attack service) that already drives `ExecuteAutobonus(OnHit)`
  so SC_AUTOSPELL procs ride the same dispatch point.

## rAthena reference (source of truth)

Canonical source is `skill.cpp` / `status.cpp` (monolithic).

- **Grant — `skill_autospell` (`skill.cpp:20772-20817`):**
  - `lv = pc_checkskill(sd, skill_id)` (learned level of the chosen spell);
    `skill_lv = sd->menuskill_val` (the AutoSpell level cast).
  - Refuse if `skill_lv == 0 || lv == 0` (must have learned the spell).
  - **maxlv** (the level the spell auto-casts at) — RENEWAL: `skill_lv / 2`, except
    Cold/Fire/Lightning Bolt with `SC_SPIRIT val2 == SL_SAGE` → 10. PRE-RE: a per-spell
    table (NapalmBeat→3, bolts→1..3 by skill_lv, SoulStrike→1..3, Fireball→1..2,
    FrostDiver→1; anything else → return 0) (`:20785-20810`).
  - `maxlv = min(lv, maxlv)`.
  - `sc_start4(SC_AUTOSPELL, 100, val1=skill_lv, val2=skill_id, val3=maxlv, val4=0,
    skill_get_time(SA_AUTOSPELL, skill_lv))` (`:20814`).
- **SC setup — `status.cpp:10619` `case SC_AUTOSPELL`:**
  - `val1` = AutoSpell skill level, `val2` = spell id to cast, `val3` = max level to cast.
  - **Cast chance** `val4`: RENEWAL `val4 = val1 * 2`; PRE-RE `val4 = 5 + val1*2`
    (percent). This is the per-hit proc chance.
- **Proc — on-hit, `skill.cpp` around the autospell block (`:2337`+ is the *card*
  autospell list; the SC proc is the `pc_autocast`/`SC_AUTOSPELL` check in the attack
  path):** on a successful normal attack, if `SC_AUTOSPELL` is active, roll its `val4`%
  chance; on success cast spell `val2` at level `val3` at the target, marking
  `state.autocast` so it bypasses cast time / SP cost gating as rAthena does. The cast
  goes through the normal skill-cast entry (`skill_castend_id` / `skill_castend_pos`).

## Scope — every sub-system that must be touched

- [ ] **`AutoSpell` method** (`SkillSideEffectService.cs:52-59`): change signature to
      carry the AutoSpell skill level (the cast level) and the granted spell id — e.g.
      `AutoSpell(Entity caster, ushort autoSpellLevel, ushort grantedSkillId)`. Plumb the
      caster's learned level of `grantedSkillId` (need a skill-level reader — see gotcha).
  - [ ] Validate: caster has learned `grantedSkillId` (level > 0) and `autoSpellLevel > 0`.
  - [ ] Compute `maxlv` per the pre-RE/RE table (match the build's renewal flag the rest
        of the codebase uses).
  - [ ] `_sc.Start(caster, StatusType.Autospell, val1: autoSpellLevel, val2:
        grantedSkillId, val3: maxlv, val4: <chance>, durationMs: <skill_get_time>,
        src: caster)`. Compute `val4` = the proc chance (`5 + val1*2` pre-RE / `val1*2`
        RE) — or compute it in the SC handler if the SC table derives val4 from val1.
  - [ ] Return true only when the SC is attached; false on validation refusal (so the
        client gets the right fail/ack).
- [ ] **SC_AUTOSPELL setup** in the status-change service: if the C# `StatusChangeService`
      computes derived `val` fields per SC (like rAthena's `status_change_start` switch),
      add the `Autospell` arm to populate `val4` (proc chance) from `val1` so the proc
      reads it. If the service stores raw vals only, compute val4 in `AutoSpell` and pass
      it directly.
- [ ] **On-hit proc hook**: in the attack/battle chain that already calls
      `IPlayerBonusService.ExecuteAutobonus(pc, OnHit)`, add a SC_AUTOSPELL check:
  - [ ] If the attacker has `StatusType.Autospell` active, roll `_rng.Next(100) < val4`.
  - [ ] On success, cast spell `val2` at level `val3` at the current target through the
        normal skill-cast service, flagged as an auto-cast (no SP/cast-time/range gating
        beyond what rAthena keeps — match `state.autocast`).
  - [ ] Ranged/arrow attacks halve the rate in the *card* autospell; SC_AUTOSPELL uses the
        flat `val4` — match rAthena (no halving for the skill SC unless the source says so).
- [ ] **Wiring**: confirm the proc hook runs once per landed normal attack, not per skill.

## Done criteria

- Casting SA_AUTOSPELL with a learned Fire Bolt attaches `StatusType.Autospell` with
  `val2 = FireBolt id`, `val3 = maxlv`, and a duration matching `skill_get_time`.
- Normal attacks under the buff proc Fire Bolt at the computed level with the `val4`%
  chance (verifiable with a seeded RNG).
- Casting AutoSpell without having learned the chosen spell refuses (no SC).
- No "deferred per PARITY-REMAINING" comment and no bare `return true` remain in `AutoSpell`.

## Test plan

- `Map.Server.Tests/Skills/AutoSpellParityTests`:
  - Grant attaches the SC with correct val1/val2/val3 and duration; refuses when the
    spell is unlearned.
  - maxlv table: pin a couple of cases (RE `skill_lv/2`; pre-RE bolt levels) against the
    chosen renewal mode.
- On-hit proc test (attack-chain test): with `Autospell` SC active and seeded RNG below
  threshold, a normal attack triggers exactly one cast of `val2` at `val3`; above
  threshold, no cast.

## Notes / gotchas

- **Skill-level reader**: `SkillSideEffectService` does not currently take an
  `IPlayerSkillService`. `ScriptedBonusHost` has `IPlayerSkillService? _skillSvc`
  (used in INFRA-09's `getskilllv`); inject the same here to read the caster's learned
  level of the granted spell, or plumb the level from the casting handler if it already
  resolved it.
- **Don't double-implement** vs the card `bonus3 bAutoSpell` autobonus path — that one is
  fine and uses `AddAutobonus(OnHit)`. The skill uses the SC mechanism; they coexist.
- Match the **renewal flag** the rest of Map.Server uses for the maxlv + val4 formulas;
  picking the wrong branch silently changes proc rates and cast levels.
- The proc must cast at the **target** of the triggering attack; ground-target spells
  (rare for autospell) cast at the target's cell.
