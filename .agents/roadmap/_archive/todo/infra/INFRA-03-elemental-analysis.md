# INFRA-03 — Sorcerer Elemental Analysis (SO_EL_ANALYSIS) item conversion

> **Epic:** Infra parity · **Status:** ❌ Not started · **Size:** S · **Player-visible:** yes
> **Depends on:** none · **Blocks:** none

## Problem

The Sorcerer **Elemental Analysis** skill converts elemental-point items between their
"refined" form (Flame Heart / Mystic Frozen / Rough Wind / Great Nature) and their "raw"
form (Bloody Red / Crystal Blue / Wind of Verdure / Yellow Live) — level 1 splits one
refined into ~5–9 raw, level 2 combines 10 raw into 1 refined (with a fail chance). Today
the C# method returns `false` and converts nothing, so a Sorcerer cannot produce the
elementals needed to summon/feed elementals or craft.

This is **not** a DB-table skill in rAthena — the id pairs are hardcoded in a `switch`.

## Current state (C#)

- `Map.Server/Skills/SkillProductionService.cs:282-291` — `ElementalAnalysis(PlayerEntity
  caster, int sourceItemId)`: logs "no recipe loaded" and `return false`. The docstring
  wrongly says it needs an `elemental_analysis_db` table; rAthena has no such table.
  The signature also lacks `skill_lv` (which selects split vs combine) and the per-item
  amount, both of which the real skill needs.

## rAthena reference (source of truth)

Canonical source is `skill.cpp` (monolithic; the split-file paths in docstrings do not
exist here).

- `skill.cpp:23831` `skill_elementalanalysis(map_session_data& sd, int32 n, uint16
  skill_lv, uint16* item_list)` — `item_list` is `n` pairs of `(inventory_index+2,
  amount)`:
  - `idx = item_list[i*2+0] - 2`; `del_amount = item_list[i*2+1]`.
  - **Level 2 (combine):** `del_amount -= del_amount % 10` (round down to a multiple of
    10); `add_amount = del_amount / 10`.
  - **Level 1 (split):** `add_amount = del_amount * (5 + rnd()%5)` (×5..×9 per source).
  - Refuse if the slot is empty or `del_amount > stack amount` → `clif_skill_fail`.
  - **Hardcoded id pairs (`:23859-23873`):**
    | source (nameid) | product |
    |---|---|
    | Level 1 (split): | |
    | `ITEMID_FLAME_HEART` | `ITEMID_BLOODY_RED` |
    | `ITEMID_MISTIC_FROZEN` | `ITEMID_CRYSTAL_BLUE` |
    | `ITEMID_ROUGH_WIND` | `ITEMID_WIND_OF_VERDURE` |
    | `ITEMID_GREAT_NATURE` | `ITEMID_YELLOW_LIVE` |
    | Level 2 (combine): | |
    | `ITEMID_BLOODY_RED` | `ITEMID_FLAME_HEART` |
    | `ITEMID_CRYSTAL_BLUE` | `ITEMID_MISTIC_FROZEN` |
    | `ITEMID_WIND_OF_VERDURE` | `ITEMID_ROUGH_WIND` |
    | `ITEMID_YELLOW_LIVE` | `ITEMID_GREAT_NATURE` |
    | any other | fail |
  - `pc_delitem(idx, del_amount)` — consume the source first (`:23875`).
  - **Level 2 fail chance:** `if (skill_lv == 2 && rnd()%100 < 25)` → fail, **items are
    lost** (already deleted) (`:23880-23883`).
  - Add `add_amount` of the product (identified). If the bag is full, drop on the floor
    when `battle_config.skill_drop_items_full` (`:23890-23897`).
  - Loops over all `n` submitted entries; returns 0 on success, 1 on any failure.

Same id pairs forward (combine) and reverse (split) — the switch is symmetric, keyed by
source id, with `skill_lv` deciding the amount math and the fail roll.

## Scope — every sub-system that must be touched

- [ ] **Resolve the 8 item ids.** Add named constants (or pull from the item catalog by
      Aegis name) for FLAME_HEART, MISTIC_FROZEN, ROUGH_WIND, GREAT_NATURE, BLOODY_RED,
      CRYSTAL_BLUE, WIND_OF_VERDURE, YELLOW_LIVE. Confirm the numeric ids against the
      seeded catalog (rAthena ids: Flame Heart 994, Mystic Frozen 995, Rough Wind 996,
      Great Nature 997, Bloody Red 990, Crystal Blue 991, Wind of Verdure 992, Yellow
      Live 993 — **verify**). Build a static `(source → product)` map; the split/combine
      direction is implied by which set the source belongs to.
- [ ] **Change the signature** to `ElementalAnalysis(PlayerEntity caster, ushort
      skillLevel, IReadOnlyList<(int inventoryIndex, int amount)> items)` to match the
      real skill (or accept a single (index, amount, lvl) if the handler only submits one
      at a time — match the calling handler's shape).
- [ ] **`ElementalAnalysis` body:**
  - [ ] For each entry: bounds-check, verify `amount <= stack`.
  - [ ] Compute `delAmount` / `addAmount` per the level math above (lvl2 rounds to
        multiple of 10; lvl1 uses `5 + rng%5`).
  - [ ] Look up the product via the source→product map; refuse (no mutation for this
        entry) if the source is not an elemental-point item.
  - [ ] Consume `delAmount` of the source (decrement / remove row, push
        `RemovedInventoryIds`).
  - [ ] Lvl2: roll `rng%100 < 25` → fail (items already consumed, no product added).
  - [ ] Add `addAmount` of the product (identified, refine 0, no cards) to the bag.
- [ ] **Inject a `Random`** for seedable rolls (lvl1 yield + lvl2 fail).
- [ ] **Caller / handler** wiring: ensure the SO_EL_ANALYSIS skill behavior passes the
      submitted item list + skill level into this method.

No EF / no DB — purely hardcoded id arithmetic.

## Done criteria

- Lvl1 on 1× Flame Heart yields 5–9 Bloody Red and consumes the Flame Heart.
- Lvl2 on 10× Bloody Red yields 1 Flame Heart (when the fail roll misses) and consumes 10
  (rounded down from any multiple-of-10 submission); a forced fail consumes the input and
  yields nothing.
- Lvl2 on 25× Bloody Red consumes 20 (rounds to multiple of 10) and yields 2.
- Non-elemental source → no mutation for that entry.
- No `return false` stub and no "no recipe loaded" / `elemental_analysis_db` comment remain.

## Test plan

- `Map.Server.Tests/Skills/ElementalAnalysisParityTests`:
  - Lvl1 split with seeded RNG (e.g. forced `rng%5 == 2` → ×7): assert product count and
    source consumed.
  - Lvl2 combine 10→1 success (forced fail-roll miss) and forced fail (input lost, no
    product).
  - Lvl2 rounding: 25 in → 20 consumed, 2 out.
  - Unknown source id → no mutation.
  - Each of the 8 source ids maps to the correct product, both directions.

## Notes / gotchas

- **Item ids must be verified against the seeded catalog**, not copied from an assumed
  rAthena constant table — id drift between rAthena versions is common for these.
- Lvl1 yield is random (`5 + rnd()%5`), so the test must inject a seeded `Random`.
- Lvl2 silently destroys input on the 25% fail — that is intended parity, not a bug;
  document it in the method so a future reader doesn't "fix" it.
- The skill can process **multiple** stacks in one cast (the `n`-entry loop); don't
  collapse to a single conversion if the handler submits a list.
