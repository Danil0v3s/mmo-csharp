# INFRA-07 — Map-register SQL persistence ($globalvar / $@temp round-trip)

> **Epic:** Infra parity · **Status:** ❌ Not started · **Size:** M · **Player-visible:** indirect
> **Depends on:** none · **Blocks:** none · **Overlaps:** SCRIPT-07 (note below)

## Problem

Permanent server-scoped script variables (`$globalvar`) are lost on every map-server
restart, and `$@temp` arrays are never persisted at all. `MapRegService.Init()` and
`Final()` are empty no-ops. Any NPC script that stores state in a `$` variable — event
counters, MVP timers, donation totals, instance gates — resets to 0 on restart, silently
breaking quest/event scripts that assume the value survives.

## Current state (C#)

- `Map.Server/Scripting/MapReg/MapRegService.cs`:
  - `_ints` / `_strs` are `ConcurrentDictionary<int, long/string>` keyed by an **int hash
    only** (`:15-16`).
  - `ReadReg`/`SetReg`/`ReadRegStr`/`SetRegStr`/`DestroyReg` operate purely in memory
    (`:21-43`).
  - `Init() { /* SQL load deferred */ }` (`:45`), `Final() { /* SQL flush deferred */ }`
    (`:46`) — **both empty**. `Reload()` (`:47`) clears + re-Inits (so it just wipes).
  - `ConfigRead(...) => true` (`:48`).
- `Map.Server/Scripting/MapReg/IMapRegService.cs` — the contract is **int-keyed**
  (`ReadReg(int key)`, `SetReg(int key, long)`, etc.). There is no `varname` anywhere in
  the API.
- `Map.Server/Program.cs:496` — registered as a singleton; **no current callers** (the
  script engine that would call it isn't wired yet), so the int-key contract can still be
  adjusted without breaking consumers.

## rAthena reference (source of truth)

Canonical source is `mapreg.cpp` (~355 lines).

- Variables are keyed by a 64-bit `uid = reference_uid(add_str(varname), index)` —
  i.e. a `(name-string-id, array-index)` pair, **not** a free int. The string id comes
  from the interned-string table; the original `varname` is recoverable via `get_str(num)`.
- **Load — `script_load_mapreg` (`mapreg.cpp:181-225`):**
  `SELECT varname, index, value FROM mapreg`. For each row: `uid = reference_uid(
  add_str(varname), index)`; if `varname` ends in `$` → `mapreg_setregstr`, else
  `mapreg_setreg(strtoll(value))`. Skips duplicates.
- **Save — `script_save_mapreg` (`mapreg.cpp:230-259`):** only when `mapreg_dirty`. For
  each saved reg: `num = script_getvarid(uid)`, `i = script_getvaridx(uid)`,
  `name = get_str(num)`; `UPDATE mapreg SET value=... WHERE varname=name AND index=i`.
- **Insert on first set** (`mapreg_setreg`/`setregstr`, `:62`/`:121`): when not loading
  (`skip_insert == false`), a new var does an `INSERT` into `mapreg`; subsequent writes
  mark `mapreg_dirty` and ride the periodic `UPDATE` flush.
- **Autosave**: `MAPREG_AUTOSAVE_INTERVAL = 300*1000` (5 min) timer →
  `script_save_mapreg` (`:264`).
- **Final / Reload** (`:298-325`): both flush via `script_save_mapreg` first, then
  reload (Reload clears temp `$@` vars and reloads permanent ones).
- Only **permanent** `$` (and `#` account-perm) vars persist; `$@` temp vars are
  in-memory only (never written to SQL). Confirm the `#` account vars are handled
  elsewhere (they're account-scoped, char-server territory).

## Scope — every sub-system that must be touched

- [ ] **EF entity** `Core.Database/Entities/MapRegEntity.cs`: composite key
      `(VarName varchar(32), Index uint)`; `Value varchar(255)`. Table `mapreg` (match the
      rAthena schema exactly so a shared DB interops).
- [ ] **Configuration** `Core.Database/Configurations/MapRegEntityConfiguration.cs`:
      composite PK on `(VarName, Index)`, column lengths matching rAthena (`varname`
      varchar(32), `value` varchar(255)).
- [ ] **Repository** `IMapRegRepository` + `MapRegRepository`: `GetAllAsync()`,
      `UpsertAsync(varName, index, value)`, `DeleteAsync(varName, index)`. Register in DI.
- [ ] **Migration**: `dotnet ef migrations add DB-MapReg` from `Core.Database`.
- [ ] **Seed (optional)**: an empty `mapreg` table; no seed rows needed (scripts populate
      it at runtime). The table just needs to exist.
- [ ] **Round-trip the varname (the core problem).** The current int-key API cannot
      reconstruct `varname`/`index` for the SQL `UPDATE`. Resolve by ONE of:
  - **Preferred — store the name.** Have the int key be `reference_uid`-equivalent and
    keep a side map `int key → (string varName, uint index)` populated whenever the script
    engine first interns the variable. The service then writes `(varName, index, value)`
    rows. This requires the script-engine caller to register the name at intern time
    (a `RegisterName(int key, string varName, uint index)` hook, or richer overloads:
    `SetReg(string varName, uint index, long value)`).
  - **Alternative — name-carrying API.** Add overloads
    `SetReg(string varName, uint index, long value)` / `ReadReg(string varName, uint
    index)` that compute the int key internally AND record the name. Keep the int-key
    methods for hot reads. (No external consumers today, so the surface can grow freely.)
  - Do **not** try to reverse a hash back into a string — that's the gotcha that makes the
    empty `Init` look "deferred". The name must be captured at write time.
- [ ] **`Init()`**: load all `mapreg` rows; for each, intern the name + index → key,
      populate `_ints`/`_strs`, and seed the `key → (name,index)` side map. Strings are
      detected by `varName` ending in `$` (same as rAthena).
- [ ] **`SetReg`/`SetRegStr`/`DestroyReg`**: mark a dirty set; first write of a new var
      upserts, deletes remove the row. Track dirtiness like rAthena (`mapreg_dirty`).
- [ ] **Periodic flush + `Final()`**: flush dirty keys to SQL via the repo. Hook a 5-min
      autosave into the map-server tick/observer (mirror other timed flushes); `Final()`
      flushes on shutdown.
- [ ] **`Reload()`**: flush, clear temp (`$@`) vars, reload permanent rows.
- [ ] **`$@` temp vars stay in memory** — never written to SQL (gate the upsert on the
      `$`-permanent prefix).

## Done criteria

- A script `set $donations, 5000;` then a map-server restart → `$donations` reads 5000
  (loaded from `mapreg`).
- `$@temp` set during a session is never written to `mapreg` and is gone after restart
  (matches rAthena).
- String vars (`$name$`) round-trip via the `$`-suffix detection.
- Dirty writes flush on the 5-min autosave and on shutdown; deletes remove the row.
- No empty `Init()`/`Final()` and no "SQL load deferred" comment remain.

## Test plan

- `Core.Database` migration applies; `mapreg` table matches the rAthena column shape.
- `Map.Server.Tests/Scripting/MapRegServiceTests` (in-memory / sqlite repo):
  - Set int + string vars → Final flush → fresh service Init → same values.
  - `$@temp` set → Final → fresh Init → absent (not persisted).
  - DestroyReg removes the row; subsequent Init does not resurrect it.
  - Name round-trip: a var set via the name-carrying path reloads under the same name.

## Notes / gotchas

- **THE gotcha:** the in-memory dicts key by an int only; the SQL row needs the original
  `varname` + `index`. You cannot derive the name from the key — capture it at write time
  (side map or name-carrying overloads). This is exactly why the prior author left
  `Init`/`Final` empty.
- Match the **`mapreg` table schema** to rAthena (`varname` varchar(32), `index`
  uint, `value` varchar(255), composite PK) so a shared DB stays interoperable.
- **Overlaps SCRIPT-07** — that ticket covers the script-engine side ($ variable wiring).
  Coordinate the name-capture hook with whatever SCRIPT-07 defines for variable interning
  so the two don't implement competing key schemes. Whichever lands first defines the API;
  the other consumes it.
- `#` account-scoped permanent vars are **not** mapreg — they belong to char-server /
  account storage. Don't fold them in here.
