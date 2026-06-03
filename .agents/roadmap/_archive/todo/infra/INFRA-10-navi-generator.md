# INFRA-10 — Navi list generator (navi.cpp) — DEFERRAL DECISION

> **Epic:** Infra parity · **Status:** ⏸️ Deferred (decision ticket) · **Size:** XL (if pursued) · **Player-visible:** no
> **Depends on:** none · **Blocks:** none

## Problem

`NaviService` is an all-no-op stub: `CreateLists` returns `false`, every `Write*` method
is empty, `PathSearch` returns `false`, `MapType` returns `0`. This corresponds to
rAthena's `navi.cpp`, a **build-time** tool that exports navmesh / distance files for the
client's in-game navigation UI ("click a destination, get walk directions"). The question
this ticket answers is **not** "how do we implement it" but **"should we, and when"** —
because shipping clients already bundle rAthena-generated navi files, so the runtime
server does not need to regenerate them for players to navigate.

## Current state (C#)

- `Map.Server/Navi/NaviService.cs`:
  - `CreateLists(outputDirectory)` (`:15-24`) — logs "deferred (P2.2.e)" and `return
    false`.
  - `PathSearch(...) => false` (`:25`), `MapType(...) => 0` (`:26`),
    `FileExists(...)` (`:27`) is the only real method.
  - `WriteHeader`/`WriteFooter`/`WriteMapHeader`/`WriteMap`/`WriteMapDistance`/
    `WriteMapDistances`/`WriteMapDistHeader`/`WriteNpc`/`WriteNpcDistance`/
    `WriteNpcDistances`/`WriteObjectLists`/`WriteSpawn`/`WriteWarp` (`:29-41`) — **all
    empty**.
  - Docstring: "No-op until the generator ports — entry points are here so the
    GM-command tier (`@navi_generate`) has a service to invoke."

## rAthena reference (source of truth)

Canonical source is `navi.cpp` (`rathena/src/map/navi.cpp`).

- `navi_create_lists()` is the entry point. It writes two LUA-like data files consumed by
  the client's navigation system:
  - **`navi_map` / object lists** — for every map: header, the map entry, then every NPC,
    warp, and monster spawn on that map (`write_map`, `write_npc`, `write_warp`,
    `write_spawn`, `write_object_lists`).
  - **`navi_link` / distance tables** — pairwise reachability/distance between maps and
    between NPCs, computed by BFS over walkable cells and warp-graph traversal
    (`write_map_distances`, `write_npc_distances`, the `write_*_distance` row writers).
- It is invoked **at server boot only when explicitly enabled** (a `--generate-navi` style
  flag / config), runs once, writes the files, and is otherwise dormant. It is not part of
  the gameplay tick. The client reads the generated files locally; the server does not
  serve navigation at runtime.

## Decision (recommended): KEEP DEFERRED / WON'T-DO for runtime

**Recommendation: do not implement for the runtime server.** Rationale:

1. **No player-visible gap.** Live clients ship with rAthena-generated navi data. A C#
   server that doesn't regenerate them changes nothing a player sees — navigation still
   works off the bundled files.
2. **It's a build tool, not gameplay.** Porting it buys map-tooling parity, not server
   behavior parity. Every other INFRA ticket fixes a thing a player can do; this one
   doesn't.
3. **High cost, narrow benefit.** A faithful port needs: the full map-cell walkability
   model (gat/mapcache reader), the NPC/warp/spawn registries enumerated per map, a BFS
   path/distance engine over cells, the warp-graph, and the exact LUA-ish output format
   the client parser expects (byte-for-byte, or the client rejects it). That's an XL
   effort gated on map-cache + NPC/warp/spawn data being fully loaded — most of which is
   only needed *by this tool*.

**Action for this ticket:** replace the "deferred per PARITY-REMAINING.md §P2.2.e"
phrasing in `NaviService` with an explicit **won't-do-for-runtime** rationale comment
(pointing at this ticket), so future readers don't mistake it for unfinished work. Keep
the service + entry points (they satisfy the `@navi_generate` GM-command binding and the
`INaviService` contract).

## Tripwire — when this flips to "do it"

Reopen and implement only if **any** of these become true:

- We ship a **custom map** (new field/dungeon not in stock rAthena) and want the client's
  navigation UI to route to/within it — the bundled navi files won't know about it.
- We add/move **NPCs or warps** that players must navigate to via the in-game "navi"
  button and the stale bundled data sends them wrong.
- Product explicitly wants **server-authoritative pathfinding** (e.g. server-side
  auto-walk validation, bot detection on path plausibility) that needs the distance/BFS
  engine `navi.cpp` builds.
- We adopt a **client build** that expects freshly-generated navi files and refuses the
  bundled ones.

## Scope — IF pursued (do not start without the tripwire)

- [ ] Map-cell walkability reader (mapcache / gat) — likely the largest dependency; may
      already exist in `Map.Server/Map*` cell loading.
- [ ] Enumerate NPCs, warps, monster spawns per map from the loaded registries.
- [ ] BFS distance engine over walkable cells + warp-graph traversal (`write_map_distances`
      / `write_npc_distances`).
- [ ] Port the `Write*` methods to emit the exact client-expected LUA-ish format — verify
      byte-compatibility against a reference rAthena-generated file.
- [ ] Wire `CreateLists` to run once on demand (boot flag / `@navi_generate`), never on the
      gameplay tick.
- [ ] Golden-file test: generated output matches a known-good rAthena navi file for a
      stock map subset.

## Done criteria (this ticket, as a decision)

- `NaviService` carries an explicit won't-do-for-runtime rationale comment referencing
  this ticket (not a vague "deferred"), and the tripwire list above is recorded.
- The roadmap README reflects INFRA-10 as a deliberate deferral, not an open gap.
- No further code change unless a tripwire fires.

## Test plan

- None for the deferral decision (no behavior change). If/when pursued, the golden-file
  comparison above is the acceptance test.

## Notes / gotchas

- This is the **only** INFRA ticket that is correctly a no-op today — don't let a future
  "implement all stubs" sweep port it reflexively. The cost/benefit is genuinely
  unfavorable for a runtime server.
- If a custom map ever ships, this jumps from XL-deferred to required — flag it at that
  moment, not before.
- Keep the `INaviService` surface intact regardless; removing it would break the
  `@navi_generate` GM command binding.
