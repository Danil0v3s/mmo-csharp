# <TICKET-ID> — <Short title>

> **Epic:** <epic> · **Status:** ❌ Not started · **Size:** S/M/L/XL · **Player-visible:** yes/no
> **Depends on:** <ticket ids or "none"> · **Blocks:** <ticket ids or "none">

## Problem

What is wrong / missing today, in plain terms, and why it matters for a player.
State the *current C# behavior* concretely (quote the offending method/return value).

## Current state (C#)

- `path/to/File.cs:line` — what it does now (e.g. "returns `false` unconditionally").
- List every relevant file + method. Be exhaustive — a dev should not have to re-discover the surface.

## rAthena reference (source of truth)

- `rathena/src/map/<file>.cpp:<fn>` — the correct behavior, summarized.
- Quote the key formula / state transition / packet shape.
- Note the monolithic-switch caveat: the canonical source is `skill.cpp`/`battle.cpp`/`status.cpp`
  switch arms (the `rathena-fork/src/map/skills/...` split-file paths in some C# docstrings do not exist
  in this checkout).

## Scope — every sub-system that must be touched

Enumerate EVERY piece so the implementer does not create a stub or defer:
- [ ] Entity / field additions (with EF migration if persisted)
- [ ] Repository / DB loader
- [ ] Service method bodies (name each)
- [ ] Packet definitions (Core.Server/Packets) + handlers (Map.Server/Handlers)
- [ ] IPC proto + char-side RPC (if persisted state)
- [ ] Wiring into the game loop / observer / lifecycle
- [ ] Client-visible packets (ZC_*) emitted

## Done criteria

- Concrete, testable acceptance conditions (numbers match rAthena for cases X/Y/Z).
- No `// TODO`, no `data-pending`, no log-only no-op left in the touched files.

## Test plan

- Unit/regression tests to add (file + what they pin).
- Manual/live-client check if applicable.

## Notes / gotchas

- Anything discovered during the audit that will trip the implementer.
