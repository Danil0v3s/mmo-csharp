# INFRA-02 — Geneticist Change Material (GN_CHANGEMATERIAL) recipe + rate/qty DB

> **Epic:** Infra parity · **Status:** ❌ Not started · **Size:** L · **Player-visible:** yes
> **Depends on:** none · **Blocks:** none

## Problem

The Geneticist **Change Material** skill (GN_CHANGEMATERIAL) — which converts stacks of
mob-loot "elemental" / crafting materials into usable refined materials — does nothing.
`ChangeMaterial` returns `false` unconditionally with a debug log. The source comment
wrongly claims it is `produce_db`-driven like ProduceMix; it is **partly** produce_db
(the recipe selection) but the success rate and the *variable output quantity* come from
a **separate** `skill_changematerial_db[]` table that the C# port has no model for.

## Current state (C#)

- `Map.Server/Skills/SkillProductionService.cs:205-212` — `ChangeMaterial(PlayerEntity
  caster, int sourceItemId)`: logs "no produce_db recipe loaded" and `return false`.
  The signature (single `sourceItemId`) is also wrong for the real skill, which takes a
  **list** of (inventory index, amount) submitted materials.
- `Map.Server/Skills/IProduceRecipeService.cs` + `ProduceRecipeService.cs` — the
  produce-recipe loader (recipe id → produce item + materials, indexed by id and by
  require-skill). This is the parallel to mirror for the change-material rate/qty table.
- `Map.Server/Skills/Behaviors/Merchant/ChangeMaterial.cs` — the skill behavior wrapper
  (has baselines under `Map.Server.Tests/Skills/Baselines/Merchant/ChangeMaterial_{1,5}.json`).
- `Core.Database/Entities/ProduceRecipeDbEntity.cs` + migration
  `20260524193920_DB8jProduceRecipe.cs` + seed `Seeds/Scripts/seed_produce_recipe_db.sql`
  — the existing produce catalog pattern to copy (entity + child material table + repo +
  migration + seed + loader service).

## rAthena reference (source of truth)

Canonical source is `skill.cpp` (monolithic; the rAthena split-file paths in C# docstrings
do not exist here). GN_CHANGEMATERIAL spans **three** distinct pieces:

1. **Recipe selection — `skill_changematerial` (`skill.cpp:23904-23955`):**
   - Walks `skill_produce_db[]` looking for rows where `itemlv == 26` (the magic
     "change-material recipe" item level) and `nameid > 0` (`:23913`).
   - For each candidate recipe, counts how many full **sets** (`p`) the submitted item
     list satisfies: each required material `mat_id[j]` must appear in the submitted list
     with `amount` an exact multiple of `mat_amount[j]` (`:23933-23934` — "must be in
     exact amount"). Refuses unidentified materials (`:23929-23932`).
   - When a recipe matches (`p > 0`), calls
     `skill_produce_mix(sd, GN_CHANGEMATERIAL, produce_db[i].nameid, 0,0,0, p, i)` and
     returns 1 (`:23945`). `p` = number of result sets to make.
   - No match → `clif_msg_skill(GN_CHANGEMATERIAL, MSI_SKILL_RECIPE_NOTEXIST)`.

2. **Success rate — inside `skill_produce_mix` `case GN_CHANGEMATERIAL` (`skill.cpp:23127-23134`):**
   - `make_per = skill_changematerial_db[i].rate * 10` where `i` is the index whose
     `nameid` equals the produced item id. So rate comes from the **separate**
     `skill_changematerial_db`, *not* produce_db.

3. **Variable output quantity — `skill.cpp:23422-23439`:**
   - On success, for the matching `skill_changematerial_db[i]`, iterate its
     `MAX_SKILL_CHANGEMATERIAL_SET` `(qty[j], qty_rate[j])` pairs.
   - For each pair: `if (rnd()%1000 < qty_rate[j])` then add `total_qty = qty * qty[j]`
     of the product (`qty` = the `p` sets from step 1). Multiple pairs can fire → the
     player can get several different yield tiers in one cast.

**Struct** (`skill.cpp:76-83`):
```c
struct s_skill_changematerial_db {
    t_itemid nameid;                              // produced item id (join key)
    uint16 rate;                                  // success rate (×10 → per-1000)
    uint16 qty[MAX_SKILL_CHANGEMATERIAL_SET];     // per-set output multiplier
    uint16 qty_rate[MAX_SKILL_CHANGEMATERIAL_SET];// per-set roll chance (/1000)
};
```
Loaded from `skill_changematerial_db.txt`, 5 base cols + `2*MAX_SKILL_CHANGEMATERIAL_SET`
qty/qty_rate cols (`skill.cpp:26367` `skill_parse_row_changematerialdb`).

## Scope — every sub-system that must be touched

- [ ] **EF entity** `Core.Database/Entities/ChangeMaterialDbEntity.cs`:
      `Id` (PK), `ProduceNameId` (join key, the produced item id), `Rate` (uint16).
- [ ] **Child entity** `ChangeMaterialQtyEntity.cs`: `Id`, `ChangeMaterialId` (FK),
      `SlotIndex`, `Qty`, `QtyRate` — one row per `(qty[j], qty_rate[j])` pair.
- [ ] **Configurations** under `Core.Database/Configurations/` for both (table names
      `change_material_db` / `change_material_qty_db`, FK + index on `ProduceNameId`).
- [ ] **Repository** `Core.Database/Repositories/Api/IChangeMaterialDbRepository.cs` +
      `Impl/ChangeMaterialDbRepository.cs`: `GetAllAsync()`, `GetAllQtysAsync()` (mirror
      `IProduceRecipeDbRepository`). Register in DI.
- [ ] **Migration**: `dotnet ef migrations add DB-ChangeMaterialDb` from `Core.Database`.
- [ ] **Seed**: `Core.Database/Seeds/Scripts/seed_change_material_db.sql` generated via
      `Tools.RathenaImporter` from `rathena/db/.../skill_changematerial_db.txt` (or the
      YAML equivalent if the checkout uses YAML). Wire into `DatabaseSeeder`.
- [ ] **Loader service** `Map.Server/Skills/ChangeMaterialDbService.cs` +
      `IChangeMaterialDbService` (parallel to `ProduceRecipeService`): cache by
      `ProduceNameId` → `(Rate, IReadOnlyList<(Qty,QtyRate)>)`. `Reload()` from repo.
- [ ] **Mark produce recipes** as change-material recipes: the produce recipe entity
      already has `ItemLv`; ensure `ItemLv == 26` rows are seeded and selectable.
      `IProduceRecipeService` needs a way to enumerate all recipes with `ItemLv == 26`
      (add `GetByItemLevel(byte)` or filter in the change-material path).
- [ ] **`ChangeMaterial` method** — change signature to take the submitted list
      `IReadOnlyList<(int inventoryIndex, int amount)>` (matching the real skill), and:
  - [ ] Reject any submitted material that is unidentified.
  - [ ] For each `itemLv==26` produce recipe, compute the max full sets `p` the
        submission satisfies (exact-multiple rule per material).
  - [ ] When `p > 0`: consume `p * mat_amount[j]` of each material; look up
        `ChangeMaterialDbService[produceNameId]`; for each `(qty, qtyRate)` pair roll
        `rnd%1000 < qtyRate` and on hit add `p * qty` of the product. Return true.
  - [ ] No match → return false (caller emits `MSI_SKILL_RECIPE_NOTEXIST`).
- [ ] **Inject** `IChangeMaterialDbService` into `SkillProductionService` ctor + DI.
- [ ] **Update** `Behaviors/Merchant/ChangeMaterial.cs` if it calls the old single-arg
      signature; refresh the two baseline JSONs to reflect real consume/produce.

## Done criteria

- Submitting the exact materials for a seeded change-material recipe consumes them in
  exact multiples and yields the product(s) with the per-set probability table applied.
- The success rate equals `changematerial_db.rate * 10` (per-1000), distinct from the
  produce_db recipe.
- Unidentified materials, wrong amounts (non-multiples), or no matching recipe all refuse
  without consuming.
- No `return false` stub and no "no produce_db recipe loaded" comment remain.

## Test plan

- `Map.Server.Tests/Skills/ChangeMaterialDbServiceTests` — loader hydrates entity +
  qty children, indexes by produce id.
- `Map.Server.Tests/Skills/ChangeMaterialParityTests` — with a seeded recipe + qty table
  and forced RNG: exact-multiple submission yields expected product counts; partial /
  non-multiple submission refuses; unidentified material refuses.
- Refresh `Baselines/Merchant/ChangeMaterial_{1,5}.json` and re-run the behavior baseline
  test so they capture real consume/produce instead of the no-op.

## Notes / gotchas

- **Two tables, one skill.** The recipe (which materials → which product) is `produce_db`
  rows with `itemlv == 26`; the rate + yield distribution is `change_material_db`. They
  join on the produced item id. Do not collapse them.
- `qty_rate` is out of **1000**, `rate` is `×10` → effectively out of 1000 too. Multiple
  qty pairs can fire in a single cast (it's not a single roll).
- `MAX_SKILL_CHANGEMATERIAL_SET` is small (check the header, typically 5) — the child
  table will have ≤5 rows per recipe.
- The `itemlv == 26` sentinel is rAthena's overloading of the produce table; make sure
  the importer carries `ItemLv` through and the seed includes those rows (they may live
  in a different source file than ordinary produce recipes).
