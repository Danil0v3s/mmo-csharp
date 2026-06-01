# SC-08 — P0.5 leftovers: SC spread trigger + flag set, nostatus map gate, companion calc, status_isimmune matrix

> **Epic:** Status parity hardening · **Status:** ✅ Done (2026-06-01) · **Size:** L · **Player-visible:** yes
> **Depends on:** none · **Blocks:** none

## Problem

Five `status.cpp` infrastructure functions are partially ported but have functional gaps that
the parity doc (`.agents/migrations/map/status-parity.md`) marks ✅ on the method-exists axis while
the behavior-leaf is still missing:

1. **`status_change_spread`** — the `Spread` method exists but has **no caller**, and the
   `SpreadEffect` flag is set on only 2 of the 18 SCs rAthena flags. So Deadly Infect never
   propagates DoTs.
2. **`status_change_isDisabledOnMap`** — implemented and reads `MapFlag.NoStatus`, but via a
   fragile `Name.GetHashCode()` map lookup; verify the `nostatus` mapflag table is actually
   populated and the gate fires.
3. **Companion `status_calc_*`** (homunculus / mercenary / elemental) — all three just call
   `CalcMob` + set level; the companion-specific stat refresh on level/equip/SC is partial.
4. **`status_isimmune` matrix** — only the mob-mode + Hermode/DeadlyDefeasance bits are honored;
   the PC card-bonus resistance matrix (bAddDefRate / bAddItemHealRate / bAddRaceTolerance) is not
   applied.

## Verified current state (C#)

- `Map.Server/Status/StatusChangeService.cs:324-342` — `Spread(source, target)`: iterates
  `source`'s SCs, re-applies any flagged `ScfFlag.SpreadEffect` to `target` with remaining
  duration. Correct logic, but `grep '\.Spread('` in `Map.Server/` finds **zero callers** outside
  the definition.
- `Map.Server/Status/StatusFlagDefaults.cs:49-50` — only `Bleeding` and `Burning` carry
  `ScfFlag.SpreadEffect`. rAthena flags 18 SCs (list below).
- `Map.Server/Status/StatusChangeService.cs:352-368` — `IsDisabledOnMap(mapId, type)`: bypasses
  `ScfFlag.Permanent`, looks up the map by `(uint)map.Name.GetHashCode() == mapId`, returns
  `_mapFlags.IsSet(map.Name, MapFlag.NoStatus)`. Returns false if `_mapFlags`/`_world` are null.
- `Map.Server/Status/StatusCalcService.cs:192-221` — `CalcHomunculus`/`CalcMercenary`/`CalcElemental`
  all delegate to `CalcMob` + optional level override; no companion-specific refresh.
- `Map.Server/Status/StatusOps/StatusOpsService.cs:~330` — `IsImmune`: `MobMode.StatusImmune` bit
  only; PC card-bonus matrix documented as deferred in `PARITY-REMAINING.md §P2.2`.

## rAthena reference (source of truth)

- **Spread caller:** `rathena/src/map/battle.cpp:1903` and `:1940` — both gated on
  `SC__DEADLYINFECT` (Shadow Chaser Deadly Infect): on a melee (`BF_SHORT`) hit dealing damage,
  `rnd()%100 < 30 + 10*val1` → `status_change_spread(bl, src)` (target→attacker) and
  `status_change_spread(src, bl)` (attacker→target). NOT a passive Burning/Influenza aura — spread
  is the Deadly Infect mechanic.
- **`status_change_spread`:** `status.cpp:14963`; `if (sc->getSCE(type) && flag[SCF_SPREADEFFECT])`
  → restart on the other bl. (status.cpp:14984.)
- **SpreadEffect SCs (`db/re/status.yml`, 18 total):** Poison, Curse, Silence, Confusion, Blind,
  Bleeding, Hallucination, Burning, Freezing, Toxin, Paralyse, Venombleed, Magicmushroom,
  Deathhurt, Pyrexia, Oblivioncurse, Leechesend, _Bodypaint.
- **`status_isimmune`:** `status.cpp:9065-9081` — `SC_HERMODE`→100, `SC_DEADLY_DEFEASANCE`→0, then
  PC `special_state.no_magic_damage >= gtb_sc_immunity`. The card-bonus resistance multipliers
  (bAddDefRate etc.) are applied in the bonus pipeline (`pc.cpp` / `battle.cpp` tolerance reads),
  not in `status_isimmune` itself.
- **Companion calc:** `status.cpp:2872` (homun, db-driven Hp/SpFactor), `:2887` (merc), `:2920`
  (elemental) — they recompute from the companion db on level/equip change.

## Scope — every sub-system that must be touched

- [x] **SpreadEffect on all 18 SCs** — ✅ `StatusFlagDefaults` now flags exactly the 18 (Poison,
      Curse, Silence, Confusion, Blind, Bleeding, Hallucination, Burning, Freezing, Toxin, Paralyse,
      Venombleed, Magicmushroom, Deathhurt, Pyrexia, Oblivioncurse, Leechesend, Bodypaint). Since
      most have explicit Register Flags, `GetEffectiveFlags` now OR-merges the table's `SpreadEffect`
      bit so the flag is authoritative regardless of the handler's own Flags.
- [x] **Deadly Infect spread trigger** — ✅ wired into `DamageService.ApplyScPostResolve`: on a
      damaging hit, if the attacker or target has `SC__DEADLYINFECT`, roll `30 + 10*Val1` % →
      `_sc.Spread(...)` in both directions (battle.cpp:1903/1940). (Melee-only `BF_SHORT` gating →
      the shared range-threading note, COMBAT-25.)
- [x] **`status_isimmune` Hermode/DeadlyDefeasance** — ✅ `IsImmune` returns true under Hermode,
      false under DeadlyDefeasance (strips), then the mob MD_STATUSIMMUNE bit. The PC card-bonus
      tolerance matrix (bAddDefRate/…) ➡️ **SC-21**.
- [ ] **Companion refresh** (homun/merc/elem level-up stat recompute) ➡️ **SC-22**.
- [ ] **Harden `IsDisabledOnMap`** (GetHashCode → `_world.GetById`) — verified it fires (null-guard
      intact, uses the codebase-standard map-id scheme); the robust-lookup swap ➡️ **SC-22**.
- [ ] **`status_change_refresh` weapon-swap call site** (Refresh has no caller today) ➡️ **SC-22**.

## Done criteria

- ✅ A character hitting a Deadly-Infect target in melee propagates the spread-flagged DoTs in both
  directions at the `30+10*Val1`% rate (`SC08SpreadImmuneTests.DeadlyInfect_*`).
- ✅ `StatusFlagDefaults` flags exactly the 18 rAthena SpreadEffect SCs (`The18SpreadSCs_*`).
- ✅ `IsImmune` returns true under Hermode, false under DeadlyDefeasance, honors the mob mode bit.
  *(PC tolerance matrix ➡️ SC-21.)*
- ➡️ Companion level-up refresh ➡️ **SC-22**; nostatus robust map-id lookup ➡️ **SC-22**;
  weapon-swap `Refresh` wiring ➡️ **SC-22**.

## Test plan

- `StatusSpreadTests`: source with Poison+Bleeding+Curse, both flagged; `Spread` propagates all
  three with remaining duration; non-flagged SCs (e.g. Blessing) do not spread.
- `DeadlyInfectSpreadTests`: melee hit with `SC__DEADLYINFECT` Val1=5 → ~80% spread chance both
  directions; no spread on magic/ranged.
- `NoStatusMapflagTests`: SC apply refused on a `nostatus` map; Permanent SC still applies; map-id
  resolution doesn't collide.
- `CompanionCalcTests` (extend existing): level-up refreshes homun/merc/elem MaxHp.
- `StatusImmuneTests`: Hermode→100, DeadlyDefeasance→0, StatusImmune mob mode honored.
- Regression: `StatusChangeServiceTests`, `StatusChangeRefreshTests`, `CompanionCalcTests`.

## Notes / gotchas

- Spread is a Deadly Infect mechanic, NOT a passive aura — do not make Burning/Influenza propagate
  on their own tick. The original P0.5 note ("Burning/Influenza/Misty don't propagate") is
  imprecise: they propagate only *via* Deadly Infect's `status_change_spread` call.
- `Influenza`/`MistyFrost` are NOT in the rAthena SpreadEffect list — do not add them; the 18-SC
  list above is authoritative (from `db/re/status.yml`).
- `IsDisabledOnMap` returns false when `_mapFlags`/`_world` are unset (test/headless) — keep that
  null-guard so unit tests without a world don't refuse every SC.
- The card-bonus matrix (bAddDefRate etc.) lives in the equip-bonus pipeline, not the SC engine; if
  that pipeline is out of scope, scope this ticket to the SC-engine pieces (spread + flags +
  mapflag + companion + Hermode/DeadlyDefeasance immune bits) and leave the card matrix to its own
  PARITY-REMAINING §P2.2 ticket — but say so explicitly, don't silently skip.

## History

- 2026-06-01 · Landed the SC-engine half of P0.5. Flagged all 18 rAthena SCF_SPREADEFFECT SCs in
  StatusFlagDefaults; made GetEffectiveFlags OR-merge the table's SpreadEffect bit so it's
  authoritative over explicit handler Flags. Wired the Shadow Chaser Deadly Infect spread trigger
  into DamageService.ApplyScPostResolve (roll 30+10*Val1% → Spread both directions, battle.cpp:1903).
  Added Hermode(→immune)/DeadlyDefeasance(→strips) to StatusOpsService.IsImmune. SC08SpreadImmuneTests
  (5). 3726/3726 green. Filed SC-21 (PC card-bonus tolerance matrix) + SC-22 (companion level-up
  refresh, status_change_refresh weapon-swap wiring, robust nostatus map-id lookup).
