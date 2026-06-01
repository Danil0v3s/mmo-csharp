# SCRIPT-07 — Variable / register system consolidation (+ mapreg, arrays, getd/setd, getvariableofnpc)

> **Epic:** Scripting parity · **Status:** ❌ Not started · **Size:** L · **Player-visible:** no (correctness/data-integrity)
> **Depends on:** none · **Blocks:** SCRIPT-06 (instance var scope hangs off the consolidated service), SCRIPT-10 (town NPCs use perm/account vars + arrays)
> **Overlaps:** INFRA-07 (mapreg EF entity)

## Problem

There are **two parallel, unsynchronized player-variable systems writing the same DB tables.**
This is a live data-corruption hazard: a script that reads via one and writes via the other
sees stale data, and a logout flush from both can double-write or clobber.

1. `Map.Server/Persistence/PlayerStateService.cs` — loads/saves `perm`/`account`/`accountGlobal`
   into `PropertyBag`s on the session (`session.VarRegs`), using DbSets
   `CharacterRegistersNum`/`Str`, `AccountRegistersNum`/`Str`, `GlobalAccountRegistersNum`/`Str`.
   This is what `ctx.player.perm` / `.account` / `.accountGlobal` actually expose.
2. `Map.Server/Scripting/Vars/PlayerVarService.cs` — a `ConcurrentDictionary` cache with its own
   `LoadAsync`/`FlushAsync`, using **different entity types** `CharRegNumEntity` / `AccRegNumEntity`
   / `GlobalAccRegNumEntity` that map to the **same physical tables**. Nothing reads from the
   `PropertyBag`s; the two caches never share state.

Separately, the **mapreg** scope (`$var` permanent global, `$@var` temp global) is in-memory only:
`MapRegService.Init()`/`Final()` are `{ }` no-ops (`MapRegService.cs:45-46`), so `$`-vars never
persist. And **arrays** (`setarray`/`getarray`/`cleararray`/`copyarray`/`getarraysize`), the
dynamic-name indirection `getd`/`setd`, and `getvariableofnpc` are all missing or stubbed —
arrays are used pervasively by real scripts.

## Current state (C#)

- `Map.Server/Persistence/PlayerStateService.cs` — PropertyBag-based, the one wired to `ctx.player`.
- `Map.Server/Persistence/PlayerVarScope.cs` / `PlayerVarRegs.cs` — the PropertyBag scope + snapshot
  diff used by `PlayerStateService` save.
- `Map.Server/Scripting/Vars/PlayerVarService.cs` — the rival ConcurrentDictionary service
  (`ReadNum`/`WriteNum`/`ReadStr`/`WriteStr`/`LoadAsync`/`FlushAsync`), with `VarScope`
  enum (Char/CharTemp/Account/GlobalAccount). **Not used by `ctx.player`.**
- `Map.Server/Scripting/MapReg/MapRegService.cs:45-47` — `Init`/`Final`/`Reload` no-op SQL.
- `PlayerContext.cs:54-100` — exposes `session`(@var), `perm`(var), `account`(#var),
  `accountGlobal`(##var) as PropertyBags from `session.VarRegs`.
- Arrays / `getd` / `setd` / `getvariableofnpc` — no real implementation (the PropertyBag model
  has no array indexing; `NpcInfo.vars` is a PropertyBag with no cross-NPC access path).
- `Core.Database/Entities/` — has both `CharacterRegisters*` and `CharReg*` style entities (the
  duplication root). **No `mapreg` entity** (INFRA-07).

## rAthena reference (source of truth)

`script.cpp` + `map.cpp` (mapreg) + `pc.cpp` (pc_readreg/pc_setreg).

- Variable scopes by prefix: `name` = char perm, `name$` = char perm string, `@name` = char temp,
  `#name` = account local, `##name` = account global, `$name` = mapreg perm global, `$@name` =
  mapreg temp global, `.name` = NPC-scope, `.@name` = scope-local (per-execution), `'name` =
  instance scope. Index suffix `[n]` = array element; arrays are sparse maps `(varname, index)→val`.
- `map.cpp mapreg_setreg`/`mapreg_readreg` + `mapreg_db` — `$`-vars persist to the `mapreg` SQL
  table on `Final()` (and dirty-flush); `$@`-vars are runtime-only. `script_config.mapreg` table.
- `script.cpp:6291 BUILDIN(setarray)` — write a run of elements starting at `array[index]`.
  `:6341 cleararray` — set N elements to a value. `:6394 copyarray` — copy a range between arrays.
  `:6488 getarraysize` — highest set index + 1. `getarray` (project JS helper) — read the whole
  array as a JS array.
- `script.cpp:17938 BUILDIN(setd)` / `:18077 BUILDIN(getd)` — dynamic variable name: `setd("var"+i, v)`
  resolves the scope from the constructed name string at runtime. Must honor every prefix above
  and the `[index]` suffix.
- `script.cpp:20383 BUILDIN(getvariableofnpc)` — read a `.name` NPC-scope variable belonging to
  *another* named NPC. Needs cross-NPC scope lookup.

## Scope — every sub-system that must be touched

- [ ] **Pick ONE player-var backend and delete the other.** Recommended: keep the
      `PlayerStateService` PropertyBag path (it's what `ctx.player` already uses) and **remove**
      `Scripting/Vars/PlayerVarService.cs` + its rival `CharReg*/AccReg*/GlobalAccReg*` entities,
      OR migrate everything onto `PlayerVarService` and back the PropertyBags with it. Either way:
      **one cache, one set of EF entities, one flush.** Update DI registration so nothing resolves
      the removed service.
- [ ] **Array support** — extend the player-var model with `(name, index)` keys so
      `ctx.player.perm.foo[3] = x` (or an explicit `array` helper surface) works, plus the
      builtins `setarray`/`getarray`/`cleararray`/`copyarray`/`getarraysize`. PropertyBag alone
      can't index — either store arrays as a nested map under the bag key or move to the
      `(name,index)` keyed service.
- [ ] **getd/setd** — a name-parsing resolver that maps a runtime string `"$@foo[2]"` to
      (scope, name, index) and dispatches read/write to the right backing store (player scopes,
      mapreg, npc scope, instance scope).
- [ ] **mapreg persistence (INFRA-07)** — add the `mapreg` EF entity + configuration + migration;
      implement `MapRegService.Init()` (load `$`-vars from SQL) and `Final()`/dirty-flush (persist
      `$`-vars; `$@` stays runtime). Remove the "SQL load deferred" comments.
- [ ] **NPC scope + getvariableofnpc** — give `.name` vars a per-NPC store keyed by NPC, and a
      lookup-by-name path so `getvariableofnpc(.foo$, "OtherNpc")` resolves.
- [ ] **Instance scope hook** — expose a `VarScope.Instance` keyed by instance id for SCRIPT-06.
- [ ] **`scripts/types/api.d.ts`** — document the array + getd/setd + scope surface so authors
      use the supported API.

## Done criteria

- Exactly one player-variable cache/flush path exists; the rival service + duplicate entities are
  deleted; `dotnet build` has no reference to the removed type.
- A write via `ctx.player.perm.x = 5` is the same value any other read path sees in the same
  session, and persists across relog (one flush, no double-write).
- `setarray`/`getarray`/`cleararray`/`copyarray`/`getarraysize` behave per rAthena (sparse,
  index 0-based, `getarraysize` = highest+1).
- `setd("$g_"+i, v)` then `getd("$g_"+i)` round-trips and resolves the correct scope by prefix.
- `$`-mapreg values persist to SQL and reload on boot; `$@` reset on restart.
- `getvariableofnpc(.state, "GateKeeper")` reads that NPC's `.state`.
- **No `ScriptStub.Call` left in the var/array/mapreg surface; `MapRegService.Init/Final` no
  longer no-op.**

## Test plan

- `Map.Server.Tests/Scripting/VariableSystemTests.cs`: write via `ctx.player.perm`, read via the
  surviving service path, assert equality; flush + reload (EF in-memory or test DB) → persisted.
- Array tests: setarray/getarraysize/cleararray/copyarray exact semantics; sparse index gaps.
- getd/setd scope-resolution table test (one case per prefix, incl. `[index]`).
- mapreg persistence: set `$x`, run `Final`, new service `Init`, assert reload; `$@x` resets.
- A regression that would have caught the double-write: write perm via one path, ensure no second
  conflicting row is produced on flush.

## Notes / gotchas

- This is primarily a **de-duplication + correctness** ticket; resist adding behavior beyond what
  the two systems already attempt. The win is "one source of truth."
- The two entity families map to the same tables — when deleting one, make sure the EF model
  configuration for the surviving one still covers all columns (composite key on
  charId/accountId + key + index).
- `.@` scope-local vars are per-script-execution — they belong on the running `DialogContext`, not
  any persisted store; make sure getd/setd routes `.@` there and never to DB.
- Coordinate with INFRA-07 so the mapreg entity isn't added twice.
