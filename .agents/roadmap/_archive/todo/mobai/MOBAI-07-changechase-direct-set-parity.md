# MOBAI-07 — Changechase target-set: reconcile the CanChangeTarget gate with rAthena's direct set

> **Epic:** Mob AI parity · **Status:** ❌ Not started · **Size:** S · **Player-visible:** yes
> **Depends on:** MOBAI-03 (introduced the gated changechase branch) · **Blocks:** none

## Problem

MOBAI-03 added the `MD_CHANGECHASE` branch to `MobAiService.Tick`: a RUSH/FOLLOW
mob switches to an enemy that walked into its melee reach mid-chase. Per the
MOBAI-03 ticket's explicit instruction ("Honor `CanChangeTarget` — Rush state
requires `ChangeTargetChase`"), the C# switch is **gated on `CanChangeTarget`**:

```csharp
var newTarget = _changeTarget.TryChangeChase(mob, reach);
if (newTarget != null && _changeTarget.CanChangeTarget(mob, newTarget))
    mob.TargetId = (int)newTarget.Id.Value;          // MobAiService.cs (changechase else-if)
```

But rAthena's `mob_ai_sub_hard_changechase` (`mob.cpp:1348-1369`) does **not** call
`mob_can_changetarget` — it sets the new target **directly** (`(*target) = bl`)
once the enemy passes `battle_check_target(BCT_ENEMY)` + `status_check_skilluse` +
`battle_check_range(rhw.range)`. Consequence of the C# divergence: a pure
`MD_CHANGECHASE` mob in **RUSH** state that lacks `MD_CHANGETARGETCHASE` will
**not** switch in C# (because `CanChangeTarget(Rush)` requires `ChangeTargetChase`),
whereas rAthena **would** switch it. FOLLOW-state mobs are unaffected
(`CanChangeTarget(Follow)` is always true), so the common case matches; only the
RUSH-without-ChangeTargetChase case diverges.

## Current state (C#)

- `Map.Server/Mob/MobAiService.cs` — the changechase `else if` arm (added by
  MOBAI-03) calls `TryChangeChase` then gates the assignment on
  `_changeTarget.CanChangeTarget(mob, newTarget)`.
- `Map.Server/Mob/MobChangeTargetService.cs` — `TryChangeChase(mob, range)` finds
  the first live enemy PC within `range` (melee reach); `CanChangeTarget` (`:18`)
  is the FSM×mode matrix (Rush→`ChangeTargetChase`, Follow→always).
- `Map.Server.Tests/Mob/MobChangeTargetModeTests.cs` — covers the FOLLOW path
  (`ChangeChase_in_follow_switches_to_enemy_in_melee_range`); the RUSH-state
  divergence is **not** yet pinned.

## rAthena reference (source of truth)

- `rathena/src/map/mob.cpp:1348-1369` `mob_ai_sub_hard_changechase` — sets the
  target pointer directly; no `mob_can_changetarget` call. The gate matrix
  (`mob_can_changetarget`, `mob.cpp:1235`) is consulted only by the **attacker-
  driven** retarget arm (`mob.cpp:1806`), not by changechase.
- `rathena/src/map/mob.cpp:1881-1887` — the `else if (mode&MD_CHANGECHASE && ...)`
  dispatch into `mob_ai_sub_hard_changechase`.

## Scope — every sub-system that must be touched

- [ ] Decide the intended behavior and make C# match rAthena: either (a) drop the
      `CanChangeTarget` gate on the changechase assignment so a RUSH-state
      `MD_CHANGECHASE` mob switches directly (rAthena-faithful), or (b) if the
      conservative gate is deliberately retained, record the rationale inline +
      in `MobChangeTargetService.cs` and close this ticket as "won't fix / by
      design" with the parity note. Default to (a) unless a regression argues for (b).
- [ ] If (a): remove the `&& _changeTarget.CanChangeTarget(...)` clause from the
      changechase arm in `MobAiService.cs`; the `MD_CHANGECHASE` bit + RUSH/FOLLOW
      state + melee-reach check are the only gates (matching `mob.cpp:1348`).
- [ ] Update the MOBAI-03 docstring/comment that says "Gated by the FSM matrix
      (CanChangeTarget …)" to reflect the final decision.
- [ ] No EF migration, no packets — pure AI targeting.

## Done criteria

- A `MD_CHANGECHASE` mob in **RUSH** state **without** `MD_CHANGETARGETCHASE`
  switches to an enemy that steps into its melee reach (matching rAthena), OR the
  conservative gate is explicitly documented as intended with a cited rationale.
- The FOLLOW-state behavior is unchanged (still switches).
- No `// TODO`, no unexercised branch.

## Test plan

- `Map.Server.Tests` `MobChangeTargetModeTests`: add
  `ChangeChase_in_rush_without_changetargetchase_switches` (asserts the RUSH-state
  switch happens with only `MD_CHANGECHASE` set) — or, for option (b), a test
  pinning that it deliberately does NOT switch, with the rationale referenced.
- Regression: the existing FOLLOW-path changechase test stays green.

## Notes / gotchas

- This is a narrow case (RUSH state + `MD_CHANGECHASE` + no `MD_CHANGETARGETCHASE`).
  Most aggressive mobs that carry `MD_CHANGECHASE` also carry the change-target
  bits, so the live-visible impact is small — but it is a real rAthena divergence.
- Keep the **attacker-driven** retarget arm (`NotifyAttacked` → `TrySetTarget`)
  gated on `CanChangeTarget` — that path *does* mirror `mob.cpp:1806` and must not
  be changed by this ticket.
