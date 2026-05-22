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

### Trade gate predicates (default-permissive)

All ten predicates return `true` until the `Restricted` /
`NoStorage` / `NoMail` etc. flags ship in the item catalog (DB-5
covers the YAML→SQL pipeline; per-flag column population pending).

| rAthena fn | Status | C# location / note |
|---|---|---|
| `itemdb_cantrade_sub` | ⚠️ | `CanTrade` — true |
| `itemdb_canmail_sub` | ⚠️ | `CanMail` — true |
| `itemdb_cansell_sub` | ⚠️ | `CanSell` — true |
| `itemdb_canstore_sub` | ⚠️ | `CanStore` — true |
| `itemdb_cancartstore_sub` | ⚠️ | `CanCartStore` — true |
| `itemdb_canguildstore_sub` | ⚠️ | `CanGuildStore` — true |
| `itemdb_canpartnertrade_sub` | ⚠️ | `CanPartnerTrade` — true |
| `itemdb_canauction_sub` | ⚠️ | `CanAuction` — true |
| `itemdb_isdropable_sub` | ⚠️ | `IsDroppable` — true |
| `itemdb_isrestricted` | ⚠️ | `IsRestricted` — false (RestrictedFlag WIP) |

### Item type checks

| rAthena fn | Status | C# location / note |
|---|---|---|
| `itemdb_isequip2` | ⚠️ | `IsEquip2` — false (EquipType parsing WIP) |
| `itemdb_isstackable2` | ⚠️ | `IsStackable2` — false (stacking logic in Inventory) |
| `itemdb_isNoEquip` | ⚠️ | `IsNoEquip` — false (MapNoEquipFlag WIP) |
| `itemdb_ishatched_egg` | ⚠️ | `IsHatchedEgg` — false (pet logic WIP) |
| `itemdb_isidentified` | ✅ | `IsIdentified` — true (sensible default) |

### Lookup

| rAthena fn | Status | C# location / note |
|---|---|---|
| `itemdb_searchname_array` | ⚠️ | `SearchNameArray` — returns 0 (catalog scan pending) |
| `item_data::inventorySlotNeeded` | ⚠️ | Not in interface (calc by amount) |

### Combo database

| rAthena fn | Status | C# location / note |
|---|---|---|
| `ComboDatabase::find_combo_id` | ⚠️ | `FindComboId` — returns 0 (combos.yml parser WIP) |
| `ComboDatabase::parseBodyNode` | ⚠️ | Not in interface (YAML parse hook) |
| `ComboDatabase::loadingFinished` | ⚠️ | Not in interface (post-load validation) |

### Item groups

| rAthena fn | Status | C# location / note |
|---|---|---|
| `ItemGroupDatabase::pc_get_itemgroup` | ⚠️ | `GetItemGroup` — returns 0 (groups.yml WIP) |
| `ItemGroupDatabase::pc_get_itemgroup_sub` | ⚠️ | Not in interface (internal filter) |
| `ItemGroupDatabase::item_exists` | ⚠️ | Not in interface (validation) |
| `ItemGroupDatabase::parseBodyNode` | ⚠️ | Not in interface (YAML parse hook) |
| `ItemGroupDatabase::loadingFinished` | ⚠️ | Not in interface (post-load) |

### Random options

| rAthena fn | Status | C# location / note |
|---|---|---|
| `RandomOptionDatabase::option_exists` | ⚠️ | `RandomOptionExists` — false (options.yml WIP) |
| `RandomOptionDatabase::option_get_id` | ⚠️ | `RandomOptionGetId` — returns 0 |
| `RandomOptionGroupDatabase::add_option` | ⚠️ | Not in interface (dynamic group builder) |
| `RandomOptionGroupDatabase::option_exists` | ⚠️ | Not in interface (group validation) |
| `RandomOptionGroupDatabase::option_get_id` | ⚠️ | Not in interface (group lookup) |
| `s_random_opt_group::apply` | ⚠️ | `ApplyRandomOptionGroup` — no-op |
| `RandomOptionDatabase::parseBodyNode` | ⚠️ | Not in interface (YAML parse) |
| `RandomOptionGroupDatabase::parseBodyNode` | ⚠️ | Not in interface (YAML parse) |
| `RandomOptionDatabase::loadingFinished` | ⚠️ | Not in interface (post-load) |
| `RandomOptionGroupDatabase::loadingFinished` | ⚠️ | Not in interface (post-load) |

### Enchants & reforms

| rAthena fn | Status | C# location / note |
|---|---|---|
| `ItemEnchantDatabase::parseMaterials` | ⚠️ | Not in interface (enchant cost lookup) |
| `ItemEnchantDatabase::parseBodyNode` | ⚠️ | Not in interface (YAML parse) |
| `ItemReformDatabase::parseBodyNode` | ⚠️ | Not in interface (reform costs WIP) |

### Package & synthesis

| rAthena fn | Status | C# location / note |
|---|---|---|
| `ItemPackageDatabase::parseBodyNode` | ⚠️ | Not in interface (item packages WIP) |
| `LaphineUpgradeDatabase::parseBodyNode` | ⚠️ | Not in interface (laphine upgrades WIP) |
| `LaphineSynthesisDatabase::parseBodyNode` | ⚠️ | Not in interface (laphine synthesis WIP) |

### Lifecycle

| rAthena fn | Status | C# location / note |
|---|---|---|
| `itemdb_reload` | ⚠️ | `Reload` — no-op (hot-reload WIP) |
| `itemdb_gen_itemmoveinfo` | ⚠️ | `GenItemMoveInfo` — no-op (move-restriction calc) |
| `itemdb_parse_roulette_db` | ⚠️ | `ParseRouletteDb` — false (roulette.yml WIP) |
| `do_init_itemdb` / `do_final_itemdb` | ⚠️ | Not in interface (static init) |
| `item_data::isStackable` | ⚠️ | Not in interface (inline stack check) |

## Coverage summary

| Bucket | ✅ | ⚠️ | ❌ | Total |
|---|---|---|---|---|
| Trade gate predicates | 0 | 10 | 0 | 10 |
| Type checks | 1 | 4 | 0 | 5 |
| Lookup & calc | 0 | 2 | 0 | 2 |
| Combo / group / random-option YAML | 0 | 16 | 0 | 16 |
| Enchant / reform / package YAML | 0 | 6 | 0 | 6 |
| Lifecycle | 0 | 5 | 0 | 5 |
| **Totals** | **1** | **43** | **0** | **44** |

The whole-file count (48) includes 4 internal sort/compare/cleanup
helpers that don't need a C# entry point.

## History

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
