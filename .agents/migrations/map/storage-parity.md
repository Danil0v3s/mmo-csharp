# storage.cpp parity · 2026-05-22 (T9.D — per-fn rollup)

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
| `storage_additem` | ✅ | `StorageService.AddFromInventory` |
| `storage_delitem` | ✅ | `StorageService.DelItem` |
| `storage_storageadd` | ✅ | `StorageService.AddFromInventory` |
| `storage_storageget` | ✅ | `StorageService.TakeToInventory` |
| `storage_storageaddfromcart` | ⚠️ | Cart→storage; cart not yet ported. PARITY-REMAINING §P2.2.d |
| `storage_storagegettocart` | ⚠️ | Storage→cart; cart not yet ported. PARITY-REMAINING §P2.2.d |
| `storage_storagesave` | ✅ | `StorageService.CloseAsync` |
| `storage_storageclose` | ✅ | `StorageService.CloseAsync` |
| `storage_storage_quit` | ✅ | Implicit on character quit |
| `storage_sortitem` | ⚠️ | Sort wrapper not exposed; CompareItem available. PARITY-REMAINING §P2.2.d |
| `compare_item` / `storage_comp_item` | ✅ | `GuildStorageService.CompareItem` (name-id comparator) |
| `do_init_storage` / `do_final_storage` | ✅ | Implicit via DI |
| `do_reconnect_storage` | ❌ | Char-reconnect persistence not wired |

### Guild storage

| rAthena fn | Status | C# location / note |
|---|---|---|
| `guild2storage` / `guild2storage2` | ✅ | `IGuildStorageService` lookup |
| `storage_guild_delete` | ✅ | `IGuildStorageService.Delete` |
| `storage_guild_storageopen` | ✅ | `IGuildStorageService.Open` |
| `storage_guild_log` | ✅ | `IGuildStorageService.Log` |
| `storage_guild_log_read` / `_sub` | ❌ | Audit log query not wired |
| `storage_guild_additem` / `_additem2` | ✅ | `AddItem` / `AddItem2` |
| `storage_guild_delitem` | ✅ | `DelItem` |
| `storage_guild_storageadd` | ✅ | `Add` (inventory→guild storage) |
| `storage_guild_storageget` | ✅ | `Get` (guild storage→inventory) |
| `storage_guild_storageaddfromcart` | ⚠️ | Cart→guild storage (cart pending). PARITY-REMAINING §P2.2.d |
| `storage_guild_storagegettocart` | ⚠️ | Guild storage→cart (cart pending). PARITY-REMAINING §P2.2.d |
| `storage_guild_storagesave` | ✅ | `StorageSave` (intif) |
| `storage_guild_storagesaved` | ✅ | `StorageSaved` (intif ACK) |
| `storage_guild_storageclose` | ✅ | `Close` |
| `storage_guild_storage_quit` | ✅ | `Quit` |

### Premium storage

| rAthena fn | Status | C# location / note |
|---|---|---|
| `storage_premiumStorage_open` | ⚠️ | Premium storage display data-pending. PARITY-REMAINING §P2.2.d |
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
