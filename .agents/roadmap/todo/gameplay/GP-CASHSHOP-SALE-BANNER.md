# GP-CASHSHOP-SALE-BANNER — timed limited-time-sale scheduling + live banner end-to-end

> **Epic:** gameplay · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** GP-CASHSHOP · **Unlocks:** none

## The deliverable

> A GM can **schedule a limited-time cash-shop sale** (`@sale`/sale db), players see the **live
> "on sale" banner + countdown + remaining stock**, can **refresh the remaining count**, and the
> sale **starts/ends on its timers** with the banner appearing/closing — live client; the schedule
> **persists** across restart (the `sales` table).

## Player story / why it matters

GP-CASHSHOP delivered the core cash shop (open/browse/buy) plus the **login** sale-notify emit
(`sale_notify_login` → `clif_sale_start` + `clif_sale_amount`, already wired through
`CashShopService.SaleNotifyLogin` → `ICashShopClientService.SendActiveSales`). What's still
missing is the rest of the timed-sale subsystem that *creates and drives* those sales:

1. The `@sale` admin command (add/remove a sale window) — `atcommand.cpp:atcommand_sale`.
2. Sale **persistence** — the `sales` table (`sale_read_db_sql` / insert / delete-expired-on-boot),
   so scheduled sales survive a restart (today `SaleAddItem` is in-memory + timer only).
3. The **refresh** request — `CZ_REQ_CASH_BARGAIN_SALE_ITEM_INFO` (0x09ac) → `clif_sale_amount`.
4. The **start/end broadcasts** to everyone (not just login): `clif_sale_start` on window open,
   `clif_sale_end` (`ZC_NOTIFY_BARGAIN_SALE_CLOSE` 0x09b3) on window close.

## Current state — per layer

| Layer | Exists? | Where / what's missing |
|---|---|---|
| Sale timers (in-memory) | ✅ | `Map.Server/Shop/Cash/CashShopService.cs` — `SaleAddItem`/`SaleFindItem`/`SaleRemoveItem`/`ActiveSaleNotifications` |
| Login notify emit | ✅ | `SaleNotifyLogin` → `ICashShopClientService.SendActiveSales` (0x09b2 + 0x09c4) — GP-CASHSHOP |
| `@sale` command | ☐ | not present |
| Sale persistence (`sales` table) | ☐ | no entity / repo; `SaleAddItem` is RAM + `Timer` only |
| Refresh handler | ☐ | `CZ_REQ_CASH_BARGAIN_SALE_ITEM_INFO` (0x09ac) not handled |
| Start/end broadcast | ☐ | only login notify; no on-open broadcast, no `ZC_NOTIFY_BARGAIN_SALE_CLOSE` (0x09b3) |

## rAthena reference

- `rathena/src/map/cashshop.cpp` — `sale_add_item`, `sale_remove_item`, `sale_start_timer`,
  `sale_end_timer`, `sale_notify_login`, `sale_read_db_sql`, the `sales` SQL table.
- `rathena/src/map/clif.cpp` — `clif_sale_start` (0x09b2), `clif_sale_amount` (0x09c4),
  `clif_sale_end` (0x09b3), `clif_parse_sale_refresh` (0x09ac), `clif_parse_sale_add`/`_remove`.
- `rathena/src/map/atcommand.cpp` — `atcommand_sale`.

## Scope

- [ ] **Data/persistence**: a `sales` entity + repository; `SaleAddItem` writes a row, boot loads
      live windows + deletes expired, restart re-arms the timers.
- [ ] **Admin command**: `@sale` add/remove driving `SaleAddItem`/`SaleRemoveItem`.
- [ ] **CZ handler**: `CZ_REQ_CASH_BARGAIN_SALE_ITEM_INFO` (0x09ac) → `clif_sale_amount`.
- [ ] **ZC emits**: start/end broadcast (`ZC_NOTIFY_BARGAIN_SALE_SELLING` on open to all,
      `ZC_NOTIFY_BARGAIN_SALE_CLOSE` 0x09b3 on end). The selling/amount packets already exist.
- [ ] **Wiring**: the start/end timers call the broadcast; account-limited-sale list optional.

## Done criteria

- `@sale add <item> <amount> <start> <end>` → at start the banner appears for everyone, the
  countdown ticks, the remaining-count refresh works, at end the close packet fires.
- Restart mid-window → the sale resumes (persisted) and the banner is restored on login.

## Test plan

- Service: sale persistence round-trip (add → row → reload → active window restored).
- Handler: refresh → amount packet; start/end → broadcast packets.

## Notes

- Filed by GP-CASHSHOP (turn 1). The login notify path + the sale-discounted buy price already
  work; this is the *scheduling + live banner* layer around them. `PACKETVER_SUPPORTS_SALES` in
  rAthena gates the whole subsystem.
