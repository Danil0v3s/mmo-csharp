# COMBAT-100 — Per-race vellum vanish (bHPVanishRaceRate/bSPVanishRaceRate damage override)

> **Epic:** combat · **Status:** ❌ Not started · **Size:** S · **Player-visible:** yes
> **Depends on:** COMBAT-83 · **Blocks:** none
> **Filed by:** COMBAT-83 — the per-race "vellum" vanish is a damage-OVERRIDE mechanic distinct from
> the COMBAT-44 flat drain; it needs the SP-as-damage display the C# lacks, so it is its own ticket.

## Problem

`bonus3 bHPVanishRaceRate, r, rate, per` / `bSPVanishRaceRate` populate `hp_vanish_race[r]` /
`sp_vanish_race[r]` (rate, per). rAthena `battle_vellum_damage` (battle.cpp:10175): on a PC weapon
hit vs race r, if the summed `[race]+[RC_ALL]` rate rolls, the hit's damage is REPLACED by
`apply_rate(target.max_hp, per)` (HP) or `apply_rate(target.max_sp, per)` with `isspdamage=true`
(SP) — overriding the normal weapon damage. The C# models neither the per-race maps nor the override.

## Current state (C#)

- `Map.Server/Inventory/EquipBonusBundle.cs` — no `HpVanishRace`/`SpVanishRace` maps.
- `Map.Server/Combat/BattleCalculator.cs` / `DamageService.cs` — no vellum damage override; the
  BattleDamage struct has no `IsSpDamage` flag (needed for the SP variant's client display).

## rAthena reference (source of truth)

- `battle.cpp:10175 battle_vellum_damage`; `pc.cpp:5125/5133` (the bonus3 parse → hp/sp_vanish_race).

## Scope — every sub-system that must be touched

- [ ] Add `HpVanishRaceRate/HpVanishRacePer[RaceSize]` + the SP pair to `EquipBonusBundle` (+ Reset);
      parse the `bonus3 bHPVanishRaceRate/bSPVanishRaceRate, r, rate, per` forms in ScriptedBonusHost.
- [ ] Apply the override on the PC weapon auto-attack (skillId 0): roll `[race]+[RC_ALL]`; on success
      set the hit damage to `apply_rate(max_hp, per)` (HP) — HP and SP don't stack (else-if).
- [ ] SP variant: add an `IsSpDamage` flag to BattleDamage + the ZC display so the SP override shows
      as SP loss (rAthena `wd.isspdamage`).

## Done criteria

- A vellum (bHPVanishRaceRate) weapon hits a matching-race mob for `per%` of its max HP (overriding
  the swing); the SP variant drains/display SP.

## Test plan

- A guaranteed-rate vellum override replaces the swing damage with `max_hp * per/100`.
