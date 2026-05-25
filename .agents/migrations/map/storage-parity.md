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
| `storage_storageaddfromcart` | ⚠️ | Cart→storage transfer: cart surface lives in `PlayerInventoryHelpers` but `IStorageService` has no `AddFromCart` method yet. Tracked under PARITY-REMAINING §P2.2.d. |
| `storage_storagegettocart` | ⚠️ | Storage→cart transfer: same — no `IStorageService.GetToCart`. PARITY-REMAINING §P2.2.d. |
| `storage_storagesave` | ✅ | `StorageService.CloseAsync` ([StorageService.cs:135](/Map.Server/Storage/StorageService.cs)) |
| `storage_storageclose` | ✅ | `StorageService.CloseAsync` ([StorageService.cs:135](/Map.Server/Storage/StorageService.cs)) |
| `storage_storage_quit` | ✅ | Implicit on character quit |
| `storage_sortitem` | ⚠️ | Sort wrapper not exposed; `CompareItem` available on `IGuildStorageService` ([GuildStorageService.cs:36](/Map.Server/Storage/Guild/GuildStorageService.cs)). PARITY-REMAINING §P2.2.d. |
| `compare_item` / `storage_comp_item` | ✅ | `GuildStorageService.CompareItem` ([GuildStorageService.cs:36](/Map.Server/Storage/Guild/GuildStorageService.cs)) — name-id comparator |
| `do_init_storage` / `do_final_storage` | ✅ | DI-implicit via `Program.cs` (`AddSingleton<IStorageService, StorageService>` + `AddSingleton<IGuildStorageService, GuildStorageService>`) |
| `do_reconnect_storage` | ❌ | Char-reconnect persistence flush not wired. rAthena's reconnect loop walks `guild_storage_db` flushing dirty closed entries; map↔char gRPC reconcile loop ([IpcClient.RunReconcileLoopAsync](/Core.Server/IPC/IpcClient.cs)) doesn't yet trigger a guild-storage flush on session restore. |

### Guild storage

| rAthena fn | Status | C# location / note |
|---|---|---|
| `guild2storage` / `guild2storage2` | ✅ | `IGuildStorageService` lookup |
| `storage_guild_delete` | ✅ | `IGuildStorageService.Delete` |
| `storage_guild_storageopen` | ✅ | `IGuildStorageService.Open` |
| `storage_guild_log` | ✅ | `IGuildStorageService.Log` |
| `storage_guild_log_read` / `_sub` | ❌ | `guild_storage_log` EF table exists ([Core.Database/Configurations/GuildStorageLogEntityConfiguration.cs](/Core.Database/Configurations/GuildStorageLogEntityConfiguration.cs)) but no `IGuildStorageLogRepository` query surface — audit log read path is genuinely absent. |
| `storage_guild_additem` / `_additem2` | ✅ | `AddItem` / `AddItem2` |
| `storage_guild_delitem` | ✅ | `DelItem` |
| `storage_guild_storageadd` | ✅ | `Add` (inventory→guild storage) |
| `storage_guild_storageget` | ✅ | `Get` (guild storage→inventory) |
| `storage_guild_storageaddfromcart` | ⚠️ | `IGuildStorageService.AddFromCart` present but stubbed ([GuildStorageService.cs:20](/Map.Server/Storage/Guild/GuildStorageService.cs)); cart data exists, body deferred. PARITY-REMAINING §P2.2.d. |
| `storage_guild_storagegettocart` | ⚠️ | `IGuildStorageService.GetToCart` present but stubbed ([GuildStorageService.cs:22](/Map.Server/Storage/Guild/GuildStorageService.cs)). PARITY-REMAINING §P2.2.d. |
| `storage_guild_storagesave` | ✅ | `StorageSave` (intif) |
| `storage_guild_storagesaved` | ✅ | `StorageSaved` (intif ACK) |
| `storage_guild_storageclose` | ✅ | `Close` |
| `storage_guild_storage_quit` | ✅ | `Quit` |

### Premium storage

| rAthena fn | Status | C# location / note |
|---|---|---|
| `storage_premiumStorage_open` | ⚠️ | `IGuildStorageService.PremiumOpen` present but stubbed ([GuildStorageService.cs:32](/Map.Server/Storage/Guild/GuildStorageService.cs)); display data wire deferred. PARITY-REMAINING §P2.2.d. |
| `storage_premiumStorage_load` | ✅ | `PremiumLoad` |
| `storage_premiumStorage_save` | ✅ | `PremiumSave` |
| `storage_premiumStorage_close` | ✅ | `PremiumClose` |
| `storage_premiumStorage_quit` | ✅ | `PremiumQuit` |

## Coverage summary

| Bucket | ✅ | ⚠️ | ❌ | Total |
|---|---|---|---|---|
| Account storage | 13 | 3 | 1 | 17 |
| Guild storage | 12 | 2 | 1 | 15 |
| Premium storage | 4 | 1 | 0 | 5 |
| **Totals** | **29** | **6** | **2** | **37** |

The ~6 functions not in the table are pure internals (storage
comparator inline, etc.).

## History

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
