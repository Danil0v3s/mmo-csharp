# SKILL-15 — ScDefTable depth: bespoke-formula SCs + min_rate/min_duration + resist-buff adds

> **Epic:** Skills · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** SKILL-01 (ScDefTable + GetScDef) · **Blocks:** none

## Problem

SKILL-01's `ScDefTable` covers the standard renewal CC set (Poison/Stun/Silence/Bleeding/
Sleep/StoneWait/Freeze/Curse/Blind/Confusion) plus the two composite forms (Fear/Burning).
The renewal `status_get_sc_def` switch has more arms with **bespoke formulas**, and `GetScDef`
currently omits the per-SC `min_rate`/`min_duration` floors and the resist-buff/item additions.
So some debuffs resist with the wrong curve (or not at all) and the rate/duration floors aren't
honored. ➡️ Inherited from SKILL-01.

## Current state (C#)

- `Map.Server/Status/ScDefTable.cs` — 12 entries (standard + Fear/Burning). Missing arms:
  JointBeat, DeepSleep, Netherworld, MarshOfAbyss, Stasis, WhiteImprison, Freezing,
  OblivionCurse, the GC poison family (Toxin/Paralyse/VenomBleed/MagicMushroom/DeathHurt/
  Pyrexia/LeechEsend), Bite, ElectricShocker, Crystalize, VacuumExtreme, Kyougaku, Paralysis,
  VoiceOfSiren, B_Trap, NoRecoverState, DecreaseAgi (sc_def2 form + player tick-halving).
- `Map.Server/Status/StatusChangeService.cs` `GetScDef` — uses a flat `min_rate`-0 / `min_duration`-1
  floor (comment marks it SKILL-15), and skips `SC_SCRESIST` / `SC_SIEGFRIED` resist-buff adds
  and the player `reseff` item-resistance loop (rate + renewal duration).

## rAthena reference (source of truth)

- `status.cpp:9392-9594` — the full `status_get_sc_def` switch (renewal `#ifdef RENEWAL`
  branches). Bespoke arms set custom `sc_def`/`sc_def2`/`tick_def`/`tick_def2`.
- `status.cpp:9630-9652` — `SC_SCRESIST` (`+val1*100`), `SC_SIEGFRIED` (`+val3*100` for the
  listed elements), `SC_LEECHESEND`/`SC_OBLIVIONCURSE` immunity short-circuits.
- `status.cpp:9665-9698` — `sd->reseff` item resistance (rate; and renewal duration).
- `status.cpp:9679` `rate = max(rate, scdb->min_rate)` + `:9702 i64max(tick, scdb->min_duration)`
  from `db/re/status.yml` (per-SC `MinRate` / `Duration` floors).

## Scope — every sub-system that must be touched

- [ ] Extend `ScDefEntry` to express the bespoke arms (custom sc_def2 / tick_def / per-stat
      composite, the `DecreaseAgi` `sc_def2 = mdef*100` + player tick/2 special). Port the
      remaining renewal arms for SCs whose `StatusType` exists.
- [ ] Add per-SC `min_rate` + `min_duration` (from status.yml; surface via the existing
      status-db flag cache or a small table) and apply them in `GetScDef`.
- [ ] Add the `SC_SCRESIST` / `SC_SIEGFRIED` resist-buff adds (read the target's active SCs)
      and the `reseff` item-resistance loop (rate + renewal duration).

## Done criteria

- A representative bespoke SC (e.g. DeepSleep INT/level tick reduction, DecreaseAgi MDEF rate
  reduction + player half-duration) matches a hand-computed rAthena value.
- `min_rate` floor: a heavily-resisted CC still lands at its status.yml minimum, not 0.
- `SC_SCRESIST`/`Siegfried` on the target measurably raise resistance.

## Test plan

- Unit-test 3–4 newly-ported bespoke arms against hand-computed rAthena values.
- Unit-test the min_rate floor and the SCRESIST/Siegfried add.

## Notes / gotchas

- Several listed SCs may not have `StatusType` members yet — cover those that exist; add the
  rest when their SC ports (note, don't stub).
- Keep the renewal sign convention for `tick_def2` (subtracted; negative lengthens) consistent
  with SKILL-01.
