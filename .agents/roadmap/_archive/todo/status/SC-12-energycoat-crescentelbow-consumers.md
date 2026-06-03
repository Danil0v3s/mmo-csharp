# SC-12 — Energycoat SP-tier reduction + Crescentelbow reflect (combat consumers)

> **Epic:** Status parity hardening · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** none · **Blocks:** none · **Split from:** SC-04

## Problem

Two `DamageService`-side consumers from SC-04's starved set need more than a Val read:

1. **Energycoat** (`SC_ENERGYCOAT`) — physical damage reduction scaled by the bearer's
   remaining SP, charging SP per hit. Currently a bare `PresenceMarker` with no damage read.
   The renewal reduction is NOT in `battle.cpp` where the SC-04 audit looked (only the
   `GN_HELLS_PLANT_ATK` interaction at battle.cpp:1825 references it) — locate the real
   renewal formula (likely `status_calc_def`/`def2` or a `pc.cpp`/`status.cpp` SP-tier table)
   before implementing, since the classic `6*(1+per/20)` form is pre-renewal.
2. **Crescentelbow** (`SC_CRESCENTELBOW`, SR) — reflects a HP/job-level-scaled % of melee
   damage AND knocks the attacker back AND autocasts `SR_CRESCENTELBOW_AUTOSPELL`. More than a
   reflect: needs the caster-HP term, `skill_blown`, and a skill autocast.

## Current state (C#)

- `Map.Server/Status/StatusEffectRegistry.cs` — Energycoat is a `PresenceMarker` (~5227);
  Crescentelbow OnStart sets `Val2 = 50 + 5*Val1` (~1478) with no `DamageService` reader.
- `Map.Server/Combat/DamageService.cs` `ApplyScDamageReduction` (reduction) / `ApplyScPostResolve`
  (reflect) — the homes for these; SC-04 added Kaupe/Kaahi here.

## rAthena reference (source of truth)

- Crescentelbow: `battle.cpp:7318` — `if (tsc->getSCE(SC_CRESCENTELBOW) && wd->flag&BF_SHORT &&
  rnd()%100 < val2)`: `ratio = (hp_src/100)*val1*lv_target/125; rdamage = rdamage*ratio/100 +
  wd->damage*(10 + val1*20/10)/10`; `skill_blown(...)`; autocast `SR_CRESCENTELBOW_AUTOSPELL`;
  then `status_change_end(target, SC_CRESCENTELBOW)`.
- Energycoat: renewal consumer TBD — confirm against `status.cpp`/`battle.cpp` (the SC-04 grep
  found only the Hell's Plant arm). Pre-renewal: `battle_calc_damage` `damage * (100-reduce)/100`,
  reduce by SP tier, SP charged per hit.

## Scope — every sub-system that must be touched

- [ ] Locate + implement the renewal Energycoat physical-damage reduction in `ApplyScDamageReduction`
      (SP-tier %), charging SP per hit; replace the bare PresenceMarker with the consumer.
- [ ] Implement Crescentelbow in `ApplyScPostResolve` (melee-only, roll Val2%): reflect the
      HP/job-level-scaled damage back to the attacker, knockback, autocast the follow-up skill,
      end the SC. Thread caster job level (store at apply if combat lacks the caster ref).

## Done criteria

- Energycoat reduces physical damage by the correct SP-tier % and decrements SP per hit.
- Crescentelbow reflects the rAthena-formula damage on a melee hit and ends after one trigger.

## Test plan

- `EnergycoatTests`: reduction scales with SP tier; SP decremented per hit.
- `CrescentelbowTests`: melee hit reflects the formula damage and ends the SC; ranged does not trigger.

## Notes / gotchas

- Confirm the renewal Energycoat formula before coding — do not port the pre-renewal one blindly.
- Crescentelbow's autocast is a skill cast; reuse the autospell path the autobonus system uses.
