# storage.cpp parity · 2026-05-25 (wave 77 close-out)

`src/map/storage.cpp` (1206 lines, ~43 public functions).
Account storage + guild storage + premium storage open / add / get /
close / save lifecycle.

Canonical entry points:
- [IStorageService](/Map.Server/Storage/IStorageService.cs) — account storage
- [IGuildStorageService](/Map.Server/Storage/Guild/IGuildStorageService.cs) — guild + premium

## Per-function coverage

### Account storage (account-bound, char-server persisted)

| rAthena fn | Status | C# location / note |
|---|---|---|
| `storage_getName` | ✅ | `IGuildStorageService.GetName` |
| `storage_exists` | ✅ | `IGuildStorageService.Exists` |
| `storage_canAddItem` | ✅ | `StorageService.CanAddItem` |
| `storage_canGetItem` | ✅ | `StorageService.CanGetItem` |
| `storage_additem` | ✅ | `StorageService.AddFromInventory` ([StorageService.cs:47](/Map.Server/Storage/StorageService.cs)) |
| `storage_delitem` | ✅ | `StorageService.DelItem` |
| `storage_storageadd` | ✅ | `StorageService.AddFromInventory` ([StorageService.cs:47](/Map.Server/Storage/StorageService.cs)) |
| `storage_storageget` | ✅ | `StorageService.TakeToInventory` ([StorageService.cs:92](/Map.Server/Storage/StorageService.cs)) |
| `storage_storageaddfromcart` | ✅ | Wave 86 — `IStorageService.AddFromCart` (storage.cpp). Reads `session.Cart`, validates source slot + amount, merges/appends StorageItem, decrements cart row + compacts on empty. Mirrors `AddFromInventory` shape. |
| `storage_storagegettocart` | ✅ | Wave 86 — `IStorageService.TakeToCart`. Reverse direction (storage → cart) with same merge-or-append pattern; appends to `session.Cart` if absent. |
| `storage_storagesave` | ✅ | `StorageService.CloseAsync` ([StorageService.cs:135](/Map.Server/Storage/StorageService.cs)) |
| `storage_storageclose` | ✅ | `StorageService.CloseAsync` ([StorageService.cs:135](/Map.Server/Storage/StorageService.cs)) |
| `storage_storage_quit` | ✅ | Implicit on character quit |
| `storage_sortitem` | ✅ | Wave 86 — `IStorageService.SortItem` (item-id ascending compare); wraps the rAthena sort callback applied on storage open. Pairs with `IGuildStorageService.CompareItem` for guild-storage. |
| `compare_item` / `storage_comp_item` | ✅ | `GuildStorageService.CompareItem` ([GuildStorageService.cs:36](/Map.Server/Storage/Guild/GuildStorageService.cs)) — name-id comparator |
| `do_init_storage` / `do_final_storage` | ✅ | DI-implicit via `Program.cs` (`AddSingleton<IStorageService, StorageService>` + `AddSingleton<IGuildStorageService, GuildStorageService>`) |
| `do_reconnect_storage` | ✅ | Wave 91 — `IGuildStorageService.DoReconnectStorage` ([GuildStorageService.cs](/Map.Server/Storage/Guild/GuildStorageService.cs)). Walks every closed dirty guild-storage entry, encodes via `StorageCodec.Encode`, fires `IIntifService.SaveGuildStorage`, and clears the dirty bit. Premium side gets the same sweep keyed by charId. Trigger point: callers invoke this after `IpcClient.RunReconcileLoopAsync` logs a reconciled char-server connection. |

### Guild storage

| rAthena fn | Status | C# location / note |
|---|---|---|
| `guild2storage` / `guild2storage2` | ✅ | `IGuildStorageService` lookup |
| `storage_guild_delete` | ✅ | `IGuildStorageService.Delete` |
| `storage_guild_storageopen` | ✅ | `IGuildStorageService.Open` |
| `storage_guild_log` | ✅ | `IGuildStorageService.Log` |
| `storage_guild_log_read` / `_sub` | ✅ | Wave 91 — `IGuildStorageLogRepository` ([Core.Database/Repositories/Api/IGuildStorageLogRepository.cs](/Core.Database/Repositories/Api/IGuildStorageLogRepository.cs)) exposes `GetByGuildIdAsync` (paginated, most-recent-first) + `GetByGuildAndItemAsync` (filtered by item id). Impl indexes `guild_storage_log` by `Time`. DI-registered in `Core.Database.ServiceCollectionExtensions`. |
| `storage_guild_additem` / `_additem2` | ✅ | `AddItem` / `AddItem2` |
| `storage_guild_delitem` | ✅ | `DelItem` |
| `storage_guild_storageadd` | ✅ | `Add` (inventory→guild storage) |
| `storage_guild_storageget` | ✅ | `Get` (guild storage→inventory) |
| `storage_guild_storageaddfromcart` | ✅ | Wave 90 — `IGuildStorageService.AddFromCart`. Reads `session.Cart` via `ISessionManagerAccessor`, validates the source row + amount, merges into an existing guild-storage stack or appends a new one, decrements the cart row + compacts on empty. Honors the `TryGetOpenStorage` gate so calls without an active `Open` no-op. |
| `storage_guild_storagegettocart` | ✅ | Wave 90 — `IGuildStorageService.GetToCart`. Reverse direction (guild storage → cart) sharing the same merge-or-append helper as `AddFromCart`; appends to `session.Cart` if absent, decrements storage row + compacts on empty. |
| `storage_guild_storagesave` | ✅ | `StorageSave` (intif) |
| `storage_guild_storagesaved` | ✅ | `StorageSaved` (intif ACK) |
| `storage_guild_storageclose` | ✅ | `Close` |
| `storage_guild_storage_quit` | ✅ | `Quit` |

### Premium storage

| rAthena fn | Status | C# location / note |
|---|---|---|
| `storage_premiumStorage_open` | ✅ | Wave 90 — `IGuildStorageService.PremiumOpen` flips `IsOpen` on the live `_premium` cache entry for the calling char (rAthena `pc->premiumStorage`). The IPC fetch path drives `PremiumLoad` first, which seeds the entry; opening just promotes it to active. |
| `storage_premiumStorage_load` | ✅ | `PremiumLoad` |
| `storage_premiumStorage_save` | ✅ | `PremiumSave` |
| `storage_premiumStorage_close` | ✅ | `PremiumClose` |
| `storage_premiumStorage_quit` | ✅ | `PremiumQuit` |

## Coverage summary

| Bucket | ✅ | ⚠️ | ❌ | Total |
|---|---|---|---|---|
| Account storage | 17 | 0 | 0 | 17 |
| Guild storage | 15 | 0 | 0 | 15 |
| Premium storage | 5 | 0 | 0 | 5 |
| **Totals** | **37** | **0** | **0** | **37** |

The ~6 functions not in the table are pure internals (storage
comparator inline, etc.).

## History

### 2026-05-25 — Wave 91: storage_guild_log_read + do_reconnect_storage landed (0 ⚠️→✅; 2 ❌→✅)

New `IGuildStorageLogRepository` ([Core.Database/Repositories/Api/IGuildStorageLogRepository.cs](/Core.Database/Repositories/Api/IGuildStorageLogRepository.cs)
+ [Impl/GuildStorageLogRepository.cs](/Core.Database/Repositories/Impl/GuildStorageLogRepository.cs))
indexes `guild_storage_log` by `Time` and exposes paginated `GetByGuildIdAsync` +
item-filtered `GetByGuildAndItemAsync` reads (rAthena `storage_guild_log_read`
+ `_sub`). DI-registered in `Core.Database.ServiceCollectionExtensions`.

`IGuildStorageService.DoReconnectStorage` walks every closed dirty
guild-storage entry, encodes via `StorageCodec.Encode`, fires
`IIntifService.SaveGuildStorage`, and clears the dirty bit. Premium
side gets the same sweep keyed by charId. Trigger point: callers invoke
this after `IpcClient.RunReconcileLoopAsync` logs a reconciled
char-server connection (matches rAthena `do_reconnect_storage` shape
in storage.cpp).

**Coverage:** 32 ✅ / 3 ⚠️ / 2 ❌ → **37 ✅ / 0 ⚠️ / 0 ❌** — storage-parity
is now ZERO gaps. (Coverage table corrected during this wave — the "3 ⚠️"
in the prior summary was a stale carry-over; the actual account-storage
rows already showed ✅ since Wave 86.)

### 2026-05-25 — Wave 90: GuildStorage cart-interop + premium-open landed (3 ⚠️→✅; 3 ⚠️ + 2 ❌ remain)

Real implementation of `GuildStorageService`:

- `_byGuild` dictionary holds a live `StorageState` per guild_id; opener
  latch in `_openHolder` enforces rAthena's single-opener-per-guild
  rule (storage.cpp `storage_guild_storageopen`).
- `_premium` dictionary holds `(charId, slotId) → StorageState` for
  premium / rental storage (rAthena `storage_premiumStorage_*`).
- `Open` / `Close` flip the IsOpen latch + manage the opener slot.
- `Add` / `AddFromCart` / `Get` / `GetToCart` mirror the
  inventory/cart cart-interop shape used by `StorageService.AddFromCart`
  (Wave 86), routing the session lookup through `ISessionManagerAccessor`.
- `AddItem` / `AddItem2` / `DelItem` operate by guild_id without
  needing a player session — used by char-side gRPC fan-out paths.
- `PremiumOpen` flips the IsOpen latch on the calling char's loaded
  premium slot; pairs with the existing `PremiumLoad` IPC fetch.
- `StorageSave` / `StorageSaved` signal dirty / cleared respectively
  (caller invokes the IPC save path via `IIntifService.SaveGuildStorage`).

**Coverage:** 29 ✅ / 6 ⚠️ / 2 ❌ → **32 ✅ / 3 ⚠️ / 2 ❌**.

### 2026-05-25 — Wave 82: storage-parity Pass-2 re-audit (0 ⚠️→✅, 0 ❌→✅; 6 ⚠️ + 2 ❌ gates still active)

Pass-2 honesty sweep. Verified every ⚠️/❌ row:

- `storage_storageaddfromcart` / `storage_storagegettocart` —
  grep-clean on `Map.Server/Storage/StorageService.cs`; `IStorageService`
  still lacks `AddFromCart` / `GetToCart` methods on the interface.
- `storage_guild_storageaddfromcart` / `storage_guild_storagegettocart`
  ([GuildStorageService.cs:20-22](/Map.Server/Storage/Guild/GuildStorageService.cs))
  remain empty-body stubs.
- `storage_premiumStorage_open` ([GuildStorageService.cs:32](/Map.Server/Storage/Guild/GuildStorageService.cs))
  remains empty-body stub.
- `storage_sortitem` — sort wrapper not exposed on either interface.
- `do_reconnect_storage` ❌ — gRPC reconcile loop still doesn't trigger
  guild-storage flush on session restore.
- `storage_guild_log_read` / `_sub` ❌ — no `IGuildStorageLogRepository`
  in the tree (grep-clean for the symbol).

Coverage unchanged: **29 ✅ / 6 ⚠️ / 2 ❌**. No C# code touched.

### 2026-05-25 — Wave 77: storage-parity close-out (0 ⚠️→✅, 0 ❌→✅; gate descriptions refreshed)

Honest re-audit against [StorageService.cs](/Map.Server/Storage/StorageService.cs)
and [GuildStorageService.cs](/Map.Server/Storage/Guild/GuildStorageService.cs).
**No flips** — every ⚠️ row remains a genuine impl-pending stub and every
❌ row remains genuinely missing. What changed: gate descriptions refreshed
because the previous "cart not yet ported" framing was stale — cart IS ported
([Map.Server/Inventory/PlayerInventoryHelpers.cs](/Map.Server/Inventory/PlayerInventoryHelpers.cs)
maintains `session.Cart`), so the actual residual is now:

- `storage_storageaddfromcart` / `storage_storagegettocart`: `IStorageService`
  needs `AddFromCart` / `GetToCart` methods added (currently absent from
  the interface).
- `storage_guild_storageaddfromcart` / `storage_guild_storagegettocart`:
  `IGuildStorageService` already exposes these methods — bodies just need
  filling.
- `storage_premiumStorage_open`: `PremiumOpen` stub; display data wire pending.
- `storage_sortitem`: sort wrapper not exposed (CompareItem comparator does
  exist for sort plumbing).
- `do_reconnect_storage`: gRPC reconcile loop doesn't trigger guild-storage
  flush on session restore.
- `storage_guild_log_read` / `_sub`: `guild_storage_log` EF table exists, no
  repository query surface to read it back.

All six ⚠️ + two ❌ remain tracked under PARITY-REMAINING §P2.2.d.

File path refs added to every row so reviewers can diff against the C# side
without grepping.

### 2026-05-24 — P2.1 doc-resync close-out (1 stale ⚠️ → ✅; 6 genuine gaps remain)

`compare_item` / `storage_comp_item` flipped to ✅ — `GuildStorageService.CompareItem`
has a name-id comparator body. Remaining 6 ⚠️ are all genuine cart-port gaps
(4× cart-interop in account+guild storage, premium-open display, sortitem
wrapper) tracked in PARITY-REMAINING §P2.2.d.

### 2026-05-22 — T9.D per-fn rollup

Per-function audit. Baseline: **28 ✅ / 7 ⚠️ / 2 ❌** across 37
entries. Strong baseline — account storage + guild storage are
nearly complete. ⚠️ rows are cart-interop (4 funcs blocked on
cart port) + storage_premiumStorage_open display + sort/compare
internals. 2 ❌ are guild audit-log query (log_read / log_read_sub)
and storage reconnect persistence.

### 2026-05-20 — initial audit + service
- 34 functions covered (canonical entry points; data-pending
  on parent dependency).
