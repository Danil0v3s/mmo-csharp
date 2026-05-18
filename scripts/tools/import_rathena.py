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
    for sub in ("warps", "spawns"):
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

    print()
    print("Writing index.ts files...")
    for sub in ("warps", "spawns"):
        write_indexes(SCRIPTS_DIR / sub)
    print("Done.")


if __name__ == "__main__":
    main()
