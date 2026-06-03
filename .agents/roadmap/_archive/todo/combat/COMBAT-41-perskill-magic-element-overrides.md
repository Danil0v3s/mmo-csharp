# COMBAT-41 — Bespoke per-skill magic/misc element overrides

> **Epic:** Combat parity · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** COMBAT-19 (base element resolver), SC-16 (sorcerer option element)
> **Blocks:** none
> **Filed by:** COMBAT-19 — the per-skill switch in battle_get_magic_element it did not port.

## Problem

COMBAT-19 ported the base `skill_get_ele` chain (declared element + ELE_WEAPON /
ELE_ENDOWED / ELE_RANDOM) into `BattleElementService`. rAthena's
`battle_get_magic_element` (battle.cpp:3597-3670) then applies a per-skill `switch`
that overrides the element for specific skills based on the caster's status changes,
ammo, spirit charms, or miscflag. Those overrides are not yet implemented, so e.g.
Psychic Wave does not pick up the sorcerer option element, Adoramus under Ancilla is
not forced Neutral, and Hell Inferno's dark second hit is not applied.

## Current state (C#)

- `Map.Server/Combat/BattleElementService.cs:GetMagicElement` / `GetMiscElement` —
  resolve the declared element + the three sentinels only; no per-skill switch.
- `ELE_ENDOWED` is resolved to the caster's `Stats.WeaponElement` (correct while every
  endow updates that field); rAthena reads `status_get_attack_sc_element` directly.

## rAthena reference (source of truth)

- `battle.cpp:3597-3670` `battle_get_magic_element` switch: NPC_EARTHQUAKE → Neutral,
  WL_HELLINFERNO (mflag&2 → Dark), NPC/SO_PSYCHIC_WAVE (option-SC val3),
  KO_KAIHOU (spiritcharm), AB_ADORAMUS (SC_ANCILLA → Neutral),
  LG_RAYOFGENESIS (SC_INSPIRATION → Neutral), WM_REVERBERATION / TR_* (arrow element),
  SU_CN_METEOR / SH_HYUN_ROK* (SC_COLORS_OF_HYUN_ROK_* → element),
  SS_ANKOKURYUUAKUMU (SKILL_ALTDMG_FLAG → Fire).
- `status_get_attack_sc_element` — the precise ELE_ENDOWED source.

## Scope — every sub-system that must be touched

- [ ] Add a per-skill override pass after the base resolution in `GetMagicElement`
      (gate each arm on the cited SC / ammo / spiritcharm / miscflag). Thread the
      `miscflag` into the resolver where a skill needs it (HellInferno, Ankokuryuu).
- [ ] Implement `ELE_ENDOWED` via the actual endow-SC element accessor when it diverges
      from `Stats.WeaponElement` (coordinate with SC-11 endow completion).
- [ ] Wire the arrow-element (`bonus.arrow_ele`) source for the song skills.

## Done criteria

- Psychic Wave under a sorcerer option SC resolves that option's element.
- Adoramus under SC_ANCILLA resolves Neutral; Ray of Genesis under SC_INSPIRATION Neutral.
- Hell Inferno's flagged second hit resolves Dark.

## Test plan

- `Combat41MagicElementOverrideTests`: each ported arm → expected element given the SC /
  flag; default (no SC) falls through to the base resolution.

## Notes / gotchas

- Several arms overlap status-change tickets (SC-16 sets the sorcerer option Val3 element);
  this ticket only consumes those values for element resolution.
