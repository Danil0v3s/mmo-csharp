# COMBAT-07 — Renewal cast-time formula + item/card/skill cast bonuses

> **Epic:** Combat parity · **Status:** 🚧 In progress · **Size:** L · **Player-visible:** yes
> **Depends on:** COMBAT-06 (per-skill cast-rate tables) · **Blocks:** none

## Problem

Cast times are wrong on two counts:

1. **The renewal variable-cast formula is missing the DEX/INT sqrt reduction.**
   `SkillCastTimingService.VfCastFix` (`SkillCastTimingService.cs:126-151`) splits fixed vs
   variable cast but never applies rAthena's
   `time = time * (1 - sqrt((dex*2 + int) / vcast_stat_scale))`. So a high-DEX/INT caster gets
   no variable-cast reduction — every spell casts at near its full base time.
2. **Item/card cast bonuses are populated but never read** (pure wiring gap). The bundle has
   `VarCastRate / FixCastRate / AddVarCastMs / AddFixCastMs / DelayRate`
   (`EquipBonusBundle.cs:77-81`) and `BonusScriptExtractor.ApplyFlat` populates
   `variablecastrate / fixedcastrate / delayrate` (`BonusScriptExtractor.cs:118-120`), but
   `SkillCastTimingService` never touches `caster.EquipBonuses` — the comments at
   `SkillCastTimingService.cs:51-53, 146-148` explicitly defer it.

Also missing: per-skill `skillcastrate / skillvarcast / skillfixcast` player tables, and
`SA_ABRACADABRA` `abra_db` wiring (`SkillCastTimingService.cs:156`). SC overlays
(Suffragium/Memorize/Slowcast/Bragi/Izayoi/Paralysis) are already wired in `CastFixSc`.

## Current state (C#)

- `Map.Server/Skills/SkillCastTimingService.cs:34-59` `CastFix` (pre-renewal) — DEX scale +
  global `cast_rate`; comment at `:51` defers item/card bonuses.
- `Map.Server/Skills/SkillCastTimingService.cs:72-123` `CastFixSc` — SC overlays implemented
  (Slowcast, Paralysis, Suffragium, Memorize, Izayoi, PoemBragi). Correct.
- `Map.Server/Skills/SkillCastTimingService.cs:126-151` `VfCastFix` (renewal) — splits
  fixed/variable using `default_fixed_castrate`; **no DEX/INT sqrt**; comment at `:146` defers
  `add_varcast/add_fixcast/varcastrate/fixcastrate` + per-skill tables.
- `Map.Server/Skills/SkillCastTimingService.cs:154-186` `DelayFix` — DEX/AGI scale + global
  `delay_rate`; comment at `:156` defers `SA_ABRACADABRA`; does **not** read
  `bundle.DelayRate`.
- `Map.Server/Inventory/EquipBonusBundle.cs:77-81` — `VarCastRate/FixCastRate/AddVarCastMs/
  AddFixCastMs/DelayRate` fields exist; populated by the extractor; **zero readers**.
- `Map.Server/Entities/PlayerEntity.cs:145` — `EquipBonuses` is on the entity, reachable from
  `caster` in the timing service.

## rAthena reference (source of truth)

Canonical: `skill.cpp` (not split files).

- `skill.cpp:20324` `skill_vfcastfix(bl, time, skill_id, skill_lv)` (renewal). Key lines
  confirmed by reading:
  - `:20444`: `time = time * (1 - sqrt(((float)(status_get_dex(bl)*2 + status_get_int(bl)) /
    battle_config.vcast_stat_scale)));` (gated by `!(flag&1)` — the variable-cast bypass).
  - Variable-cast-rate reductions accumulate into `reduce_cast_rate` and
    `time = time * (1 - min(reduce_cast_rate,100)/100)`.
  - Fixed cast: `fixcast_r` is the max of SC fixed-cast reducers (e.g. Sacrament `:20436`);
    final `time = max(time,0) + (1 - min(fixcast_r,100)/100) * max(fixed,0)` (`:20446`).
  - Item/card inputs feed `reduce_cast_rate` / `varcast` / `fixcast` before these steps
    (`sd->bonus.varcastrate`, `sd->bonus.add_varcast`, `sd->bonus.fixcastrate`,
    `sd->bonus.add_fixcast`, and per-skill `sd->skillcastrate`/`skillfixcast`/`skillvarcast`).
- `skill_castfix_sc` / `skill_delayfix` (`skill.cpp` ~20193-20565) — delay reductions read
  `sd->bonus.delayrate` and per-skill `sd->skilldelay`.
- `SA_ABRACADABRA`: random skill pick from `abra_db`; its cast/delay are special-cased to 0 in
  `skill_delayfix` (the `:156` comment).

## Scope — every sub-system that must be touched

- [ ] **Add the DEX/INT sqrt reduction to `VfCastFix`.** Add `vcast_stat_scale` to
      `IBattleConfigService` (rAthena default 530). Apply
      `variableTime = (int)(variableTime * (1 - Math.Sqrt((double)(caster.Stats.Dex*2 +
      caster.Stats.IntStat) / scale)))`, gated like rAthena (`flag&1` bypass; clamp ≥ 0).
- [ ] **Read `caster.EquipBonuses` in `VfCastFix`.** Cast caster to `PlayerEntity`; apply:
      `variableTime += bundle.AddVarCastMs;` then `variableTime = variableTime *
      (100 - bundle.VarCastRate) / 100;` and `fixedTime += bundle.AddFixCastMs;
      fixedTime = fixedTime * (100 - bundle.FixCastRate) / 100;` (match rAthena clamp/order:
      add ms first, then rate). Mobs/NPC keep the early return (`:129`).
- [ ] **Read `bundle.DelayRate` in `DelayFix`** (`SkillCastTimingService.cs:183`): after the
      global `delay_rate`, apply `time = time * (100 - bundle.DelayRate) / 100`.
- [ ] **Per-skill cast tables.** COMBAT-06 adds `bundle.SkillCastRate / SkillVarCast /
      SkillFixCast / SkillDelay` maps (skillId→value). Read them in `VfCastFix`/`DelayFix`
      keyed on `skillId`.
- [ ] **`SA_ABRACADABRA`**: wire the `abra_db` random-skill table and zero its cast/delay in
      `DelayFix` (remove the `:156` deferral comment).
- [ ] **`CastFix` (pre-renewal)**: the project is renewal-only (CLAUDE.md), so the main path is
      `VfCastFix`. Apply the item/card reads there; leave `CastFix` as the documented
      pre-renewal fallback but remove the misleading "deferred" comment or note it's
      pre-renewal-only.
- [ ] **No DB migration / no packets** — `ZC_USESKILL_ACK` cast-time field already flows from
      the resolved cast time; this just changes the number.

## Done criteria

- A caster with DEX 130 / INT 99 casting a 5000ms-variable spell gets the renewal sqrt
  reduction: `5000 * (1 - sqrt((260+99)/530)) ≈ 5000 * (1 - 0.823) ≈ 885ms` variable (then +
  fixed). Matches rAthena within integer floor.
- Equipping a `bonus bVariableCastrate,-30;` (−30% var cast → faster) reduces the variable
  portion by 30% on top of the DEX/INT reduction.
- `bonus2 bVariableCastTime,WZ_STORMGUST,-50;`-style per-skill reduction affects only that
  skill.
- `bonus bDelayrate,-20;` reduces after-cast delay 20%.
- SA_ABRACADABRA has 0 cast delay.

## Test plan

- Unit-test `VfCastFix` sqrt: tabulate (dex,int)→variableTime for several points against
  hand-computed rAthena values (scale 530).
- Unit-test bundle reads: same skill with/without `VarCastRate/AddVarCastMs/FixCastRate`,
  assert the exact resulting ms.
- Unit-test per-skill cast-rate map affects only the keyed skill.
- Unit-test `DelayFix` honors `bundle.DelayRate`.
- Manual: high-DEX wizard casting Storm Gust visibly casts faster than a low-DEX one;
  equipping a cast-reduction card shortens the bar further.

## Notes / gotchas

- `vcast_stat_scale` default is 530 in rAthena; expose it as config rather than a literal.
- Order matters: rAthena applies SC reductions (`CastFixSc`, already done), the DEX/INT sqrt,
  then card/item rate, with fixed cast handled separately and **not** reduced by DEX/INT.
  Don't let card var-cast-rate touch the fixed portion.
- `caster` is typed `Entity`; only PCs have `EquipBonuses`. Guard the cast and skip for
  mob/NPC (they already early-return at `:129`).
- The SC overlays in `CastFixSc` already consume Suffragium/Memorize charges; don't
  double-apply them in `VfCastFix`.
