# skill.cpp parity · 2026-05-20

`src/map/skill.cpp` (26 438 lines, 162 enumerated public functions plus
five `*Database::parseBodyNode` loaders) is the largest single file in
the rAthena map server. It owns:

- `s_skill_db` accessors — every `skill_get_*` reads one column.
- Cast lifecycle: `skill_use_id` / `skill_castfix` / `skill_delayfix` /
  `skill_check_condition_castbegin` / `castend` / `skill_consume_*`.
- Damage dispatch: `skill_attack` / `skill_attack_area` /
  `skill_area_sub` / `skill_additional_effect` /
  `skill_counter_additional_effect`.
- Ground units: `skill_unit_*` family (place / move / onleft / onout /
  onplace_timer / timer_sub_onplace / ondamaged).
- Block / cooldown timers: `skill_blockpc_start` /
  `skill_blockhomun_start` / `skill_blockmerc_start` /
  `skill_addtimerskill`.
- Map-flag gates: `skill_isNotOk` / `_hom` / `_mercenary` /
  `_npcRange` / `skill_pos_maxcount_check`.
- Special helpers — production (`skill_produce_mix`,
  `skill_arrow_create`, `skill_repairweapon`, `skill_weaponrefine`),
  combo gating (`skill_combo`, `skill_check_pc_partner`,
  `skill_banding_count`), and a long tail of one-off skill commands
  (`skill_frostjoke_scream`, `skill_magicdecoy`, `skill_spellbook`,
  `skill_select_menu`, `skill_graffitiremover`, …).

## Status legend

- ✅ implemented — full or near-full parity with rAthena
- ⚠️ partial — exists but has documented gaps
- ❌ missing — no C# equivalent

## Subsystem coverage

### Database loader (`SkillDatabase`, parseBody, get_index)

| rAthena fn | Status | C# location / note |
|---|---|---|
| `SkillDatabase::parseBodyNode` | ⚠️ | [SkillDbLoader](/Map.Server/Skills/SkillDbLoader.cs) reads `skill_db` SQL rows into `SkillDefinition`; YAML loader pending |
| `SkillDatabase::clear` | ✅ | `SkillDb.Reload()` rebuilds the dictionary |
| `SkillDatabase::loadingFinished` | ⚠️ | No post-load fixup; rAthena resolves combo chains here |
| `SkillDatabase::get_index` | ✅ | Dictionary lookup in `SkillDb.Get` |
| `AbraDatabase::parseBodyNode` | ❌ | Abracadabra random-skill table |
| `MagicMushroomDatabase::parseBodyNode` | ❌ | SC_MAGICMUSHROOM proc table |
| `ReadingSpellbookDatabase::parseBodyNode` | ❌ | Sage Reading Spell Book |
| `SkillArrowDatabase::parseBodyNode` | ❌ | Arrow Crafting recipes |
| `do_init_skill` / `do_final_skill` | ✅ | DI lifecycle |
| `skill_reload` | ⚠️ | `SkillDb.Reload` exists; arrow / abra / spellbook reload pending |

### `skill_get_*` accessors (~50 functions)

All `skill_get_*` resolve to a single `SkillDefinition` field. We expose
the catalog row via `ISkillDb.Get(id)` and the C# port reads the
relevant property directly. Where a knob isn't tracked yet, an explicit
fall-through default is returned.

| rAthena fn | Status | C# location / note |
|---|---|---|
| `skill_get_index` | ✅ | `SkillDb.Get(id)` (returns the record itself) |
| `skill_get_max` | ✅ | `SkillDefinition.MaxLevel` |
| `skill_get_range` / `_range2` | ✅ | `SkillDefinition.Range` |
| `skill_get_hp` / `_hp_rate` | ❌ | Not on SkillDefinition — HP cost knobs |
| `skill_get_sp` / `_sp_rate` | ✅ | `SkillDefinition.SpCost[level]` |
| `skill_get_ap` / `_ap_rate` / `_giveap` | ❌ | AP cost (4th-job mechanic) |
| `skill_get_mhp` | ❌ | % MaxHP scaling |
| `skill_get_zeny` | ❌ | Zeny cost |
| `skill_get_cast` / `_fixed_cast` / `_delay` / `_walkdelay` | ⚠️ | `CastTimeMs[level]` covers cast; fixed/delay/walkdelay are 0 |
| `skill_get_cooldown` | ✅ | `SkillDefinition.CooldownMs[level]` |
| `skill_get_time` / `_time2` / `_time3` | ⚠️ | `StatusDurationMs` covers `time`; time2/time3 are 0 |
| `skill_get_type` | ✅ | `SkillDefinition.DamageKind` mapped to rAthena BF_* |
| `skill_get_inf` / `_inf2_` / `_nk_` | ⚠️ | `SkillTargetMode` covers `inf`; inf2 flags + nk flags are not yet a bitfield |
| `skill_get_ele` | ✅ | `SkillDefinition.Element` |
| `skill_get_num` | ❌ | Hit count |
| `skill_get_blewcount` | ❌ | Knockback cell count |
| `skill_get_castdef` | ❌ | Cast-defense rate |
| `skill_get_castcancel` | ❌ | Whether the cast cancels on hit |
| `skill_get_castnodex` / `_delaynodex` | ❌ | Dex-cast-no-reduction bitfield |
| `skill_get_nocast` | ❌ | Map-type cast blocker bitfield |
| `skill_get_maxcount` | ❌ | Per-cell unit cap |
| `skill_get_state` | ❌ | Required user state (mounted, sitting, …) |
| `skill_get_weapontype` | ❌ | Allowed weapon mask |
| `skill_get_ammotype` / `_ammo_qty` | ❌ | Ammo requirement |
| `skill_get_splash` / `_splash_` | ❌ | Splash radius |
| `skill_get_unit_id` / `_id2` | ❌ | Ground-unit type id |
| `skill_get_unit_target` / `_bl_target` | ❌ | Ground-unit target mask |
| `skill_get_unit_interval` | ⚠️ | Hard-coded per skill in `SkillUnitService.SpecFor` |
| `skill_get_unit_range` | ❌ | Ground-unit per-cell range |
| `skill_get_unit_layout_type` | ⚠️ | `SkillUnitService` uses a fixed square radius; layout enum missing |
| `skill_get_unit_flag_` | ❌ | UF_* flag bitfield |
| `skill_get_spiritball` | ❌ | Spirit Ball cost |
| `skill_get_elemental_type` | ❌ | Required elemental partner type |

### Cast lifecycle

| rAthena fn | Status | C# location / note |
|---|---|---|
| `skill_use_id` (entry) | ✅ | [SkillCastService.StartCast](/Map.Server/Skills/SkillCastService.cs) |
| `skill_castfix` | ❌ | DEX cast-time formula — applied flat from `CastTimeMs` today |
| `skill_castfix_sc` | ❌ | SC overrides (Suffragium, Memorize, …) |
| `skill_delayfix` | ❌ | After-cast delay AGI scaling |
| `skill_vfcastfix` | ❌ | Variable-fixed cast formula |
| `skill_check_condition_castbegin` | ⚠️ | Inline checks in `StartCast` cover SP / range / map-flag; equip/req/state slots pending |
| `skill_check_condition_castend` | ❌ | Re-check at end of cast (interrupt detection) |
| `skill_check_condition_char_sub` | ❌ | Helper for party-member condition checks |
| `skill_consume_hpspap` | ❌ | HP/SP/AP deduction after cast finishes — SP only is deducted pre-cast today |
| `skill_consume_requirement` | ❌ | Ammo / item consumption |
| `skill_castend_damage_id` | ⚠️ | [WeaponSkillResolver](/Map.Server/Skills/Resolvers/WeaponSkillResolver.cs) + MagicSkillResolver + MiscSkillResolver cover damage path |
| `skill_castend_nodamage_id` | ⚠️ | [HealSkillResolver](/Map.Server/Skills/Resolvers/HealSkillResolver.cs) + StatusSkillResolver cover support skills |
| `skill_castend_pos2` | ❌ | Ground-targeted resolver — uses `ISkillUnitService.Place` but no canonical entry |
| `skill_castend_map` | ❌ | Map-warp resolver (Teleport, Greed, Save) |
| `skill_isNotOk` | ⚠️ | `noskill` map-flag check inline in `StartCast`; nonpvp/duel/etc gates pending |
| `skill_isNotOk_hom` | ❌ | Homunculus skill-gate check |
| `skill_isNotOk_mercenary` | ❌ | Mercenary skill-gate check |
| `skill_isNotOk_npcRange` | ❌ | NPC range gate (skill cast through NPC) |
| `skill_pos_maxcount_check` | ❌ | Cap concurrent ground units per caster |
| `skill_disable_check` | ❌ | Per-skill toggle (skill_db.disable_check flag) |
| `skill_mirage_cast` | ❌ | Mirage Visor proc |

### Damage application

| rAthena fn | Status | C# location / note |
|---|---|---|
| `skill_attack` | ⚠️ | Resolvers each call `IDamageService.ApplyDamage`; no central `skill_attack` helper |
| `skill_attack_area` | ❌ | Splash helper (used by Bash AoE / Magnum Break / Pulse Strike) |
| `skill_attack_blow` | ❌ | Knockback application |
| `skill_area_sub` / `_sub_count` | ❌ | Cell iteration helper |
| `skill_additional_effect` | ❌ | Status proc after a hit (rAthena post-damage chain) |
| `skill_counter_additional_effect` | ❌ | Target-side reactive procs |
| `skill_calc_heal` | ✅ | [HealSkillResolver](/Map.Server/Skills/Resolvers/HealSkillResolver.cs) |
| `skill_autospell` | ❌ | SC_AUTOSPELL — equip-driven skill autocast |
| `skill_break_equip` | ❌ | Acid Demonstration / Strip Weapon equip-break |
| `skill_strip_equip` | ❌ | Rogue Strip skills |
| `skill_block_check` | ❌ | Reflect / no-damage gate |
| `skill_onskillusage` | ❌ | OnUseSkill bonus script hook |
| `skill_check_bl_sc` | ❌ | Status-block-sub for AoE |

### Ground units

| rAthena fn | Status | C# location / note |
|---|---|---|
| `skill_unitsetting` (in cpp) | ⚠️ | [SkillUnitService.Place](/Map.Server/Skills/SkillUnitService.cs) |
| `skill_unit_move` / `_move_sub` | ❌ | Unit follows caster (Lullaby, Magnetic Earth) |
| `skill_unit_move_unit` / `_unit_group` | ❌ | Move a single unit / whole group |
| `skill_unit_onleft` | ❌ | SC end / aura drop when caster leaves the cell |
| `skill_unit_onout` | ❌ | Entity stepped out of cell |
| `skill_unit_onplace_timer` | ⚠️ | Inline in `SkillUnitService.Tick`; needs per-effect dispatch |
| `skill_unit_timer_sub_onplace` | ❌ | Per-unit periodic tick helper |
| `skill_unit_ondamaged` | ❌ | Unit took damage (Ice Wall destruction) |
| `skill_clear_unitgroup` | ❌ | Force-clear all of a caster's groups |
| `skill_clear_group` | ❌ | Clear by skill id |
| `skill_delunit` / `_delunitgroup_` | ❌ | Manual cleanup |
| `skill_dance_overlap` | ❌ | Dancer/Bard skill overlap rule |
| `skill_getareachar_skillunit_visibilty` (+ `_single` / `_sub`) | ❌ | Visibility filtering for invisible units (Pneuma, Lullaby) |
| `ext_skill_unit_onplace` | ❌ | External wrapper used by chrif callbacks |
| `*_unit_pos` (earthstrain, firerain, firewall, icewall, wallofthorn) | ❌ | Layout offsets per layout type |
| `skill_init_unit_layout` / `_nounit_layout` | ❌ | Boot-time layout matrix init |

### Block / cooldown / timers

| rAthena fn | Status | C# location / note |
|---|---|---|
| `skill_blockpc_start` / `_clear` | ⚠️ | `SkillCastService._cooldowns` covers per-skill cooldown; no global Frenzy-style block flag |
| `skill_blockhomun_start` / `_clear` | ❌ | Homun skill cooldown |
| `skill_blockmerc_start` / `_clear` | ❌ | Merc skill cooldown |
| `skill_addtimerskill` / `_cleartimerskill` | ❌ | Delayed-fire skill timers (Storm Gust strike windows) |
| `skill_block_check` | ❌ | NPC_INVINCIBLE / OFF state |

### Name + lookup

| rAthena fn | Status | C# location / note |
|---|---|---|
| `skill_name2id` | ❌ | Reverse string→id lookup |
| `skill_dummy2skill_id` | ❌ | Dummy-skill remap (e.g. NPC_DUMMYSKILL) |
| `skill_get_index` (alias) | ✅ | `SkillDb.Get` |
| `skill_split_str` | ❌ | Tokenizer helper (used by .conf loaders) |

### Production / arrow / refine

| rAthena fn | Status | C# location / note |
|---|---|---|
| `skill_produce_mix` | ❌ | Generic production formula (Pharmacy, Cooking, Forge) |
| `skill_arrow_create` | ❌ | Arrow Crafting |
| `skill_changematerial` | ❌ | Geneticist Change Material |
| `skill_repairweapon` | ❌ | Blacksmith Weapon Repair |
| `skill_weaponrefine` | ❌ | Blacksmith Weapon Refining |
| `skill_identify` | ❌ | Merchant Identify |
| `skill_elementalanalysis` | ❌ | Alchemist Elemental Analysis |

### Combo / partner / banding

| rAthena fn | Status | C# location / note |
|---|---|---|
| `skill_combo` | ❌ | Combo chain advance |
| `skill_is_combo` | ❌ | Whether skill is part of a combo |
| `skill_combo_toggle_inf` | ❌ | Combo inf-bit toggling |
| `skill_check_pc_partner` | ❌ | Royal Guard / Sura partner checks |
| `skill_banding_count` | ❌ | Royal Guard Banding members count |

### Special skill helpers

| rAthena fn | Status | C# location / note |
|---|---|---|
| `skill_sit` | ❌ | Tension Relax sit-bonus |
| `skill_greed` | ❌ | Greed loot pickup |
| `skill_frostjoke_scream` | ❌ | Frost Joke / Scream proc |
| `skill_magicdecoy` | ❌ | Warlock Magic Decoy |
| `skill_poisoningweapon` | ❌ | GC Poisoning Weapon |
| `skill_spellbook` | ❌ | Warlock Reading Spell Book |
| `skill_select_menu` | ❌ | Skill Selection (Arrullo / Service for You) |
| `skill_graffitiremover` | ❌ | Remove graffiti unit |
| `skill_detonator` | ❌ | Detonator on traps |
| `skill_maelstrom_suction` | ❌ | Maelstrom skill-absorb |
| `skill_check_camouflage` | ❌ | Stalker Camouflage check |
| `skill_check_cloaking` | ❌ | Assassin Cloaking check |
| `skill_check_shadowform` | ❌ | Shadow Chaser Shadow Form |
| `skill_toggle_magicpower` | ❌ | Sage Magic Power toggle |
| `skill_reveal_trap_inarea` | ❌ | Trap Reveal |
| `skill_shimiru_check_cell` | ❌ | Shimiru cell-check |
| `skill_isammotype` | ❌ | Ammo-type check |

### usave (skill-use save)

| rAthena fn | Status | C# location / note |
|---|---|---|
| `skill_usave_add` | ❌ | Record per-character last-cast |
| `skill_usave_trigger` | ❌ | Replay last-cast (e.g. SC_DOUBLECAST) |

## Coverage summary

| Bucket | Done | Partial | Missing |
|---|---|---|---|
| Database loader / parseBody | 2 | 3 | 5 |
| `skill_get_*` accessors | 6 | 4 | 25 |
| Cast lifecycle | 1 | 4 | 16 |
| Damage application | 1 | 1 | 11 |
| Ground units | 0 | 2 | 17 |
| Block / cooldown / timers | 0 | 1 | 4 |
| Name + lookup | 1 | 0 | 3 |
| Production | 0 | 0 | 7 |
| Combo / partner / banding | 0 | 0 | 5 |
| Special helpers | 0 | 0 | 17 |
| usave | 0 | 0 | 2 |
| **Totals** | **11** | **15** | **112** |

138 entries tracked (some `skill_get_*` collapse onto a single
SkillDefinition field). 11 (8%) done, 15 (11%) partial, 112 (81%)
missing. The biggest holes are the `skill_get_*` field gaps
(SkillDefinition needs ~20 new columns) and the special-helper long
tail (Abracadabra, Magic Mushroom, Spell Book, …).

## Implementation plan

Waves prioritised by gameplay impact (cast-correctness > content
breadth > admin).

1. **SK-H1** — Surface every `skill_get_*` as a canonical accessor.
   Either extend `SkillDefinition` with the missing field or expose
   it as a method on `ISkillDb`. Returning rAthena defaults for not-
   yet-tracked columns is fine; the entry point removes the
   "where do I read this knob" question.
2. **SK-H2** — Cast / delay / vfcast fix (`skill_castfix`,
   `skill_castfix_sc`, `skill_delayfix`, `skill_vfcastfix`).
   Wires DEX/AGI scaling onto the cast pipeline.
3. **SK-H3** — Consume + requirement check
   (`skill_check_condition_castbegin/castend`,
   `skill_consume_hpspap`, `skill_consume_requirement`).
4. **SK-H4** — Castend dispatchers
   (`skill_castend_damage_id` / `_nodamage_id` / `_pos2` / `_map`).
   Canonical entry points wrapping the existing resolver dispatch.
5. **SK-H5** — `skill_attack` + `skill_attack_area` + `skill_area_sub`.
   Central damage helper consumed by AoE skills.
6. **SK-H6** — Block / cooldown timers
   (`skill_blockpc_start/clear`, `skill_blockhomun_start/clear`,
   `skill_blockmerc_start/clear`, `skill_block_check`,
   `skill_disable_check`, `skill_addtimerskill / cleartimerskill`).
7. **SK-H7** — `skill_isNotOk` family + `skill_pos_maxcount_check`.
   Map-flag-style gating.
8. **SK-M1** — `skill_additional_effect` + `skill_counter_additional_effect`.
9. **SK-M2** — `skill_unit_*` movement + ondamaged.
10. **SK-M3** — `skill_calc_heal` already done; add `skill_autospell`,
    `skill_break_equip`, `skill_strip_equip`.
11. **SK-M4** — Production paths (mix, arrow, refine, identify, repair,
    elementalanalysis, changematerial).
12. **SK-M5** — Combo + partner + banding.
13. **SK-L1** — Special skill helpers (greed, frostjoke, magicdecoy,
    spellbook, selectmenu, graffitiremover, detonator, maelstrom,
    camouflage, cloaking, shadowform, magicpower, reveal_trap,
    shimiru, mirage, isammotype, sit).
14. **SK-L2** — `skill_usave_add/trigger` + `skill_name2id` +
    `skill_dummy2skill_id` + layout init.
15. **SK-L3** — Auxiliary parseBody loaders (Abra / MagicMushroom /
    ReadingSpellbook / SkillArrow) + `skill_reload`.

The bar for ✅ on this file is "every rAthena public function has a
canonical C# entry point." Implementations may be `data-pending` on a
specific upstream (skill_db.yml YAML loader, SC bitfield, layout
matrix table), as long as the entry point exists and the dependency
is documented.

## History

### 2026-05-20 — initial audit
- Enumerated 162 rAthena public functions in skill.cpp.
- 11 done / 15 partial / 112 missing across 11 subsystems.
- 15-wave plan; SK-H1 (`skill_get_*` accessor table) is next.
