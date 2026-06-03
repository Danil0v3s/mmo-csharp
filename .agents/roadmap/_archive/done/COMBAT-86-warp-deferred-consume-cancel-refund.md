# COMBAT-86 — AL_WARP deferred requirement-consume + cancel-refund (SKILL_NOCONSUME_REQ)

> **Epic:** Combat parity · **Status:** ✅ Done (2026-06-03) · **Size:** M · **Player-visible:** yes
> **Depends on:** COMBAT-67 (Warp Portal unit + chooser) · **Blocks:** none
> **Filed by:** COMBAT-67 — it shipped the portal + the memo-set path; the consume-timing
> half is a cast-pipeline change, split out.

## Problem

rAthena marks the AL_WARP cast `SKILL_NOCONSUME_REQ`: the SP + the Blue Gemstone are **not**
consumed when the chooser appears — they are consumed only when the player **picks** a
destination (`skill_castend_map` → `skill_consume_requirement(sd, menuskill_id, lv, 2)`).
Picking **"cancel"** consumes nothing (a full refund, since nothing was spent). The C# port
consumes SP at cast (`StartCast`/`StartCastAt`), so:

1. Cancelling the chooser still costs SP (should be free).
2. The Blue Gemstone is never consumed at selection (it should be consumed on a successful pick).

## Current state (C#)

- `Map.Server/Skills/SkillCastService.cs:StartCast`/`StartCastAt` — consume SP at cast for every
  skill, including AL_WARP. No `SKILL_NOCONSUME_REQ` deferral.
- `Map.Server/Skills/SkillCastEndService.cs:CastEndMap` `case AL_WARP` — places the portal
  (COMBAT-67) but does **not** consume the gemstone / SP at selection, and the `"cancel"` branch
  just returns false (SP already spent at cast).
- `Map.Server/Skills/Behaviors/Acolyte/WarpPortal.cs` — notes the no-consume intent but doesn't
  enforce it.

## rAthena reference (source of truth)

- `skill.cpp` — the `SKILL_NOCONSUME_REQ` flag set on the AL_WARP cast; `skill_castend_map`
  `case AL_WARP` consume + the `"cancel"` early-return.
- `skill.cpp skill_consume_requirement(sd, skill_id, lv, type)` (type 2 = the menu/selection
  consume).

## Scope — every sub-system that must be touched

- [x] Added a no-consume-at-cast path for menu skills (AL_WARP/AL_TELEPORT): `StartCast`/`StartCastAt`
      skip the SP consume via `IsDeferredConsumeMenuSkill` (the SP-affordability check still runs).
- [x] Consume SP at selection (`CastEndMap` AL_WARP/AL_TELEPORT success) via `ConsumeOnWarp`; the
      `"cancel"` / refused-warp path consumes nothing. ➡️ The **Blue Gemstone** item-consume is
      blocked by the skill_db Required-item column (`ConsumeRequirement` type-2 is data-pending) →
      **COMBAT-108**.
- [x] Verified AL_TELEPORT shares the deferral (both lv1 "Random" and lv2 "Random"+"SavePoint" open
      a chooser → flow through `CastEndMap`), so it defers SP the same way.

## Done criteria

- ✅ Cancelling the Warp chooser costs no SP (Combat48: cancel → Sp 100 unchanged).
- ✅ A successful destination pick consumes SP exactly once (geffen / SavePoint → Sp 100→65). ➡️ The
  Blue Gemstone consume is **COMBAT-108** (require-item column gap).

## Test plan

- Cancel → SP unchanged, gemstone count unchanged.
- Successful pick → SP reduced by the AL_WARP cost, gemstone −1, and the portal placed.

## Notes / gotchas

- The menuskill state (which skill/level is pending) must survive between the cast and the
  CZ_SELECT_WARPPOINT reply — check how the C# tracks the in-flight chooser (menuskill_id/lv).

## History

- 2026-06-03 — Deferred the menuskill SP consume (SKILL_NOCONSUME_REQ): `SkillCastService.StartCast`/
  `StartCastAt` now skip the SP deduction for AL_WARP/AL_TELEPORT (the SP-affordability gate still
  runs), and `SkillCastEndService.CastEndMap` consumes it via `ConsumeOnWarp` only on a successful
  warp/portal-placement — "cancel" / a refused warp costs nothing. AL_TELEPORT shares the path (both
  levels open a chooser). Combat48WarpTests +4 (deferred-at-cast gate, pick consumes 35 SP, cancel
  refunds, savepoint consumes). Full suite 4176 pass (1 fail = pre-existing INFRA-11 replay gate).
  Filed COMBAT-108 for the Blue Gemstone item-consume (blocked by the skill_db Required-item column —
  `ConsumeRequirement` type-2 is data-pending; folds into COMBAT-92).
