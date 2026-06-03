# INFRA-08 — Game log SQL tables (pick / zeny / mvp / chat / branch / feeding / npc)

> **Epic:** Infra parity · **Status:** ❌ Not started · **Size:** L · **Player-visible:** no
> **Depends on:** none · **Blocks:** none

## Problem

The game-event audit logs (item pick/drop, zeny transfers, MVP drops, chat, dead-branch
summons, pet/homun feeding, NPC dialogue) are **not persisted** — every `GameLogService`
method just writes an `ILogger` line. rAthena writes these to dedicated SQL tables gated
by `log_config` filter masks. With no SQL logging, GMs have no audit trail for item
duping, RMT, zeny laundering, or harassment investigations, and the `log_config` filter
knobs (which item types / price / amount / refine thresholds to log) are ignored.

## Current state (C#)

- `Map.Server/Logging/GameLogService.cs` — every method is ILogger-only:
  `Atcommand` (`:18`), `Branch` (`:21`), `Cash` (`:24`), `Chat` (`:27`), `Feeding`
  (`:30`), `MvpDrop` (`:33`), `Npc` (`:36`), `Pick`/`PickPc`/`PickMob` (`:39-46`),
  `Zeny` (`:48`). `SetDefaults` (`:51`) empty; `ConfigRead(...) => true` (`:52`).
  Docstring admits the SQL tables "land when their EF Core entities ship" and notes
  `atcommandlog` is already covered by `Gm.AtCommandLogger`.
- `Map.Server/Program.cs:506` — `IGameLogService → GameLogService` singleton.
- **Already-shipped entities (reuse, do not recreate):**
  - `Core.Database/Entities/PickLogEntity.cs` — full picklog shape (`Time, CharId, Type,
    NameId, Amount, Refine, Card0..3, Option{Id,Val,Parm}0..4, UniqueId, Map, Bound,
    EnchantGrade`). Config `PickLogEntityConfiguration.cs` exists.
  - `Core.Database/Entities/ZenyLogEntity.cs` — `Time, CharId, SrcId, Type, Amount, Map`.
    Config `ZenyLogEntityConfiguration.cs` exists.
  - **No repositories** wire these into `GameLogService` yet
    (`Core.Database/Repositories/` has no PickLog/ZenyLog repo).
- `Map.Server/Gm/AtCommandLogger` already owns `atcommandlog`; `GameLogService.Atcommand`
  should defer to it (or be left ILogger-only) — do **not** duplicate.

## rAthena reference (source of truth)

Canonical source is `log.cpp` + the `log_config` struct (`map.hpp`). Schemas in
`rathena/sql-files/logs.sql`.

- **`log_config` filter (`log.cpp:25-43,46,147-164`):** item logging is gated by a
  bitmask `filter` (LOG_FILTER_ALL / HEALING / ETC_AMMO / USABLE / WEAPON / ARMOR / CARD /
  PETITEM / PRICE / AMOUNT / REFINE / CHANCE). `log_config.price_items_log`,
  `amount_items_log`, `refine_items_log`, `rare_items_log` are the thresholds for the
  PRICE/AMOUNT/REFINE/CHANCE filters. Each table also has its own enable flag
  (`log_config.branch`, `.zeny`, `.mvpdrop`, `.chat`, `.feeding`, `.npc`,
  `.enable_logs` mask for pick types).
- **Per-table inserts:**
  - `log_pick_sub` (`log.cpp:205-`) → `picklog` — gated by `enable_logs & type` and the
    item filter. Columns match `PickLogEntity`. `type` is a char enum
    (M/P/L/T/V/S/N/C/...).
  - `log_zeny` (`log.cpp:279-`) → `zenylog` — gated by `log_config.zeny` and
    `abs(amount) >= log_config.zeny` threshold. Columns: `time, char_id, src_id, type,
    amount, map`.
  - `log_mvpdrop` (`log.cpp:310-`) → `mvplog` — `mvp_date, kill_char_id, monster_id,
    prize (item id), mvpexp (bigint), map`. Gated by `log_config.mvpdrop`.
  - `log_branch` (`log.cpp:173-`) → `branchlog` — `branch_date, account_id, char_id,
    char_name, map`. Gated by `log_config.branch`.
  - `log_chat` → `chatlog` — `time, type ('O','W','P','G','M','C'), type_id, src_charid,
    src_accountid, src_map, src_map_x, src_map_y, dst_charname, message`. Gated by
    `log_config.chat` mask (per chat-type bits).
  - `log_feeding` → `feedinglog` — `time, char_id, target_id, target_class, type
    ('P','H','O'), intimacy, item_id, map, x, y`. Gated by `log_config.feeding` mask.
  - `log_npc` → `npclog` — `npc_date, account_id, char_id, char_name, map, mes`. Gated by
    `log_config.npc`.
  - `cashlog` is already partially modeled by the `Cash` method's intent; include it if
    the `Cash` call is exercised (columns: `time, char_id, type, cash_type, amount, map`).

`logs.sql` table column shapes (verified) — `branchlog`, `chatlog`, `feedinglog`,
`mvplog`, `npclog`, `picklog`, `zenylog` (see `rathena/sql-files/logs.sql:22,61,82,114,
129,176,233`).

## Scope — every sub-system that must be touched

- [ ] **EF entities** (reuse `PickLogEntity` + `ZenyLogEntity`; **add** the missing ones):
  - [ ] `MvpLogEntity` (mvplog: `MvpDate, KillCharId, MonsterId, Prize, MvpExp(long), Map`).
  - [ ] `BranchLogEntity` (branchlog: `BranchDate, AccountId, CharId, CharName, Map`).
  - [ ] `ChatLogEntity` (chatlog: `Time, Type, TypeId, SrcCharId, SrcAccountId, SrcMap,
        SrcMapX, SrcMapY, DstCharName, Message`).
  - [ ] `FeedingLogEntity` (feedinglog: `Time, CharId, TargetId, TargetClass, Type,
        Intimacy, ItemId, Map, X, Y`).
  - [ ] `NpcLogEntity` (npclog: `NpcDate, AccountId, CharId, CharName, Map, Mes`).
  - [ ] `CashLogEntity` (cashlog) if `Cash` is wired.
- [ ] **Configurations** for each new entity (table name + column lengths matching
      `logs.sql`; `MyISAM`/InnoDB per the rest of the schema).
- [ ] **Repositories** `IGameLogRepository` (one façade) or per-table repos with
      `InsertAsync(entity)`. Fire-and-forget inserts are acceptable (logging must never
      block the game loop) — queue + async flush, or `Task.Run`-style append that doesn't
      await on the tick thread. Reuse the existing `PickLog`/`ZenyLog` entities here.
- [ ] **Migration**: `dotnet ef migrations add DB-GameLogTables` from `Core.Database`.
- [ ] **Seed**: tables only (no rows). Add the `CREATE TABLE` equivalents via the
      migration; no `Tools.RathenaImporter` data needed (these are append-only logs).
- [ ] **`GameLogService` ConfigRead**: load the `log_config` masks + thresholds from the
      map-server config (`appsettings` `Server.Log.*` or a ported `log_athena.conf`).
      Store `filter`, `enable_logs`, per-table enable flags, and the price/amount/refine/
      rare thresholds. `SetDefaults()` populates rAthena defaults.
- [ ] **Gate + insert in each method**: `Pick`/`PickPc`/`PickMob` apply the item filter +
      `enable_logs & type` then insert `PickLogEntity`; `Zeny` applies the threshold then
      inserts `ZenyLogEntity`; `MvpDrop`/`Branch`/`Chat`/`Feeding`/`Npc` apply their enable
      flag then insert. Keep the ILogger line *and* add the SQL insert (don't drop the
      structured log).
- [ ] **`Atcommand`**: leave to `Gm.AtCommandLogger` — do not insert here (avoid
      double-logging). Document the delegation.
- [ ] **Inject** the repo(s) into `GameLogService` ctor + DI; the service is a singleton
      so use an `IServiceScopeFactory` to open a scoped `DbContext` per insert (mirror
      `RefineService` / `ProduceRecipeService` which take `IServiceScopeFactory`).

## Done criteria

- Picking up / dropping an item that passes the configured filter writes a `picklog` row
  with the correct `type` char, cards, options, refine, map; a filtered-out item writes no
  row.
- Zeny transfer above the threshold writes a `zenylog` row; below threshold writes none.
- MVP kill writes `mvplog`; dead-branch summon writes `branchlog`; chat writes `chatlog`
  with the right type char + coords; feeding writes `feedinglog`; NPC dialogue writes
  `npclog` — each only when its enable flag is on.
- `log_config` filter masks + thresholds are loaded from config, not hardcoded to "log
  everything".
- No method is ILogger-only (except `Atcommand`, which delegates to `AtCommandLogger`).

## Test plan

- `Core.Database` migration applies; the 5 new tables + reused 2 match `logs.sql` shapes.
- `Map.Server.Tests/Logging/GameLogServiceTests` (sqlite/in-memory repo):
  - Filter gating: an item below the price/amount/refine threshold → no picklog row; above
    → row with correct fields. Zeny threshold gate.
  - Each enable flag off → no row; on → row. Chat type char mapping (O/W/P/G/M/C).
  - `ConfigRead` parses masks + thresholds; `SetDefaults` yields rAthena defaults.

## Notes / gotchas

- **Reuse `PickLogEntity` + `ZenyLogEntity`** — they already exist with full column shape;
  only 5 new entities (+ optional cashlog) are needed.
- **Never block the game loop** on a log insert — singleton service + scoped DbContext per
  insert, fire-and-forget. A failed log insert must be swallowed (warn), never thrown into
  the tick.
- **`type` enums are single chars** (picklog M/P/L/..., zenylog T/V/P/..., chatlog
  O/W/P/G/M/C, feedinglog P/H/O). Store as `char(1)`/string(1); map the C# call's
  `char operation` / `byte who` / `string scope` to the right enum char (e.g. who `'P'`
  → picklog type 'P').
- **Residual sub-gap (do not fix here, just note):** `GuildStorage.Log()` only
  debug-logs; the `guild_storage_log` table is unwired (`IGuildStorageLogRepository`
  exists in `Core.Database/Repositories/` but isn't called from the guild-storage path).
  That belongs to the guild-storage flow, not this ticket.
- Don't double-log `atcommand` — `Gm.AtCommandLogger` already owns `atcommandlog`.
