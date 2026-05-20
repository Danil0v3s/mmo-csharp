# battle.cpp parity · 2026-05-20

`src/map/battle.cpp` (12 432 lines, 41 unique `battle_*` public
functions, plus the `struct Damage` packet) is the damage pipeline:
weapon / magic / misc damage calc, element fix, defense reduction,
card modifiers, zone scaling (PvP / GvG / BG), reflect, drain,
delayed damage, friend/foe gating, ammo + autocast hooks. The
companion header `battle.hpp` (793 lines) exports the
`battle_config` struct (~600 knobs) and several damage flag enums.

## Status legend

- ✅ implemented — full or near-full parity with rAthena
- ⚠️ partial — exists but has gaps documented inline
- ❌ missing — no C# equivalent

## Subsystem coverage

### Damage calculation chain

| rAthena fn | Status | C# location / note |
|---|---|---|
| `battle_calc_attack` | ⚠️ | [BattleCalculator.CalcWeaponAttack](/Map.Server/Combat/BattleCalculator.cs) covers weapon only; magic + misc branches missing |
| `battle_calc_weapon_attack` | ✅ | `BattleCalculator.CalcWeaponAttack` |
| `battle_calc_base_damage` | ✅ | `BattleCalculator` inline base-ATK formula |
| `battle_calc_damage` | ⚠️ | applied inline; `Damage` struct doesn't carry the rAthena fields (`basedamage`, `isspdamage`, `damage2`) |
| `battle_attr_fix` | ✅ | [ElementTable](/Map.Server/Status/ElementTable.cs) — element matrix verbatim |
| `battle_calc_cardfix` | ❌ | Card modifier accumulation (attacker / target cards, NK flags) |
| `battle_addmastery` | ❌ | Weapon-mastery + lord-knight bonus passive |
| `battle_calc_chorusbonus` | ❌ | Minstrel/Wanderer chorus ATK bonus |
| `battle_calc_return_damage` | ❌ | Reflect calculation (Auto Guard, Shield Reflect, Maya's Purple Card) |
| `battle_do_reflect` | ❌ | Reflect damage application |

### Zone-specific damage rates

| rAthena fn | Status | C# location / note |
|---|---|---|
| `battle_calc_gvg_damage` | ❌ | `gvg_*_damage_rate` scaling missing |
| `battle_calc_bg_damage` | ❌ | `bg_*_damage_rate` scaling missing |
| `battle_calc_pk_damage` | ❌ | `pk_*_damage_rate` scaling missing |

### Damage application

| rAthena fn | Status | C# location / note |
|---|---|---|
| `battle_damage` | ⚠️ | [DamageService.ApplyDamage](/Map.Server/Combat/DamageService.cs) covers HP delta + death routing; doesn't honor walkdelay / dmotion |
| `battle_fix_damage` | ⚠️ | Same as `battle_damage` — caller passes raw damage |
| `battle_delay_damage` | ❌ | Delayed-damage timer (skill cast → land window) |
| `battle_damage_area` | ❌ | AoE damage application helper |
| `battle_vanish_damage` | ❌ | Vanish series full-HP drain (Vanishing Buster, Soul Vanishing) |
| `battle_vellum_damage` | ❌ | Vellum equipment damage type (% of MaxHP) |
| `battle_status_block_damage` | ❌ | SC-driven damage blockers (Steel Body etc.) |

### Target / range / check

| rAthena fn | Status | C# location / note |
|---|---|---|
| `battle_check_target` | ✅ | [DamageService.CanDamage](/Map.Server/Combat/DamageService.cs) — same-party/guild + nopvp |
| `battle_check_range` | ✅ | [AttackService.InRange](/Map.Server/Combat/AttackService.cs) — Chebyshev king-move |
| `battle_gettarget` | ❌ | Returns the active target of an attacking entity |
| `battle_gettargeted` | ❌ | Returns the set of entities currently targeting `target` |
| `battle_getenemy` | ❌ | Nearest-enemy scan helper |
| `battle_get_master` | ❌ | Lookup master/owner (pet, homun, merc, slave) |
| `battle_getcurrentskill` | ❌ | Read the unit's in-flight skill id |
| `battle_check_undead` | ❌ | Undead element / race check |
| `battle_check_coma` | ❌ | Coma proc roll |
| `is_infinite_defense` | ❌ | Infinite-def check (Steel Body, certain mobs) |
| `battle_can_hit_bg_target` | ❌ | BG zone friendly-fire gate |
| `battle_can_hit_gvg_target` | ❌ | GvG zone friendly-fire gate |

### Combat entry

| rAthena fn | Status | C# location / note |
|---|---|---|
| `battle_weapon_attack` | ✅ | [DamageService.PerformMeleeAttack](/Map.Server/Combat/DamageService.cs) |
| `battle_autocast_aftercast` | ❌ | Auto-cast skill after a swing (Magnum proc, etc.) |
| `battle_autocast_elembuff_skill` | ❌ | Element-buff auto-cast (Flame Launcher, etc.) |
| `battle_consume_ammo` | ❌ | Ammo decrement on ranged attacks |

### Drain / reflect / element

| rAthena fn | Status | C# location / note |
|---|---|---|
| `battle_drain` | ❌ | HP/SP drain on hit (Hunter Bow card, etc.) |
| `battle_get_weapon_element` | ❌ | Resolve attacker's weapon element |
| `battle_get_magic_element` | ❌ | Resolve magic element (skill default vs cast override) |
| `battle_get_misc_element` | ❌ | Resolve misc-attack element |

### Battle config (battle_athena.conf)

| rAthena fn | Status | C# location / note |
|---|---|---|
| `battle_get_value` | ❌ | Read battle_config knob by name |
| `battle_set_value` | ❌ | Write battle_config knob by name |
| `battle_config_read` | ❌ | Parse battle_athena.conf |
| `battle_set_defaults` | ❌ | Default battle_config values |
| `battle_adjust_conf` | ❌ | Cross-knob validation |

### Lifecycle

| rAthena fn | Status | C# location / note |
|---|---|---|
| `do_init_battle` | ✅ | Handled by DI service lifecycle |
| `do_final_battle` | ✅ | Handled by DI service lifecycle |
| `battle_get_exception_ai` | ❌ | mob AI exception list (mob_db modes) |

## Coverage summary

| Bucket | Done | Partial | Missing |
|---|---|---|---|
| Damage calculation chain | 3 | 2 | 5 |
| Zone-specific damage rates | 0 | 0 | 3 |
| Damage application | 0 | 2 | 5 |
| Target / range / check | 2 | 0 | 10 |
| Combat entry | 1 | 0 | 3 |
| Drain / reflect / element | 0 | 0 | 4 |
| Battle config | 0 | 0 | 5 |
| Lifecycle | 2 | 0 | 1 |
| **Totals** | **8** | **4** | **36** |

48 of 41 entries tracked here (some helpers shared across
subsystems). Of those, 8 (17%) are full parity, 4 (8%) partial,
36 (75%) missing. Damage application + target helpers carry the
most gameplay weight; battle_config knobs are admin-only and ship
last.

## Implementation plan

Waves prioritised by gameplay impact (combat correctness >
side-system polish > admin knobs).

1. **B-H1** — `IBattleCardService` (`battle_calc_cardfix` +
   `battle_addmastery`). Card modifiers + weapon mastery flow into
   every weapon swing — most visible damage delta vs rAthena.
2. **B-H2** — Reflect (`battle_calc_return_damage` +
   `battle_do_reflect`). Auto Guard / Shield Reflect / Maya card.
3. **B-H3** — Zone damage rates
   (`battle_calc_gvg_damage` / `battle_calc_bg_damage` /
   `battle_calc_pk_damage`). Wraps `BattleCalculator` output via
   a per-map post-mod.
4. **B-H4** — Target helpers (`battle_gettarget`,
   `battle_getcurrentskill`, `battle_get_master`,
   `battle_gettargeted`, `battle_getenemy`,
   `is_infinite_defense`, `battle_check_undead`,
   `battle_check_coma`). Read-mostly helpers consumed by skills.
5. **B-M1** — Delayed damage (`battle_delay_damage`) + AoE helper
   (`battle_damage_area`). Required for projectile skills (Storm
   Gust strike, Magnus delayed waves).
6. **B-M2** — Drain (`battle_drain`), ammo consumption
   (`battle_consume_ammo`), autocast hooks
   (`battle_autocast_aftercast`, `battle_autocast_elembuff_skill`).
7. **B-M3** — Element resolvers (`battle_get_weapon_element` /
   `battle_get_magic_element` / `battle_get_misc_element`).
8. **B-M4** — Vanish + Vellum + status-block damage
   (`battle_vanish_damage`, `battle_vellum_damage`,
   `battle_status_block_damage`).
9. **B-L1** — `battle_config` loader (`battle_set_defaults`,
   `battle_config_read`, `battle_adjust_conf`, get/set). Ships
   600+ knobs but most have working defaults already.
10. **B-L2** — BG/GvG friendly-fire gates + AI exception list.

## History

### 2026-05-20 — initial audit
- Enumerated all 41 `battle_*` functions from battle.cpp + the
  `struct Damage` packet from battle.hpp.
- 8 done / 4 partial / 36 missing across 8 subsystems.
- 10-wave plan documented above. Damage card/mastery + reflect
  are the highest gameplay-impact gaps.
