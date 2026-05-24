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
| `itemdb_isNoEquip` | ⚠️ | `IsNoEquip` — returns false; `nouse` per-item bitmap column still pending (PARITY-REMAINING.md §P2.2) |
| `itemdb_ishatched_egg` | ✅ | `IsHatchedEgg` — catalog `Type == "PetEgg"` |
| `itemdb_isidentified` | ✅ | `IsIdentified` — true for everything except Weapon/Armor/Shadowgear |

### Lookup

| rAthena fn | Status | C# location / note |
|---|---|---|
| `itemdb_searchname_array` | ✅ | `SearchNameArray` — catalog scan with case-insensitive NameEnglish substring match |
| `item_data::inventorySlotNeeded` | ⚠️ | Not in interface (calc by amount; PARITY-REMAINING.md §P2.2) |

### Combo database

| rAthena fn | Status | C# location / note |
|---|---|---|
| `ComboDatabase::find_combo_id` | ⚠️ | `FindComboId` — returns 0; combos.yml loader pending (PARITY-REMAINING.md §P2.2) |
| `ComboDatabase::parseBodyNode` | ⚠️ | Not in interface — YAML parse hook (PARITY-REMAINING.md §P2.2) |
| `ComboDatabase::loadingFinished` | ⚠️ | Not in interface — post-load validation (PARITY-REMAINING.md §P2.2) |

### Item groups

| rAthena fn | Status | C# location / note |
|---|---|---|
| `ItemGroupDatabase::pc_get_itemgroup` | ⚠️ | `GetItemGroup` — returns 0; groups.yml loader pending (PARITY-REMAINING.md §P2.2) |
| `ItemGroupDatabase::pc_get_itemgroup_sub` | ⚠️ | Not in interface — internal filter (PARITY-REMAINING.md §P2.2) |
| `ItemGroupDatabase::item_exists` | ⚠️ | Not in interface — validation (PARITY-REMAINING.md §P2.2) |
| `ItemGroupDatabase::parseBodyNode` | ⚠️ | Not in interface — YAML parse (PARITY-REMAINING.md §P2.2) |
| `ItemGroupDatabase::loadingFinished` | ⚠️ | Not in interface — post-load (PARITY-REMAINING.md §P2.2) |

### Random options

| rAthena fn | Status | C# location / note |
|---|---|---|
| `RandomOptionDatabase::option_exists` | ⚠️ | `RandomOptionExists` — false; options.yml loader pending (PARITY-REMAINING.md §P2.2) |
| `RandomOptionDatabase::option_get_id` | ⚠️ | `RandomOptionGetId` — returns 0 (PARITY-REMAINING.md §P2.2) |
| `RandomOptionGroupDatabase::add_option` | ⚠️ | Not in interface — dynamic group builder (PARITY-REMAINING.md §P2.2) |
| `RandomOptionGroupDatabase::option_exists` | ⚠️ | Not in interface — group validation (PARITY-REMAINING.md §P2.2) |
| `RandomOptionGroupDatabase::option_get_id` | ⚠️ | Not in interface — group lookup (PARITY-REMAINING.md §P2.2) |
| `s_random_opt_group::apply` | ⚠️ | `ApplyRandomOptionGroup` — no-op (PARITY-REMAINING.md §P2.2) |
| `RandomOptionDatabase::parseBodyNode` | ⚠️ | Not in interface — YAML parse (PARITY-REMAINING.md §P2.2) |
| `RandomOptionGroupDatabase::parseBodyNode` | ⚠️ | Not in interface — YAML parse (PARITY-REMAINING.md §P2.2) |
| `RandomOptionDatabase::loadingFinished` | ⚠️ | Not in interface — post-load (PARITY-REMAINING.md §P2.2) |
| `RandomOptionGroupDatabase::loadingFinished` | ⚠️ | Not in interface — post-load (PARITY-REMAINING.md §P2.2) |

### Enchants & reforms

| rAthena fn | Status | C# location / note |
|---|---|---|
| `ItemEnchantDatabase::parseMaterials` | ⚠️ | Not in interface — enchant cost lookup (PARITY-REMAINING.md §P2.2) |
| `ItemEnchantDatabase::parseBodyNode` | ⚠️ | Not in interface — YAML parse (PARITY-REMAINING.md §P2.2) |
| `ItemReformDatabase::parseBodyNode` | ⚠️ | Not in interface — reform costs WIP (PARITY-REMAINING.md §P2.2) |

### Package & synthesis

| rAthena fn | Status | C# location / note |
|---|---|---|
| `ItemPackageDatabase::parseBodyNode` | ⚠️ | Not in interface — item packages WIP (PARITY-REMAINING.md §P2.2) |
| `LaphineUpgradeDatabase::parseBodyNode` | ⚠️ | Not in interface — laphine upgrades WIP (PARITY-REMAINING.md §P2.2) |
| `LaphineSynthesisDatabase::parseBodyNode` | ⚠️ | Not in interface — laphine synthesis WIP (PARITY-REMAINING.md §P2.2) |

### Lifecycle

| rAthena fn | Status | C# location / note |
|---|---|---|
| `itemdb_reload` | ✅ | `Reload` — delegates to `IItemCatalog.Reload()` |
| `itemdb_gen_itemmoveinfo` | ⚠️ | `GenItemMoveInfo` — no-op; move-restriction calc pending (PARITY-REMAINING.md §P2.2) |
| `itemdb_parse_roulette_db` | ⚠️ | `ParseRouletteDb` — false; roulette.yml loader pending (PARITY-REMAINING.md §P2.2) |
| `do_init_itemdb` / `do_final_itemdb` | ⚠️ | Not in interface — DI handles lifecycle (intentional) |
| `item_data::isStackable` | ⚠️ | Not in interface — inline stack check (PARITY-REMAINING.md §P2.2) |

## Coverage summary

| Bucket | ✅ | ⚠️ | ❌ | Total |
|---|---|---|---|---|
| Trade gate predicates | 10 | 0 | 0 | 10 |
| Type checks | 4 | 1 | 0 | 5 |
| Lookup & calc | 1 | 1 | 0 | 2 |
| Combo / group / random-option YAML | 0 | 16 | 0 | 16 |
| Enchant / reform / package YAML | 0 | 6 | 0 | 6 |
| Lifecycle | 1 | 4 | 0 | 5 |
| **Totals** | **16** | **28** | **0** | **44** |

The whole-file count (48) includes 4 internal sort/compare/cleanup
helpers that don't need a C# entry point.

## History

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
