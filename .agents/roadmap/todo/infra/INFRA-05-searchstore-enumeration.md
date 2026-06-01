# INFRA-05 — Universal Catalog / Search Store enumeration over vending + buying stores

> **Epic:** Infra parity · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** none · **Blocks:** none

## Problem

The **Universal Catalog** (search store — the client window that searches all player
vending stalls and buying stores for an item by name / price / cards) returns **nothing**.
`Query` returns `0`, `Next`/`Click`/`QueryNext`/`QueryRemote` return `false`. Neither
`IVendingService` nor `IBuyingStoreService` exposes a way to enumerate active shops, so
the search service has no data source. A player using the catalog sees an empty result
list regardless of how many stalls are open.

## Current state (C#)

- `Map.Server/Shop/SearchStore/SearchStoreService.cs:25-31`:
  - `Query(...) => 0` — never scans.
  - `Next(...) => false`, `Click(...) => false`, `QueryNext(...) => false`,
    `QueryRemote(...) => false`.
  - `Open` records `EffectId` + `MaxResults` into a per-PC `SearchSession`
    (`:20-24`, `:34-38`) — pagination/result state has nowhere to live.
- `Map.Server/Shop/SearchStore/ISearchStoreService.cs` — full interface; docstring says
  enumeration is "deferred ... once they expose enumerable shop lists".
- `Map.Server/Shop/Vending/IVendingService.cs` — has `Search(nameId)`,
  `SearchAll(nameId)` (bool, single-item) but **no `GetAllShops()`**.
  `VendingService.cs:18` keeps `Dictionary<EntityId, Stall>` + `_accountIndex`
  (`:19`); `Stall` (`:111-116`) holds `List<(short index, short qty, int price)> Items`.
- `Map.Server/Shop/Buying/IBuyingStoreService.cs` — has `Search`/`SearchAll` but **no
  `GetAllShops()`**. Internal store model mirrors vending (per-buyer stall with offers).

## rAthena reference (source of truth)

Canonical source is `searchstore.cpp` (monolithic, ~361 lines).

- `searchstore_open` — sets `sd->searchstore.open = true`, `effect`, `uses` (max results),
  `type` (vending vs buying).
- `searchstore_query(sd, type, min_price, max_price, *itemlist, item_count, *cardlist,
  card_count)`:
  - Picks the search function by `type`: `SEARCHTYPE_VENDING` → walk all vending stalls;
    `SEARCHTYPE_BUYING_STORE` → walk all buying stores.
  - For each shop, for each item row: match if (no item filter OR `nameid` in itemlist)
    AND (price within `[min_price, max_price]`, with 0 meaning unbounded) AND (no card
    filter OR the item's cards intersect cardlist). Vending matches sell price; buying
    matches the buy offer price.
  - Accumulates up to `searchstore.uses` results; sets `searchstore.items` /
    pagination cursor. Emits the result page to the client.
- `searchstore_querynext` / `searchstore_next` — page forward through the accumulated
  result set (the query is run once; paging just advances the cursor over stored results).
- `searchstore_click(sd, account_id, store_id, nameid)` — resolves the chosen result back
  to a live shop (by account/store id) and either navigates to / previews it. Refuses if
  the shop/item no longer exists.
- `searchstore_queryremote(sd, account_id)` — remote (cross-map) variant; resolves the
  store owner by account id.

## Scope — every sub-system that must be touched

- [ ] **Add `IEnumerable<ShopSnapshot> GetAllShops()` to `IVendingService`** and
      `IBuyingStoreService`. Define a shared read-only `ShopSnapshot` record:
      `(int OwnerAccountId, int OwnerCharId, EntityId OwnerEntityId, int StoreId,
      string Title, short MapId, byte StoreType, IReadOnlyList<ShopItem> Items)` where
      `ShopItem` = `(int NameId, short Amount, int Price, uint[] Cards, byte Refine)`.
      Place it under `Map.Server/Shop/` so both services + the search service share it.
- [ ] **Implement `GetAllShops()`** in `VendingService` (project each `Stall` → snapshot;
      include owner account id from `_accountIndex`, item cards/refine from the inventory
      rows the stall references) and in `BuyingStoreService` (project each buying stall's
      offers; `StoreType` distinguishes buying from vending). Buying-store offers carry no
      cards — surface empty card arrays.
- [ ] **`SearchStoreService.Query`** — implement the real scan:
  - [ ] Select the source by `storeType` (vending vs buying; rAthena `SEARCHTYPE_*`).
  - [ ] Filter each shop item by `itemIds` (empty = any), `[minPrice, maxPrice]` (0 =
        unbounded on that bound), and `cardIds` (empty = any; otherwise the item's cards
        must contain all/any per rAthena — match the source semantic).
  - [ ] Skip the searcher's own shops (rAthena does not return your own stall).
  - [ ] Accumulate up to `MaxResults` into the PC's `SearchSession`; store the full
        result list + a page cursor. Return the count found (or the first-page count —
        match what the handler expects to emit).
- [ ] **`SearchSession`** — extend to hold `List<SearchResult>` + `int Cursor` +
      the last query filters (for QueryNext re-scan if rAthena re-queries).
- [ ] **`Next` / `QueryNext`** — advance the cursor over stored results and return whether
      another page exists; emit the next page packet.
- [ ] **`Click`** — resolve `(accountId, storeId, nameId)` to a live shop via
      `GetAllShops()`; return true + trigger the navigate/preview if it still exists,
      false otherwise.
- [ ] **`QueryRemote`** — resolve a store by owner `accountId`; same shape as Click but by
      account only.
- [ ] **Client packets**: ensure the search-store handler emits the result page
      (`ZC_SEARCH_STORE_INFO_ACK` / `ZC_SEARCH_STORE_INFO_FAILED` and the click/remote
      acks). If those `ZC_*` packets are already defined, just feed them; if not, add the
      definitions under `Core.Server/Packets/Out/ZC/` per rAthena `clif.cpp`
      `clif_search_store_info_*`.

No EF — vending/buying state is already in-memory (player shops are not persisted except
for autotrade, which is out of scope here).

## Done criteria

- With two vending stalls open (one selling Red Potion @50z, one @500z) and a buying
  store, a `Query(VENDING, minPrice=0, maxPrice=100, itemIds=[RedPotion], cardIds=[])`
  returns exactly the @50z stall.
- Price bounds, item filter, and card filter each independently narrow results matching
  rAthena.
- The searcher's own stall is excluded.
- `Next`/`QueryNext` page through a >MaxResults result set; `Click`/`QueryRemote` resolve
  a live shop and refuse a stale one.
- No `=> 0` / `=> false` stub bodies remain in `SearchStoreService`.

## Test plan

- `Map.Server.Tests/Shop/SearchStoreServiceTests` (with fake `IVendingService` /
  `IBuyingStoreService` returning canned `GetAllShops()`):
  - Item filter, price-range filter (incl. 0-as-unbounded), card filter, and store-type
    selection each pin a specific result subset.
  - Own-shop exclusion.
  - Paging: result set > MaxResults → first page = MaxResults, `Next` yields the rest.
  - `Click` on a present vs removed shop → true / false.
- `VendingServiceTests` / `BuyingStoreServiceTests` — `GetAllShops()` projects open stalls
  with correct owner/item/price/card fields and drops closed ones.

## Notes / gotchas

- **Card-match semantic**: rAthena's card filter — confirm whether it's "item contains
  *all* requested cards" vs "*any*". Read `searchstore.cpp` `searchstore_*_compare`
  before implementing.
- Buying stores match the **buy** price (what the owner will pay); vending matches the
  **sell** price. Don't conflate the two price meanings.
- `storeType` maps to rAthena `SEARCHTYPE_VENDING` / `SEARCHTYPE_BUYING_STORE` — confirm
  the byte values the client sends.
- Player shops are transient (in-memory) — `GetAllShops()` is a live read; no persistence
  layer involved.
