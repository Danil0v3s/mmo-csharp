# COMBAT-45 — pc_bonus consumers: speed/weight/crit/usesp + unbreakable/intravision flags

> **Epic:** Combat parity · **Status:** ✅ Done (2026-06-02) · **Size:** M · **Player-visible:** yes
> **Depends on:** COMBAT-23 (the fields are parsed; this wires the remaining consumers)
> **Blocks:** none
> **Filed by:** COMBAT-23 — the single-value/flag consumers it parsed but did not wire.

## Problem

COMBAT-23 parsed the single-value tail (`bHealPower`, `bHPrecovRate`/`bSPrecovRate`,
`bSpeedRate`, `bCriticalRate`, `bUseSPrate`, `bAddMaxWeight`, `bSpeedAddRate`,
`bHealPower2`) and the 1-arg flag form (`bNoCastCancel`, `bUnbreakable*`, `bIntravision`)
into `EquipBonusBundle`, and wired the consumers for `HealPower` (heal output) +
`HpRecovRate`/`SpRecovRate` (natural regen) + `NoCastCancel` (COMBAT-08). The remaining
fields are captured but have no consumer:

1. **bSpeedRate / bSpeedAddRate** — need a `status_calc_speed` port (the C# PC speed is a
   flat 150; rAthena folds speed bonuses + the SC speed table).
2. **bCriticalRate** — flat crit-rate add in `is_attack_critical`.
3. **bUseSPrate** — SP-cost % modifier in the skill SP-requirement check.
4. **bAddMaxWeight** — max-weight in the weight/overweight service.
5. **bHealPower2** — heal-RECEIVED % on the heal target.
6. **bUnbreakable\* / bIntravision** — equip break/strip gate + see-hidden.

## Current state (C#)

- `EquipBonusBundle` has all the fields (COMBAT-23); only `HealPower`/`HpRecovRate`/
  `SpRecovRate`/`NoCastCancel` are consumed.
- `Map.Server/Status/StatusCalcService.cs` — PC `s.Speed = 150` flat; no speed_rate fold.

## rAthena reference (source of truth)

- `status.cpp` `status_calc_speed` (speed_rate / speed_add_rate + SC speed table);
  `battle.cpp:2980` is_attack_critical (+critical_rate); `pc.cpp` SP cost / max weight;
  `pc.cpp` equip-break gate (unbreakable mask).

## Scope — every sub-system that must be touched

- [x] Folded `SpeedRate` (non-stackable min) + `SpeedAddRate` (stackable) into
      `StatusCalcService`'s PC speed (`150 * (100 + SpeedRate + SpeedAddRate) / 100`).
      The SC speed table ➡️ COMBAT-65.
- [x] `bCriticalRate` — it's a PERCENT modifier (rAthena `status->cri *= (100+rate)/100`,
      base 100), applied in `CalcPc` (the stat calc) after the flat crit fold — NOT a
      flat add in `is_attack_critical` as the ticket assumed.
- [x] Skill SP-cost: `UseSpRate` % modifier in a shared `SkillRequirementService.SpCost`
      used by both the check and the consume.
- [x] Weight service: `AddMaxWeight` added to the weight cap.
- [x] Heal target: `HealPower2` (heal received) applied in `Heal.CalcRenewalHeal`.
- [x] `Unbreakable*` equip-break gate + `Intravision` see-hidden ➡️ COMBAT-65.

## Done criteria

- ➡️ from COMBAT-23: `bonus bSpeedRate,25;` increases move speed per rAthena. ✅
- crit-rate ✅ / usesp ✅ / max-weight ✅ / heal-power2 ✅ each verified;
  unbreakable ➡️ COMBAT-65.

## Test plan

- `Combat45PcBonusConsumerTests`: speed fold, crit-rate add, SP-cost %, max-weight,
  heal-power2, unbreakable-blocks-break.

## Notes / gotchas

- `bSpeedRate` is non-stackable (rAthena keeps `min(-val)`); the bundle already stores it
  that way. A lower speed value = faster movement.

## History

- 2026-06-02 · Wired 5 single-value pc_bonus consumers: `bSpeedRate`/`bSpeedAddRate`
  → PC speed fold (StatusCalcService), `bCriticalRate` → crit % modifier in CalcPc
  (it's a percent, not a flat add — rAthena status.cpp:4389), `bUseSPrate` →
  `SkillRequirementService.SpCost` (shared check+consume), `bAddMaxWeight` → weight cap
  (PlayerWeightStatusService), `bHealPower2` → heal-received in `Heal.CalcRenewalHeal`.
  Combat45PcBonusConsumerTests (5). Full Map.Server.Tests green except the pre-existing
  INFRA-11 replay gate. Filed COMBAT-65 (Unbreakable equip-break gate + Intravision
  see-hidden + the SC speed table).
