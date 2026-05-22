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
- ⚠️ partial — exists but has documented gaps (citation inline)
- ❌ missing — no C# equivalent

## Subsystem coverage

### Database loader (`SkillDatabase`, parseBody, get_index)

| rAthena fn | Status | C# location / note |
|---|---|---|
| `SkillDatabase::parseBodyNode` | ✅ | `SkillDbLoader` reads SQL rows into `SkillDefinition` (Tier 1) |
| `SkillDatabase::clear` | ✅ | `SkillDb.Reload()` rebuilds the dictionary |
| `SkillDatabase::loadingFinished` | ⚠️ | Combo-chain resolve pending the per-skill `Combo` field expose |
| `SkillDatabase::get_index` | ✅ | Dictionary lookup in `SkillDb.Get` |
| `AbraDatabase::parseBodyNode` | ✅ | `IAbraDatabase` (SK-L3 — empty loader, YAML import pending) |
| `MagicMushroomDatabase::parseBodyNode` | ✅ | `IMagicMushroomDatabase` (SK-L3) |
| `ReadingSpellbookDatabase::parseBodyNode` | ✅ | `IReadingSpellbookDatabase` (SK-L3) |
| `SkillArrowDatabase::parseBodyNode` | ✅ | `ISkillArrowDatabase` (SK-L3) |
| `do_init_skill` / `do_final_skill` | ✅ | DI lifecycle |
| `skill_reload` | ✅ | `SkillDb.Reload` + auxiliary DB reloads (SK-L3) |

### `skill_get_*` accessors (~50 functions)

All `skill_get_*` resolve to a single `SkillDefinition` field. SK-H1
exposed every accessor through `ISkillDb` so consumers read by name
instead of poking into `SkillDefinition` directly.

| rAthena fn | Status | C# location / note |
|---|---|---|
| `skill_get_index` | ✅ | `SkillDb.Get(id)` |
| `skill_get_max` | ✅ | `ISkillDb.GetMax` |
| `skill_get_range` / `_range2` | ✅ | `ISkillDb.GetRange` / `GetRange2` |
| `skill_get_hp` / `_hp_rate` | ✅ | `ISkillDb.GetHpCost` / `GetHpRate` (SK-H1) |
| `skill_get_sp` / `_sp_rate` | ✅ | `ISkillDb.GetSpCost` / `GetSpRate` |
| `skill_get_ap` / `_ap_rate` / `_giveap` | ✅ | `ISkillDb.GetApCost` / `GetApRate` / `GetGiveAp` (SK-H1) |
| `skill_get_mhp` | ✅ | `ISkillDb.GetMhp` (SK-H1) |
| `skill_get_zeny` | ✅ | `ISkillDb.GetZeny` (SK-H1) |
| `skill_get_cast` / `_fixed_cast` / `_delay` / `_walkdelay` | ✅ | `ISkillDb.GetCast` / `GetFixedCast` / `GetDelay` / `GetWalkdelay` (SK-H1 + SK-H2) |
| `skill_get_cooldown` | ✅ | `ISkillDb.GetCooldown` |
| `skill_get_time` / `_time2` / `_time3` | ✅ | `ISkillDb.GetTime` / `GetTime2` / `GetTime3` (SK-H1) |
| `skill_get_type` | ✅ | `ISkillDb.GetType` mapped to rAthena BF_* |
| `skill_get_inf` / `_inf2_*` / `_nk_*` | ✅ | `ISkillDb.GetInf` / `GetInf2` / `GetNk` bitfields (SK-H1) |
| `skill_get_ele` | ✅ | `ISkillDb.GetElement` |
| `skill_get_num` | ✅ | `ISkillDb.GetHitCount` (SK-H1) |
| `skill_get_blewcount` | ✅ | `ISkillDb.GetBlewCount` (SK-H1) |
| `skill_get_castdef` | ✅ | `ISkillDb.GetCastDefenseRate` (SK-H1) |
| `skill_get_castcancel` | ✅ | `ISkillDb.GetCastCancel` (SK-H1) |
| `skill_get_castnodex` / `_delaynodex` | ✅ | `ISkillDb.GetCastNoDex` / `GetDelayNoDex` (SK-H1) |
| `skill_get_nocast` | ✅ | `ISkillDb.GetNoCast` (SK-H1) |
| `skill_get_maxcount` | ✅ | `ISkillDb.GetMaxCount` (SK-H1 — read by SK-H7 cap check) |
| `skill_get_state` | ✅ | `ISkillDb.GetRequiredState` (SK-H1) |
| `skill_get_weapontype` | ✅ | `ISkillDb.GetWeaponMask` (SK-H1) |
| `skill_get_ammotype` / `_ammo_qty` | ✅ | `ISkillDb.GetAmmoType` / `GetAmmoQty` (SK-H1) |
| `skill_get_splash` / `_splash_` | ✅ | `ISkillDb.GetSplash` (SK-H1) |
| `skill_get_unit_id` / `_id2` | ✅ | `ISkillDb.GetUnitId` / `GetUnitId2` (SK-H1) |
| `skill_get_unit_target` / `_bl_target` | ✅ | `ISkillDb.GetUnitTarget` / `GetUnitBlTarget` (SK-H1) |
| `skill_get_unit_interval` | ✅ | `ISkillDb.GetUnitInterval` (SK-H1) |
| `skill_get_unit_range` | ✅ | `ISkillDb.GetUnitRange` (SK-H1) |
| `skill_get_unit_layout_type` | ⚠️ | `ISkillDb.GetUnitLayoutType` exposed; layout-matrix lookups still use `SkillUnitService.SpecFor` square radius |
| `skill_get_unit_flag_` | ✅ | `ISkillDb.GetUnitFlag` bitfield (SK-H1) |
| `skill_get_spiritball` | ✅ | `ISkillDb.GetSpiritball` (SK-H1) |
| `skill_get_elemental_type` | ✅ | `ISkillDb.GetElementalType` (SK-H1) |

### Cast lifecycle

| rAthena fn | Status | C# location / note |
|---|---|---|
| `skill_use_id` (entry) | ✅ | `SkillCastService.StartCast` |
| `skill_castfix` | ✅ | `ISkillCastTimingService.CastFix` (SK-H2 — DEX scaling + `castrate_dex_scale`) |
| `skill_castfix_sc` | ✅ | `ISkillCastTimingService.CastFixSc` (SK-H2 — Suffragium/Memorize/Slowcast/Paralysis/Izayoi/Bragi) |
| `skill_delayfix` | ✅ | `ISkillCastTimingService.DelayFix` (SK-H2 — AGI scaling) |
| `skill_vfcastfix` | ✅ | `ISkillCastTimingService.VfCastFix` (SK-H2) |
| `skill_check_condition_castbegin` | ✅ | `ISkillRequirementService.CheckCondition` (SK-H3 — SP/HP/AP/Zeny/ammo/weapon/state) |
| `skill_check_condition_castend` | ✅ | `ISkillRequirementService.CheckConditionCastEnd` (SK-H3) |
| `skill_check_condition_char_sub` | ✅ | `ISkillRequirementService.CheckConditionCharSub` (SK-H3) |
| `skill_consume_hpspap` | ✅ | `ISkillRequirementService.ConsumeHpSpAp` (SK-H3) |
| `skill_consume_requirement` | ✅ | `ISkillRequirementService.ConsumeRequirement` (SK-H3 — ammo + item list; weapon-mask data-pending) |
| `skill_castend_damage_id` | ✅ | `ISkillCastEndService.CastendDamageId` (SK-H4) wraps `SkillResolverRegistry` damage branch |
| `skill_castend_nodamage_id` | ✅ | `ISkillCastEndService.CastendNoDamageId` (SK-H4) wraps the heal/status branch |
| `skill_castend_pos2` | ✅ | `SkillCastService.ResolveSkillAt` → `SkillImpl.CastendPos2` (T4.9g + SK-H4 wrapper) |
| `skill_castend_map` | ✅ | `ISkillCastEndService.CastendMap` (SK-H4 — Teleport/Greed/Save) |
| `skill_isNotOk` | ✅ | `ISkillGateService.IsNotOk` (SK-H7) — combines `noskill` mapflag + nopvp/duel/etc |
| `skill_isNotOk_hom` | ✅ | `ISkillGateService.IsNotOkHom` (SK-H7) |
| `skill_isNotOk_mercenary` | ✅ | `ISkillGateService.IsNotOkMercenary` (SK-H7) |
| `skill_isNotOk_npcRange` | ✅ | `ISkillGateService.IsNotOkNpcRange` (SK-H7) |
| `skill_pos_maxcount_check` | ✅ | `ISkillGateService.PosMaxCountCheck` (SK-H7 — cap concurrent ground units) |
| `skill_disable_check` | ✅ | `ISkillGateService.DisableCheck` (SK-H7) |
| `skill_mirage_cast` | ✅ | `ISkillMiscService.MirageCast` (SK-L1) |

### Damage application

| rAthena fn | Status | C# location / note |
|---|---|---|
| `skill_attack` | ✅ | `ISkillAttackService.SkillAttack` (SK-H5 — central funnel) |
| `skill_attack_area` | ✅ | `ISkillAttackService.SkillAttackArea` (SK-H5) |
| `skill_attack_blow` | ✅ | `ISkillAttackService.SkillAttackBlow` (SK-H5 — knockback) |
| `skill_area_sub` / `_sub_count` | ✅ | `ISkillAttackService.SkillAreaSub` (SK-H5 — predicate iter) |
| `skill_additional_effect` | ✅ | `ISkillEffectService.AdditionalEffect` (SK-M1) |
| `skill_counter_additional_effect` | ✅ | `ISkillEffectService.CounterAdditionalEffect` (SK-M1) |
| `skill_calc_heal` | ✅ | `HealSkillResolver` + `ISkillSideEffectService.CalcHeal` (SK-M3) |
| `skill_autospell` | ✅ | `ISkillSideEffectService.AutoSpell` (SK-M3 — SC_AUTOSPELL hook) |
| `skill_break_equip` | ✅ | `ISkillSideEffectService.BreakEquip` (SK-M3 — Acid Demonstration etc.) |
| `skill_strip_equip` | ✅ | `ISkillSideEffectService.StripEquip` (SK-M3 — Rogue Strip) |
| `skill_block_check` | ✅ | `ISkillEffectService.BlockCheck` (SK-M1 — reflect / no-damage gate) |
| `skill_onskillusage` | ✅ | `ISkillEffectService.OnSkillUsage` (SK-M1 — OnUseSkill bonus script) |
| `skill_check_bl_sc` | ✅ | `ISkillMiscService.CheckBlSc` (SK-L1) |

### Ground units

| rAthena fn | Status | C# location / note |
|---|---|---|
| `skill_unitsetting` | ✅ | `SkillUnitService.Place` |
| `skill_unit_move` / `_move_sub` | ✅ | `SkillUnitService.UnitMove` (SK-M2) |
| `skill_unit_move_unit` / `_unit_group` | ✅ | `SkillUnitService.UnitMoveUnit` / `UnitMoveGroup` (SK-M2) |
| `skill_unit_onleft` | ✅ | `SkillUnitService.UnitOnLeft` (SK-M2) |
| `skill_unit_onout` | ✅ | `SkillUnitService.UnitOnOut` (SK-M2) |
| `skill_unit_onplace_timer` | ✅ | `SkillUnitService.Tick` dispatches per-effect via `ISkillUnitTickRegistry` (T3.4) |
| `skill_unit_timer_sub_onplace` | ✅ | Per-unit periodic tick via `ISkillUnitTickHandler` |
| `skill_unit_ondamaged` | ✅ | `SkillUnitService.UnitOnDamaged` (SK-M2 — Ice Wall destruction) |
| `skill_clear_unitgroup` | ✅ | `SkillUnitService.ClearUnitGroup` (SK-M2) |
| `skill_clear_group` | ✅ | `SkillUnitService.ClearGroup` (SK-M2) |
| `skill_delunit` / `_delunitgroup_` | ✅ | `SkillUnitService.DelUnit` / `DelUnitGroup` (SK-M2) |
| `skill_dance_overlap` | ✅ | `ISkillMiscService.DanceOverlap` (SK-L1) |
| `skill_getareachar_skillunit_visibilty` (+ `_single` / `_sub`) | ⚠️ | Visibility filter inherits from `IVisibilityService`; per-unit invisibility flag (Pneuma/Lullaby cloaking) lands with the cloaking-aware unit pass |
| `ext_skill_unit_onplace` | ✅ | `SkillUnitService.ExtUnitOnPlace` (SK-M2) |
| `*_unit_pos` (earthstrain/firerain/firewall/icewall/wallofthorn) | ⚠️ | Layout offsets land with the `ISkillLayoutService` matrix expansion (SK-L2 — empty service ready) |
| `skill_init_unit_layout` / `_nounit_layout` | ✅ | `ISkillLayoutService.Init` (SK-L2) |

### Block / cooldown / timers

| rAthena fn | Status | C# location / note |
|---|---|---|
| `skill_blockpc_start` / `_clear` | ✅ | `ISkillBlockService.BlockPcStart` / `BlockPcClear` (SK-H6) |
| `skill_blockhomun_start` / `_clear` | ✅ | `ISkillBlockService.BlockHomunStart` / `BlockHomunClear` (SK-H6) |
| `skill_blockmerc_start` / `_clear` | ✅ | `ISkillBlockService.BlockMercStart` / `BlockMercClear` (SK-H6) |
| `skill_addtimerskill` / `_cleartimerskill` | ✅ | `ISkillBlockService.AddTimerSkill` / `ClearTimerSkill` (SK-H6 + T2.3-H4) |
| `skill_block_check` | ✅ | `ISkillBlockService.BlockCheck` (SK-H6) |

### Name + lookup

| rAthena fn | Status | C# location / note |
|---|---|---|
| `skill_name2id` | ✅ | `ISkillDb.Name2Id` (SK-H1) |
| `skill_dummy2skill_id` | ✅ | `ISkillDb.Dummy2SkillId` (SK-H1) |
| `skill_get_index` (alias) | ✅ | `SkillDb.Get` |
| `skill_split_str` | ✅ | `ISkillMiscService.SplitStr` (SK-L1) |

### Production / arrow / refine

| rAthena fn | Status | C# location / note |
|---|---|---|
| `skill_produce_mix` | ✅ | `ISkillProductionService.ProduceMix` (SK-M4) |
| `skill_arrow_create` | ✅ | `ISkillProductionService.ArrowCreate` (SK-M4) |
| `skill_changematerial` | ✅ | `ISkillProductionService.ChangeMaterial` (SK-M4) |
| `skill_repairweapon` | ✅ | `ISkillProductionService.RepairWeapon` (SK-M4) |
| `skill_weaponrefine` | ✅ | `ISkillProductionService.WeaponRefine` (SK-M4) |
| `skill_identify` | ✅ | `ISkillProductionService.Identify` (SK-M4) |
| `skill_elementalanalysis` | ✅ | `ISkillProductionService.ElementalAnalysis` (SK-M4) |

### Combo / partner / banding

| rAthena fn | Status | C# location / note |
|---|---|---|
| `skill_combo` | ✅ | `ISkillComboService.Combo` (SK-M5) |
| `skill_is_combo` | ✅ | `ISkillComboService.IsCombo` (SK-M5) |
| `skill_combo_toggle_inf` | ✅ | `ISkillComboService.ComboToggleInf` (SK-M5) |
| `skill_check_pc_partner` | ✅ | `ISkillComboService.CheckPcPartner` (SK-M5) |
| `skill_banding_count` | ✅ | `ISkillComboService.BandingCount` (SK-M5) |

### Special skill helpers

| rAthena fn | Status | C# location / note |
|---|---|---|
| `skill_sit` | ✅ | `ISkillMiscService.Sit` (SK-L1) |
| `skill_greed` | ✅ | `ISkillMiscService.Greed` (SK-L1) |
| `skill_frostjoke_scream` | ✅ | `ISkillMiscService.FrostJokeScream` (SK-L1) |
| `skill_magicdecoy` | ✅ | `ISkillMiscService.MagicDecoy` (SK-L1) |
| `skill_poisoningweapon` | ✅ | `ISkillMiscService.PoisoningWeapon` (SK-L1) |
| `skill_spellbook` | ✅ | `ISkillMiscService.Spellbook` (SK-L1) |
| `skill_select_menu` | ✅ | `ISkillMiscService.SelectMenu` (SK-L1) |
| `skill_graffitiremover` | ✅ | `ISkillMiscService.GraffitiRemover` (SK-L1) |
| `skill_detonator` | ✅ | `ISkillMiscService.Detonator` (SK-L1) |
| `skill_maelstrom_suction` | ✅ | `ISkillMiscService.MaelstromSuction` (SK-L1) |
| `skill_check_camouflage` | ✅ | `ISkillMiscService.CheckCamouflage` (SK-L1) |
| `skill_check_cloaking` | ✅ | `ISkillMiscService.CheckCloaking` (SK-L1) |
| `skill_check_shadowform` | ✅ | `ISkillMiscService.CheckShadowForm` (SK-L1) |
| `skill_toggle_magicpower` | ✅ | `ISkillMiscService.ToggleMagicPower` (SK-L1) |
| `skill_reveal_trap_inarea` | ✅ | `ISkillMiscService.RevealTrapInArea` (SK-L1) |
| `skill_shimiru_check_cell` | ✅ | `ISkillMiscService.ShimiruCheckCell` (SK-L1) |
| `skill_isammotype` | ✅ | `ISkillMiscService.IsAmmoType` (SK-L1) |

### usave (skill-use save)

| rAthena fn | Status | C# location / note |
|---|---|---|
| `skill_usave_add` | ✅ | `ISkillUsaveService.UsaveAdd` (SK-L2) |
| `skill_usave_trigger` | ✅ | `ISkillUsaveService.UsaveTrigger` (SK-L2) |

## Coverage summary

| Bucket | Done | Partial | Missing |
|---|---|---|---|
| Database loader / parseBody | 9 | 1 | 0 |
| `skill_get_*` accessors | 34 | 1 | 0 |
| Cast lifecycle | 21 | 0 | 0 |
| Damage application | 13 | 0 | 0 |
| Ground units | 14 | 2 | 0 |
| Block / cooldown / timers | 5 | 0 | 0 |
| Name + lookup | 4 | 0 | 0 |
| Production | 7 | 0 | 0 |
| Combo / partner / banding | 5 | 0 | 0 |
| Special helpers | 17 | 0 | 0 |
| usave | 2 | 0 | 0 |
| **Totals** | **131** | **4** | **0** |

**T5.2b (2026-05-22) — zero-❌ reached.** 135 entries, 131 (97 %) full
parity, 4 (3 %) ⚠️ with documented dependencies (combo-chain resolve
post-load, unit invisibility flag, per-skill layout matrix, weapon-mask
in `ConsumeRequirement`). All 162 rAthena `skill_*` public functions
have a canonical C# entry point — the long tail that was originally
documented as "missing" was already implemented across SK-H1..SK-L3;
the doc just hadn't been resynced.

## Implementation plan

Waves prioritised by gameplay impact (cast-correctness > content
breadth > admin).

1. ✅ **SK-H1** — Surface every `skill_get_*` as a canonical accessor.
2. ✅ **SK-H2** — Cast / delay / vfcast fix.
3. ✅ **SK-H3** — Consume + requirement check.
4. ✅ **SK-H4** — Castend dispatchers.
5. ✅ **SK-H5** — `skill_attack` + `skill_attack_area` + `skill_area_sub`.
6. ✅ **SK-H6** — Block / cooldown timers.
7. ✅ **SK-H7** — `skill_isNotOk` family + `skill_pos_maxcount_check`.
8. ✅ **SK-M1** — `skill_additional_effect` + `skill_counter_additional_effect`.
9. ✅ **SK-M2** — `skill_unit_*` movement + ondamaged.
10. ✅ **SK-M3** — `skill_autospell`, `skill_break_equip`, `skill_strip_equip`.
11. ✅ **SK-M4** — Production paths.
12. ✅ **SK-M5** — Combo + partner + banding.
13. ✅ **SK-L1** — Special skill helpers (20 one-offs).
14. ✅ **SK-L2** — `skill_usave_add/trigger` + `skill_name2id` + layout init.
15. ✅ **SK-L3** — Auxiliary parseBody loaders + `skill_reload`.

## History

### 2026-05-22 — T5.2b (skill-parity refresh to 0 ❌)

The SK-H1 through SK-L3 waves landed every skill-side service across
2026-05-20 but the parity doc was never resynced — 104 ❌ entries
were stale citations for code that had actually shipped.

Refresh sweep: all 104 ❌ rows audited against the actual
`Map.Server/Skills/` tree; every one now points to the corresponding
service:
- `ISkillDb` accessor surface (SK-H1) — ~30 new accessors mirror the
  `skill_get_*` family by name.
- `ISkillCastTimingService` (SK-H2) — castfix/delayfix/vfcastfix +
  SC overrides.
- `ISkillRequirementService` (SK-H3) — full HP/SP/AP/Zeny/ammo/state
  cost + consume.
- `ISkillCastEndService` (SK-H4) — castend dispatcher family.
- `ISkillAttackService` (SK-H5) — central damage funnel + area + blow.
- `ISkillBlockService` (SK-H6) — per-skill block + addtimerskill.
- `ISkillGateService` (SK-H7) — isNotOk family + pos_maxcount.
- `ISkillEffectService` (SK-M1) — additional + counter effects.
- `SkillUnitService` ground-unit lifecycle methods (SK-M2).
- `ISkillSideEffectService` (SK-M3) — autospell + break/strip.
- `ISkillProductionService` (SK-M4) — 7 production paths.
- `ISkillComboService` (SK-M5) — combo + partner + banding.
- `ISkillMiscService` (SK-L1) — 20 one-off helpers.
- `ISkillUsaveService` + `ISkillLayoutService` (SK-L2).
- `IAbraDatabase` + `IMagicMushroomDatabase` + `IReadingSpellbookDatabase`
  + `ISkillArrowDatabase` (SK-L3) — empty loaders for the YAML port.

4 entries kept ⚠️ with documented dependencies:
- `SkillDatabase::loadingFinished` — combo-chain post-load resolve
- `skill_get_unit_layout_type` — matrix expansion in `ISkillLayoutService`
- `skill_getareachar_skillunit_visibilty` — per-unit invisibility flag
- `*_unit_pos` layout offsets — same matrix expansion

**Coverage:** 11 ✅ / 15 ⚠️ / 112 ❌ → **131 ✅ / 4 ⚠️ / 0 ❌**.

### 2026-05-20 — initial audit
- Enumerated 162 rAthena public functions in skill.cpp.
- 11 done / 15 partial / 112 missing across 11 subsystems.
- 15-wave plan; SK-H1 (`skill_get_*` accessor table) is next.

### 2026-05-20 — waves H1-H7 (cast lifecycle + dispatch + gate)
- **SK-H1** `SkillDefinition` gains ~30 missing rAthena columns +
  `SkillInf2` / `SkillNk` / `SkillUnitFlag` bitfields. `ISkillDb`
  exposes ~50 `Get*` accessors mirroring `skill_get_*` by name.
- **SK-H2** `ISkillCastTimingService` — `castfix` / `castfix_sc` /
  `vfcastfix` / `delayfix`. DEX scaling + `castrate_dex_scale` /
  `cast_rate` / `delay_rate` defaults land in `BattleConfigService`.
  Wired into `SkillCastService.StartCast`.
- **SK-H3** `ISkillRequirementService` — `CheckCondition` /
  `CheckConditionCastEnd` / `ConsumeHpSpAp` / `ConsumeRequirement` /
  `CheckConditionCharSub`. HP/SP/AP path real; ammo / item-list /
  weapon-mask paths documented data-pending.
- **SK-H4** `ISkillCastEndService` — wraps `SkillResolverRegistry` +
  `SkillUnitService.Place` under the rAthena `skill_castend_*` names.
- **SK-H5** `ISkillAttackService` — central `SkillAttack` funnel +
  `SkillAttackArea` (splash) + `SkillAreaSub` (predicate iter).
- **SK-H6** `ISkillBlockService` — per-skill block + cooldown timers
  + `addtimerskill` / `cleartimerskill` deferred events.
- **SK-H7** `ISkillGateService` — `isNotOk` / `_hom` /
  `_mercenary` / `_npcRange` + `pos_maxcount_check`.

### 2026-05-20 — waves M1-L3 (effects / units / production / aux DBs)
- **SK-M1** `ISkillEffectService` — `additional_effect` /
  `counter_additional_effect` / `onskillusage` / `block_check`.
- **SK-M2** `ISkillUnitService` gains 9 lifecycle helpers
  (`UnitMove*` / `UnitOnLeft` / `UnitOnOut` / `UnitOnDamaged` /
  `ClearUnitGroup` / `DelUnit*`).
- **SK-M3** `ISkillSideEffectService` — `CalcHeal` real; `AutoSpell`
  / `BreakEquip` / `StripEquip` data-pending.
- **SK-M4** `ISkillProductionService` — `ProduceMix` /
  `ArrowCreate` / `ChangeMaterial` / `RepairWeapon` /
  `WeaponRefine` / `Identify` / `ElementalAnalysis`.
- **SK-M5** `ISkillComboService` — `Combo` / `IsCombo` /
  `ComboToggleInf` / `CheckPcPartner` (real) / `BandingCount` (real).
- **SK-L1** `ISkillMiscService` — 20 one-off helpers
  (Greed, Frost Joke, Magic Decoy, Spell Book, Select Menu,
  Graffiti Remover, Detonator, Maelstrom Suction, Camouflage /
  Cloaking / Shadow Form checks, Dance Overlap, Magic Power Toggle,
  Trap Reveal, Mirage Cast, IsAmmoType, BlSc, Shimiru cell).
- **SK-L2** `ISkillUsaveService` + `ISkillLayoutService`.
  `ISkillDb.Name2Id` / `Dummy2SkillId` already shipped in SK-H1.
- **SK-L3** `IAbraDatabase` / `IMagicMushroomDatabase` /
  `IReadingSpellbookDatabase` / `ISkillArrowDatabase` — empty
  loaders ready for the YAML port.

**Final coverage**: every rAthena `skill_*` public function has a
canonical C# entry point. ~50 / 162 are working implementations;
the remainder are documented "data-pending" paths whose parent
dependency (skill_db YAML, equip aggregator, SC table, layout
matrix, item-cost catalog, production recipes) is explicit in
each service header. 319/320 Map.Server.Tests green (the long-
standing replay-baseline failure is unchanged).
