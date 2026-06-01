# SC-16 — Sorcerer *_OPTION secondary effects: element change + bolt-autocast + Wind/Petrology modifiers

> **Epic:** Status parity hardening · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** SC-05 (fixed Val2 stat effects) · SC-02 (weapon-element precedence) · **Split from:** SC-05

## Problem

SC-05 wired the **fixed Eatk/Matk/HP-rate** stat effects of the Sorcerer elemental-spirit
`*_OPTION` buffs (equip-Atk, MATK, MaxHP% — no more phantom +Val1). The **secondary** effects
of those same options are deferred here:

1. **Weapon element change** — `HeaterOption`→Fire, `CoolerOption`→Water, `BlastOption`→Wind,
   `CursedSoilOption`→Earth (Val3 = `ELE_*`). Must set the bearer's weapon element with the
   correct precedence vs the SC-02 endow family (Fireweapon/etc.).
2. **Bolt autocast-on-attack** — `TropicOption` Val3 = `MG_FIREBOLT`, `ChillyAirOption` Val3 =
   `MG_COLDBOLT`, `WildStormOption` Val2 = `MG_LIGHTNINGBOLT` (already stored by SC-05). Autocast
   the bolt on melee hit (needs the autocast pipeline).
3. **`WindStepOption` Val2=50** — % movement-speed + flee bonus (consumer paths).
4. **`WindCurtainOption` Val2=100** — elemental-damage modifier % (combat-side read).
5. **`PetrologyOption` Val3=50** — DEF term (SC-05 applied only the 5% MaxHP).

## Current state (C#)

- `Map.Server/Status/StatusEffectRegistry.cs` — after SC-05 the `*_OPTION` bodies set the
  correct fixed Val2 and apply the equip-Atk/MATK/HP-rate stat; the element/autocast/Wind/Petrology-def
  effects are NOT applied (comments mark each `→ SC-16`). `WildStormOption`/`WindStepOption`/
  `WindCurtainOption` are presence-only with the correct Val2 stored.
- `Map.Server/Status/StatusEffectRegistry.cs` — the SC-02 endow family
  (`EndowHandler`) sets `Stats.WeaponElement` (store prev+1 in a Val scratch) — reuse this pattern.

## rAthena reference (source of truth)

- Element change: `status.cpp:8630` `status_get_weapon_element` reads the option's element id.
- Bolt autocast: the `*_OPTION` on-attack autospell arm (autospell table keyed by the option SC).
- WindStep / WindCurtain / Petrology-def: the respective `status_calc_*` / `battle_calc_*` reads.

## Scope — every sub-system that must be touched

- [ ] Element-change for Heater/Cooler/Blast/CursedSoil: set `Stats.WeaponElement` to the option's
      `ELE_*` on start (store prev for restore), coordinating precedence with the SC-02 endow SCs.
- [ ] Bolt autocast for Tropic/ChillyAir/WildStorm: on melee hit, autocast the stored bolt skill id
      (reuse the autobonus/autospell path).
- [ ] WindStep Val2=50: apply the % movement-speed + flee bonus to the right derived fields.
- [ ] WindCurtain Val2=100: wire the elemental-damage modifier % into the combat damage path.
- [ ] Petrology Val3=50: apply the DEF term (in addition to SC-05's 5% MaxHP).

## Done criteria

- Heater/Cooler/Blast/CursedSoil change the bearer's weapon element (right precedence vs endows).
- Tropic/ChillyAir/WildStorm autocast their bolt on attack.
- WindStep speed/flee, WindCurtain elemental modifier, and Petrology DEF apply per rAthena.

## Test plan

- `SorcererOptionElementTests`: each element-change option sets WeaponElement (shared with SC-02
  `WeaponEndowElementTests`).
- Autocast / WindStep / WindCurtain / Petrology-def unit tests per the above.

## Notes / gotchas

- Reuse the SC-02 `EndowHandler` weapon-element scratch pattern; do not invent a new one.
- The element-change options ALSO carry the SC-05 stat effect (Heater = 120 equip-Atk + Fire) —
  add the element on top of the existing stat body, using a free Val slot for the prev-element
  scratch (HP-rate options already use Val4 for the HP delta, so pick carefully).
