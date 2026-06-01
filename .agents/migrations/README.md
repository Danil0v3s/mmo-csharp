# rAthena → C# migration tracking (ARCHIVE / reference only)

> **⚠️ The active worklist moved.** As of **2026-06-01** the canonical parity
> roadmap is **[`.agents/roadmap/`](../roadmap/README.md)** — one self-contained
> development ticket per work item, re-baselined from a full code-vs-rAthena scan.
>
> The old meta-roadmaps and dated audits in this folder
> (`PARITY-REMAINING.md`, `PARITY-CLOSURE-ROADMAP.md`, `ROADMAP.md`,
> `GAMEPLAY-ROADMAP.md`, `CODE-COMPLETENESS-ROADMAP.md`,
> `PARITY-DEFERRAL-ANALYSIS.md`, `*audit*`, `map/AUDIT-*`, `map/SC-SKILL-AUDIT`,
> `map/SKILL-AUDIT-DETAIL`, `map/ROADMAP`) were **removed** because they claimed
> "✅ 100% parity" that the code does not actually have — the numbers measured
> per-function code presence, not working features. See the roadmap README's
> "Honest ground truth" table for what the scan found.

## What's still here (and how to read it)

These per-subsystem docs are **kept as reference** — they carry accurate rAthena
function/line citations and the history of how each piece was wired. **Their
`✅ / ⚠️ / ❌` status columns are NOT authoritative** (they overstate
completion). Use them to answer *"where is the rAthena code and how was this
ported"*, then trust `.agents/roadmap/` for *"is it actually done."*

| Path | Use it for |
|---|---|
| [`login/status.md`](login/status.md) | Login server feature map + rAthena refs |
| [`char/`](char/) | Char client packets, gRPC server, connect flow (this layer IS solid) |
| [`inter/`](inter/) | `inter.cpp` base + `int_*.cpp` module routing |
| [`map/*-parity.md`](map/) | Per-`map/*.cpp` function inventories + rAthena line refs |
| [`map/adjacent/*.md`](map/adjacent/) | MS3 combat/status/skills/items/trade design notes |
| [`map/scripting/*.md`](map/scripting/) | NPC scripting design (note: engine is now ClearScript/V8, not Jint) |
| [`map/<subsystem>.md`](map/) | MS1/MS2 world/session/movement/spawn/npc design docs |

## rAthena source of truth

`/Volumes/1TB/Projetos/rathena/src/` — `login/`, `char/`, `map/`. The canonical
behavioral reference is the monolithic switch arms in `map/{skill,battle,status,
pc,mob}.cpp` (the `rathena-fork/src/map/skills/...` split-file paths quoted in
some C# docstrings do **not** exist in this checkout).
