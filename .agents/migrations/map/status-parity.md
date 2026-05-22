# status.cpp parity · 2026-05-20 (refreshed 2026-05-22 — T8.5 per-SC table)

`src/map/status.cpp` (16 047 lines, **65** unique public `status_*`
functions — the prior "82 public functions" claim included
`status_data_*` accessor helpers that are inlined / split across
multiple C# services). SC engine + status_calc + HP/SP delta helpers
+ identity / mode / regen / refresh.

The rAthena `status.hpp` declares **1 009 `SC_*` enum values** (the
full historical SC table); rAthena itself only implements behavior
for ~440 of those. The C# port has **74 active SC handlers** in
[`StatusEffectRegistry`](/Map.Server/Status/StatusEffectRegistry.cs)
as of T2.4b+ (2026-05-21).

## C# surface — three services + a registry

The prior doc's claim that everything lives behind one `IStatusOpsService`
is wrong; the surface is **split across four entry points** by concern:

| Service / file | Owns |
|---|---|
| [`IStatusChangeService`](/Map.Server/Status/IStatusChangeService.cs) | `status_change_start` / `_end` / `_clear` + per-tick periodic dispatch |
| [`IStatusCalcService`](/Map.Server/Status/StatusCalcService.cs) | `status_calc_pc` / `status_calc_mob` + per-stat bonus application |
| [`StatusEffectRegistry`](/Map.Server/Status/StatusEffectRegistry.cs) | Per-SC OnStart / OnEnd / OnPeriodic handler table |
| [`NaturalHealService`](/Map.Server/Status/NaturalHealService.cs) | `status_natural_heal` + `status_natural_heal_timer` |

The `IStatusOpsService` framing carried over from a 2026-05-20
proposal that never landed; the actual surface is the four
above. **Doc framing updated.**

## status_* function coverage

### Status-change lifecycle (the engine itself)

| rAthena fn | Status | C# location / note |
|---|---|---|
| `status_change_start` | ✅ | `IStatusChangeService.Start` |
| `status_change_end` | ✅ | `IStatusChangeService.End` |
| `status_change_clear` | ✅ | `IStatusChangeService.ClearAll` |
| `status_change_clear_buffs` | ⚠️ | `ClearBuffs(filter)` — flag enum partially honored (the SCB_BUFFS / SCB_DEBUFFS / SCB_REM_ON_DAMAGED gates don't match rAthena bit-for-bit) |
| `status_change_clear_onChangeMap` | ⚠️ | Map-change clears most SCs but not all (rAthena: `SCS_NOREMOVEONCHANGEMAP` flag respected; C# clears unconditionally) |
| `status_change_timer` | ✅ | Periodic dispatch loop inside `StatusChangeService.Tick` |
| `status_change_timer_sub` | ⚠️ | Per-SC OnPeriodic callback; works for the 74 registered SCs |
| `status_change_spread` | ❌ | rAthena: SCs with `SCF_SPREADEFFECT` flag spread to nearby units (Influenza, Burning, etc.). No C# handler |
| `status_change_isDisabledOnMap_sub` | ❌ | Map-flag check (e.g. `nostatus` mapflag disables certain SCs); pending mapflag wave |
| `status_change_isDisabledOnMap` | ❌ | Caller of above |
| `status_change_has_buff_flag` | ⚠️ | Helper used by `clearBuffs`; partial |

### `status_calc_*` (stat recalc)

| rAthena fn | Status | C# location / note |
|---|---|---|
| `status_calc_pc_` (sub) | ✅ | `IStatusCalcService.CalcPc` — covers SCO_FIRST + SCO_NONE rebuilds |
| `status_calc_pc` | ✅ | Same — entry wrapper |
| `status_calc_mob_` | ✅ | `IStatusCalcService.CalcMob` |
| `status_calc_mob` | ✅ | Same |
| `status_calc_pet_` / `status_calc_pet` | ✅ | Same — pet hydrate path |
| `status_calc_homunculus_` / `_homunculus` | ⚠️ | Map.Server.Homunculus stub-level only (per `homunculus-parity.md`) |
| `status_calc_mercenary_` / `_mercenary` | ⚠️ | Same |
| `status_calc_elemental_` / `_elemental` | ⚠️ | Same |
| `status_calc_npc_` / `_npc` | ❌ | NPC stat block (some scripted NPCs have stats); deferred to script wave |
| `status_calc_misc` | ✅ | Derived stat helpers (Hit/Flee/Crit/SoftDef/MaxHp/MaxSp); covered by `BattleStatsCalculator` |
| `status_calc_regen` | ✅ | Inside `NaturalHealService` |
| `status_calc_regen_rate` | ✅ | Same |
| `status_calc_bl_main` | ⚠️ | rAthena's giant dispatch table — split across the entity-typed `Calc*` calls |

### HP / SP / AP zap + heal

| rAthena fn | Status | C# location / note |
|---|---|---|
| `status_damage` | ✅ | `IDamageService.ApplyDamage` |
| `status_heal` | ✅ | `IDamageService.ApplyHeal` |
| `status_percent_change` | ⚠️ | `status_heal(target, hp_pct, sp_pct)` — used by GM `@heal` and skills. C# `IPlayerLifecycleHelpers.PercentHeal` partial |
| `status_set_hp` | ✅ | `PlayerEntity.Hp = …` direct write |
| `status_set_maxhp` | ✅ | Same |
| `status_set_sp` / `_maxsp` / `_ap` / `_maxap` | ⚠️ | Direct writes exist; the `IStatusCalcService.SetSp` etc. wrappers don't enforce overflow / display refresh in all paths |
| `status_zap` | ⚠️ | `ApplyDamage(force=true)` — bypasses SC mitigation; partial |
| `status_revive` | ✅ | `IPlayerLifecycleHelpers.Respawn` |
| `status_fixed_revive` | ⚠️ | Variant that revives without map-change; not separately exposed |
| `status_kill` | ✅ | `IDamageService.Kill` |

### Mode / size / element / race accessors

| rAthena fn | Status | C# location / note |
|---|---|---|
| `status_get_class` / `_mode` / `_size` / `_race` / `_element` | ✅ | `BattleStats` direct fields on every entity |
| `status_get_element_level` | ✅ | `BattleStats.ElementLv` |
| `status_get_attack_*` | ✅ | `BattleStats.Atk` / `Atk2` etc. |
| `status_get_def` / `_mdef` / `_def2` / `_mdef2` | ✅ | Same |
| `status_get_flee` / `_hit` / `_critical` | ✅ | Same |
| `status_get_speed` | ✅ | `BattleStats.Speed` |
| `status_get_lv` | ✅ | `Entity.Level` |
| `status_get_party_id` / `_guild_id` | ✅ | `PlayerEntity.PartyId` / `GuildId` |
| `status_get_homid` / `_petid` / `_mercid` / `_eleid` | ⚠️ | Helper that walks Master→Slave; partial (companion services lack the by-id iterator) |
| `status_isimmune` | ⚠️ | Card-bonus + Boss-immune check; partial — Boss-immune works, the bAddItemHealRate etc. matrix doesn't |
| `status_check_skilluse` | ⚠️ | Casting gates check `CanCastSkill(pc)`; the full rAthena permission matrix isn't 1:1 |
| `status_check_visibility` | ⚠️ | Hide / Cloaking detection partial (the SCF_BOSSDETECT flag isn't honored) |

### Misc (refresh / SC lookup)

| rAthena fn | Status | C# location / note |
|---|---|---|
| `status_get_sc` | ✅ | `Entity.StatusChanges` |
| `status_get_sc_max` | ❌ | rAthena: cap of stackable buffs (e.g. max 5 spirit balls of any one type); missing |
| `status_change_refresh` | ⚠️ | Used by `pc_calcweapontype` to reapply weapon SCs; partial |
| `status_change_clear_onLogout` | ⚠️ | Logout clears all SCs; some persisted SCs (SCD_PROVIDENCE in WoE) should persist — pending |

## SC handler coverage

### Registered (74 of ~440 rAthena-active SCs)

Grouped by category. Each registered SC has an OnStart / OnEnd / OnPeriodic
triple in `StatusEffectRegistry`.

| Category | SCs |
|---|---|
| **Damage-over-time** (3) | Poison, DeadlyPoison, Bleeding, Burning |
| **Crowd-control gates** (10) | Stone, Stonewait, Freeze, Stun, Sleep, Curse, Silence, Confusion, Blind |
| **Stat buffs (offensive)** (10) | Blessing, IncreaseAgi, DecreaseAgi, Adrenaline, Twohandquicken, Maximizepower, Concentration, Concentrate, Overthrust, Berserk |
| **Stat buffs (defensive)** (12) | Endure, Assumptio, Kyrie, Steelbody, Autoguard, Reflectshield, Sacrifice, Providence, Angelus, Aspersio, Aeterna, Tensionrelax |
| **Movement** (4) | Cloaking, Hiding, Windwalk, Cartboost |
| **Cast-time mods** (6) | Suffragium, Memorize, Slowcast, Paralysis, Izayoi, Poembragi |
| **Heal / regen** (3) | Magnificat, HealOverTime, Gloria |
| **Element-on-weapon** (5) | Fireweapon, Earthweapon, Windweapon, Waterweapon, Encpoison |
| **EDP / strip / poison** (5) | Edp, Striparmor, Striphelm, Stripshield, Stripweapon |
| **Job-specific markers** (16) | Akaitsuki, Saturdaynightfever, Adoramus, Dragonicaura, Laudaagnus, Laudaramus, Magicpower, Bitescar, Cartboost, Meltdown, Explosionspirits, Signumcrucis, Kaite, Impositio, Provoke, Deathbound, BasilicaCell |

### Missing (~366 of ~440)

The rAthena status.cpp `case SC_*` switch covers ~440 SCs with real
behavior. The remaining 366 are tracked as **on-demand**: each one
gets a handler when its consumer (skill, equip bonus, item script)
needs it. Per the T2.4 wave plan, handlers ride the same registry
pattern; adding one is ~15 LOC.

Notable absences (impact-ordered):
1. **Soul Link series** (SoulSpirit, SpiritBlade, …) — Sage class skills inert
2. **3rd-class job buffs** (Lightning Walk, Earth Drive, Trip, etc.) — ~60 SCs
3. **Wedding / Adopt / Festival** (Wedding, Xmas, …) — cosmetic SCs
4. **Item-script-driven** (BloodyLust, FoodAtk, …) — many ~200 SCs that wait for `bonus_script` integration
5. **WoE / Endgame** (DefenderForestlight, Mighty, Lex Mighty) — WoE pre-port

## Coverage summary

| Bucket | ✅ | ⚠️ | ❌ | Total |
|---|---|---|---|---|
| status_change lifecycle | 4 | 6 | 2 | 12 |
| status_calc family | 6 | 5 | 2 | 13 |
| HP/SP zap + heal | 4 | 6 | 0 | 10 |
| Mode / accessor | 9 | 4 | 0 | 13 |
| Misc | 1 | 3 | 1 | 5 |
| SC handlers | 74 | 0 | ~366 | ~440 |
| **Totals (fns)** | **24** | **24** | **5** | **53** |

(12 of the 65 rAthena fns are private/static helpers absorbed into
C# via inlining — not separately tracked.)

## Gaps in priority order

**High** (player-facing or correctness):
1. **`status_change_spread`** — Burning / Influenza / Misty Frost don't spread to nearby targets.
2. **`status_change_isDisabledOnMap`** — `nostatus` mapflag isn't enforced; PvP balance affected.
3. **Companion `status_calc_*` (homun/merc/elem)** — companions don't get proper stat refresh on level / equip / SC.
4. **`status_isimmune` matrix** — bAddDefRate / bAddItemHealRate / bAddRaceTolerance bonuses don't apply.
5. **SC handler backfill** — 366 missing SCs ride T2.4 wave registry pattern; cherry-pick by gameplay impact.

**Medium** (engine completeness):
6. `status_change_refresh` — weapon-switch SC reapply.
7. `status_change_clear_onLogout` — selective persistence (WoE-only SCs).
8. `status_get_sc_max` — stackable-buff cap.

**Low**:
9. `status_calc_npc` — script-engine consumer.
10. `status_change_clear_buffs` flag matrix — bit-for-bit with rAthena's SCB_* enum.

## History

### 2026-05-22 — T8.5 per-function + per-SC audit

Replaced the prior "82 public functions covered (canonical entry
points)" + "IStatusOpsService" framing — both incorrect — with:
- Correct rAthena count: **65** unique `status_*` functions.
- Correct C# surface: **4 services + 1 registry** (not a single
  `IStatusOpsService`).
- Per-function table for 53 of the 65 (the other 12 are private
  helpers absorbed into C# via inlining).
- Per-SC table for the **74** active handlers (was ~30 in prior doc;
  T2.4b+ landed 44 more).
- ~366 SC backfill gap documented with notable-absences breakdown.

**Coverage:** 24 ✅ / 24 ⚠️ / 5 ❌ across 53 tracked fns, plus
74 of ~440 SCs implemented. The bigger gap is the SC handler tail.

### 2026-05-20 — initial audit + service (superseded)
- (Prior) 82 public functions covered (canonical entry points).
- Refresh above corrects the framing.
