# MS3 · Combat

**Phase:** MS3 (adjacent)
**Depends on:** [movement.md](../movement.md), [entities.md](../entities.md), [mob-db.md](../mob-db.md), [status.md](status.md), [items.md](items.md)
**Blocks:** anything that depends on entities killing each other (mob spawn already handles death; quests' kill counters)

The first big MS3 work. Combat ties together attack actions, damage formulas, status flags, hit/flee/crit chance, knockback, death, and the loot/exp chain. rAthena's [battle.cpp](/Volumes/1TB/Projetos/rathena/src/map/battle.cpp) is 12K lines; expect this to be ~3 weeks of focused work for one engineer.

## Source of truth

- [rathena/src/map/battle.cpp](/Volumes/1TB/Projetos/rathena/src/map/battle.cpp) — damage calc, hit chance, attack flow
- [rathena/src/map/unit.cpp](/Volumes/1TB/Projetos/rathena/src/map/unit.cpp) — `unit_attack`, `unit_attack_timer` (the attack loop), `unit_attack_timer_sub`
- [rathena/src/map/pc.cpp](/Volumes/1TB/Projetos/rathena/src/map/pc.cpp) — `pc_damage`, `pc_dead`, exp distribution
- [rathena/src/map/mob.cpp](/Volumes/1TB/Projetos/rathena/src/map/mob.cpp) — `mob_damage`, `mob_dead`, drops

## Scope (MS3 first pass)

**In scope:**
- Auto-attack (continuous melee): `CZ_REQUEST_ACT (0x0089)` from client → walk to target if out of range → swing at `ASPD` rate.
- Damage calculation: rAthena pre-renewal **or** renewal formula (must pin a mode, same as packetver decision).
- Hit / flee / crit / perfect dodge.
- Damage type: melee, ranged, magic.
- Element + race + size modifiers.
- Death: HP=0 → broadcast death packet, drop loot (items doc), distribute exp.
- Mob aggression toward players within `view_range` (mob_db.View?) for aggressive mobs.

**Out of scope:**
- Skill damage (skills doc).
- Status-based effects (curse, freeze, sleep): defined in status doc; damage modifiers applied here.
- PvP / GvG rules (battlegrounds, WoE) — separate later phase.
- Tank threat / aggro tables — first pass uses simple "last hit" attribution.

## Done

Nothing.

## Pending

1. **Pin combat formula version.** Renewal vs pre-renewal damage formulas differ wildly (defense as %, MATK split, etc.). Same source-of-truth decision as `RenewalMode` config. Match the [world.md](../world.md) renewal pick.

2. **`UnitData` extension** on `Entity`: ATK, MATK, DEF, MDEF, HIT, FLEE, CRI, ASPD, attack range. Players read from inventory + bonuses (items doc); mobs from `MobDbEntry`.

3. **`AttackService`:**
   - `TryStartAttack(attacker, target)` — validates target alive + in attack range, sets `attacker.AttackState = (target, nextSwingAt)`.
   - `Tick` — for each entity with an active attack, if `nextSwingAt <= now`, perform damage calc + emit packets, schedule next swing.
   - Continuous attack: keeps swinging until target dies or moves out of range or attacker stops.

4. **`DamageCalculator`** — port rAthena's `battle_calc_attack` (the function that returns a `Damage` struct with hits, total damage, flags). This is the gnarly center of combat math.

5. **Death + exp distribution.** Last-hit-wins for MS3; mob's exp pool split between party (if any) per rAthena rules. Calls `SetCharacterOnline` lifecycle update (the char might not log off but stats change; that's the inventory IPC).

6. **Drops.** On mob death, roll its drop table (`mob_db.Drops`) and instantiate `ItemEntity` on the map (items doc).

### Acceptance
- A player can auto-attack a Poring next to them; HP ticks down on both sides; mob dies; player receives exp.
- Damage formula matches rAthena's renewal output for a known stat combo (sanity-check vs rAthena online calculator).
- Two players attacking the same mob: kill-credit goes to last hit; both party members get exp split (party doc must land first).

## History
- **2026-05-16** — Plan stub.
