# SC-08 — P0.5 leftovers: SC spread trigger + flag set, nostatus map gate, companion calc, status_isimmune matrix

> **Epic:** Status parity hardening · **Status:** 🚧 In progress · **Size:** L · **Player-visible:** yes
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

- [ ] **Set `ScfFlag.SpreadEffect` on all 18 SCs** in `StatusFlagDefaults` (add Poison, Curse,
      Silence, Confusion, Blind, Hallucination, Freezing, Toxin, Paralyse, Venombleed, Magicmushroom,
      Deathhurt, Pyrexia, Oblivioncurse, Leechesend, Bodypaint — Bleeding/Burning already done).
- [ ] **Wire the Deadly Infect spread trigger** in the damage pipeline (`DamageService` / the
      post-damage SC hook): if the target (or attacker) has `SC__DEADLYINFECT` and the hit is melee
      with damage > 0, roll `30 + 10*Val1` % → call `_sc.Spread(...)` in both directions matching
      battle.cpp:1903/1940. Add the `Deadlyinfect` SC if not present and confirm its Val1.
- [ ] **Harden `IsDisabledOnMap`**: replace the `GetHashCode()` map lookup with the same map-id
      resolution `MovementService` uses (or a direct `_world.GetById(mapId)`); confirm the
      `nostatus` mapflag is loaded from the map db / script `setmapflag`. Add the gate enforcement at
      the `Start` entry (`StatusChangeService.cs:72` already calls it — verify the path is reachable
      and the mapflag is set).
- [ ] **Companion refresh**: ensure `CalcHomunculus`/`CalcMercenary`/`CalcElemental` recompute the
      companion-specific stats (homun HpFactor/SpFactor, intimacy/hunger-driven bonuses; merc/elem
      db scaling) on level/equip/SC change, not just at spawn. If the companion db loader feeds
      MobDbEntry (as the homun comment claims), add a regression test proving a level-up refreshes
      MaxHp; if not, port the factor math.
- [ ] **`status_isimmune` PC matrix**: apply the PC card-bonus resistance multipliers
      (bAddDefRate / bAddItemHealRate / bAddRaceTolerance) in the bonus/tolerance read path
      (coordinate with the equip-bonus aggregator). At minimum honor Hermode (→100% immune) and
      DeadlyDefeasance (→0% / strips immunity) in the C# `IsImmune`.
- [ ] **`status_change_refresh`** (weapon-switch SC reapply): confirm `IStatusChangeService.Refresh`
      (ST.7) End+Start cycles the weapon-element family on weapon change; add the call site if a
      weapon-swap path doesn't invoke it.

## Done criteria

- A character hitting a Deadly-Infect target in melee propagates the target's spread-flagged DoTs
  (Poison/Bleeding/Burning/etc.) to the attacker at the `30+10*Val1`% rate, and vice versa.
- `StatusFlagDefaults` flags exactly the 18 rAthena SpreadEffect SCs.
- Applying any SC on a `nostatus` map is refused (except `ScfFlag.Permanent`), via a robust map-id
  lookup (no `GetHashCode` collision risk).
- A homunculus/mercenary/elemental level-up refreshes its derived stats (MaxHp grows).
- `IsImmune` returns 100 under Hermode and 0 under DeadlyDefeasance; the PC tolerance matrix applies
  to incoming damage/heal where the card bonuses are set.
- No method in the touched set is a documented no-op for its leaf behavior.

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
