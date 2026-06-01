# SKILL-14 — Bulk-migrate plugin SC-proc rolls onto the apply-rate engine

> **Epic:** Skills · **Status:** ❌ Not started · **Size:** L · **Player-visible:** yes
> **Depends on:** SKILL-01 (the rate-aware `Start` + `GetScDef` entry point) · **Blocks:** none

## Problem

SKILL-01 built the resist pipeline (`IStatusChangeService.Start(rate, …, flag)` + `GetScDef`
+ `ScDefTable`) and migrated **3 representative plugins** (MeteorStorm, Adoramus, Bash). The
remaining ~163 `Behaviors/` files that still roll an SC proc with `Random.Shared.Next(100) <
chance` immediately before `ctx.Sc.Start(...)` bypass the engine: their debuffs ignore the
target's stat resist, level-diff, and boss immunity. SKILL-01's Done criterion "no plugin
calls `Random.Shared.Next` purely to gate an SC apply" is NOT yet met — this ticket finishes
the sweep. ➡️ Inherited from SKILL-01.

## Current state (C#)

- `Map.Server/Skills/Behaviors/**` — ~163 files still match the pre-roll pattern
  `if (rng.Next(100) < chance) ctx.Sc.Start(...)` (or `Random.Shared`). Grep:
  `grep -rln 'Random' Map.Server/Skills/Behaviors | wc -l` ≈ 166 minus the 3 migrated.
- The engine entry point exists: `StatusChangeService.Start(target, type, int rate, val1..4,
  durationMs, source, ScStartFlag flag, nowTick)` — pass `rate = chance * 100`.

## rAthena reference (source of truth)

- Each skill's `skill_additional_effect` arm (`skill.cpp`) passes a RAW percent rate to
  `sc_start`/`sc_start4`; the engine (`status_change_start` → `status_get_sc_def`) resists +
  rolls. The C# split mirrors that: plugin supplies `rate = chance*100`, engine resists.

## Scope — every sub-system that must be touched

- [ ] Audit every `Behaviors/` file using `Random`. Bucket each (per SKILL-01's mechanics):
  1. **Pure SC-proc roll** → delete the `if (rng.Next…)` guard; call `Start(… rate: chance*100
     …, source: src)`. Drop the `rng` ctor param if it was used only for this.
  2. **SC-proc + non-SC side-roll** → move the proc to the engine; keep `rng` for the side-roll
     (prefer reading durations from `skill_db` per SKILL-04 where applicable).
  3. **Non-SC randomness** (cell offset, spell pick, coin) → leave alone.
- [ ] Annotate each migrated call site with the rAthena rate expression
      (e.g. `// skill.cpp <ARM>: <expr>% → rate <expr>*100`).
- [ ] Ensure every proc passes `source` (needed for level-diff + boss resist).
- [ ] Add the grep-guard test (see Test plan).

## Done criteria

- No `Behaviors/` file rolls `Random*.Next(...)` immediately to gate a `ctx.Sc.Start(...)`.
- Remaining `Random` usages are non-SC (placement/pick) only.
- A spot-check of 10 migrated debuff skills lands less often on high-resist / boss targets.

## Test plan

- Grep-guard unit/CI test: assert no `Random` call directly precedes an `Sc.Start` in
  `Behaviors/` (regex scan over the source tree, or a Roslyn check).
- Spot regression: 5–10 migrated plugins call `Start` with `rate = chance*100` and `source` set.

## Notes / gotchas

- Watch the rate units: whole-percent chance × 100 (1/100-% units). Off-by-100 makes every
  proc 100× too rare/common.
- Self-buffs an ally grants (e.g. status applied to the caster/party) must use the no-rate
  `Start` (guaranteed) — don't route those through `rate`.
