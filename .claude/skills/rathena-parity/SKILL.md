---
name: rathena-parity
description: Drive end-to-end rAthena → C# migration for one rAthena source file. Use this skill whenever the user invokes `/rathena-parity <path>`, mentions a specific rAthena file they want to port (e.g. "let's work on skill.cpp", "scan mob.cpp for parity gaps", "finish the status.cpp port"), asks to "complete pc.cpp parity" or any other rAthena source-file parity goal, or requests an audit doc / wave plan for a `src/map/*.cpp` file. Also trigger when the user wants to convert stubs from a previous parity pass into real implementations. Make sure to use this skill whenever rAthena source files (.cpp/.hpp in `/Volumes/1TB/Projetos/rathena/`) are mentioned even if the user doesn't explicitly say "parity" — driving migration work IS the project goal, and this skill captures the standard workflow we've been running across sessions.
---

# rAthena → C# parity driver

You are continuing a long-running migration of rAthena (C++ MMO server) to a C# stack at `/Volumes/1TB/Projetos/mmo-csharp/`. The project conventions live in `/Volumes/1TB/Projetos/mmo-csharp/CLAUDE.md`; the migration audit docs live in `/Volumes/1TB/Projetos/mmo-csharp/.agents/migrations/`.

This skill captures the seven-step workflow we've used to port `atcommand.cpp` and `pc.cpp` to a canonical C# entry-point for every function. Each step builds on the previous one and the whole loop produces multiple git commits plus a living audit doc.

## When to use

- User asks to port, audit, or "drive parity" for any rAthena `.cpp` / `.hpp` file under `/Volumes/1TB/Projetos/rathena/src/map/`.
- User invokes `/rathena-parity <path>` directly.
- User wants to convert previously-stubbed parity work into real implementations.
- User wants a wave plan, audit doc refresh, or coverage summary for a specific rAthena source file.

## The seven-step loop

Run these in order. Each step has a clear deliverable and the user expects a commit after each meaningful wave.

### 1. Enumerate the rAthena surface

Use `scripts/enumerate.sh <rathena-file>` (bundled with this skill) to list every public function in the file. The script understands the rAthena naming conventions (`pc_*`, `skill_*`, `mob_*`, `status_*`, `clif_*`, etc.) and prints one function per line.

For a `.cpp` file, also peek at the companion `.hpp` for exported types, enums, and macros — those become C# enums / interfaces.

### 2. Read the existing audit doc

Look for `/Volumes/1TB/Projetos/mmo-csharp/.agents/migrations/map/<filename-without-ext>-parity.md` first. If it exists, that's your starting point: it already has the function inventory and the latest coverage table. Read it end-to-end so the new history entry layers cleanly on top.

If the audit doc doesn't exist yet, copy the skeleton from `assets/audit_doc_template.md` (bundled) and fill in the rAthena file path, line count, and subsystem-grouped function list. The audit doc shape is taken from `.agents/migrations/map/pc-parity.md` — keep it as the canonical exemplar.

### 3. Scan the existing C# surface

For every function from step 1, grep `/Volumes/1TB/Projetos/mmo-csharp/Map.Server/` (and `Core.Server/` for shared packets) to find an existing C# entry point. Look at:

- Service interfaces (`I*Service.cs`).
- Handler classes (`Handlers/`).
- Static helpers and extension methods.
- Entity field surface (state can be on `PlayerEntity` / `MobEntity` / session).

Categorise each rAthena function:

- ✅ **implemented** — full or near-full parity. Cite the C# file + line.
- ⚠️ **partial** — exists but with gaps. Document the gap inline.
- ❌ **missing** — no C# equivalent.

### 4. Update the audit doc

Refresh the per-subsystem coverage tables with the categorisation from step 3. Add a `### YYYY-MM-DD — <wave name>` history entry describing the snapshot. The doc is a living artifact — every wave appends a history entry rather than rewriting the whole tree.

The doc-update commit can land independently of code changes; do it before any implementation work so a fresh reviewer can read where things stand.

### 5. Plan the implementation in waves

Group missing/partial items by gameplay impact:

- **High** — directly visible to the player or required for combat correctness (e.g. damage formulas, equip sync, skill cast).
- **Medium** — server-side behavior that affects gameplay but not the wire (e.g. exp curves, drop tables, mob AI).
- **Low** — admin / lifecycle / niche (e.g. GM commands behind permission gates, debug logging).

Within each tier, pick 5-10 items per wave so each commit stays reviewable. Document the plan in the audit doc under `## Implementation plan` so the user can sign off before the code lands.

### 6. Implement waves following the conventions

For each item in the plan:

- Add the canonical C# entry point as an `I*Service` interface + concrete impl. Match the rAthena function signature shape.
- Live in the right subsystem folder: `Status/`, `Combat/`, `Inventory/`, `Skills/`, `Movement/`, `Mob/`, `Spawn/`, `Items/`, `Visibility/`, `Scripting/`, `Gm/`, `World/`.
- Cite the rAthena `file:line` in an inline comment so reviewers can diff against C++.
- Register in `/Volumes/1TB/Projetos/mmo-csharp/Map.Server/Program.cs` via `builder.Services.AddSingleton<I…, …>()`.
- Honor CLAUDE.md conventions: 1:1 parity, no in-memory shortcuts for persisted state, packet handlers via `[PacketHandler]` attribute, repositories injected directly, `ILogger<T>` for logging.
- Add tests under `/Volumes/1TB/Projetos/mmo-csharp/Map.Server.Tests/` when adding behavior with parity implications.
- For items whose backend isn't ported yet, ship a working in-memory + log implementation so the API surface is canonical. Document the "data-pending" path inline. Never leave a `_logger.LogDebug("X stub: ...")` — either the impl works for the data it has, or the missing data dependency is named in the comment.

Commit per wave with a message that names the wave + the rAthena `file:line` references touched.

### 7. Validate + restart

After each wave:

- `dotnet build Map.Server --nologo --no-restore 2>&1 | grep -E "error CS|Error\(s\)"` — must say `0 Error(s)`.
- `dotnet test --nologo --filter "FullyQualifiedName!~PacketReplayTests" 2>&1 | tail -8` — every suite must pass (currently 435 tests; the replay test fails on pre-existing diff and is filtered out).
- If servers are running, restart `Map.Server` so the live stack picks up the new bits.

## Conventions cheat sheet

| Topic | Convention |
|---|---|
| File layout | One service per file. Interface + impl in same folder. |
| DI | All services registered in `Program.cs`. Lazy `IServiceProvider` for cycles. |
| State on entity | Long-lived per-PC fields → `PlayerEntity`. Per-session → `MapSessionData`. |
| Per-character persisted data | `CharEntity` columns via EF Core repos; no `ConcurrentDictionary` shortcuts. |
| Logging | `ILogger<T>`. Info for lifecycle, warn for recoverable, error for unexpected. |
| Threading | Game loop is single-threaded. Don't add locks in handler code; queue to game loop. |
| Packets | `[PacketHandler]` attribute. New packets in `Core.Server/Packets/`. |
| Commits | Per wave. rAthena file:line citations in the body. Sign with the Co-Authored-By line from CLAUDE.md. |
| Tests | `Map.Server.Tests/` for behavior. Mock `IServerConnectionService`, not `ServerConnectionManager`. |

## The audit doc shape

The exemplar is [pc-parity.md](/Volumes/1TB/Projetos/mmo-csharp/.agents/migrations/map/pc-parity.md). Critical sections:

1. **Header** — rAthena file path + line count + function count + one-line subsystem summary.
2. **Status legend** — three symbols (✅ ⚠️ ❌). Keep this stable so readers can grep.
3. **Subsystem coverage** — one table per subsystem (Lifecycle, Skill, Inventory, etc.). Each row is `| rAthena fn | Status | C# location/note |`.
4. **Coverage summary** — small roll-up table at the end of the function lists: bucket / done / partial / missing.
5. **Implementation plan** — numbered wave list.
6. **History** — reverse-chronological `### YYYY-MM-DD — <wave>` entries. Always append; never rewrite.

When in doubt, mirror pc-parity.md.

## Special situations

**No matching audit doc**: copy `assets/audit_doc_template.md` and fill the header + the function inventory from step 1. First-wave commit can be doc-only.

**Function exists but with subtle gaps**: mark ⚠️ partial, describe the gap in one sentence, and add it to the plan. Don't expand into a sub-table — readers skim.

**Backend genuinely missing**: ship the canonical entry point with a documented "data-pending" no-op (not a stub-log). The interface still exists so future calls don't have to rewrite call sites.

**Long sweep**: when the user says "complete X parity" or "finish all stubs", treat it as a multi-wave directive. Spawn TaskCreate entries for each wave so progress is visible, and commit after each one.

## Bundled helpers

- `scripts/enumerate.sh` — list rAthena functions from a .cpp/.hpp file.
- `scripts/coverage.sh` — quick grep over the C# tree for a given rAthena symbol.
- `assets/audit_doc_template.md` — skeleton for new per-file parity docs.

## What "done" looks like

A complete parity pass on a single rAthena file ends with:

- An audit doc whose final table has zero ❌ rows. All entries are ✅ or ⚠️ with documented gaps.
- One commit per wave, citing rAthena file:line in the body.
- A final history entry summarising the pass + the gap list for follow-up.
- All tests green; Map.Server boots; live client can exercise the affected gameplay if applicable.
- Zero `stub:` log lines remain in the touched services.

When you hit that state, restart the Map.Server, summarise the changes for the user, and surface what dependent subsystems would unblock the remaining ⚠️ entries.
