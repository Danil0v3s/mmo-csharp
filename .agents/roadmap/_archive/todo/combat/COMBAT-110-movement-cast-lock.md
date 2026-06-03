# COMBAT-110 — Movement cast-lock: block move while casting unless SA_FREECAST / LG_EXEEDBREAK

> **Epic:** combat · **Status:** ❌ Not started · **Size:** S · **Player-visible:** yes
> **Depends on:** COMBAT-88, COMBAT-70 · **Blocks:** none
> **Filed by:** COMBAT-88 — it added the auto-attack cast-lock; confirming scope item 2 revealed the
> movement path has no cast gate at all (everyone can walk while casting).

## Problem

rAthena `unit_can_move` (unit.cpp:1682) returns false while casting
(`ud->skilltimer != INVALID_TIMER`) UNLESS the cast is `LG_EXEEDBREAK` or the caster has
`SA_FREECAST` (and the skill is not an `INF2_ISGUILD` skill). The C# `MovementService` has no
cast gate — a casting player can walk freely, rooting-divergence parallel to the attack one
COMBAT-88 fixed.

## Current state (C#)

- `Map.Server/Movement/MovementService.cs` — `TryStartWalk` / the move path has no `IsCasting`
  check and no `ISkillCastService` dependency.
- `Map.Server/Skills/SkillCastService.cs:IsCasting` + `GetCurrentCast` expose the mid-cast state +
  the current cast skill id (for the LG_EXEEDBREAK exemption).

## rAthena reference (source of truth)

- `unit.cpp:1668 unit_can_move` (the skilltimer + SA_FREECAST + LG_EXEEDBREAK + INF2_ISGUILD gate).

## Scope

- [ ] Give `MovementService` access to `ISkillCastService` (a `Lazy<T>` seam if a DI cycle appears —
      the COMBAT-59/70 pattern) and refuse `TryStartWalk` when the caster is mid-cast and not exempt
      (SA_FREECAST and not a guild skill, or LG_EXEEDBREAK).

## Done criteria

- A non-Free-Cast player cannot walk while casting; a Free-Cast player (or an LG_EXEEDBREAK cast) can.

## Test plan

- A casting non-FREECAST player's TryStartWalk is refused; a FREECAST player's is allowed.
