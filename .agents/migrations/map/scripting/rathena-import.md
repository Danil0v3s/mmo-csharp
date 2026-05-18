# rAthena → TypeScript import status

Inventory of what the importer at [`scripts/tools/import_rathena.py`](../../../scripts/tools/import_rathena.py) does and doesn't pull from rAthena's `npc/re/` tree, and why. Re-run the importer to refresh the generated `.ts` files when rAthena's source changes:

```bash
python3 scripts/tools/import_rathena.py [<rathena-root>]
```

It walks the source tree, parses declarative lines, and writes mirrored `.ts` files plus per-directory `index.ts` files under `scripts/{warps,spawns,mapflags,shops}/`.

## What gets imported

Each line is a single rAthena directive on a tab-separated row. The importer hand-parses one line at a time — no script-body evaluation.

| rAthena directive | Source tree | Output | Count | Registrar |
|---|---|---|---|---|
| `warp` / `warp2` | `npc/re/warps/` | `scripts/warps/` | 1,289 | `registerWarp` |
| `monster` / `boss_monster` | `npc/re/mobs/` | `scripts/spawns/` | 3,024 | `registerSpawn` |
| `mapflag` | `npc/re/mapflag/` | `scripts/mapflags/` | 2,373 | `registerMapFlag` |
| `shop` / `cashshop` / `itemshop` / `pointshop` / `marketshop` | `npc/re/{merchants,quests,jobs,cities,custom,kafras}/` | `scripts/shops/` | 107 | `registerShop` |

Boot-time confirmation in [`Map.Server/Scripting/ScriptHost.cs`](../../../Map.Server/Scripting/ScriptHost.cs):

```
Scripts loaded: 2 NPCs / 1 floating / 107 shops / 1289 warps / 3024 spawns / 2373 mapflags
```

The script registry holds every record. Per-system consumers (`WarpService.Build`, `MapServerImpl.PopulateMobSpawnRegistry`) filter to maps actually hosted on this map server, so the "X on unhosted maps" log lines are expected on a 2-map dev instance.

## What is *not* imported (and why)

### Script-bodied NPCs

Lines of the shape:

```
prontera,150,150,4\tscript\tKafra Employee\t115,{
  mes "[Kafra]";
  mes "Welcome!";
  ...
}
```

These contain rAthena scripting language (`mes` / `menu` / `set` / `warp` / `if` / loops / arrays / etc.) which our engine doesn't yet interpret. Authors will rewrite each NPC against the TypeScript [API surface](../../../scripts/types/api.d.ts) (`registerNpc({ onClick: async ctx => ... })`) one at a time as gameplay subsystems come online.

Tracked separately in [phase-1-loader.md](phase-1-loader.md) → Phase 2+ work.

### `duplicate(template)` placements — 4,684 lines

```
moc_fild07,380,202,1\tduplicate(Continental Guard#man)\tContinental Guard#man1\t852
```

These are *placement-only* references to a scripted template defined elsewhere — they have no behavior of their own, just `(map, x, y, dir, display_name, sprite, template-name)`. The template is a full scripted NPC, so duplicates are blocked on the same script-engine port as full scripts.

**Why not import them now as placeholder placements?** Considered; deferred. A duplicate without its template is a no-op visual prop; the surface it would need (`registerDuplicate({ template, ... })`) only becomes useful once the template's `registerNpc(...)` exists. Migrating template + duplicates in lockstep avoids a temporary surface that nothing yet consumes.

### Empty-body decoratives — 48 lines

```
moro_vol,104,109,0\tscript\tCombat Laphine#sol01\t4_M_FAIRYSOLDIER,{ end; }
```

Looks like a script declaration but the body is literally `{ end; }` (do-nothing). Pure visual filler. Small enough population (48) that we'll bundle them into the script-engine port rather than design a one-off path.

### Sprite-name shops

A handful of inline-shop lines use a sprite *name* (e.g. `4_M_KAGE`) instead of a numeric sprite id. Skipped — no `sprite_name → id` map yet. Affects a small number of entries; they'll come in when the sprite-name table lands.

### Floating mapflags

`-\tmapflag\t<flag>\t<value>` (the `-` map sentinel) is rare and not tied to a real hosted map. The importer drops these.

## Re-running the importer

The importer wipes `scripts/{warps,spawns,mapflags,shops}/` and regenerates from scratch on every run — local edits to generated files are lost. Hand-authored TS content for these categories should live in [`scripts/npcs/`](../../../scripts/npcs/) or its own sibling subtree instead.

## History

- **2026-05-18** — Initial import: warps (1,289), mob spawns (3,024) committed as `bed669a`. Mapflags (2,373) + inline shops (107) added in `cb57d98`. Duplicate placements + empty-body decoratives + full scripted NPCs deferred until the script-engine port hosts scripted templates.
- **2026-05-17** — DB-catalog approach (commit `96443ef`) reverted in `f111dc7`; same source data is now script-driven via the importer.
