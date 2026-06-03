# INFRA-01 — Weapon Refine skill (WS_WEAPONREFINE) rate/cost/break parity

> **Epic:** Infra parity · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** none · **Blocks:** none

## Problem

The Whitesmith / blacksmith **Weapon Refine** skill is a guaranteed +1 with no
material cost and no failure. In rAthena it rolls a success chance from the refine
catalog, consumes the per-weapon-level ore (Phracon / Emveretarcon / Oridecon), and
on failure does nothing (the player just loses the ore). A player can today refine a
weapon from +0 to +20 for free with zero risk — this both trivializes the economy
(no ore sink) and breaks parity with the refine UI / `@refine` GM cmd that *do* go
through the catalog.

The `RefineService` (which fully loads `refine_group_db` / `refine_level_db` /
`refine_chance_db` and exposes the rate + price + material per attempt) is **not
injected** into `SkillProductionService`, so the skill ignores it entirely.

## Current state (C#)

- `Map.Server/Skills/SkillProductionService.cs:237-262` — `WeaponRefine` does:
  validates slot bounds, `NameId != 0`, `row.Type is "Weapon" or "Armor"`,
  `item.Refine < MAX_REFINE` (20), not broken, then unconditionally `item.Refine++`
  and returns true. No rate roll, no ore lookup, no ore consume, no failure path.
  Docstring admits "without that catalog wired, the +1 path runs unconditionally".
- `MAX_REFINE = 20` (`SkillProductionService.cs:41`) — note rAthena caps weapon-refine
  *via this skill* at **+10** (`skill.cpp:20680` `item->refine >= 10`), and also at
  `sd.menuskill_val` (the level chosen in the refine menu). The +20 cap is the catalog
  max, not the skill max.
- `SkillProductionService` ctor (`:43-55`) injects `ISessionManagerAccessor`,
  `IItemCatalog`, `ISkillArrowDatabase`, `IProduceRecipeService`, `ILogger` — **no
  `IRefineService`**.
- `Map.Server/Inventory/RefineService.cs:47-51` — `GetRefineChance(group, itemLvl,
  refineLvl, chanceType)` returns `RefineAttempt?(Rate, Price, MaterialAegis)` where
  `Rate` is in 1/10000 units (catalog "chance" column). `GetRefineBonus(group,
  itemLvl, refineLvl)` returns the stat bonus.
- `Map.Server/Inventory/IRefineService.cs:31` — interface + `RefineAttempt(int Rate,
  int Price, string MaterialAegis)` record (`:38`).

## rAthena reference (source of truth)

Canonical source is `skill.cpp` (the split-file paths in docstrings do not exist here).

- `skill.cpp:20659` `skill_weaponrefine(map_session_data& sd, int32 idx)`:
  - Hardcoded ore table by weapon level: `{ ITEMID_PHRACON, ITEMID_EMVERETARCON,
    ITEMID_ORIDECON, ITEMID_ORIDECON, (RENEWAL: 0) }` (`:20660-20668`).
  - Refuses if `ditem->type != IT_WEAPON`, `no_refine`, or `weapon_level < 1`.
  - Refuses if `item->refine >= sd.menuskill_val || item->refine >= 10`
    (`:20680`) → emits `clif_upgrademessage(2)` ("already refined enough").
  - `pc_search_inventory(material[weapon_level-1])`; if missing → `clif_upgrademessage(3)`.
  - `info = refine_db.findLevelInfo(*ditem, *item)`; `cost = info->costs[REFINE_COST_NORMAL]`.
  - **Success chance:** `per = cost->chance / 100`; then if `class_ & JOBL_THIRD`
    `per += 10`, else `per += (job_level - 50) / 2` (`:20712-20716`).
  - `pc_delitem(material, 1)` consumes **one** ore **before** the roll (`:20718`).
  - `if (per > rnd() % 100)` → success: `item->refine++`, re-equip if it was equipped,
    `clif_refine(ITEMREFINING_SUCCESS)`, `NOTIFYEFFECT_REFINE_SUCCESS`, fame point on
    +10 forged weapon (`:20719-20748`).
  - **Failure path** (not in this excerpt, continues ~`:20760`): `clif_refine(
    ITEMREFINING_FAILURE)`, the weapon is **destroyed** (`pc_delitem` of the weapon
    itself) — rAthena weapon refine *breaks the weapon on failure* (unlike the
    NPC refine UI which can downgrade). Confirm against the tail of the function.

Note `cost->chance` is the same column `RefineService` surfaces as `RefineAttempt.Rate`
(units: chance out of 10000 for the DB, but the skill divides by 100 → percent). Verify
the unit: `RefineService` stores the raw `Rate`; the skill uses `Rate/100` as a percent.
If the seeded `chance` column is already 0..100 the `/100` is wrong — pin this in the
test against a known +4→+5 weapon row.

## Scope — every sub-system that must be touched

- [ ] **`SkillProductionService` ctor**: inject `IRefineService _refine`. Update DI
      registration in `Map.Server/Program.cs` (constructor change is auto-resolved if
      `IRefineService` is already registered — verify; `RefineService` is a singleton).
- [ ] **`WeaponRefine` body** (`:237-262`):
  - [ ] Resolve the item's refine **group** + **item level** from the catalog row
        (`IItemCatalog`). Map weapon level → group name (Weapon1..Weapon4) and item
        level the same way `RefineService` keys its rows. Confirm the group-name mapping
        the seed used (see `IRefineService` docstring: Armor / Weapon1..4 / Shadow_*).
  - [ ] Cap at `min(MAX_REFINE_VIA_SKILL=10, menuskill_val)` instead of the bare 20.
        (Plumb the menu level through if the handler passes it; if not, gate at 10.)
  - [ ] Resolve the per-weapon-level ore item id (Phracon=1010, Emveretarcon=1011,
        Oridecon=984, Oridecon for lvl4). Refuse if the caster lacks one.
  - [ ] `var attempt = _refine.GetRefineChance(group, itemLvl, item.Refine, "Normal")`.
        Refuse cleanly if null.
  - [ ] Compute `per = attempt.Rate / 100` (verify unit) + job bonus (third job +10,
        else `(jobLevel-50)/2`). Plumb job class + job level from the session/PlayerEntity.
  - [ ] Consume **one** ore (decrement / remove inventory row, push `RemovedInventoryIds`).
  - [ ] Roll `_rng.Next(100) < per` (or `Next(10000)` against the raw rate — match the
        unit chosen). On success `item.Refine++`. On failure: **destroy the weapon**
        (remove the inventory row, push removed id) to match rAthena, OR if the C# refine
        UI elsewhere uses downgrade/keep semantics, match *that* and document the divergence.
  - [ ] Return true on a completed attempt (success *or* handled failure); false only on
        validation refusal — match what the handler/observer expects for the success FX.
- [ ] **Inject a `Random`** (or reuse the pattern in `SkillSideEffectService` which takes
      `Random? rng = null`) so the roll is test-seedable.
- [ ] **Client FX**: if the production handler emits `ZC_ACK_WEAPONREFINE` /
      refine-success/fail packets, ensure the success vs failure branch drives the right
      ack. (Check the calling handler — `WeaponRefine` may be invoked from a menu handler
      that already owns the packet emit.)

No new EF entity — the refine catalog tables already exist and are loaded by
`RefineService`.

## Done criteria

- A +0 weapon refine attempt with no ore in the bag returns false and mutates nothing.
- With ore present: the success rate equals `(catalog rate for this group/itemLvl/level)
  + job bonus`, the ore count drops by exactly 1 per attempt regardless of outcome, and
  a forced-fail roll either destroys the weapon or applies the documented failure
  semantic — never silently +1.
- Refine is capped at +10 via this skill (not +20).
- No `// without that catalog wired` comment and no unconditional `item.Refine++` remain
  in `WeaponRefine`.

## Test plan

- `Map.Server.Tests/Skills/...` — add `WeaponRefineParityTests`:
  - Seeded RNG forced below threshold → success, refine +1, ore -1.
  - Seeded RNG forced above threshold → failure path (weapon gone / downgraded per
    chosen semantic), ore -1.
  - No ore → refuse, no mutation.
  - Refine already +10 → refuse.
  - Rate math: pin one known catalog row (e.g. Weapon3 +4→+5) and assert the computed
    percent matches `rate/100 + jobBonus` for a 3rd-job vs a 2nd-job (job_level 50) caster.
- Use an in-memory `RefineService` test ctor seeded with a couple of `RefineAttempt`
  rows, or mock `IRefineService`.

## Notes / gotchas

- **Rate unit ambiguity** is the #1 trap: `RefineAttempt.Rate` is the raw catalog
  `chance` column; rAthena does `cost->chance / 100`. The seed (`DB-8h`) decides whether
  `chance` is 0..10000 or 0..100. Open the seeded values before picking `/100` vs raw.
- Job bonus needs **job class** (third-job flag) and **job level**, which live on
  `MapSessionData` / `CharEntity`, not `PlayerEntity` — plumb them through (same gap
  INFRA-09 hits for `getParam(Class)`).
- The NPC refine UI and `@refine` may already consume the catalog differently; keep the
  skill path consistent with whatever the UI handler does for the failure semantic, and
  note the divergence from rAthena if the C# UI never destroys.
- Phracon/Emveretarcon/Oridecon item ids: confirm against the seeded item catalog
  (rAthena ids 1010/1011/984) rather than hardcoding blind.
