# GP-AUTOTRADE-RUNTIME — offline autotrade vendors/buyers stay open across logout

> **Epic:** gameplay · **Status:** ❌ Not started · **Size:** XL · **Player-visible:** yes
> **Depends on:** none · **Unlocks:** GP-VEND (autotrade), GP-BUYSTORE (autotrade)

## The deliverable

> A player can `@autotrade` their vending or buying shop and log out; the shop stays **on the map and
> sellable/buyable** for everyone else, and **rehydrates on server boot** — matching rAthena's offline
> autotrade vendors (`do_init_vending_autotrade` / `do_init_buyingstore_autotrade`).

## Player story / why it matters

GP-VEND made the live vending shop work end-to-end (open → others see the stall → browse → buy →
sold-out close); GP-BUYSTORE does the same for buying stores. The one feature both leave open is
**autotrade**: keeping the shop alive after the owner disconnects. This is not a vending- or
buyingstore-specific packet gap — it's a **shared runtime subsystem** that neither has:

1. A **headless on-map shop entity** — a player-detached presence (cart + zeny holder + no-op packet
   sink) that stays in the world after the socket closes, so buyers can still click and trade against
   the persisted cart, and the trade results still save.
2. The **`@autotrade` flow** — flag the session, persist the stall, then disconnect into the headless
   presence instead of a full logout.
3. **Boot hydrate** — load the persisted offline shops and respawn the headless presences.

Because this is a cross-cutting runtime (the DB tables already exist: `VendingEntity`/
`VendingItemEntity`/`BuyingStoreEntity` + cart persistence; the gap is the *runtime*), it is its own
ticket shared by both shop tickets rather than built twice.

## Current state — per layer

| Layer | Exists? | Where / what's missing |
|---|---|---|
| Persistence tables | ✅ | `Core.Database/Entities/VendingEntity.cs` + `VendingItemEntity.cs` + `BuyingStoreEntity.cs`; `IVendingRepository` |
| Cart persistence | ✅ | `CartInventoryEntity` / `ICartInventoryRepository` |
| Live shop service | ✅ | `VendingService` / `BuyingStoreService` (open/browse/buy work while online) |
| `@autotrade` command | ☐ | not present |
| Headless on-map shop presence | ☐ | no player-detached entity/session abstraction |
| Persist-on-autotrade-logout | ☐ (stub) | `VendingService.Reopen`/`InitAutotrade` + `BuyingStoreService` equivalents are log-only |
| Boot hydrate | ☐ | `InitAutotrade` doesn't read the rows / respawn |

## rAthena reference

- `rathena/src/map/vending.cpp` — `vending_reopen`, `do_init_vending_autotrade`, the `vending` /
  `vending_items` SQL.
- `rathena/src/map/buyingstore.cpp` — `buyingstore_reopen`, `do_init_buyingstore_autotrade`.
- `rathena/src/map/atcommand.cpp` — `@autotrade` (`atcommand_autotrade`): sets `sd->state.autotrade`,
  saves, and quits into the offline presence.
- The autotrade map-session is a normal `map_session_data` with `state.autotrade=1` whose `fd` is
  closed but whose `bl` stays mapped.

## Scope — every layer

- [ ] A headless/offline shop presence: a `MapSessionData`-like holder (cart + character-data/zeny +
      no-op packet sink) kept in the entity registry after the socket closes; buyers' purchase/sell
      paths resolve it the same as a live session.
- [ ] `@autotrade` command: flag the session, persist the stall + offers (`VendingEntity` +
      `VendingItemEntity`; buying-store equivalent), then transition the player into the headless
      presence instead of a full disconnect.
- [ ] Persist-on-mutate: keep the persisted rows in sync as the offline shop sells/buys.
- [ ] Boot hydrate: `InitAutotrade` reads the persisted shops + cart and respawns the headless
      presences (`vending_reopen` / `buyingstore_reopen`).

## Done criteria

- `@autotrade` a vending shop → log out → another player buys from it → the trade completes (cart/zeny
  persist) while the owner is offline.
- Restart the server → the offline shop reappears at its saved cell and is still sellable.
- Same for buying stores.

## Test plan

- Service tests: persist round-trip (open → `@autotrade` → row written → load → headless presence
  sellable).
- Integration: offline purchase against a hydrated vendor mutates the persisted cart.

## Notes

- Filed by GP-VEND (turn 3). Shared with GP-BUYSTORE (its FEATURE-36 autotrade). The live shops work
  without it; this is the offline/persistent layer. Sized XL — the headless-presence runtime is the
  bulk.
