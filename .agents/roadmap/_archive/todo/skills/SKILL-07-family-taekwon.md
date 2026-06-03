# SKILL-07 — Family: Taekwon / Star Gladiator / Soul Reaper (37 shells of 91)

> **Epic:** Skills · **Status:** ❌ Not started · **Size:** XL · **Player-visible:** yes
> **Depends on:** SKILL-01 (SC apply-rate), SKILL-04 (skill_db durations) · **Blocks:** none

## Problem

The Taekwon family folder (`TK_*`, `SG_*` Star Gladiator, `SJ_*` Soul Linker/Soul
Reaper, `SP_*` Soul Reaper) has **37 of 91 plugin files that are bare shells** — a
plugin class with no overridden hook beyond a cosmetic
`BroadcastSkillNoDamage` / inherited default ratio. They register (so the DI audit
passes) and animate, but they do nothing: no SC applied, no soul/spirit ball
allocated, no damage formula, no `_ATK` partner fired.

Two concrete failure clusters:

1. **`SP_*` / `SJ_*` Soul/Star buff stubs.** `SoulCollect` ("Grants soulballs; ball
   allocation TODO"), `SoulRevolution` ("SP transfer — animation only"), and the
   rest of the Soul-Reaper SC-apply skills broadcast the cast and return. The
   soulball/spiritball economy and the SC grants (Soul Energy, Soul Unity, the Sun/
   Moon/Star feeling/hatred states) are absent.

2. **`_ATK` partners reference missing bodies.** Several Taekwon damage skills split
   into a kick + an `_ATK` follow-up; the kick shell fires the animation but never
   the `_ATK` damage id, so the damage half is dropped.

## Current state (C#)

- `Map.Server/Skills/Behaviors/Taekwon/` — 91 files; **37 have no `override`** beyond the ctor (verified by grep). Representative shells:
  - `SoulCollect.cs` — `CastendNoDamageId` → `BroadcastSkillNoDamage` only; doc: *"Grants soulballs; ball allocation TODO."* (SP_SOULCOLLECT, `skill.cpp:9320`).
  - `SoulRevolution.cs` — same shape; doc: *"SP transfer to target — animation only."* (SP_SOULREVOLVE, `skill.cpp:10895`).
  - `SoulGathering.cs`, `SoulEnergy`/`SoulUnity`/`SoulDivision`/`SoulCurse`/`SoulExplosion`/`SpiritofRebirth` — SC-apply / soulball stubs.
  - `FeelingtheSunMoonandStars`, `HatredoftheSunMoonandStars`, `FalconsSoul`/`GolemsSoul`/`ShadowsSoul`/`FairysSoul` — Star Gladiator feeling/hate/spirit states.
- `SP_SOULENERGY` has no castend `case` in this checkout — it's a soulball-count / `pc_checkskill` gate read by other Soul skills (passive; classify per SKILL-06 (B), don't no-op-plugin it).
- Soulball / spiritball / star-feeling state: confirm `ctx.Orbs` (`IPlayerOrbService`) exposes soulball + spirit-sphere counters; Star feeling/hate state likely needs a per-PC field (check `PlayerEntity`).

## rAthena reference (source of truth)

- `rathena/src/map/skill.cpp:9320` `SP_SOULCOLLECT` — adds soul balls up to the max (`pc_addsoulball` loop). `:10895` `SP_SOULREVOLVE` — consumes soul balls to restore the target's SP. `:9320`-region Soul Reaper arms apply `SC_SOULENERGY`/`SC_SOULUNITY`/`SC_SOULCURSE`/etc. via `sc_start` with `skill_get_time`.
- `rathena/src/map/skill.cpp:14971` `SG_SUN_WARM` (+ moon/star warm) — ground/aura damage tied to the day-of-week feeling state. The Sun/Moon/Star "feeling" + "hatred" arms set `pc->feel_map` / `pc->hate_target` state read by the warm/comfort/blessing skills.
- `rathena/src/map/skill.cpp:1770` `TK_JUMPKICK`, `:5795` `SJ_NEWMOONKICK` — kick damage arms; the kick + `_ATK` split is in `battle_calc_attack_skill_ratio` (`battle.cpp:4590`) per child id.
- Monolithic-switch caveat: canonical source is `skill.cpp` (the `SP_*`/`SG_*`/`SJ_*`/`TK_*` castend arms) + `battle.cpp:4590` ratio + `pc.cpp` soulball/feeling state.

## Scope — every sub-system that must be touched

- [ ] **Soulball economy** — wire `SoulCollect` to `ctx.Orbs.AddSoulball` up to the cap; `SoulRevolution` to consume balls → `ctx.StatusOps.Heal` SP on the target. Confirm `IPlayerOrbService` has soulball add/count/consume; add if missing (transient runtime counter, not persisted mid-session).
- [ ] **Soul Reaper SC grants** — implement each `SP_*`/`SJ_*` SC-apply shell as a `StatusSkillImpl` (or `CastendNoDamageId` body) that applies its SC via `ctx.Sc.Start(rate, duration)` — rate through SKILL-01, duration through SKILL-04 `GetTime`. One per: Soul Energy, Soul Unity, Soul Division, Soul Curse, Soul Explosion, Spirit of Rebirth, etc. Cite each `skill.cpp` arm.
- [ ] **Star Gladiator feeling / hatred state** — implement `FeelingtheSunMoonandStars` / `HatredoftheSunMoonandStars` to set the PC's feel-map / hate-target state, and the four spirit skills (`Falcon/Golem/Shadow/Fairy`) to set their SC. Needs a per-`PlayerEntity` feeling-state field (add if absent; transient + char-save if rAthena persists it — check `pc.cpp` save). The Sun/Moon/Star Warm/Comfort/Blessing skills then read that state.
- [ ] **Kick + `_ATK` damage** — each kick shell (`TK_JUMPKICK`, `SJ_NEWMOONKICK`, the storm/turn/counter kicks) fires its damage via `WeaponSkillImpl.CalculateSkillRatio` per the `battle.cpp:4590` arm, and where rAthena splits into an `_ATK` follow-up, fires the `_ATK` child (coordinate with SKILL-06 (C)).
- [ ] **Passive ids** — `SP_SOULENERGY` and any other `pc_checkskill`-only id: wire the read at the consuming site, no no-op plugin (SKILL-06 (B)).
- [ ] **DI** — every new/changed plugin stays registered in `Program.cs`; no orphan, no duplicate id.
- [ ] **No new packets** beyond the existing `clif_skill_*` broadcasts; the soulball-count packet (`ZC_SPIRITS`-family) — confirm it's already emitted by the orb service.

## Done criteria

- `SoulCollect` actually adds soul balls (count rises, capped); `SoulRevolution` restores the target's SP and consumes balls (test).
- Each Soul Reaper SC skill applies its SC at the rAthena rate + duration (test per SC).
- Star Gladiator feeling/hatred sets the PC state, and a Warm/Comfort skill reads it (test the round-trip).
- Kick skills deal damage with the `battle.cpp:4590` ratio; `_ATK` follow-ups fire (test).
- Zero shells remain in `Taekwon/` (no plugin whose only body is `BroadcastSkillNoDamage`), except genuine animation-only skills documented as such with the rAthena arm proving they have no effect.
- No `TODO` / "animation only" / "ball allocation TODO" comments remain on skills that DO have an effect.

## Test plan

- `TaekwonSoulballTests` — SoulCollect raises count to cap; SoulRevolution drains balls + heals SP.
- `SoulReaperScTests` — each SC skill applies the right SC at the right duration (seeded SkillDb).
- `StarGladiatorFeelingTests` — set feeling via the feeling skill, assert a Warm skill reads it.
- `TaekwonKickTests` — kick ratio matches `battle.cpp:4590`; `_ATK` follow-up lands.
- DI audit green.

## Shell sub-clusters (the 37)

- **Soul Reaper `SP_*` (≈14):** SoulCollect, SoulRevolution, SoulGathering, SoulEnergy, SoulUnity, SoulDivision, SoulCurse, SoulExplosion, SpiritofRebirth, SoulOfHeavenAndEarth, etc. — soulball economy + SC grants. (`skill.cpp:9320`/`:10895` region.)
- **Star Gladiator feeling/hate/spirit (≈8):** Feeling/Hatred of the Sun/Moon/Stars, FalconsSoul/GolemsSoul/ShadowsSoul/FairysSoul. — feel-map / hate-target state. (`skill.cpp:14971` warm region.)
- **TK/SJ kick damage + `_ATK` (≈10):** jump/storm/turn/counter kicks. — ratio + `_ATK` follow-up. (`skill.cpp:1770`/`:5795`, `battle.cpp:4590`.)
- **Documentation/passive (≈5):** SP_SOULENERGY-style `pc_checkskill` gates — classify per SKILL-06 (B), no no-op plugin.

## Notes / gotchas

- Soulball vs spiritball vs spirit-sphere are three different counters in rAthena — don't collapse them. Soul Reaper uses soul balls (`pc_addsoulball`), Star Gladiator/Monk use spirit spheres.
- The `BroadcastSkillNoDamage`-only body is the tell-tale shell signature in this family — grep for plugins whose only statement is that broadcast and triage each against its `skill.cpp` arm.
- Star Gladiator feeling/hate state is day-of-week + map dependent in rAthena; if the C# server doesn't model game-day yet, the feeling skill can still set the state and the Warm skill read it — note the day-gate as a follow-up if the calendar isn't modeled, but DON'T leave the state unset.
- SKILL-01 + SKILL-04 must land first so the SC grants use the apply-rate + skill_db duration path, not literals.
