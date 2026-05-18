#!/usr/bin/env python3
"""
One-shot importer: rAthena's `npc/re/{warps,mobs}/*.txt` → TypeScript
files under `scripts/{warps,spawns}/`.

For each rAthena `.txt` containing declarative warp/monster lines we
emit a sibling `.ts` at the matching relative path with one big
`registerWarp({...}, {...}, ...)` (or `registerSpawn(...)`) call.

Each directory gets an `index.ts` that side-effect-imports every `.ts`
in the directory (excluding `index.ts` itself) and every sub-directory.

Script-bodied warps (those declared with `<TAB>script<TAB>` instead of
`<TAB>warp<TAB>`) are skipped — they're full NPC scripts that the
TypeScript engine can't host yet.

Usage:
    python3 scripts/tools/import_rathena.py [<rathena-root>]

`<rathena-root>` defaults to `/Volumes/1TB/Projetos/rathena`.
"""
from __future__ import annotations

import os
import re
import sys
from dataclasses import dataclass
from pathlib import Path


REPO = Path(__file__).resolve().parents[2]  # …/mmo-csharp
SCRIPTS_DIR = REPO / "scripts"

DEFAULT_RATHENA = Path("/Volumes/1TB/Projetos/rathena")


# ---- parsing ---------------------------------------------------------------

@dataclass
class Warp:
    from_map: str
    from_x: int
    from_y: int
    area_xs: int
    area_ys: int
    to_map: str
    to_x: int
    to_y: int
    type: str  # "warp" or "warp2"


@dataclass
class Spawn:
    map: str
    x: int
    y: int
    xs: int
    ys: int
    mob_id: int
    amount: int
    delay1: int
    delay2: int
    name: str
    boss: bool


@dataclass
class MapFlag:
    map: str
    flag: str
    value: str | None


@dataclass
class ShopEntry:
    map: str
    x: int
    y: int
    dir: int
    kind: str       # "shop" | "cashshop" | "itemshop" | "pointshop" | "marketshop"
    name: str
    sprite: int
    cost_item: int | None      # itemshop
    cost_variable: str | None  # pointshop
    items: list[tuple[int, int, int]]  # (item_id, price, stock-for-market)


# rAthena warp line:
#   srcmap,sx,sy,dir<TAB>warp|warp2<TAB>warpname<TAB>spanxs,spanys,dstmap,dstx,dsty
WARP_RE = re.compile(
    r"^\s*([^,\s]+),(\d+),(\d+),\d+\t(warp|warp2)\t[^\t]+\t"
    r"(\d+),(\d+),([^,\s]+),(\d+),(\d+)\s*$"
)

# rAthena monster line — position has 3 OR 5 commas:
#   map,x,y<TAB> or map,x,y,xs,ys<TAB>
# Then: monster|boss_monster<TAB>name<TAB>class,amount[,delay1[,delay2[,event[,size,ai]]]]
SPAWN_RE = re.compile(
    r"^\s*([^,\s]+),(\d+),(\d+)(?:,(\d+),(\d+))?\t"
    r"(monster|boss_monster)\t([^\t]+)\t"
    r"(\d+),(\d+)(?:,(\d+))?(?:,(\d+))?(?:,[^,]*)?(?:,\d+,\d+)?\s*$"
)

# rAthena mapflag line:
#   <mapname>\tmapflag\t<flag>[\t<value>]
# Floating mapflags (no map prefix, just `-`) are rare and not useful for us.
MAPFLAG_RE = re.compile(
    r"^\s*([^,\s]+)\tmapflag\t(\S+)(?:\t(.+?))?\s*$"
)

# rAthena inline shop line:
#   <map>,<x>,<y>,<dir>\t<kind>\t<name>\t<sprite>,<item>:<price>[,<item>:<price>...]
# pointshop variant: \t<kind>\t<name>\t<sprite>,<costvar>,<item>:<price>[,...]
# itemshop variant : \t<kind>\t<name>\t<sprite>,<costitem>[:<discount>],<item>:<price>[,...]
# marketshop entries: <item>:<price>:<stock>
# Capturing the body as a single blob and parsing per-kind below is simpler
# than one giant regex.
SHOP_RE = re.compile(
    r"^\s*([^,\s]+),(\d+),(\d+),(\d+)\t"
    r"(shop|cashshop|itemshop|pointshop|marketshop)\t([^\t]+)\t(.+?)\s*$"
)


def parse_warps(text: str) -> list[Warp]:
    out: list[Warp] = []
    for line in text.splitlines():
        if line.startswith("//") or not line.strip():
            continue
        m = WARP_RE.match(line)
        if not m:
            continue
        out.append(Warp(
            from_map=m.group(1),
            from_x=int(m.group(2)), from_y=int(m.group(3)),
            type=m.group(4),
            area_xs=int(m.group(5)), area_ys=int(m.group(6)),
            to_map=m.group(7),
            to_x=int(m.group(8)), to_y=int(m.group(9)),
        ))
    return out


def parse_mapflags(text: str) -> list[MapFlag]:
    out: list[MapFlag] = []
    for line in text.splitlines():
        if line.startswith("//") or not line.strip():
            continue
        m = MAPFLAG_RE.match(line)
        if not m:
            continue
        # Skip floating mapflags (map = `-`) — they don't pin to a real map.
        if m.group(1) == "-":
            continue
        out.append(MapFlag(
            map=m.group(1),
            flag=m.group(2),
            value=(m.group(3).strip() if m.group(3) else None),
        ))
    return out


def parse_shops(text: str) -> list[ShopEntry]:
    out: list[ShopEntry] = []
    for line in text.splitlines():
        if line.startswith("//") or not line.strip():
            continue
        m = SHOP_RE.match(line)
        if not m:
            continue
        kind = m.group(5)
        body = m.group(7)
        # Body always starts with `<sprite>,...`. Split tail tokens by commas
        # and walk per-kind.
        tokens = [t.strip() for t in body.split(",") if t.strip()]
        if not tokens:
            continue
        try:
            sprite = int(tokens[0])
        except ValueError:
            # Some files use a sprite NAME (e.g. "4_M_KAGE") rather than an id.
            # We don't have a sprite-name → id table, so skip for now.
            continue
        idx = 1
        cost_item: int | None = None
        cost_variable: str | None = None
        if kind == "itemshop":
            # token[1] = <costitem>[:<discount>]
            if idx >= len(tokens):
                continue
            cost_part = tokens[idx]
            cost_item = int(cost_part.split(":", 1)[0])
            idx += 1
        elif kind == "pointshop":
            if idx >= len(tokens):
                continue
            cost_variable = tokens[idx]
            idx += 1
        items: list[tuple[int, int, int]] = []
        for tok in tokens[idx:]:
            parts = tok.split(":")
            if len(parts) < 2:
                continue
            try:
                item_id = int(parts[0])
                price = int(parts[1])
            except ValueError:
                continue
            stock = int(parts[2]) if (kind == "marketshop" and len(parts) >= 3) else -1
            items.append((item_id, price, stock))
        if not items:
            continue
        out.append(ShopEntry(
            map=m.group(1),
            x=int(m.group(2)), y=int(m.group(3)), dir=int(m.group(4)),
            kind=kind, name=m.group(6).strip(), sprite=sprite,
            cost_item=cost_item, cost_variable=cost_variable,
            items=items,
        ))
    return out


def parse_spawns(text: str) -> list[Spawn]:
    out: list[Spawn] = []
    for line in text.splitlines():
        if line.startswith("//") or not line.strip():
            continue
        m = SPAWN_RE.match(line)
        if not m:
            continue
        out.append(Spawn(
            map=m.group(1),
            x=int(m.group(2)), y=int(m.group(3)),
            xs=int(m.group(4) or 0), ys=int(m.group(5) or 0),
            boss=(m.group(6) == "boss_monster"),
            name=m.group(7).strip(),
            mob_id=int(m.group(8)),
            amount=int(m.group(9)),
            delay1=int(m.group(10) or 5000),
            delay2=int(m.group(11) or 0),
        ))
    return out


# ---- emission --------------------------------------------------------------

def ts_string(s: str) -> str:
    """Quote a string for TS, escaping backslashes and double quotes."""
    return '"' + s.replace("\\", "\\\\").replace('"', '\\"') + '"'


def emit_warps_ts(warps: list[Warp], src_rel: Path) -> str:
    lines = [
        f"// Auto-generated from rAthena {src_rel}.",
        "// Re-generate with: python3 scripts/tools/import_rathena.py",
        "",
        "registerWarp(",
    ]
    for w in warps:
        type_field = "" if w.type == "warp" else f', type: "warp2"'
        lines.append(
            f"    {{ from: {{ map: {ts_string(w.from_map)}, x: {w.from_x}, y: {w.from_y} }}, "
            f"area: {{ xs: {w.area_xs}, ys: {w.area_ys} }}, "
            f"to: {{ map: {ts_string(w.to_map)}, x: {w.to_x}, y: {w.to_y} }}{type_field} }},"
        )
    lines.append(");")
    lines.append("")
    return "\n".join(lines)


def emit_mapflags_ts(flags: list[MapFlag], src_rel: Path) -> str:
    lines = [
        f"// Auto-generated from rAthena {src_rel}.",
        "// Re-generate with: python3 scripts/tools/import_rathena.py",
        "",
        "registerMapFlag(",
    ]
    for f in flags:
        value_field = f", value: {ts_string(f.value)}" if f.value else ""
        lines.append(
            f"    {{ map: {ts_string(f.map)}, flag: {ts_string(f.flag)}{value_field} }},"
        )
    lines.append(");")
    lines.append("")
    return "\n".join(lines)


def emit_shops_ts(shops: list[ShopEntry], src_rel: Path) -> str:
    lines = [
        f"// Auto-generated from rAthena {src_rel}.",
        "// Re-generate with: python3 scripts/tools/import_rathena.py",
        "",
        "registerShop(",
    ]
    for s in shops:
        if s.kind == "marketshop":
            items_str = ", ".join(
                f"{{ itemId: {iid}, price: {price}, stock: {stock} }}"
                for iid, price, stock in s.items
            )
        else:
            items_str = ", ".join(
                f"{{ itemId: {iid}, price: {price} }}"
                for iid, price, _ in s.items
            )
        # rAthena price -1 means "use item-db buy price"; we keep -1 verbatim
        # so the registrar / consumer can decide what to do with it later.
        extra = ""
        if s.kind == "itemshop":
            extra = f", costItem: {s.cost_item}"
        elif s.kind == "pointshop":
            extra = f", costVariable: {ts_string(s.cost_variable or '')}"
        lines.append(
            f"    {{ kind: {ts_string(s.kind.replace('shop', '') or 'shop')}, "
            f"map: {ts_string(s.map)}, x: {s.x}, y: {s.y}, dir: {s.dir}, "
            f"sprite: {s.sprite}, name: {ts_string(s.name)}{extra}, "
            f"items: [{items_str}] }},"
        )
    lines.append(");")
    lines.append("")
    return "\n".join(lines)


def emit_spawns_ts(spawns: list[Spawn], src_rel: Path) -> str:
    lines = [
        f"// Auto-generated from rAthena {src_rel}.",
        "// Re-generate with: python3 scripts/tools/import_rathena.py",
        "",
        "registerSpawn(",
    ]
    for s in spawns:
        # Area: when xs/ys are both 0, that's rAthena's point-or-anywhere
        # convention; we still encode the (x,y) so the registrar sees it.
        # rAthena: x=0 y=0 xs=0 ys=0 = anywhere walkable on the map.
        if s.x == 0 and s.y == 0 and s.xs == 0 and s.ys == 0:
            area_field = ""
        else:
            area_field = (
                f", area: {{ x: {s.x}, y: {s.y}, xs: {s.xs}, ys: {s.ys} }}"
            )
        boss_field = ", boss: true" if s.boss else ""
        # If display name matches the auto-generated default, omit it. But
        # we don't have the mob_db here, so always preserve the override.
        name_field = f", name: {ts_string(s.name)}" if s.name else ""
        respawn_field = (
            f", respawn: {{ baseMs: {s.delay1}, jitterMs: {s.delay2} }}"
            if s.delay1 != 5000 or s.delay2 != 0 else ""
        )
        lines.append(
            f"    {{ map: {ts_string(s.map)}{area_field}, "
            f"mobId: {s.mob_id}, amount: {s.amount}"
            f"{respawn_field}{boss_field}{name_field} }},"
        )
    lines.append(");")
    lines.append("")
    return "\n".join(lines)


# ---- index.ts generation ---------------------------------------------------

INDEX_HEADER = (
    "// Auto-generated by scripts/tools/import_rathena.py. Do not edit by\n"
    "// hand — re-run the importer to refresh. Imports every sibling .ts\n"
    "// file in this directory plus every sub-directory's index.\n\n"
)


def write_indexes(root: Path) -> None:
    """For each directory under `root`, write an index.ts that imports
    every sibling .ts (excluding index.ts itself) and every sub-dir."""
    for dirpath, dirnames, filenames in os.walk(root):
        d = Path(dirpath)
        dirnames.sort()
        filenames.sort()

        ts_files = sorted(
            f for f in filenames
            if f.endswith(".ts") and f != "index.ts"
        )
        sub_dirs = sorted(dirnames)

        if not ts_files and not sub_dirs:
            continue

        lines = [INDEX_HEADER]
        for f in ts_files:
            stem = f[:-3]
            lines.append(f'import "./{stem}";')
        for sub in sub_dirs:
            lines.append(f'import "./{sub}";')
        (d / "index.ts").write_text("".join(lines[:1]) + "\n".join(lines[1:]) + "\n")


# ---- driver ----------------------------------------------------------------

def convert_tree(src_root: Path, dst_root: Path, parse_fn, emit_fn, label: str) -> None:
    """Walk every .txt under `src_root`, emit a sibling .ts under
    `dst_root` (mirrored relative path) when the file has at least one
    parseable record."""
    if not src_root.exists():
        print(f"  {label}: source {src_root} not found — skipped")
        return

    converted = 0
    skipped_empty = 0
    total_records = 0
    for src_path in sorted(src_root.rglob("*.txt")):
        rel = src_path.relative_to(src_root)
        text = src_path.read_text(encoding="utf-8", errors="replace")
        records = parse_fn(text)
        if not records:
            skipped_empty += 1
            continue

        dst_path = dst_root / rel.with_suffix(".ts")
        dst_path.parent.mkdir(parents=True, exist_ok=True)
        dst_path.write_text(emit_fn(records, rel))
        converted += 1
        total_records += len(records)

    print(f"  {label}: {converted} files, {total_records} records ({skipped_empty} source files had no declarative lines)")


def main() -> None:
    rathena_root = Path(sys.argv[1]) if len(sys.argv) > 1 else DEFAULT_RATHENA
    if not rathena_root.exists():
        print(f"error: rAthena root {rathena_root} does not exist", file=sys.stderr)
        sys.exit(1)

    print(f"rAthena source: {rathena_root}")
    print(f"output: {SCRIPTS_DIR}")

    # Wipe any previously generated content so deletes propagate. We only
    # touch the auto-generated subtrees, never main.ts / npcs / shops / etc.
    # NB: scripts/shops/ is hand-written + generated. We DO clear it because
    # the importer is the sole writer of inline-shop output.
    for sub in ("warps", "spawns", "mapflags", "shops"):
        target = SCRIPTS_DIR / sub
        if target.exists():
            for p in sorted(target.rglob("*"), reverse=True):
                if p.is_file():
                    p.unlink()
                elif p.is_dir():
                    p.rmdir()

    print()
    print("Converting:")
    convert_tree(
        rathena_root / "npc" / "re" / "warps",
        SCRIPTS_DIR / "warps",
        parse_warps,
        emit_warps_ts,
        "warps",
    )
    convert_tree(
        rathena_root / "npc" / "re" / "mobs",
        SCRIPTS_DIR / "spawns",
        parse_spawns,
        emit_spawns_ts,
        "spawns",
    )
    convert_tree(
        rathena_root / "npc" / "re" / "mapflag",
        SCRIPTS_DIR / "mapflags",
        parse_mapflags,
        emit_mapflags_ts,
        "mapflags",
    )
    # Inline shops live in many subtrees, not under one canonical dir.
    # Walk a few known parents — merchants/, quests/, jobs/, cities/, custom/.
    for sub in ("merchants", "quests", "jobs", "cities", "custom", "kafras"):
        convert_tree(
            rathena_root / "npc" / "re" / sub,
            SCRIPTS_DIR / "shops" / sub,
            parse_shops,
            emit_shops_ts,
            f"shops/{sub}",
        )

    print()
    print("Writing index.ts files...")
    for sub in ("warps", "spawns", "mapflags", "shops"):
        target = SCRIPTS_DIR / sub
        if target.exists():
            write_indexes(target)
    print("Done.")


if __name__ == "__main__":
    main()
