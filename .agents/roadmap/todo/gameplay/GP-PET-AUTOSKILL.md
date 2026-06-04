# GP-PET-AUTOSKILL — pet casts its attack skill in combat

> **Epic:** gameplay · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** SCR-DOMAIN (the `petskillattack` builtin) · **Unlocks:** none

## The deliverable

> A pet with a configured attack skill rolls and casts it on the master's target while fighting —
> matching rAthena `pet_attackskill` — gated by intimacy, equip, and the skill's rate/bonusrate.

## Player story / why it matters

GP-PET landed the full pet lifecycle (tame/hatch/feed/rename/return) + follow + assist (basic attack)
+ the loot bag. The one remaining combat piece is the pet's **special attack skill** — e.g. a Poring's
or a high-tier pet's `petskillattack`. rAthena's `pet_attackskill` (pet.cpp:708) rolls
`a_skill->rate + intimate * a_skill->bonusrate / 1000` out of 100 and, on success, casts the skill on
the target (ground skill → `unit_skilluse_pos`, else `unit_skilluse_id`).

**Why this is split out (genuine dependency, not a deferral):** the pet's attack skill (`pd->a_skill`)
is NOT in `pet_db` as data — it is set exclusively by the `petskillattack <skill>,<lv>,<rate>,<bonusrate>`
**script command** inside the pet_db `SupportScript`/`Script` field. The C# `PetDbEntity` carries the
raw `Script`/`SupportScript` strings, but `petskillattack` is a scripting builtin that does not exist
until the NPC scripting runtime lands (SCR-DOMAIN). Without it there is no skill to cast, so any
implementation now would be a stub. Per the standing pivot (scripting truly last), this waits for the
scripting domain.

## Current state — per layer

| Layer | Exists? | Where / what's missing |
|---|---|---|
| Basic attack assist | ✅ | `Map.Server/Mob/SummonAiService.cs` — pet latches master's target |
| Attack-skill data (`a_skill`) | ☐ | only set by `petskillattack` script command → needs SCR-DOMAIN |
| `pet_attackskill` roll + cast | ☐ (stub) | `Map.Server/Pet/PetOps/PetOpsService.cs` `AttackSkill` returns 0 |
| Skill-cast dispatch for a pet (BL_PET) | partial | `ISkillUnitService`/skill cast exists for PCs; pet-as-caster path unverified |

## rAthena reference

- `rathena/src/map/pet.cpp` `pet_attackskill` (708) — gates (`pet_status_support`, `a_skill`,
  `pet_equip_required`), `canact_tick`, the `rate + intimate*bonusrate/1000` roll, target validation
  (`range3`), and the `unit_skilluse_pos`/`unit_skilluse_id` cast.
- The `petskillattack` / `petskillattack2` / `petskillsupport` script commands (set `a_skill` etc.).

## Scope — every layer

- [ ] Once SCR-DOMAIN provides `petskillattack`, populate `PetEntity.AttackSkill` (id/lv/rate/bonusrate)
      from the pet's SupportScript run on hatch.
- [ ] Implement `PetOpsService.AttackSkill` as rAthena `pet_attackskill`: the gates + roll + skill cast
      (ground vs id) through the skill engine with the pet as caster.
- [ ] Wire it into the pet attack tick (when the pet has a target).

## Done criteria

- A pet with `petskillattack` configured casts that skill on the master's target at the configured
  rate (scaled by intimacy), respecting equip/intimacy gates and `range3`.

## Test plan

- Service test: `AttackSkill` rolls + dispatches the configured skill when gates pass; no-op when they
  don't (no skill, low intimacy, out of range, on cooldown).

## Notes

- Filed by GP-PET (turn 6). Blocked on the scripting runtime — the only GP-PET piece that genuinely
  needs it. Everything else (lifecycle, loot, persistence) is independent.
