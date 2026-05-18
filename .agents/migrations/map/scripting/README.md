# Scripting — TypeScript migration plan

This subdir tracks the port of rAthena's NPC script system to a TypeScript-authored, JavaScript-executed runtime on the C# map server.

- [rathena-reference.md](rathena-reference.md) — distilled enumeration of rAthena's scripting language & runtime. Reference, not plan.
- [phase-1-loader.md](phase-1-loader.md) — **current focus.** Engine + entity placement. NPCs render but `onClick` is stubbed; closures are captured for Phase 2.
- [rathena-import.md](rathena-import.md) — inventory of what the rAthena → TS importer pulls in (warps / mob spawns / mapflags / inline shops) and what's deferred to the script-engine port (`duplicate(...)`, empty-body decoratives, full scripted NPCs).

The original sketch at [../npc.md](../npc.md) (hand-rolled mini-AST over rAthena syntax) is **superseded** by the decisions below.

## Scope decision: TypeScript authoring, JavaScript runtime

The rAthena scripting language is ~28k lines of bespoke parser + bytecode VM with ~1,000 builtins, coroutine-style suspension, 9 sigil-encoded variable scopes, sparse arrays, automatic coercion, and direct SQL. Faithfully porting all of that is ~6 engineer-months for a language nobody outside our project knows.

TypeScript gets us:

- **Typed authoring.** Every builtin call, player field, menu option, and event hook is type-checked. Refactor across the whole NPC corpus with confidence. This is the headline win.
- **`async/await` for suspension.** `await ctx.mes("hi")` / `await ctx.menu([...])` map directly to rAthena's `mes` / `menu` / `next` / `close` coroutine model. The host resolves the awaited Promise when the client clicks.
- **Native module system.** Shared logic (kafra dialogs, job-change flows, drop tables) lives in `lib/` and gets `import`ed — no custom "global function registry". A function is just an export.
- **Editor support out of the box.** VSCode, IntelliSense, Go-to-definition, refactor-rename — all free.
- **JSON-shaped object literals.** Shop items, drop tables, menu options read naturally.

What we give up:

- **Header-style rAthena declarations.** Authors write `registerNpc({ map, x, y, ... })` instead of `prontera,150,150,4\tscript\t…`. The rAthena corpus becomes reference material, not executable source.
- **rAthena scripts as-is.** Each NPC we want from rAthena's corpus has to be ported (hand or translator-assisted) to the new API. Trade made deliberately — the corpus is finite (~4,000 scripts) and a one-time cost.

## Runtime: Jint (default) or ClearScript (fallback)

| | Jint | ClearScript (V8) |
|---|---|---|
| Implementation | pure C# | wraps V8 |
| Native deps | none | libv8 |
| async/await | supported (newer) | first-class (V8 native) |
| Performance | adequate; mostly I/O-bound here | best in class |
| Sandboxing | strong; designed for embedding | possible but lower priority for us (trusted scripts) |
| Footprint | small | larger |

Default: **Jint**. Scripts are trusted (our team writes them), the workload is I/O-bound on dialog suspension (not compute-bound), and the no-native-deps story keeps deployment simple. If Phase 2 benchmarks show suspension-resume contention at scale, ClearScript is a drop-in alternative (same Promise-based suspension surface).

## API surface — five typed registrars

Each kind of script-managed content has its own registrar. No polymorphic `register()`; the type system enforces shape.

| Registrar | For | Shape |
|---|---|---|
| `registerNpc(...npcs)` | Scripted NPCs with a world position. Hooks (`onClick`, `onTouch`, `onInit`, `onTimer`, `onPCDeath`, …). | Closures + position + sprite |
| `registerFloatingNpc(...npcs)` | Event-only scripts with no world position. Hooks only (`onInit`, `onTimer`, `onClock`, `onPCLogin`, …). Replaces rAthena's `-` map sentinel. | Closures only, name-keyed for cross-script `doevent` dispatch |
| `registerShop(...shops)` | Declarative shops (zeny / cash / item / point / market via `kind` discriminator). No closures. | Position + item list + cost discriminator |
| `registerWarp(...warps)` | Declarative warp portals. No closures. | From / to / trigger area |
| `registerSpawn(...spawns)` | Declarative mob spawns. No closures (per-mob `onDeath` event labels handled by Phase 6). | Map + area + mob + amount + respawn |

**Every registrar takes varargs.** Idiomatic pattern: each NPC lives in its own file as an `export const`, and an `index.ts` aggregates them:

```ts
// scripts/npcs/cities/prontera/kafra.ts
import type { NpcRegistration } from "@server/api";
export const kafra: NpcRegistration = { map: "prontera", x: 146, y: 90, ... };

// scripts/npcs/cities/prontera/index.ts
import { kafra } from "./kafra";
import { libraryCurator } from "./library_curator";
import { guards } from "./guards";  // NpcRegistration[]
registerNpc(kafra, libraryCurator, ...guards);
```

Each NPC becomes a pure data value — testable, inspectable, mergeable as JSON. Registration is the orchestration step that an aggregator file owns. Single-arg form `registerNpc(kafra)` still works (varargs with one entry).

Global helper functions are **plain TS exports**, imported normally — no special registrar.

## File layout

### Server side

```
Map.Server/Scripting/
├── ScriptHost.cs                — Jint engine, lifecycle, hot-reload
├── INpcRegistry.cs / NpcRegistry.cs
├── NpcSpawnService.cs           — at-boot entity placement, mirrors MobSpawnService
├── Registrars/
│   ├── RegisterNpc.cs           — JS-callable; marshals JsValue → NpcRegistration record
│   ├── RegisterFloatingNpc.cs
│   ├── RegisterShop.cs
│   ├── RegisterWarp.cs
│   └── RegisterSpawn.cs
└── Records/                     — typed C# records mirroring the TS shapes
    ├── NpcRegistration.cs
    ├── ShopRegistration.cs
    ├── WarpRegistration.cs
    └── SpawnRegistration.cs

Map.Server/Handlers/
└── ContactNpcHandler.cs         — CZ_CONTACTNPC; Phase 1: stub, Phase 2: invokes onClick closure
```

### Scripts project (separate top-level dir)

```
scripts/
├── package.json
├── tsconfig.json
├── types/
│   └── api.d.ts                 — THE contract: registrars, NpcContext, PlayerContext, constants
├── lib/                         — shared logic, plain exports
│   ├── kafra.ts
│   └── ...
├── npcs/
│   ├── index.ts                 — `import "./cities"; import "./quests"; ...`
│   ├── cities/
│   │   ├── index.ts             — `import "./prontera"; import "./geffen"; ...`
│   │   ├── prontera.ts          — registerNpc(...) calls
│   │   └── ...
│   ├── quests/index.ts
│   └── jobs/index.ts
├── shops/index.ts               — registerShop(...) calls
├── warps/index.ts               — registerWarp(...) calls (when migrating from DB)
├── spawns/index.ts              — registerSpawn(...) calls (when migrating from DB)
├── main.ts                      — THE entry point: `import "./npcs"; import "./shops"; ...`
└── dist/                        — tsc output; Map.Server loads dist/main.js
```

**The runtime loads a single file: `dist/main.js`.** That file's side-effect imports walk the rest of the tree at evaluation time, triggering every `register*()` call. Adding a new NPC = add a `.ts` file + add one `import` line to the nearest `index.ts`. No C#-side discovery, no glob scanning, no manifest.

## What stays in DB vs what stays in scripts

| Content | rAthena form | Our storage |
|---|---|---|
| Map flags | `npc/re/mapflag/*.txt` | DB ✅ (`map_flag` — 2,251 rows). No script equivalent needed; pure config. |
| Warps | `npc/re/warps/*.txt` warp/warp2 lines | DB today (1,279 rows); `registerWarp()` available going forward. Both feed the same in-memory map at boot. Migration TBD. |
| Mob spawns | `npc/re/mobs/*.txt` monster lines | DB today (2,950 rows); `registerSpawn()` available. Same dual-source story. |
| Shops | `npc/re/merchants/*.txt` shop/cashshop/… lines | **`registerShop()` in TS.** Declarative but lives with the NPCs. |
| Scripted NPCs | `script` with body | **`registerNpc()` / `registerFloatingNpc()` in TS.** |
| Constants (`script_constants.hpp`) | header file | **Code-gen into `api.d.ts`** at build time. ~1,000 enum values; authors get autocomplete. |
| Item / equip / unequip scripts | embedded in `item_db.yml` | DB today (already in `item_db`); Phase 9 wires script-engine binding. |

The pattern: **anything declarative can live in either DB or scripts** — both forms coexist behind the same in-memory registry. Logic-bearing content lives only in scripts.

## Phases

| Phase | Goal | Status |
|---|---|---|
| **1 — Engine + render** | Jint host, scripts project skeleton, five `register*()` functions, NPC entity placement, visibility integration. `onClick` is stubbed. Hand-write 2–3 test NPCs to validate the path. See [phase-1-loader.md](phase-1-loader.md). | 🔁 In planning |
| 2 — Dialog execution | Wire `onClick` dispatch, suspension primitives (`mes`/`next`/`menu`/`select`/`input`/`close`/`close2`/`progressBar`/`sleep`), client-event resolution. First end-to-end interactive NPC. | ❌ |
| 3 — Player state builtins | `ctx.player.*` field reads/writes (zeny, hp, sp, baseLevel, jobLevel, …), `getItem`/`delItem`/`countItem`, `warp`, `savepoint`, `heal`, `getExp`, `sc_start`, etc. (~50 commands). | ❌ |
| 4 — Variable persistence | Wire the 4 player-scope tables (`session`/`perm`/`account`/`accountGlobal`) and 2 global-scope tables to `GameDbContext`. Expose to TS via typed proxy objects. | ❌ |
| 5 — Event hooks | `onInit`, `onTouch` (cell-bit + dispatch), `onTimer<ms>`, `onPCLogin`, `onPCDeath`, `onPCKill`, `onNPCKill`, `onClock<HHMM>`, `onAgitStart/End/Init`. | ❌ |
| 6 — Quest / achievement, party / guild / clan | Builtins for the social systems. | ❌ |
| 7 — Instances | `instance_*` builtins, per-instance variable scope. | ❌ |
| 8 — rAthena script translator | `Tools.RathenaTranspiler` — bulk-translates rAthena `.txt` files to TS modules. Mechanical 70–80% pass, `// FIXME` comments for the rest. | ❌ |
| 9 — Item / equip scripts | Stat-calc context, `bonus`/`bonus2`/`autobonus`/`bonus_script`. Different ctx shape (no dialog). | ❌ |
| 10 — Battleground / WoE / channels / atcommand bridge | The remaining ~200 builtins. | ❌ |

## Open decisions

- **Server-side hot-reload** during dev. File-watcher on `scripts/dist/`; on change, evict the affected NPCs from the registry and re-evaluate the module. Worth designing in from Phase 1.
- **`api.d.ts` source of truth** — hand-authored now; codegen from C# attributes later (Phase 5+ when builtins grow past ~30). Drift risk in the interim is manageable.
- **Warp / spawn DB migration to TS** — defer indefinitely. The DB-seeded entries work; `registerWarp/registerSpawn` exist for new content. No reason to force the existing 4,200 rows into TS unless authors start preferring the in-script form.
- **Floating-NPC name uniqueness** — `registerFloatingNpc({ name: "EventManager" })` is keyed by name for `doevent("EventManager::OnFoo")` dispatch. Names must be unique across the corpus; the registry rejects duplicates at boot.

## Conventions

- The rAthena reference path (`/Volumes/1TB/Projetos/rathena/...`) is the source of truth for parity decisions on builtins and semantics. Authoring style is ours.
- Each phase has its own doc with **Done / Pending / Acceptance / History** sections.
- When a phase completes, append a History entry and update the table above.
- `scripts/` is its own npm project. CI runs `tsc --noEmit` to type-check; the compiled `dist/` is either committed or built fresh per deploy (TBD in Phase 1).

## History

- **2026-05-17** — **Pivot from rAthena-text loader to Jint+TS modules.** Authors write `.ts`; runtime loads `dist/main.js`; five typed registrars (`registerNpc`/`registerFloatingNpc`/`registerShop`/`registerWarp`/`registerSpawn`) replace any custom header syntax. rAthena scripts become reference material — translator deferred to Phase 8. Phase 1 still ends with NPCs visible in-world; the source of NPCs is now hand-written TS, not the rAthena tree. See [phase-1-loader.md](phase-1-loader.md).
- **2026-05-17** — Scope decision recorded: Lua, not a port of the rAthena VM. (Superseded same day by TS decision above.) Subdir created. Original sketch at [../npc.md](../npc.md) superseded.
