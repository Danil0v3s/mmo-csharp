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
- Damage calculation: rAthena **renewal** formula (defense as %, ATK/MATK split, etc.). Pre-renewal is out of scope.
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

**Scaffolding slice (MS3 first pass — the HP-mutation pipeline that everything else plugs into):**

- HP / MaxHp surfaced on entities:
  - [`PlayerEntity`](../../../../Map.Server/Entities/PlayerEntity.cs) carries `Hp` / `MaxHp` (defaults 40/40 — placeholder until status recalc lands).
  - [`MobEntity`](../../../../Map.Server/Entities/MobEntity.cs) gained an explicit `MaxHp` (mirrors `DbEntry.Hp` at spawn) alongside the existing current-HP setter.
- [`ZC_NOTIFY_ACT3`](../../../../Core.Server/Packets/Out/ZC/ZC_NOTIFY_ACT3.cs) (0x08c8, 34 bytes) — renewal 32-bit damage packet. Includes the `DamageActionType` enum (`Normal`, `Flee`, `Critical`, …) so future damage paths just set the right code.
- [`IDamageService`](../../../../Map.Server/Combat/IDamageService.cs) / [`DamageService`](../../../../Map.Server/Combat/DamageService.cs):
  - `ApplyDamage(target, amount, source?)` → clamps to remaining HP, mutates HP, broadcasts `ZC_NOTIFY_ACT3` to AOI, fires the death pipeline on HP=0.
  - Mob death routes through `IMobSpawnService.KillMob` (reuses the existing vanish broadcast + respawn schedule).
  - PC death: vanish broadcast + registry removal. Savepoint warp / corpse-revive UX lands with the broader respawn flow.
- [`@damage <amount>`](../../../../Map.Server/Gm/Commands/DamageCommand.cs) (GroupId ≥ 60) — applies flat damage to the nearest mob in AOI; lets the pipeline be exercised end-to-end without auto-attack timing.
- 5 tests in [Map.Server.Tests/Combat/](../../../../Map.Server.Tests/Combat/) covering HP-clamp, ACT broadcast, mob death routing, PC death, and the flee/zero-damage branch.

## Pending

All MS3 combat / status / skill foundation items below are now **Done** — see the History block. What's left:

1. **`battle_config.item_rate_*` drop modifiers** — boss / heal / use / equip / card multipliers per `conf/battle/drops.conf`. Hook point: `MobSpawnService.RollAndDropLoot` rate clamp.

2. **MVP drops + MVP-only rewards** — `mob_db.MvpDrops` is read but not yet emitted. Needs the MVP-rank check (top damager rather than last hitter) before the drop roll.

3. **Per-attacker tdmg table** — rAthena tracks damage per attacker for MVP rank, share-rules, exp scaling. Today's last-hit attribution covers basic gameplay; the full table lands with quest / achievement triggers that depend on it.

4. **Skill damage post-defense passes** — `battle_calc_cardfix`, `battle_calc_attack_post_defense`, weapon-mastery bonuses, refine bonuses. Tightens BattleCalculator output to per-equip card/refine values once those parse.

### Acceptance
- A player can auto-attack a Poring next to them; HP ticks down on both sides; mob dies; player receives exp.
- Damage formula matches rAthena's renewal output for a known stat combo (sanity-check vs rAthena online calculator).
- Two players attacking the same mob: kill-credit goes to last hit; both party members get exp split (party doc must land first).

## History
- **2026-05-16** — Plan stub.
- **2026-05-16** — Scaffolding slice shipped: HP on entities, ZC_NOTIFY_ACT3 wire format, IDamageService.ApplyDamage with HP-mutation + death pipeline, @damage GM command, 5 unit tests. Drop rolling / damage formula / auto-attack loop / EXP distribution remain queued.
- **2026-05-16** — Drop rolling closed via `IItemCatalog` (DB-backed item_db) → `MobSpawnService.RollAndDropLoot` → `IItemDropService.DropOnFloor`. Damage formula, auto-attack loop, EXP distribution, MVP/party rules still queued.
- **2026-05-19** — **MS3 combat foundation complete.** Major slice over multiple commits. **BattleStats** (`Map.Server/Status/BattleStats.cs`) now mirrors rAthena `struct status_data` per-entity. **StatusCalcService** runs at session enter (PCs) + spawn (mobs) and ports renewal `status_calc_misc` + `status_base_atk`. **BattleCalculator** (`Map.Server/Combat/BattleCalculator.cs`) ports `battle_calc_weapon_attack` slice 1 — crit roll, hit/flee roll, base damage with rAthena renewal crit×1.4 tail, full 4-level ATTRIBUTE_DB element table, renewal RE-DEF formula `dmg*(4000+eDEF)/(4000+10*eDEF) - sDEF`. **AttackService** drives `unit_attack_timer` cadence with chase / range check / single-shot vs continuous. **ExpService** ports `pc_gainexp` + `pc_checkbaselevelup` walks for the Novice exp table; status-points awarded on level-up, full-heal on level-up. **StatusChangeService** + StatusEffectRegistry — engine for buffs/DoTs/HoTs with 5 starter SCs (Poison/Blessing/IncreaseAgi/DecreaseAgi/HealOverTime), refresh-on-restart, OnPeriodic via damage pipeline. **SkillCastService** + SkillDb — 6 starter skills (Bash/Heal/IncAgi/Blessing/FireBolt/ColdBolt) with full cast-lifecycle (range/sp/cooldown/cast time/resolve by damage-kind). **SkillUnitService** ground effects (Magnus Exorcismus / Storm Gust). **NaturalHealService** baseline regen with sitting bonus + walking gate. **PcDeathService** + **PcSetposService** — death penalty / savepoint respawn / cross-map warp. **MobAiService** aggressive target acquisition. **SummonAiService** generic follow + assist for pet/homun/merc/elem/slave. **MobSkillEntry** + Always/LowHp condition triggers for `mobskill_use`. **PartyShareService** even-share + bonus (+10% per extra member). **EquipBonusAggregator** equip→stats. Loot-protection windows (owner + party). **PetService** summon + hunger/intimacy. **CZ_USE_ITEM** + **ItemUseService** starter potion table. Pickup → InventoryService.GiveItem closes the loot loop. **CZ_STATUS_CHANGE** + StatPointTable (renewal cost formula) → pc_statusup; **CZ_UPGRADE_SKILLLEVEL** + pc_checkskill gate. **Persistence** sync: live BaseExp/Level/JobLevel/StatusPoints/SkillPoints/LastX/LastY on autosave. Test suite: 263 → 274 (all green excluding the long-standing replay-baseline failure which is unrelated to this work).
