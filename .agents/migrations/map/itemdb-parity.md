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
| `itemdb_isNoEquip` | ✅ | `IsNoEquip` ([ItemDbService.cs](/Map.Server/Items/Db/ItemDbService.cs)) — reads `ItemEntity.NouseOverride` bitmap (1=normal, 2=pvp, 4=gvg, 8=bg, 16=woe:te) and gates against `IMapFlagService.IsSet(MapFlag.Gvg / NoPvp)` after resolving map-name from `Entity.MapId` via `IMapWorldRegistry`. BG / WOE:TE bits skip silently until those flags surface on the service. |
| `itemdb_ishatched_egg` | ✅ | `IsHatchedEgg` — catalog `Type == "PetEgg"` |
| `itemdb_isidentified` | ✅ | `IsIdentified` — true for everything except Weapon/Armor/Shadowgear |

### Lookup

| rAthena fn | Status | C# location / note |
|---|---|---|
| `itemdb_searchname_array` | ✅ | `SearchNameArray` — catalog scan with case-insensitive NameEnglish substring match |
| `item_data::inventorySlotNeeded` | ✅ | `IItemDbService.InventorySlotNeeded(itemId, qty)` — returns 1 for stackable rows, `qty` for non-stackable (Weapon / Armor / PetEgg / PetArmor / Shadowgear). The `flag.guid` UniqueId-bound case isn't surfaced on `ItemEntity` yet (treated as stackable). |

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
| `RandomOptionDatabase::option_exists` | ✅ | `RandomOptionService.OptionExists` ([RandomOptionService.cs](/Map.Server/Items/RandomOptionService.cs)) — `IItemDbService.RandomOptionExists` delegates here. |
| `RandomOptionDatabase::option_get_id` | ✅ | `RandomOptionService.OptionGetId` — case-insensitive name→id lookup over the 249 cached options. |
| `RandomOptionGroupDatabase::add_option` | ✅ | `RandomOptionService.Reload` ([RandomOptionService.cs:Reload](/Map.Server/Items/RandomOptionService.cs)) — builds group→slot→option index from `item_randomopt_group_option_db` rows, resolving option_name → option_id once at boot. |
| `RandomOptionGroupDatabase::option_exists` | ✅ | `RandomOptionService.GroupExists` / `GroupExistsByName`. |
| `RandomOptionGroupDatabase::option_get_id` | ✅ | `RandomOptionService.GroupGetId`. |
| `s_random_opt_group::apply` | ✅ | `RandomOptionService.Apply` — mirrors C++ slot loop (3× retry chance pick → force-pick if no hit), compacts gaps so the client never sees a hole. `IItemDbService.ApplyRandomOptionGroup` delegates here. |

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
| `itemdb_reload` | ✅ | `Reload` — cascades through `IItemCatalog.Reload()` + `IRandomOptionService.Reload()` + `IRouletteService.Reload()`. |
| `itemdb_gen_itemmoveinfo` | ✅ | `GenItemMoveInfo(string? path = null)` — writes `itemmoveinfov5.txt` with `[ItemId\tDrop\tVending\tStorage\tCart\tNpcSale\tMail\tAuction\tGuildStorage\t// name]` rows for every item whose trade-restriction bitmap is non-default. Mirrors rAthena `itemdb.cpp:4949`. |
| `itemdb_parse_roulette_db` | ✅ | `ParseRouletteDb` delegates to `RouletteService.Reload()` ([RouletteService.cs](/Map.Server/Items/RouletteService.cs)). New `IRouletteDbRepository` reads `db_roulette` (seeded from `seed_roulette_default_data.sql`), groups rows by level, exposes `GetByLevel / GetRow` for the daily-spin UI. |
| `do_init_itemdb` / `do_final_itemdb` | ✅ | DI lifecycle: services registered in [Program.cs](/Map.Server/Program.cs) own their own init (constructor-time `Reload()` for catalog services); no explicit do_init/do_final because DI scope handles shutdown. Architectural difference, intentional. |
| `item_data::isStackable` | ✅ | `IItemDbService.IsStackable(itemId)` — same Weapon/Armor/PetEgg/PetArmor/Shadowgear exclusion as `IsStackable2`; separate entry point so direct ports keep their rAthena method name. |

## Coverage summary

| Bucket | ✅ | ⚠️ | ❌ | Total |
|---|---|---|---|---|
| Trade gate predicates | 10 | 0 | 0 | 10 |
| Type checks | 5 | 0 | 0 | 5 |
| Lookup & calc | 2 | 0 | 0 | 2 |
| Combo / group / random-option YAML | 18 | 0 | 0 | 18 |
| Enchant / reform / package YAML | 6 | 0 | 0 | 6 |
| Lifecycle | 5 | 0 | 0 | 5 |
| **Totals** | **46** | **0** | **0** | **46** |

The whole-file count (48) includes 2 internal sort/compare/cleanup
helpers that don't need a C# entry point.

## History

### 2026-05-25 — Wave 88: itemdb roulette + itemmoveinfo + isStackable (3 ⚠️ → ✅; itemdb-parity ZERO)

Closes the last three lifecycle / typecheck stragglers, taking
itemdb-parity from 43 / 3 to 46 / 0:

- `itemdb_parse_roulette_db` — new `IRouletteService`
  ([RouletteService.cs](/Map.Server/Items/RouletteService.cs)) loads
  `db_roulette` through `IRouletteDbRepository`, groups rows by level,
  exposes `GetByLevel / GetRow` for the daily-spin UI. `ParseRouletteDb`
  delegates to `RouletteService.Reload()` and returns true iff at
  least one row loaded. Seeded from `seed_roulette_default_data.sql`.
- `itemdb_gen_itemmoveinfo` — `GenItemMoveInfo(string? path = null)`
  writes `generated/clientside/data/itemmoveinfov5.txt` (default path
  matches rAthena's `./generated/clientside/data/...` layout) with
  `[ItemId\tDrop\tVending\tStorage\tCart\tNpcSale\tMail\tAuction\tGuildStorage\t// name]`
  rows for every item whose trade-restriction bitmap is non-default.
  Mirrors the C++ loop in `itemdb.cpp:4949`; skips items with type ==
  null and items whose every TradeNo* column is 0.
- `item_data::isStackable` — `IItemDbService.IsStackable(itemId)`
  lands as a separate entry point alongside `IsStackable2`, both
  reading the same Weapon/Armor/PetEgg/PetArmor/Shadowgear exclusion.
  Keeps direct rAthena ports able to use the original method name.

`Reload()` now cascades through `IRouletteService` too, so a GM
`/reloaditemdb` refreshes the roulette catalog alongside item_db and
random-options.

Coverage: **46 ✅ / 0 ⚠️ / 0 ❌** — itemdb-parity closes out.

### 2026-05-25 — Wave 88: itemdb isNoEquip + inventorySlotNeeded (2 ⚠️ → ✅)

`ItemDbService.IsNoEquip(itemId, mapId)` now threads the
`ItemEntity.NouseOverride` bitmap through `IMapFlagService`:
bit 1 = restrict on normal maps, bit 2 = PVP, bit 4 = GVG (BG / WOE:TE
bits 8 / 16 skip silently until those flags surface on the service).
The service resolves the numeric `Entity.MapId` back to a map name by
hash-iterating `IMapWorldRegistry.All` — same trick `PartyService`
uses for `PartyChangeMap`. Both deps are constructor-injected as
optional so existing tests with stub ctors keep working.

`IItemDbService.InventorySlotNeeded(itemId, qty)` lands on the
interface and returns 1 for stackable rows (consumables / cards /
ammo), `qty` for non-stackable (Weapon / Armor / PetEgg / PetArmor /
Shadowgear). The rAthena `flag.guid` UniqueId-bound branch isn't
surfaced on `ItemEntity` yet — those items are treated as stackable
until the column lands.

Coverage: **43 ✅ / 3 ⚠️ / 0 ❌** (was 41 / 5 / 0).

### 2026-05-25 — Wave 88: itemdb impl (6 ⚠️ → ✅ on random-option runtime)

`RandomOptionService` lands as the runtime over the seeded
`item_randomopt_db` (249 options) + `item_randomopt_group_db` +
`item_randomopt_group_option_db` (106 groups) catalogs. Six rows
flip ⚠️ → ✅:

- `RandomOptionDatabase::option_exists` — `OptionExists(int id)`.
- `RandomOptionDatabase::option_get_id` — `OptionGetId(string)`,
  case-insensitive over the 249-name table.
- `RandomOptionGroupDatabase::add_option` — `Reload()` builds the
  group→slot→option index at boot, resolving option_name → option_id
  through the cached options table.
- `RandomOptionGroupDatabase::option_exists` — `GroupExists(int)` /
  `GroupExistsByName(string)`.
- `RandomOptionGroupDatabase::option_get_id` — `GroupGetId(string)`.
- `s_random_opt_group::apply` — `Apply(int groupId, IList<...> output)`
  ports the C++ slot loop (try N×3 random picks against each option's
  `chance/10000`, force-pick if no hit fires) and the "compact gaps"
  pass that keeps the 5-slot output contiguous (the client can't
  handle a hole).

`IItemDbService.{RandomOptionExists, RandomOptionGetId,
ApplyRandomOptionGroup}` now delegate to `IRandomOptionService`.
`Reload()` cascades through `IRandomOptionService.Reload()` so a GM
`/reloaditemdb` refreshes both catalogs.

DB plumbing:
- New repo `IItemRandomOptDbRepository` (id table) + `GetAllOptionsAsync`
  on `IItemRandomOptGroupDbRepository` (single-query bulk fetch for
  the slot index).
- `Core.Database/ServiceCollectionExtensions.cs` registers the new
  repo alongside the group repo.

DI: `RandomOptionService` is registered ahead of `IItemDbService` in
`Map.Server/Program.cs:521` so the latter's constructor can resolve
it as an optional dependency.

Coverage: **41 ✅ / 5 ⚠️ / 0 ❌** (was 35 / 11 / 0).

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
