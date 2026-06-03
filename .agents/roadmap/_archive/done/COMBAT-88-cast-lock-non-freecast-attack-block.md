# COMBAT-88 — Cast-lock: block attack/move while casting unless SA_FREECAST

> **Epic:** Combat parity · **Status:** ✅ Done (2026-06-03) · **Size:** M · **Player-visible:** yes
> **Depends on:** COMBAT-70 · **Blocks:** none
> **Filed by:** COMBAT-70 — discovered while confirming the FREECAST precondition: the C#
> auto-attack loop has no cast-lock, so *every* caster can attack while casting (not just
> Free-Cast ones).

## Problem

In rAthena, casting a spell roots the caster — you cannot auto-attack or move while the cast
bar is up **unless** you have `SA_FREECAST` (which is the whole point of the skill). The C#
`AttackService.Tick` only gates on `CanAct` (the OPT1 set: Stone/Freeze/Stun/Sleep); it has no
"mid-cast" gate, and `SkillCastService.StartCast` does not stop the caster's `AttackState`. So a
non-Free-Cast player keeps auto-attacking through a cast — a parity divergence. (COMBAT-70
relies on this permissiveness to make the FREECAST amotion modifier observable, but the
*non*-FREECAST case should be blocked.)

## Current state (C#)

- `Map.Server/Combat/AttackService.cs:Tick` — swings whenever `CanAct` passes + range/ammo OK;
  no check for an in-flight cast.
- `Map.Server/Skills/SkillCastService.cs:StartCast`/`StartCastAt` — start the cast timer but do
  not stop the caster's auto-attack.
- `Map.Server/Skills/SkillCastService.cs:IsCasting` — already exposes the mid-cast state
  (COMBAT-70 consumes it for the FREECAST delay).

## rAthena reference (source of truth)

- `unit.cpp` `unit_attack_timer_sub` / `unit_can_move` + `pc_cant_act` — the cast (`ud.skilltimer
  != INVALID_TIMER`) blocks attack/move unless `pc_checkskill(sd, SA_FREECAST) > 0` (and, for
  movement, the FREECAST movement allowance).
- Verify the exact enforcement points (attack vs move) and the FREECAST exemption.

## Scope — every sub-system that must be touched

- [x] In `AttackService.Tick`, refuse a swing when the attacker `IsCasting` **and** lacks
      SA_FREECAST — added the gate to the per-tick swing guard (`_cast?.IsCasting && !HasFreecast`),
      keeping the `AttackState` so the train resumes the instant the cast ends; FREECAST attackers
      still swing at `NextSwingDelay`'s `FreecastAdelay` (COMBAT-70 preserved).
- [x] Confirmed the movement path: `MovementService` has **no** cast gate at all (everyone can walk
      while casting). ➡️ The move-while-cast block (rAthena `unit_can_move`, with the SA_FREECAST /
      LG_EXEEDBREAK / INF2_ISGUILD exemptions) is **COMBAT-110** (needs a new `ISkillCastService`
      dependency on `MovementService`).

## Done criteria

- ✅ A non-Free-Cast player cannot auto-attack while casting (swing skipped, AttackState kept); a
  Free-Cast player can (swing lands, at the COMBAT-70 freecast delay). ➡️ Move-while-cast is COMBAT-110.

## Test plan

- A casting non-FREECAST attacker's swing is skipped; a casting FREECAST attacker swings.

## Notes / gotchas

- Don't regress COMBAT-70: FREECAST attackers must still swing (at `FreecastAdelay`) while
  casting. The block is non-FREECAST-only.

## History

- 2026-06-03 — Added the auto-attack cast-lock to `AttackService.Tick`'s per-tick swing guard: a
  caster mid-cast (`_cast.IsCasting`) is refused a swing unless it has SA_FREECAST
  (`HasFreecast` → `pc_checkskill(SA_FREECAST) > 0`), matching rAthena `unit_attack_timer_sub`. The
  `AttackState` is preserved so the swing train resumes when the cast ends; FREECAST attackers keep
  swinging at COMBAT-70's `FreecastAdelay`. AttackServiceTests +3 (non-freecast casting → no swing +
  state kept, freecast casting → swings, cast-end resumes). Full suite 4183 pass (1 fail =
  pre-existing INFRA-11 replay gate). Confirmed the movement path has no cast gate → filed COMBAT-110
  for the move-while-cast block.
