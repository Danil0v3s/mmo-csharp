# GP-PET-LOYALTY-BONUS — loyal pets grant their support bonus

> **Epic:** gameplay · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** SCR-DOMAIN (the pet bonus/support script commands) · **Unlocks:** none

## The deliverable

> A pet at loyal intimacy grants its configured support bonus to the owner (stat bonus / periodic
> heal / support skill) — matching rAthena's pet bonus + support-skill timers.

## Player story / why it matters

GP-PET's done-criteria include "loyalty bonus applies": once a pet reaches loyal intimacy
(`PET_INTIMATE_LOYAL`, 900), rAthena runs the pet's bonus chain — a passive stat bonus
(`petskillbonus` / `bonus`), a periodic heal (`petrecovery`), or a support skill (`petskillsupport`),
all configured in the pet_db `Script` / `SupportScript`.

**Why this is split out (genuine dependency):** those effects are configured only by the
`petskillbonus` / `petskillsupport` / `petrecovery` / `bonus` **script commands** in the pet_db
script fields. The C# `PetDbEntity` carries the raw `Script`/`SupportScript` strings, but those
builtins don't exist until the NPC scripting runtime lands (SCR-DOMAIN). Per the standing pivot
(scripting truly last), the loyalty bonus waits for the scripting domain — it can't be implemented now
without a stub.

## Current state — per layer

| Layer | Exists? | Where / what's missing |
|---|---|---|
| Intimacy tracking | ✅ | `PetEntity.Intimacy`; loyal gate (≥900) used by evolution/target-check |
| Bonus/support config | ☐ | only set by `petskillbonus`/`petskillsupport`/`bonus` script commands → needs SCR-DOMAIN |
| Bonus application | ☐ (stub) | `PetOpsService.ExeAutoBonus` logs only; `ClearSupportBonuses`/`AddAutoBonus` store but don't apply |

## rAthena reference

- `rathena/src/map/pet.cpp` — `pet_bonus_timer` / `pet_skill_support_timer` / `pet_recovery_timer`,
  and the `petskillbonus` / `petskillsupport` / `petrecovery` / `petloot` script commands that set them.

## Scope — every layer

- [ ] Once SCR-DOMAIN provides the pet script commands, parse the pet's SupportScript on hatch into the
      configured bonus(es).
- [ ] Apply the loyal-intimacy stat bonus / periodic heal / support skill (the `pet_*_timer` chain).

## Done criteria

- A loyal pet grants its configured stat bonus / heal / support skill; a non-loyal pet does not.

## Test plan

- Service test: a loyal pet with a configured bonus applies it; an un-loyal one doesn't.

## Notes

- Filed by GP-PET (turn 7). Pairs with GP-PET-AUTOSKILL (both need the pet script builtins). The
  intimacy/loyalty plumbing is done; only the script-configured effects wait on scripting.
