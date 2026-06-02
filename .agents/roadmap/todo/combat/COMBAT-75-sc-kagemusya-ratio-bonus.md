# COMBAT-75 — SC_KAGEMUSYA ratio bonus across the Ninja/Kagerou skill arms

> **Epic:** combat · **Status:** ❌ Not started · **Size:** S · **Player-visible:** yes
> **Depends on:** none · **Blocks:** none
> **Filed by:** COMBAT-57 — the KO_JYUMONJIKIRI arm's KAGEMUSYA bonus (one of 11), out of that ticket's scope.

## Problem

rAthena's `battle_calc_attack_skill_ratio` applies an SC_KAGEMUSYA caster bonus —
`skillratio += skillratio * sc->getSCE(SC_KAGEMUSYA)->val2 / 100` — to **11 arms** (the
Ninja / Kagerou-Oboro damage skills, e.g. KO_JYUMONJIKIRI battle.cpp:5639, KO_HUUMARANKA,
KO_SETSUDAN, KO_BAKURETSU, etc.). The C# port applies it on at most one (`SwirlingPetal`
references it), so a caster under Shadow Warrior (SC_KAGEMUSYA) does not get the ratio
multiplier on most of these skills.

## Current state (C#)

- `Map.Server/Combat/BattleCalculator.cs` / the per-skill plugins — no shared SC_KAGEMUSYA
  ratio multiplier. `SwirlingPetal` mentions it but the rest of the arm set does not apply it.
- COMBAT-57 added `CalculateSkillRatioPostDmod` (a post-RE_LVL_DMOD ratio hook) — note the
  KAGEMUSYA bonus in rAthena is applied AFTER the SC_JYUMONJIKIRI add but is a `*val2/100`
  multiply of the running skillratio (so it scales whatever ratio is present at that point).

## rAthena reference (source of truth)

- `battle.cpp` the 11 `if (sc && sc->getSCE(SC_KAGEMUSYA)) skillratio += skillratio * val2/100;`
  occurrences (grep `getSCE(SC_KAGEMUSYA)` in `battle_calc_attack_skill_ratio`).
- `status.cpp` SC_KAGEMUSYA `val2` (the Shadow Warrior ratio bonus magnitude).

## Scope — every sub-system that must be touched

- [ ] Apply the `skillratio += skillratio * KAGEMUSYA.val2 / 100` multiply to each affected
      Ninja/Kagerou skill (a shared helper consulted by those plugins, or a ratio post-pass that
      multiplies when the caster has SC_KAGEMUSYA — keyed on the affected skill-id set).
- [ ] Confirm the ordering matches rAthena (after the per-skill base + any SC_JYUMONJIKIRI add).

## Done criteria

- A caster with SC_KAGEMUSYA active deals the `×(100+val2)/100` boosted ratio on each of the 11
  affected skills.

## Test plan

- `Combat75KagemusyaTests`: a representative skill (e.g. KO_JYUMONJIKIRI) with vs without
  SC_KAGEMUSYA on the caster → boosted ratio by val2%.

## Notes / gotchas

- It multiplies the running skillratio (including the COMBAT-57 SC_JYUMONJIKIRI add), so apply it
  as the last ratio step. Reuse the COMBAT-57 ctx-aware / post-DMOD ratio path.
