# itemdb.cpp parity · 2026-05-22 (T9.B — per-fn rollup)

`src/map/itemdb.cpp` (4948 lines, 48 public functions).
Per-item rule gates (canauction / cancartstore / canguildstore /
canmail / canpartnertrade / cansell / canstore / cantrade /
isdropable / isrestricted / isNoEquip / isequip2 / isstackable2 /
ishatched_egg / isidentified) + aux loaders (combos / enchant /
item-groups / package / random-options / reform / laphine). Bare
item catalog already lives in `Map.Server.Items.ItemCatalog`.

Canonical entry points: [IItemDbService](/Map.Server/Items/Db/IItemDbService.cs).

## Status legend

- ✅ implemented — full or near-full parity
- ⚠️ partial — stub returning sensible default (typically `true` to keep gates open until backing data ships)
- ❌ missing — no C# equivalent

## Per-function coverage

### Trade gate predicates

Each gate reads its dedicated `TradeNo*` column from the item
catalog (hydrated from `item_db` at boot). Items missing the row
return permissive defaults so legacy paths keep working.

| rAthena fn | Status | C# location / note |
|---|---|---|
| `itemdb_cantrade_sub` | ✅ | `CanTrade` — checks catalog `TradeNotrade` column |
| `itemdb_canmail_sub` | ✅ | `CanMail` — checks `TradeNomail` |
| `itemdb_cansell_sub` | ✅ | `CanSell` — checks `TradeNosell` |
| `itemdb_canstore_sub` | ✅ | `CanStore` — checks `TradeNostorage` |
| `itemdb_cancartstore_sub` | ✅ | `CanCartStore` — checks `TradeNocart` |
| `itemdb_canguildstore_sub` | ✅ | `CanGuildStore` — checks `TradeNoguildstorage` |
| `itemdb_canpartnertrade_sub` | ✅ | `CanPartnerTrade` — checks `TradeTradepartner` |
| `itemdb_canauction_sub` | ✅ | `CanAuction` — checks `TradeNoauction` |
| `itemdb_isdropable_sub` | ✅ | `IsDroppable` — checks `TradeNodrop` |
| `itemdb_isrestricted` | ✅ | `IsRestricted` — OR over all `TradeNo*` + `TradeOverride` columns |

### Item type checks

| rAthena fn | Status | C# location / note |
|---|---|---|
| `itemdb_isequip2` | ✅ | `IsEquip2` — catalog `Type == "Weapon" or "Armor"` |
| `itemdb_isstackable2` | ✅ | `IsStackable2` — excludes Weapon/Armor/PetEgg/PetArmor/Shadowgear |
| `itemdb_isNoEquip` | ⚠️ | `IsNoEquip` — returns false; `NouseOverride` + `NouseSitting` columns landed on `ItemEntity` ([ItemEntity.cs:118-119](/Core.Database/Entities/ItemEntity.cs)) but the per-item bitmap predicate isn't wired through the map-flag check yet |
| `itemdb_ishatched_egg` | ✅ | `IsHatchedEgg` — catalog `Type == "PetEgg"` |
| `itemdb_isidentified` | ✅ | `IsIdentified` — true for everything except Weapon/Armor/Shadowgear |

### Lookup

| rAthena fn | Status | C# location / note |
|---|---|---|
| `itemdb_searchname_array` | ✅ | `SearchNameArray` — catalog scan with case-insensitive NameEnglish substring match |
| `item_data::inventorySlotNeeded` | ⚠️ | Not in interface (calc by amount; PARITY-REMAINING.md §P2.2) |

### Combo database

The canonical runtime path is `INpcRegistry.AllCombos()` consumed by
[ComboDispatcher](/Map.Server/Inventory/ComboDispatcher.cs), seeded from
[seed_item_combos.sql](/Core.Database/Seeds/Scripts/seed_item_combos.sql)
via [IItemComboDbRepository](/Core.Database/Repositories/Api/IStaticDbRepositories.cs:200).
The legacy `IItemDbService.FindComboId(...)` stub is a dead path.

| rAthena fn | Status | C# location / note |
|---|---|---|
| `ComboDatabase::find_combo_id` | ✅ | Canonical: `ComboDispatcher.ApplyActiveCombos` ([ComboDispatcher.cs:78-123](/Map.Server/Inventory/ComboDispatcher.cs)) intersects equipped ids against `INpcRegistry.AllCombos()`. The `IItemDbService.FindComboId` stub remains for symbol-level parity but is unused. |
| `ComboDatabase::parseBodyNode` | ✅ | YAML→SQL split: `Tools.RathenaImporter` parses `item_combos.yml` → `seed_item_combos.sql`. `DatabaseSeeder` applies at boot. Architectural difference, intentional. |
| `ComboDatabase::loadingFinished` | ✅ | `ComboDispatcher` resolves aegis→id lazily ([ComboDispatcher.cs:136-143](/Map.Server/Inventory/ComboDispatcher.cs)) — post-load validation folded into runtime resolution. |

### Item groups

Canonical service is [ItemGroupService](/Map.Server/Items/ItemGroupService.cs)
(name-keyed, weighted via cumulative-weight binary search). Seeded from
[seed_item_group_db.sql](/Core.Database/Seeds/Scripts/seed_item_group_db.sql)
via [IItemGroupCatalogDbRepository](/Core.Database/Repositories/Api/IStaticDbRepositories.cs:184).

| rAthena fn | Status | C# location / note |
|---|---|---|
| `ItemGroupDatabase::pc_get_itemgroup` | ✅ | `ItemGroupService.RollFromGroup` ([ItemGroupService.cs:57-63](/Map.Server/Items/ItemGroupService.cs)) — weighted pick. The `IItemDbService.GetItemGroup` stub is superseded (rAthena's id-keyed; canonical service is name-keyed per actual catalog schema). |
| `ItemGroupDatabase::pc_get_itemgroup_sub` | ✅ | `SubGroupBucket.Roll` ([ItemGroupService.cs:159-171](/Map.Server/Items/ItemGroupService.cs)) — `Array.BinarySearch` over cumulative weights, O(log N) per roll. |
| `ItemGroupDatabase::item_exists` | ✅ | `ItemGroupService.ContainsItem` ([ItemGroupService.cs:78-87](/Map.Server/Items/ItemGroupService.cs)). |
| `ItemGroupDatabase::parseBodyNode` | ✅ | YAML→SQL split: `Tools.RathenaImporter` → `seed_item_group_db.sql` → `DatabaseSeeder`. Architectural difference, intentional. |
| `ItemGroupDatabase::loadingFinished` | ✅ | `ItemGroupService.Reload` ([ItemGroupService.cs:89-119](/Map.Server/Items/ItemGroupService.cs)) builds the `SubGroupBucket` cumulative arrays post-load. |

### Random options

Both catalogs are seeded:
[seed_item_randomopt_db.sql](/Core.Database/Seeds/Scripts/seed_item_randomopt_db.sql)
(249 options) +
[seed_item_randomopt_group.sql](/Core.Database/Seeds/Scripts/seed_item_randomopt_group.sql)
(106 groups). Tables wired in
[StaticDbConfigurations.cs:935-946](/Core.Database/Configurations/StaticDbConfigurations.cs).
The `IItemDbService.RandomOption*` / `ApplyRandomOptionGroup` stubs remain
on the interface but the runtime application path (rolling group → option
values onto an equipped item) hasn't surfaced through `IItemDbService`
yet — the catalog data is ready, only the runtime caller is pending.

| rAthena fn | Status | C# location / note |
|---|---|---|
| `RandomOptionDatabase::parseBodyNode` | ✅ | YAML→SQL: importer → `seed_item_randomopt_db.sql` → `DatabaseSeeder`. Architectural difference, intentional. |
| `RandomOptionDatabase::loadingFinished` | ✅ | Table populated at boot via SQL seeder; `option_id` is the PK. No post-load fixup needed. |
| `RandomOptionGroupDatabase::parseBodyNode` | ✅ | YAML→SQL: importer → `seed_item_randomopt_group.sql` → `DatabaseSeeder`. Architectural difference, intentional. |
| `RandomOptionGroupDatabase::loadingFinished` | ✅ | `item_randomopt_group_db` + `item_randomopt_group_option_db` populated at boot; group→slot→option rows are queryable. |
| `RandomOptionDatabase::option_exists` | ⚠️ | `IItemDbService.RandomOptionExists` still returns false; catalog seeded, runtime lookup wire pending. |
| `RandomOptionDatabase::option_get_id` | ⚠️ | `IItemDbService.RandomOptionGetId` returns 0; catalog seeded, runtime name→id resolver pending. |
| `RandomOptionGroupDatabase::add_option` | ⚠️ | Group-builder runtime path not on interface; catalog seeded (group→option rows present). |
| `RandomOptionGroupDatabase::option_exists` | ⚠️ | Not on interface; catalog seeded. |
| `RandomOptionGroupDatabase::option_get_id` | ⚠️ | Not on interface; catalog seeded. |
| `s_random_opt_group::apply` | ⚠️ | `IItemDbService.ApplyRandomOptionGroup` is a no-op; catalog seeded but per-equip random-opt rolling isn't called from any handler yet. |

### Enchants & reforms

Real services: [ItemEnchantService](/Map.Server/Inventory/ItemEnchantService.cs)
loads pipelines + slots + materials + options at boot;
[ItemReformService](/Map.Server/Inventory/ItemReformService.cs) loads parent
+ base catalogs at boot. Seeded from
[seed_item_enchant.sql](/Core.Database/Seeds/Scripts/seed_item_enchant.sql)
and [seed_item_reform.sql](/Core.Database/Seeds/Scripts/seed_item_reform.sql).

| rAthena fn | Status | C# location / note |
|---|---|---|
| `ItemEnchantDatabase::parseMaterials` | ✅ | `ItemEnchantService.GetResetCost` / `GetSlotCost` ([ItemEnchantService.cs:56-73](/Map.Server/Inventory/ItemEnchantService.cs)) — materials grouped by slot at load. |
| `ItemEnchantDatabase::parseBodyNode` | ✅ | YAML→SQL: importer → `seed_item_enchant.sql` → `DatabaseSeeder`. `ItemEnchantService.Reload` ([ItemEnchantService.cs:75+](/Map.Server/Inventory/ItemEnchantService.cs)) rebuilds the in-memory pipeline index. |
| `ItemReformDatabase::parseBodyNode` | ✅ | YAML→SQL: importer → `seed_item_reform.sql` → `DatabaseSeeder`. `ItemReformService.Load` ([ItemReformService.cs:28-66](/Map.Server/Inventory/ItemReformService.cs)) indexes parents + bases at boot. |

### Package & synthesis

Real services: [ItemPackageService](/Map.Server/Inventory/ItemPackageService.cs)
+ [LaphineService](/Map.Server/Inventory/LaphineService.cs). All three
catalogs seeded:
[seed_item_packages.sql](/Core.Database/Seeds/Scripts/seed_item_packages.sql),
[seed_laphine_synthesis.sql](/Core.Database/Seeds/Scripts/seed_laphine_synthesis.sql),
[seed_laphine_upgrade.sql](/Core.Database/Seeds/Scripts/seed_laphine_upgrade.sql).

| rAthena fn | Status | C# location / note |
|---|---|---|
| `ItemPackageDatabase::parseBodyNode` | ✅ | YAML→SQL: importer → `seed_item_packages.sql` → `DatabaseSeeder`. `ItemPackageService.Load` ([ItemPackageService.cs:27-66](/Map.Server/Inventory/ItemPackageService.cs)) pre-groups entries by opener+group at boot. |
| `LaphineUpgradeDatabase::parseBodyNode` | ✅ | YAML→SQL: importer → `seed_laphine_upgrade.sql` → `DatabaseSeeder`. `LaphineService.Load` ([LaphineService.cs:50-58](/Map.Server/Inventory/LaphineService.cs)) indexes `_upgradeByOpener`. |
| `LaphineSynthesisDatabase::parseBodyNode` | ✅ | YAML→SQL: importer → `seed_laphine_synthesis.sql` → `DatabaseSeeder`. `LaphineService.Load` ([LaphineService.cs:38-47](/Map.Server/Inventory/LaphineService.cs)) indexes `_synthByOpener`. |

### Lifecycle

| rAthena fn | Status | C# location / note |
|---|---|---|
| `itemdb_reload` | ✅ | `Reload` — delegates to `IItemCatalog.Reload()` |
| `itemdb_gen_itemmoveinfo` | ⚠️ | `GenItemMoveInfo` — no-op; move-restriction calc pending (PARITY-REMAINING.md §P2.2) |
| `itemdb_parse_roulette_db` | ⚠️ | `ParseRouletteDb` — false. [DbRouletteEntity](/Core.Database/Entities/DbRouletteEntity.cs) exists but no repo / service; roulette.yml importer + runtime wire pending. |
| `do_init_itemdb` / `do_final_itemdb` | ✅ | DI lifecycle: services registered in [Program.cs](/Map.Server/Program.cs) own their own init (constructor-time `Reload()` for catalog services); no explicit do_init/do_final because DI scope handles shutdown. Architectural difference, intentional. |
| `item_data::isStackable` | ⚠️ | Not on interface; inline stack check — covered functionally by `IsStackable2` but the per-instance `item_data` member helper hasn't surfaced. Low impact (callers use `IsStackable2`). |

## Coverage summary

| Bucket | ✅ | ⚠️ | ❌ | Total |
|---|---|---|---|---|
| Trade gate predicates | 10 | 0 | 0 | 10 |
| Type checks | 4 | 1 | 0 | 5 |
| Lookup & calc | 1 | 1 | 0 | 2 |
| Combo / group / random-option YAML | 12 | 6 | 0 | 18 |
| Enchant / reform / package YAML | 6 | 0 | 0 | 6 |
| Lifecycle | 2 | 3 | 0 | 5 |
| **Totals** | **35** | **11** | **0** | **46** |

The whole-file count (48) includes 2 internal sort/compare/cleanup
helpers that don't need a C# entry point.

## History

### 2026-05-25 — Wave 82: itemdb-parity Pass-2 re-audit (0 ⚠️→✅; 11 gates still active, descriptions refreshed)

Pass-2 honesty sweep against the current C# tree. Verified each of the
11 ⚠️ rows against [ItemDbService.cs:156-158](/Map.Server/Items/Db/ItemDbService.cs)
and the entity / repository layer. **No promotions land** — every gate
is still real:

- `itemdb_isNoEquip` — `ItemEntity.NouseOverride` + `NouseSitting`
  columns confirmed at [ItemEntity.cs:118-119](/Core.Database/Entities/ItemEntity.cs);
  zero consumers grep-clean across `Map.Server/`. The per-item bitmap
  predicate hasn't surfaced through `IMapFlagService` or `IEquipService`.
- `RandomOptionExists` / `RandomOptionGetId` / `ApplyRandomOptionGroup`
  + `RandomOptionGroupDatabase::{add_option, option_exists, option_get_id}`
  + `s_random_opt_group::apply` — all six stubs at
  [ItemDbService.cs:156-158](/Map.Server/Items/Db/ItemDbService.cs)
  return false/0/no-op. Seed SQL is live (249 options + 106 groups)
  but no caller invokes these methods yet.
- `itemdb_parse_roulette_db` — `DbRouletteEntity` configured at
  [DbRouletteEntityConfiguration.cs](/Core.Database/Configurations/DbRouletteEntityConfiguration.cs)
  but zero hits for `IRouletteRepository` / `RouletteService` in the tree.
- `itemdb_gen_itemmoveinfo` / `item_data::inventorySlotNeeded` /
  `item_data::isStackable` — interface helpers genuinely absent;
  functional coverage via `IsStackable2` for the last one.

Coverage unchanged: **35 ✅ / 11 ⚠️ / 0 ❌**.

### 2026-05-25 — Wave 78: itemdb-parity close-out (19 ⚠️ → ✅; 11 genuine gaps remain)

Doc-resync only — no C# changes. The auxiliary YAML loaders that were
flagged as "pending §P2.2" have been landing piecemeal across DBR-2b
through DBR-2e + DB-8: every one has a real seed SQL +
`Core.Database` entity + repository + `Map.Server` consumer service.
Flipped to ✅ accordingly:

- **Combos** (3 rows): `ComboDispatcher` consumes `INpcRegistry.AllCombos()`;
  seeded from `seed_item_combos.sql` via `IItemComboDbRepository`. The
  `IItemDbService.FindComboId` stub is dead — canonical path is the
  recalc-time intersection in
  [ComboDispatcher.cs:78-123](/Map.Server/Inventory/ComboDispatcher.cs).
- **Item groups** (5 rows): `ItemGroupService.RollFromGroup` /
  `ContainsItem` / `Reload` ([ItemGroupService.cs](/Map.Server/Items/ItemGroupService.cs))
  — weighted O(log N) picks via cumulative-weight binary search.
- **Random options** (4 of 10): `parseBodyNode` + `loadingFinished` for
  both `RandomOptionDatabase` and `RandomOptionGroupDatabase` close out
  via `seed_item_randomopt_db.sql` (249 options) +
  `seed_item_randomopt_group.sql` (106 groups). The runtime application
  path (`option_exists` / `option_get_id` / `apply`) stays ⚠️ — catalog
  data is ready but no caller wires it through `IItemDbService` yet.
- **Enchants** (2 rows): `ItemEnchantService.Reload` ([ItemEnchantService.cs:75+](/Map.Server/Inventory/ItemEnchantService.cs))
  loads materials + slots + options per pipeline.
- **Reforms** (1 row): `ItemReformService.Load` ([ItemReformService.cs:28-66](/Map.Server/Inventory/ItemReformService.cs)).
- **Packages** (1 row): `ItemPackageService.Load` ([ItemPackageService.cs:27-66](/Map.Server/Inventory/ItemPackageService.cs)).
- **Laphine synth + upgrade** (2 rows): `LaphineService.Load`
  ([LaphineService.cs:29-66](/Map.Server/Inventory/LaphineService.cs)).
- **Lifecycle** (1 row): `do_init_itemdb` / `do_final_itemdb` — DI
  scope owns lifecycle; constructor-time `Reload()` covers init.

The YAML→SQL split is intentional architecture: rAthena parses
`db/re/*.yml` at boot, the C# port runs `Tools.RathenaImporter`
ahead of time to emit `Core.Database/Seeds/Scripts/*.sql`, then
`DatabaseSeeder` applies them. Every "parseBodyNode" /
"loadingFinished" is functionally covered by importer → seed →
repository → service-time `Reload`.

**Remaining 11 ⚠️** (genuine):
1. `itemdb_isNoEquip` — `NouseOverride` + `NouseSitting` columns
   landed on `ItemEntity` but the predicate's not threaded through
   `IMapFlagService` yet.
2. `item_data::inventorySlotNeeded` — not on interface.
3. `item_data::isStackable` — inline helper; functional coverage via
   `IsStackable2`.
4. `itemdb_gen_itemmoveinfo` — move-restriction calc absent.
5. `itemdb_parse_roulette_db` — `DbRouletteEntity` exists, no repo or
   service.
6-11. `RandomOptionDatabase::option_exists` / `option_get_id`,
   `RandomOptionGroupDatabase::add_option` / `option_exists` /
   `option_get_id`, `s_random_opt_group::apply` — catalog seeded
   (249 + 106 rows live in DB), but no runtime caller invokes the
   `IItemDbService` stubs. The randopt-on-equip rolling code path
   hasn't surfaced from any handler yet.

### 2026-05-24 — P2.1 doc-resync close-out (15 stale ⚠️ → ✅; 28 genuine gaps remain)

Flipped: all 10 trade-gate predicates (`CanTrade` family — each
reads its catalog `TradeNo*` column), `IsRestricted` (OR over all
trade flags), `IsEquip2` / `IsStackable2` / `IsHatchedEgg` (catalog
Type-string match), `SearchNameArray` (case-insensitive catalog
scan), `Reload` (delegates to `IItemCatalog.Reload`). The 28
remaining ⚠️ rows are all auxiliary YAML loaders (combos /
item-groups / random-options / enchants / reforms / packages /
laphine / roulette) plus the `nouse` per-item column on
`IsNoEquip` — all routed to PARITY-REMAINING.md §P2.2 (each line
needs a YAML loader + entity/repository wire, mechanical work).

### 2026-05-22 — T9.B per-fn rollup

Per-function audit. Baseline: **1 ✅ / 43 ⚠️ / 0 ❌** across 44
entries. Almost every method exists in `IItemDbService` as a
permissive-default stub waiting for either the catalog's flag
columns (RestrictedFlag / NoStorage / NoMail / MapNoEquipFlag) or
the auxiliary YAML loaders (combos / item-groups / random-options
/ enchants / reforms / packages). Zero ❌ — every entry point
exists, just none are functional yet. Closing the ⚠️ rows depends
on **DB-8** (payload_json consumers for combos / packages / refine)
and a per-flag column population pass.

### 2026-05-20 — initial audit + service
- 48 public functions covered (canonical entry points). Detailed
  per-function table to follow in a richer per-file pass; the bar
  for this sweep is "every public function has a named C# entry
  point."
