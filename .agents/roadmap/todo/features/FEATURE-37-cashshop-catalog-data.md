# FEATURE-37 — Cash-shop catalog source data

> **Epic:** Gameplay-Shop · **Status:** ❌ Not started · **Size:** S · **Player-visible:** yes
> **Depends on:** FEATURE-13 · **Blocks:** none

## Problem

FEATURE-13 wired the real cash-shop catalog **loader** (`IItemCashDbRepository` →
`CashShopService._catalog`), but the source YAML the importer reads —
`db/item_cash.yml` — **does not exist in the `rathena/` checkout**, so
`seed_item_cash.sql` has **0 rows** ("Rows: 0" in its header). The loader works
(proven by `CashShopServiceTests.Catalog_loads_from_stubbed_repository`), but in a
live server the catalog is empty: every `cashshop_buylist` for a real item returns
`CashShopResult.PurchaseFail` because no tab lists it. A player sees an empty cash
shop.

## Current state (C#)

- `Core.Database/Seeds/Scripts/seed_item_cash.sql` — header `-- Rows: 0`, no INSERTs.
- `Tools.RathenaImporter/Converters/RenormalizedConverters.cs:215` `ItemCashRenormConverter` —
  reads `db/item_cash.yml` (`SourceYamlPath`), emits `item_cash_db` + `item_cash_entry_db` rows.
  Produces 0 rows because the source file is absent.
- `Map.Server/Shop/Cash/CashShopService.cs:LoadCatalog` — loads whatever rows exist; tab-name→index
  map (`TabIndex`) already tolerates the standard rAthena tab names.

## rAthena reference (source of truth)

- The cash-shop catalog in modern rAthena is `db/item_cash_db.yml` (or `item_cash.yml` per the
  importer's expected path). Each row: `Tab:` (New/Hot/Limited/Rental/Permanent/Scrolls/
  Consumables/Other/Sale) → `Items:` list of `{ Item: <AegisName>, Price: <cashpoints> }`.
- `CashShopDatabase::parseBodyNode` (cashshop.cpp) parses exactly this shape.

## Scope — every sub-system that must be touched

- [ ] Obtain the canonical `item_cash.yml` (or `item_cash_db.yml`) from upstream rAthena and place it
      at the path `ItemCashRenormConverter.SourceYamlPath` expects (`db/item_cash.yml`).
- [ ] Confirm `ItemCashRenormConverter` parses the real shape (Tab + Items[].Item/Price); adjust the
      field names if upstream uses `item_cash_db.yml` with a different layout.
- [ ] Re-run `dotnet run --project Tools.RathenaImporter -- --yaml-only`; confirm `seed_item_cash.sql`
      now has a non-zero row count.
- [ ] Verify `TabIndex` in `CashShopService` covers every Tab string the real YAML uses (extend the
      map if upstream uses a name not already mapped).

## Done criteria

- `seed_item_cash.sql` header reports a non-zero row count and contains real INSERTs.
- On boot, `cashshop_reload` logs a non-zero catalog item count.
- A live `cashshop_buylist` for a real catalogued item (e.g. a Bubble Gum) resolves a price and
  succeeds (given the player has points).

## Test plan

- Importer determinism: re-running the importer twice produces an identical `seed_item_cash.sql`.
- `CashShopServiceTests` — add a smoke test that loads the generated seed via a scoped repo and
  resolves at least one known catalog item's price.

## Notes / gotchas

- This is a **data-availability** gap, not a code gap — the loader + BuyList are already correct.
- Aegis→nameId resolution goes through `IItemCatalog.GetByAegisName`; items referenced by the catalog
  must exist in the seeded `item_db` or they're silently skipped (by design).
