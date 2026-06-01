# PACKET-08-vending-buyingstore-cashshop — Player shops client→map packet bridge

> **Epic:** Packet bridge · **Status:** ❌ Not started · **Size:** L · **Player-visible:** yes
> **Depends on:** FEATURE-vending / FEATURE-buyingstore / FEATURE-cashshop (services exist) · **Blocks:** none

## Problem

The three player-shop services are implemented — `Map.Server/Shop/Vending/VendingService.cs`,
`Map.Server/Shop/Buying/BuyingStoreService.cs`, `Map.Server/Shop/Cash/CashShopService.cs` — but
**no client→map packet drives any of them**. A player cannot open a vending stall, browse/buy
from another player's vendor, set up a buying store, sell into one, or buy from the cash shop.

## Current state (C#)

- No handler exists for vending / buying-store / cash-shop packets.
- `Map.Server/Shop/Vending/IVendingService.cs` — `Update(vendor, title, items)` (open/refresh),
  `CloseVending(vendor)`, `Reopen(vendor)`, `VendingListReq(buyer, vendorAccountId)`,
  `PurchaseReq(buyer, vendorAccountId, items)`, `Search`, `SearchAll`.
- `Map.Server/Shop/Buying/IBuyingStoreService.cs` — `Open(buyer, effectId)`,
  `Update(buyer, title, zenyLimit, offers)`, `Close(buyer)`, `Reopen(buyer)`,
  `Trade(seller, buyerAccountId, storeId, items)`, `Search`, `SearchAll`.
- `Map.Server/Shop/Cash/ICashShopService.cs` — `BuyList(pc, items: (itemId, qty, tab))`,
  `Reload`, `SaleNotifyLogin`, `SaleFindItem`.
- Existing NPC-shop handlers for reference: `Map.Server/Handlers/Shop/PurchaseItemListHandler.cs`,
  `SellItemListHandler.cs`, `SelectDealTypeHandler.cs` (pattern to follow for var-len item baskets).

## rAthena reference (source of truth)

`rathena/src/map/clif.cpp` parse functions to port:

**Vending** (`vending.cpp`):
- `clif_parse_OpenVending` → `vending_openvending` — `Update` (title + item table: index/amount/price).
- `clif_parse_CloseVending` → `vending_closevending` — `CloseVending`.
- `clif_parse_VendingListReq` → `vending_vendinglistreq` — `VendingListReq` (open a vendor's list).
- `clif_parse_PurchaseReq` / `clif_parse_PurchaseReq2` → `vending_purchasereq` — `PurchaseReq`
  (`CZ_PC_PURCHASE_ITEMLIST_FROMMC`). Vending report: `vending_reportlogin` /
  `pc_check_vending` — emit `ZC_PC_PURCHASE_RESULT_FROMMC` to buyer + `ZC_DELETEITEM_FROM_MCSTORE`
  to vendor.

**Buying store** (`buyingstore.cpp`):
- `clif_parse_ReqOpenBuyingStore` → `buyingstore_create` — `Open` + `Update` (zeny limit + buy offers).
- `clif_parse_ReqCloseBuyingStore` → `buyingstore_close` — `Close`.
- `clif_parse_ReqClickBuyingStore` → open a buying store's offer list (`buyingstore_open`).
- `clif_parse_ReqTradeBuyingStore` → `buyingstore_trade` — `Trade` (seller sells into the store).

**Search store** (shared by vend + buying): `clif_parse_SearchStoreInfo`,
`clif_parse_SearchStoreInfoNextPage`, `clif_parse_SearchStoreInfoListItemClick` →
`searchstore_query` / paging / click-to-warp — `Search` / `SearchAll`.

**Cash shop** (`cashshop.cpp`):
- `clif_parse_cashshop_open_request` / `clif_parse_cashshop_close` → open/close cash window.
- `clif_parse_cashshop_list_request` / `clif_parse_CashShopReqTab` → request the item tabs.
- `clif_parse_cashshop_buy` (`CZ_SE_PC_BUY_CASHITEM_LIST`) → `cashshop_buylist` — `BuyList`.

ZC responses: `ZC_PC_PURCHASE_MYITEMLIST` / `ZC_PC_PURCHASE_RESULT_FROMMC`,
`ZC_DELETEITEM_FROM_MCSTORE`, `ZC_STORE_ENTRY` (vendor signboard), `ZC_OPENSTORE`,
`ZC_MYITEMLIST_BUYING_STORE` / `ZC_ACK_ITEMLIST_BUYING_STORE`, `ZC_BUYING_STORE_ENTRY`,
`ZC_FAILED_TRADE_BUYING_STORE_TO_SELLER` / `ZC_DELETEITEM_BUYING_STORE`,
`ZC_SEARCH_STORE_INFO_ACK` / `ZC_SEARCH_STORE_INFO_FAILED`,
`ZC_ACK_SE_CASH_ITEM_LIST2` (cash tabs), `ZC_SE_PC_BUY_CASHITEM_RESULT` (buy result),
`ZC_ACK_TOEXCHANGE_CASH_ITEM_LIST` / point balance. **Read `clif_packetdb.hpp` for ids.**

## Scope — every sub-system that must be touched

- [ ] **In packets** (`Core.Server/Packets/In/CZ/`):
  - [ ] Vending: `CZ_REQ_OPENSTORE` (var-len: title.80B + {index.W amount.W price.L}*),
        `CZ_REQ_CLOSESTORE`, `CZ_REQ_BUY_FROMMC` (`VendingListReq`),
        `CZ_PC_PURCHASE_ITEMLIST_FROMMC` (var-len buy basket: vendor AID + {index.W amount.W}*).
  - [ ] Buying store: `CZ_REQ_OPEN_BUYING_STORE` (var-len), `CZ_REQ_CLOSE_BUYING_STORE`,
        `CZ_REQ_CLICK_TO_BUYING_STORE`, `CZ_REQ_TRADE_BUYING_STORE` (var-len sell basket).
  - [ ] Search: `CZ_SEARCH_STORE_INFO` (var-len), `CZ_SEARCH_STORE_INFO_NEXT_PAGE`,
        `CZ_SSILIST_ITEM_CLICK`.
  - [ ] Cash shop: `CZ_SE_PC_OPEN_CASHSHOP`, `CZ_SE_PC_CLOSE_CASHSHOP`,
        `CZ_SE_PC_BUY_CASHITEM_LIST` (var-len: {itemId.L qty.W tab.W}*), `CZ_REQ_SE_CASH_TAB_CODE`.
- [ ] **Out packets**: the full ZC list above.
- [ ] **PacketHeader.cs** + **appsettings.packets.json** (most are var-len).
- [ ] **Handlers**:
  - [ ] `Map.Server/Handlers/Shop/Vending/OpenStoreHandler` → `IVendingService.Update`;
        `CloseStoreHandler` → `CloseVending`; `VendingListReqHandler` → `VendingListReq`;
        `PurchaseFromMcHandler` → `IVendingService.PurchaseReq` (emits buyer result + vendor delete).
  - [ ] `Map.Server/Handlers/Shop/Buying/OpenBuyingStoreHandler` → `Open` + `Update`;
        `CloseBuyingStoreHandler` → `Close`; `ClickBuyingStoreHandler` → open list;
        `TradeBuyingStoreHandler` → `IBuyingStoreService.Trade`.
  - [ ] `Map.Server/Handlers/Shop/SearchStoreHandler` (+ next-page, item-click) → `Search`/`SearchAll`.
  - [ ] `Map.Server/Handlers/Shop/Cash/CashShopOpenHandler` / `CloseHandler` / `TabHandler` →
        list emit; `CashShopBuyHandler` → `ICashShopService.BuyList` → `ZC_SE_PC_BUY_CASHITEM_RESULT`.
- [ ] Persistence: vending/buying autotrade state and cash-point deduction go through the existing
      services; no new char-side proto unless cash points are persisted via an existing RPC.

## Done criteria

- A player can open a vending stall (signboard visible to others), another player opens the list
  and buys; zeny/items transfer correctly and both sides get the right result/delete packets.
- A buying store can be set up with a zeny limit; a seller sells matching items into it and is paid.
- Search store returns matching vendors/buying-stores across the map; click-to-warp resolves.
- Cash shop lists tabs and a purchase deducts cash points and grants items (or fails with the right
  code on insufficient points / inventory full).
- No stub, no `// TODO`.

## Test plan

- Handler tests pinning: vending purchase exceeding stock/zeny → correct fail code; buying-store
  trade beyond zeny limit → reject; cash buy with insufficient points → fail code; search filters.
- Manual: two clients vend↔buy; buying-store sell; cash-shop purchase.

## Notes / gotchas

- Vending/buying item baskets are var-len with vendor/buyer account-id prefixes — follow the
  `CZ_PC_PURCHASE_ITEMLIST` var-len decode pattern in the existing `PurchaseItemListHandler`.
- `PurchaseReq2` is the newer wire shape; pick the variant matching the target PACKETVER.
- Vendor/buying-store signboards are map-visible entities — the open packet must spawn the
  signboard (`ZC_STORE_ENTRY` / `ZC_BUYING_STORE_ENTRY`) for nearby players, handled by the service.
- Cash points: confirm whether `CashShopService.BuyList` already deducts via an existing account
  RPC; if so, do not add a new proto — just call it.
