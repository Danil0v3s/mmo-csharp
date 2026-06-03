# SKILL-11 — Family: Homunculus + Summoner + Novice residual shells

> **Epic:** Skills · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** SKILL-01 (SC apply-rate), SKILL-04 (durations) · **Blocks:** none

## Problem

Three small families share one residual-shell ticket. Each has a handful of
plugins that animate but don't fully implement their rAthena arm:

- **Homunculus — `Change` (HLIF_CHANGE).** A `StatusSkillImpl` with no `TargetSc`, so
  it applies **no SC** — but rAthena's arm (`skill.cpp:11179`) explicitly does
  `sc_start(..., type, 100, skill_lv, skill_get_time(...))`. The docstring frames it
  as "renewal-only, defers to StatusSkillImpl" but the StatusSkillImpl path with a
  null `TargetSc` is a no-op. The pre-renewal 100/100 HP/SP heal is a documented
  omission (acceptable if renewal-only, but the SC apply is NOT optional).
- **Summoner — `ShamanFormulas` / Spirit-communion masteries.** A shared
  ratio-amplifier base (correctly has no `override` because it's a helper, not a
  directly-cast skill). The open work is verifying the `SH_COMMUNE_WITH_*` /
  `SH_MYSTICAL_CREATURE_MASTERY` `pc_checkskill` reads are wired into the consuming
  skills (SKILL-06 (B) passives), and that the Shaman combat skills actually apply
  the mastery bonus.
- **Novice — `HelpAngel` (NV_HELPANGEL).** `StatusSkillImpl`; docstring: *"splashes
  party members when the caster is in a party. Party splash is TODO."* The party
  broadcast — the entire point of Help Angel — is unimplemented; it currently buffs
  only the single target. `HyperNoviceFormulas` is a shared amplifier base (the
  `HN_SELFSTUDY_*` masteries + the SC_RULEBREAK trailer) — verify the reads are wired.

## Current state (C#)

- `Map.Server/Skills/Behaviors/Homunculus/Change.cs` — `StatusSkillImpl`, no `TargetSc` override → applies nothing. (HLIF_CHANGE, `skill.cpp:11179`.) Doc claims renewal-only deferral; the SC is still missing.
- `Map.Server/Skills/Behaviors/Summoner/ShamanFormulas.cs` — shared amplifier base (no `override` is correct for a helper). Centralises `SH_MYSTICAL_CREATURE_MASTERY` flat bonus + Spirit Communion bonus. The question is whether the consuming Shaman skills call into it and whether the `pc_checkskill(SH_COMMUNE_WITH_*)` gate is read.
- `Map.Server/Skills/Behaviors/Novice/HelpAngel.cs` — `StatusSkillImpl`; doc: *"Party splash is TODO."* No `ctx.PartyMap.ForEachSameMap` broadcast. (NV_HELPANGEL, `skill.cpp:9260`.)
- `Map.Server/Skills/Behaviors/Novice/HyperNoviceFormulas.cs` — shared amplifier base (`HN_SELFSTUDY_SOCERY`/`HN_SELFSTUDY_TATICS` + SC_RULEBREAK trailer). Verify the trailer applies on every HN combat skill.
- `ctx.PartyMap` (`IPartyMapService`) — already plumbed; `party_foreachsamemap` is available for the Help Angel broadcast.
- `StatusType` — confirm SC_CHANGE (HLIF_CHANGE's `type`) + the HN/SH SCs exist; add if missing.

## rAthena reference (source of truth)

- `rathena/src/map/skill.cpp:11179` `HLIF_CHANGE` — `#ifndef RENEWAL status_percent_heal(bl,100,100); [[fallthrough]];#endif` then (shared with HAMI_BLOODLUST/HFLI_FLEET/HFLI_SPEED/MH_*) `clif_skill_nodamage(...; sc_start(src,bl,type,100,skill_lv,skill_get_time(skill_id,skill_lv)))`. So: SC applied at 100 % for `skill_get_time`, plus pre-renewal full heal.
- `rathena/src/map/skill.cpp:9260` `NV_HELPANGEL` — party broadcast (`party_foreachsamemap` style) applying the Help Angel buff/heal to every same-map party member, not just the target.
- `rathena/src/map/skill.cpp` `SH_*` / `HN_SELFSTUDY_*` — `pc_checkskill` mastery reads folded into the Shaman / Hyper Novice combat-skill ratios + the SC_RULEBREAK debuff trailer.
- Monolithic-switch caveat: canonical source is `skill.cpp` (HLIF/NV/SH/HN arms) + `battle.cpp:4590` ratio; the split-file `rathena-fork/src/map/skills/...` paths in the docstrings DO NOT exist here.

## Scope — every sub-system that must be touched

- [ ] **`Change` (HLIF_CHANGE)** — set `TargetSc` = SC_CHANGE; apply via `CastendNoDamageId` → `ctx.Sc.Start(target, SC_CHANGE, rate: guaranteed, val1: skillLevel, GetTime, src)`. Add the pre-renewal `status_percent_heal(100,100)` only if the server runs pre-renewal; otherwise document the renewal default explicitly (and stop calling it "deferred"). Add SC_CHANGE to `StatusType` if absent.
- [ ] **`HelpAngel` party splash** — implement the `ctx.PartyMap.ForEachSameMap(caster, member => apply Help Angel buff/heal)` broadcast per `skill.cpp:9260`. Falls back to single-target when the caster has no party. Remove the "Party splash is TODO" comment.
- [ ] **Summoner Shaman masteries** — verify the Shaman combat skills call `ShamanFormulas` for their ratio amplifier and that `SH_COMMUNE_WITH_*` / `SH_MYSTICAL_CREATURE_MASTERY` are read (SKILL-06 (B) passive reads); fix any stubbed read so the bonus actually applies.
- [ ] **Novice HN masteries + RULEBREAK trailer** — verify every HN combat skill applies the SC_RULEBREAK trailer via `HyperNoviceFormulas` and that `HN_SELFSTUDY_*` masteries are read; fix if stubbed.
- [ ] **DI** — all stay registered; the shared amplifier bases are NOT registered as plugins (they're helpers) — confirm that's intentional and the DI audit doesn't flag them.
- [ ] **No new packets** beyond the existing broadcasts (`clif_skill_nodamage` + the party heal packets).

## Done criteria

- `Change` applies SC_CHANGE at the rAthena duration (test: SC present after cast); pre-renewal heal applied iff pre-renewal mode.
- `HelpAngel` buffs/heals all same-map party members, not just the target (test: 3-member party, all get the effect; soloist gets single-target).
- Shaman combat skills apply the mastery bonus when `SH_COMMUNE_WITH_*`/`SH_MYSTICAL_CREATURE_MASTERY` is learned (test: ratio differs learned vs unlearned).
- Every HN combat skill applies SC_RULEBREAK and the `HN_SELFSTUDY_*` mastery bonus (test).
- No "Party splash is TODO" / "defers to StatusSkillImpl" (where it's actually a no-op) comments remain.

## Test plan

- `HomunChangeTests` — cast → SC_CHANGE present at `GetTime` duration.
- `HelpAngelTests.PartyBroadcast` — 3-member same-map party all receive the effect; soloist → single-target.
- `ShamanMasteryTests` — combat ratio rises with `SH_*` mastery learned.
- `HyperNoviceTests` — HN combat skill applies SC_RULEBREAK + mastery bonus.
- DI audit green (shared bases not double-registered).

## Why the shared-base plugins have no override (and that's correct)

`ShamanFormulas` and `HyperNoviceFormulas` are *not* directly-cast skills — they are
helper classes that the real Summoner/Novice combat plugins delegate into for their
ratio amplifier and SC trailer. They legitimately have no `CastendDamageId`/`Castend
NoDamageId` override and are NOT registered as `SkillImpl` in DI (verify the audit
doesn't expect them to be). The work for these is *consumer-side*: confirm each real
combat plugin calls into them, and that the `pc_checkskill` mastery gates
(`SH_COMMUNE_WITH_*`, `SH_MYSTICAL_CREATURE_MASTERY`, `HN_SELFSTUDY_*`) are read — these
are SKILL-06 (B) passives, so the "dispatch" is the read, not a plugin.

## HLIF_CHANGE shared arm

`skill.cpp:11179` shares one `sc_start` arm across HLIF_CHANGE / HAMI_BLOODLUST /
HFLI_FLEET / HFLI_SPEED / MH_ANGRIFFS_MODUS / MH_GOLDENE_FERSE — all at rate 100 for
`skill_get_time`. When fixing `Change`, sanity-check those sibling Homun skills apply
their SC too (they may share the same shell defect); if so, fold them into this
ticket's scope rather than leaving sibling no-ops.

## Notes / gotchas

- `Change`'s "renewal-only" framing is half-right: the *heal* is pre-renewal-only, but the *SC* applies in both modes. Don't let the framing hide the missing SC.
- The shared amplifier bases (`ShamanFormulas`, `HyperNoviceFormulas`) correctly have no `override` — they're consumed by the real skill plugins, not cast directly. Don't "fix" them into plugins; verify the consumers call them.
- `HelpAngel` party splash uses `IPartyMapService.ForEachSameMap` (already plumbed) — same pattern as Angelus/Magnificat; copy that wiring.
- SKILL-01/04 prerequisites for the SC rate + duration on `Change` and the RULEBREAK trailer.
