# SCRIPT-10 — Bulk NPC conversion (transpiler + registerDuplicate + core town NPCs)

> **Epic:** Scripting parity · **Status:** ❌ Not started · **Size:** XL · **Player-visible:** yes
> **Depends on:** SCRIPT-01 (input/close2/cutin), SCRIPT-02 (warp/savePoint/items/storage/heal/job),
>   SCRIPT-03 (onInit/onTouch/timers), SCRIPT-07 (perm/account vars + arrays); SCRIPT-04/05/08 for
>   quest/party/effect-using NPCs · **Blocks:** none (this is the payoff ticket)
> **HARD-BLOCKED:** a kafra needs working warp/savePoint/storage/menu; a job-changer needs
>   jobChange/skill grant; a tool-dealer needs shop + delItem. Do NOT start the town-NPC phase
>   until SCRIPT-01/02/03 are green.

## Problem

There are **zero real game NPCs.** `scripts/npcs/` contains only 4 dev-test fixtures under
`_dev_test/` (event_manager, phase1_test, kafra_test). The declarative corpus (warps 1289,
spawns 3024, mapflags 2373, inline shops 107) was bulk-imported and functions, but every
script-bodied NPC — kafras, tool dealers, job changers, quest givers, the entire `npc/re/**`
tree (~1100+ `.txt` files, the actual game) — is unconverted. A player logging in finds an
empty world: warps work, mobs spawn, but no one to talk to.

This ticket builds the machinery to convert the rAthena script corpus at scale, plus the
`duplicate()` mechanism (the rAthena corpus has ~4684 `duplicate(...)` placements in `re/`;
~10736 across the full tree), and hand-converts the core town NPCs as the proof + template.

## Current state (C#)

- `scripts/tools/import_rathena.py` — handles **declarative lines only** (warp/monster/mapflag/
  inline-shop); its docstring explicitly says "Script-bodied warps … are skipped — full NPC
  scripts that the TypeScript engine can't host yet." So all `script`-type NPCs are dropped.
- `scripts/npcs/` — only `_dev_test/` fixtures + `index.ts`.
- `scripts/types/api.d.ts:41-68` — registrars: `registerNpc`, `registerFloatingNpc`, `registerShop`,
  `registerWarp`, `registerSpawn`, `registerMapFlag`, `registerItem`, `registerCombo`. **No
  `registerDuplicate`.**
- `Map.Server/Scripting/Registrars/RegistrarBindings.cs` — binds the 8 globals. No duplicate binding.
- `Map.Server/Scripting/Records/NpcRegistration.cs` / `NpcRegistry.cs` — the registration record +
  store; a duplicate would reference a template's hooks + own its own placement (map/x/y/dir/sprite).

## rAthena reference (source of truth)

`npc.cpp` parser + the `npc/re/**.txt` corpus.

- NPC script syntax: `<map>,<x>,<y>,<dir>%TAB%script%TAB%<name>%TAB%<sprite>,{ <script body> }`.
  The body is the imperative dialect transpiled to a TS `onClick` hook (mes/next/menu/close +
  the builtins ticketed in SCRIPT-01..09). Labels `OnInit`/`OnTouch`/`OnTimer<ms>`/`OnPC*` become
  the corresponding `registerNpc({ onInit, onTouch, onTimer, … })` fields (SCRIPT-03).
- `duplicate(template)` placement: `<map>,<x>,<y>,<dir>%TAB%duplicate(<TemplateName>)%TAB%<name>%TAB%<sprite>`
  — reuses the named template's full script body + hooks, only the placement (map/coords/dir/
  sprite/display-name) differs. rAthena resolves the template by name at parse time
  (`npc.cpp npc_parse_duplicate`). Sprite `-1`/`HIDDEN_*` = invisible touch/click NPC.
- Core town NPCs to convert as the template set: **Kafra** (`npc/re/merchants/kafras.txt` /
  `npc/kafras/*`) — save point, storage open, teleport menu, cart rental; **Tool Dealer**
  (`npc/re/merchants/...` / inline shop + dialog); **Job Changers** (`npc/re/jobs/**` — 1st/2nd/
  trans/3rd job NPCs: class checks, item/zeny costs, `jobchange`, skill resets). These exercise
  warp/savePoint/storage/menu/shop/delItem/jobChange/skill — the SCRIPT-01/02/03 surface.

## Scope — every sub-system that must be touched

- [ ] **`Tools.RathenaTranspiler`** — a new .NET tool (or extend the Python importer; prefer a
      dedicated transpiler given the grammar complexity) that parses rAthena `script`/`duplicate`
      NPC blocks and emits TS calling the `registerNpc`/`registerDuplicate` API. Must handle:
      labels → hooks, `mes`/`next`/`menu`/`select`/`close`/`close2`, `if/else`/`switch`/`goto`/`for`/
      `while`, variable scopes (`@`/`#`/`##`/`$`/`.`/`.@`/`'`), arrays + `getd`/`setd`, arithmetic/
      string ops, and the builtin calls (mapped to `ctx.*`). Unconvertible constructs emit a flagged
      TODO comment + a skip-log so the corpus coverage is auditable (but the *committed* NPCs in this
      ticket must be fully converted — no TODOs in shipped files).
- [ ] **`registerDuplicate({ template, map, x, y, dir, name, sprite })`** — new registrar in
      `RegistrarBindings.cs` + `NpcRegistration`/`NpcRegistry` (resolve the template's hooks at
      registration, attach to a fresh placement). Add to `scripts/types/api.d.ts`. Resolve template
      ordering (a duplicate may import-load before its template — handle two-pass resolution).
- [ ] **Convert core town NPCs** (the proof set): Kafra, Tool Dealer, Job Changers (1st→3rd).
      Place under `scripts/npcs/<category>/` with per-dir `index.ts` side-effect imports (match the
      importer's directory convention). Wire them into the build (`scripts/main.ts` → `dist/main.js`).
- [ ] **Coverage report** — the transpiler emits a summary: N NPCs converted, M skipped (with the
      unsupported-construct histogram) so future waves know what builtins to finish.
- [ ] **Build pipeline** — ensure `npm run build` (tsc → `dist/main.js`) picks up the new tree and
      `ScriptHost` loads it; the dev-test fixtures stay isolated under `_dev_test/`.

## Done criteria

- `registerDuplicate(...)` works: a duplicate placement runs the template's onClick/onTouch/timers
  with its own coords/sprite/name; verified by a test placing 2 duplicates of one template.
- A real **Kafra** is in `scripts/npcs/`: clicking it offers Save / Storage / Teleport (paid),
  performs the savePoint, opens storage, and warps with zeny deducted — end to end, no stub log.
- A real **Tool Dealer** sells via `registerShop`/dialog and a **Job Changer** changes class with
  the correct item/zeny/level gates and skill reset.
- The transpiler converts the `npc/re/**` corpus to TS, producing a coverage report; the committed
  town NPCs contain **no TODO/skip markers**.
- No `_dev_test` fixture is required for these to work (real NPCs stand alone).

## Test plan

- `Map.Server.Tests/Scripting/DuplicateRegistrationTests.cs`: register a template + 2 duplicates;
  assert both placements resolve the template hooks and have distinct coords/sprite/name; clicking
  each runs the shared body.
- `Map.Server.Tests/Scripting/KafraNpcTests.cs`: drive the converted Kafra through the engine —
  Save branch persists savepoint; Storage branch opens storage; Teleport branch warps + deducts
  zeny; assert against SCRIPT-01/02 services (mocked) and ZC packets.
- Transpiler unit tests: feed representative rAthena snippets (menu, if/goto, setarray, duplicate
  header) and assert the emitted TS compiles + calls the expected `ctx.*` / registrar.
- Build smoke: `npm run build` succeeds with the new tree; `ScriptHostTests` loads `dist/main.js`.

## Notes / gotchas

- This is XL and the capstone — sequence it AFTER 01/02/03 are green or the town NPCs will be built
  on stubs. The transpiler can begin earlier (it only emits TS), but the *converted NPCs* can't be
  honest until the builtins they call are real.
- `goto`/labels and the rAthena `menu ...,L_label;` jump model don't map cleanly to async/await —
  the transpiler must restructure label jumps into structured control flow (or a state-machine
  switch). This is the hardest part of the grammar; budget for it.
- Template resolution is order-independent in rAthena (whole corpus parsed before duplicates
  resolve) — the C# registrar must do a deferred second pass, not assume template-before-duplicate
  import order.
- Sprite `-1` / `HIDDEN_NPC` / `WARPNPC` placements are invisible trigger NPCs (onTouch warps) —
  route those through `registerFloatingNpc`/`registerWarp` semantics, not a visible `registerNpc`.
- Keep the Python importer for declarative lines; this transpiler is the script-body complement, not
  a replacement. Decide one toolchain owns each line-type to avoid double-emit.
