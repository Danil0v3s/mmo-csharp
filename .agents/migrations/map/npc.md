# MS2 · NPC system

**Phase:** MS2
**Depends on:** [world.md](world.md) (cell grid for placement), [entities.md](entities.md), [visibility.md](visibility.md)
**Blocks:** anything that uses scripts (warps already partially covered in world.md, shops in MS3, quests, etc.)

NPCs in rAthena are everything that's interactive on the map and isn't a player or mob: shopkeepers, quest givers, warps, monster spawn points, GM-spawn-mob script triggers, and so on. They're all defined by **scripts** in plain-text files.

This is the biggest scope decision of the whole map migration. rAthena's [script.cpp](/Volumes/1TB/Projetos/rathena/src/map/script.cpp) is **28,422 lines** — a full bytecode VM with 1000+ builtin commands.

## Source of truth

- [rathena/src/map/npc.cpp](/Volumes/1TB/Projetos/rathena/src/map/npc.cpp) — NPC parser, dialog dispatcher, click handlers (6341 lines)
- [rathena/src/map/script.cpp](/Volumes/1TB/Projetos/rathena/src/map/script.cpp) — bytecode compiler + VM (28k lines)
- [rathena/src/map/script.hpp](/Volumes/1TB/Projetos/rathena/src/map/script.hpp) — script_state, script_data
- [rathena/npc/](/Volumes/1TB/Projetos/rathena/npc/) — 4000+ .txt script files
- [rathena/doc/script_commands.txt](/Volumes/1TB/Projetos/rathena/doc/script_commands.txt) — builtin command reference

## Scope decision (must answer before any code)

Three viable paths:

### Option A — Port the full script VM

Faithful to rAthena. Reads existing scripts unchanged. ~6 months of dedicated work for one engineer; large attack surface for bugs. Pro: every existing script "just works."

### Option B — Port a minimal subset, re-implement common scripts in C#

Port: variable assignment, `if/else`, `for`/`while`, function calls, `mes/next/menu/close/end`, basic builtins (`getcharid`, `getitem`, `delitem`, `set`, `mes`, `npctalk`, `warp`, `monster`). Re-implement complex scripts (quests, gameplay events) in typed C# handlers. Pro: ~2-3 months; covers most NPCs. Con: NPCs that use exotic builtins break.

### Option C — DB-driven NPCs only, no script engine

Define NPCs in JSON/YAML/SQL with structured fields: type (warp / shop / dialog / spawn), parameters (warp dest, shop items, dialog text + choices, mob spawn config). Pro: simple, no VM, fast to build. Con: rAthena script library is lost; every quest must be rebuilt.

### Recommendation: **Option B, with a phased rollout**

- **MS2:** parse and support **warps + shops + simple dialog NPCs** (mes/next/menu/close/end + basic if). This is enough to walk around a city and click shopkeepers.
- **MS3+:** extend builtins as gameplay systems land (skills, status, items). The script engine is the largest single piece of map-server work; treat it as an evergreen project with milestones.

The rest of this doc assumes Option B.

## Scope (MS2 — minimal subset)

**In scope:**
- Parser for rAthena's `.txt` NPC definition syntax: header line + body block.
- NPC types supported in MS2: **warp**, **shop**, **dialog** (mes/next/menu/close).
- `NpcEntity : Entity` placed on the map, sent via `ZC_NOTIFY_STANDENTRY` to viewers.
- Click handler: `CZ_CONTACTNPC (0x0090)` → run the NPC's dialog OnClick / OnTouch event.
- Dialog state machine: `mes` appends a line, `next` waits for client click, `menu` waits for choice index, `close` ends.
- `monster` builtin (limited): static mob spawn declared inline in NPC scripts → see [spawn.md](spawn.md).

**Out of scope (MS3+):**
- Quest scripts using `set @var` / `getitem` / `delitem` / status manipulation.
- Mercenary / homunculus / cart / banking scripts.
- Cash shop scripts.
- Battlegrounds / WoE scripts.
- Pets (capturing / hatching).
- Custom event triggers (`OnTimer`, `OnInit`, `OnPCLoginEvent`).

## Done

Nothing.

## Pending

### Items, in order

1. **NPC source file parser.**
   - rAthena `.txt` format: per-line, tab-separated, header tokens then body block `{ ... }`.
   - Common header forms:
     - `mapname,x,y,facing  warp  npcname  xs,ys,destmap,destx,desty` (warp)
     - `mapname,x,y,facing  script  npcname  sprite_id,{ <body> }` (script)
     - `mapname,x,y,facing  shop  npcname  sprite_id,itemid:price,...` (shop)
   - Files use `npc_*.conf` to declare imports. Scan recursively under `npc/`.

2. **NPC types as typed C# subclasses:**
   - `WarpNpc` → uses existing Warp from [world.md](world.md), exposed as a clickable entity OR a step-onto trigger depending on flag.
   - `ShopNpc` → list of (item_id, price). On click, opens client shop UI via `ZC_PC_PURCHASE_ITEMLIST`. Sale fulfilled via inventory IPC (MS3).
   - `DialogNpc` → holds a parsed script (mini-AST).

3. **Mini-AST for dialog scripts.** Token kinds: `mes "text"`, `next`, `menu "label1", L_1, "label2", L_2`, `close`, `end`, `goto LABEL`, `L_1:`. Linear interpreter — no variables, no functions, no `if` in MS2. Add `if` (compare string/int) in MS3.

4. **NPC entity + spatial registration.** On startup, every parsed NPC becomes an `NpcEntity` added to `IEntityRegistry` at its declared (map, x, y). Sent via `ZC_NOTIFY_STANDENTRY` to anyone in view.

5. **Click handler** (`CZ_CONTACTNPC`):
   - Look up NPC by id.
   - For warps: trigger map change (mostly handled via walk-into-warp instead).
   - For shops: send `ZC_PC_PURCHASE_ITEMLIST`.
   - For dialogs: start a new `DialogState` for the player's session, run until first `next` / `menu` / `close`. Send `ZC_SAY_DIALOG` per `mes`, `ZC_WAIT_DIALOG` for `next`, `ZC_MENU_LIST` for `menu`.

6. **Dialog progress handler** (`CZ_REQ_NEXT_SCRIPT (0x00b9)`, `CZ_CHOOSE_MENU (0x00b8)`, `CZ_CLOSE_DIALOG (0x0146)`).
   - Advances the `DialogState` machine.
   - Re-emits the next batch of `ZC_SAY_DIALOG` etc.

7. **OnTouch warps.** Some warps don't need click — stepping onto a cell triggers the warp. Walk system ([movement.md](movement.md)) already calls into world to detect warp cells; OnTouch warps share that mechanism.

8. **monster builtin.** When parsing a script body, if we encounter `monster <map>,<x>,<y>,"<name>",<class>,<amount>,<delay1>,<delay2>,<event>`, register a spawn entry for [spawn.md](spawn.md). The actual mob creation happens on the spawn manager's tick.

### File layout

```
Map.Server/Npc/
├── NpcEntity.cs                — base class (already stubbed in entities.md)
├── WarpNpc.cs
├── ShopNpc.cs
├── DialogNpc.cs
├── Dialog/
│   ├── DialogAst.cs            — node types (Mes, Next, Menu, Close, Goto, Label)
│   ├── DialogParser.cs         — script body → AST
│   └── DialogState.cs          — per-session execution pointer
├── Parser/
│   ├── NpcFileScanner.cs       — recursive .conf-driven file discovery
│   ├── NpcFileParser.cs        — line-by-line header parsing
│   └── NpcSyntaxError.cs
└── INpcRegistry.cs / NpcRegistry.cs  — load all + lookup by id/name

Map.Server/Handlers/
├── ContactNpcHandler.cs        — CZ_CONTACTNPC
├── NextScriptHandler.cs        — CZ_REQ_NEXT_SCRIPT
├── ChooseMenuHandler.cs        — CZ_CHOOSE_MENU
└── CloseDialogHandler.cs       — CZ_CLOSE_DIALOG
```

### Tests

1. `NpcFileParserTests`: warp header, script header, shop header — each parses cleanly; bad input throws with file:line.
2. `DialogParserTests`: minimal script with mes/next/menu/close → AST nodes in order; labels and gotos resolve.
3. `DialogStateTests`: run a script to completion; choosing menu options follows the right path.
4. Smoke: load rAthena's `npc/scripts_main.conf` and discover warps; assert ≥1 well-known warp (e.g. `prontera (273,354) → prt_fild05`).

### Acceptance

- `NpcRegistry` parses rAthena's npc tree without crashing.
- A player clicking a warp NPC in front of prontera South gate (or stepping on the OnTouch warp) ends up on `prt_fild05`.
- A player clicking a shop NPC sees the item list.
- A player clicking a simple dialog NPC progresses through `mes` → `next` → `mes` → `close`.

### Open decisions

- **rAthena script source as truth vs maintained fork.** Editing rAthena's scripts locally to use only the MS2 subset is brittle (upstream changes break us). Recommendation: load rAthena's scripts as-is; the parser gracefully degrades (unsupported builtins → log warning, skip that NPC). Track unsupported-NPC count in startup logs.
- **Sprite ids.** Need a way to map rAthena's numeric NPC sprite ids to the client's expected sprite. Usually 1:1 — no translation needed; just pass through.

## History

- **2026-05-16** — Plan written. Scope decision recorded (Option B). No implementation yet.
