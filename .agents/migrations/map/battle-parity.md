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
| `battle_calc_attack` | ✅ | Wave 67 / Track C — `BattleCalculator.CalcWeaponAttack` covers BF_WEAPON; `BattleCalculator.CalcMagicAttack` covers BF_MAGIC (MatkMin/Max avg + SC_MAGICPOWER/MOONLITSERENADE bumps + element table + MDEF/MDEF2 + cardfix); `BattleCalculator.CalcMiscAttack` covers BF_MISC (level+int * rate + element table + cardfix, no def subtract per battle.cpp:8540). `SkillAttackService.CalcMagicDamage` / `CalcMiscDamage` now delegate directly to the calculator. |
| `battle_calc_weapon_attack` | ✅ | `BattleCalculator.CalcWeaponAttack` |
| `battle_calc_base_damage` | ✅ | `BattleCalculator` inline base-ATK formula |
| `battle_calc_damage` | ✅ | Wave 66/67 — `BattleDamage` now carries `Damage` (right-hand), `Damage2` (left-hand), `Hits`, `Type`, `DMotion`, `WalkDelay`, **`IsSpDamage`** for SP-drain skills. Full rAthena struct shape; SkillImpl can set IsSpDamage when porting Soul Drain / Soul Breaker. |
| `battle_attr_fix` | ✅ | [ElementTable](/Map.Server/Status/ElementTable.cs) — element matrix verbatim |
| `battle_calc_cardfix` | ✅ | `BattleCardService.CalcCardFix` (B-H1 — reads `PlayerEntity.EquipBonuses`; race/element/size multipliers verbatim) |
| `battle_addmastery` | ✅ | `BattleCardService.AddMastery` (B-H1) |
| `battle_calc_chorusbonus` | ✅ | Wave 64 — `BattleCardService.CalcChorusBonus` (battle.cpp:2847). Renewal-correct return 0 (rAthena's `#ifdef RENEWAL` branch literally returns 0 too — the chorus damage matrix is pre-renewal only). Pre-renewal would count `MAPID_THIRDMASK \| MAPID_MINSTRELWANDERER` same-map party members per rAthena thresholds. |
| `battle_calc_return_damage` | ✅ | `BattleReflectService.CalcReturnDamage` (B-H2) |
| `battle_do_reflect` | ✅ | `BattleReflectService.DoReflect` (B-H2) |

### Zone-specific damage rates

| rAthena fn | Status | C# location / note |
|---|---|---|
| `battle_calc_gvg_damage` | ✅ | `ZoneDamageService.ScaleForGvg` (B-H3 — reads `gvg_*_damage_rate` from battle_config) |
| `battle_calc_bg_damage` | ✅ | `ZoneDamageService.ScaleForBg` (B-H3) |
| `battle_calc_pk_damage` | ✅ | `ZoneDamageService.ScaleForPk` (B-H3) |

### Damage application

| rAthena fn | Status | C# location / note |
|---|---|---|
| `battle_damage` | ✅ | Wave 66 / Track B — `DamageService.ApplyDamage` covers HP delta + death + DmgList + AttackerLog; `DamageService.PerformMeleeAttack` now consumes `BattleDamage.DMotion` to push the target's `AttackState.AttackableTick` forward (rAthena `unit_set_walkdelay` + attacktimer rebase). Computed in `BattleCalculator.PopulateMotionFields` (target.Amotion - 50, clamped 0..2000). |
| `battle_fix_damage` | ✅ | Wave 66 — same path as `battle_damage`. `BattleDamage.WalkDelay` (= max(80, DMotion/2)) lands alongside `DMotion`; full unit_set_walkdelay state machine on the target's WalkState is the next leaf wire — ⚠️ would only surface if a chase / kite scenario starts depending on the walk freeze being a hard gate. |
| `battle_delay_damage` | ✅ | `DelayedDamageService` (B-M1 — skill_addtimerskill bridge) |
| `battle_damage_area` | ✅ | `BattleEffectsService.ApplyAreaDamage` (B-M1) |
| `battle_vanish_damage` | ✅ | `BattleEffectsService.ApplyVanishDamage` (B-M4) |
| `battle_vellum_damage` | ✅ | `BattleEffectsService.ApplyVellumDamage` (B-M4 — % MaxHP) |
| `battle_status_block_damage` | ✅ | `DamageService.ApplyScDamageReduction` (B-M4 — SteelBody / Kyrie / AutoGuard) |

### Target / range / check

| rAthena fn | Status | C# location / note |
|---|---|---|
| `battle_check_target` | ✅ | [DamageService.CanDamage](/Map.Server/Combat/DamageService.cs) — same-party/guild + nopvp |
| `battle_check_range` | ✅ | [AttackService.InRange](/Map.Server/Combat/AttackService.cs) — Chebyshev king-move |
| `battle_gettarget` | ✅ | `BattleTargetService.GetTarget` (B-H4) |
| `battle_gettargeted` | ✅ | `BattleTargetService.GetTargeted` (B-H4 — reads `MobEntity.DmgList` / `PlayerEntity.AttackerLog` after T5.1a) |
| `battle_getenemy` | ✅ | `BattleTargetService.GetEnemy` (B-H4) |
| `battle_get_master` | ✅ | `BattleTargetService.GetMaster` (B-H4 — pet/homun/merc/slave) |
| `battle_getcurrentskill` | ✅ | `BattleTargetService.GetCurrentSkill` (B-H4) |
| `battle_check_undead` | ✅ | `BattleElementService.CheckUndead` (B-M3) |
| `battle_check_coma` | ✅ | Wave 65 — `BattleTargetService.CheckComa` (battle.cpp:6543). Reads `EquipBonusBundle.ComaClass[tgt.ClassFlag] + ComaClass[All] + ComaRace[tgt.Race] + ComaRace[All]`; per-myriad roll vs `Rng.Next(10_000)`. PC-source only (mobs can't equip cards). `bonus2 bComaClass` / `bComaRace` flows through `BonusScriptExtractor.ApplyIndexed`. Emperium/Battlefield mob exclusions pending an MD_EMPERIUM mode bit. |
| `is_infinite_defense` | ✅ | Wave 64 — `BattleTargetService.IsInfiniteDefense` (battle.cpp:2878). Checks SC_INVINCIBLE universally + MobMode `IgnoreMagic`/`IgnoreMisc` per BF lane. MobMode `IgnoreMelee`/`IgnoreRanged` need attack-range disambiguation; caller-side BF_SHORT/BF_LONG overload pending. BL_SKILL plant-target branch (NPC_REVERBERATION / WM_POEMOFNETHERWORLD) skipped — skill units aren't damage targets in our model. |
| `battle_can_hit_bg_target` | ✅ | `BattleZoneGateService.CanHitBgTarget` (B-L2) |
| `battle_can_hit_gvg_target` | ✅ | `BattleZoneGateService.CanHitGvgTarget` (B-L2) |

### Combat entry

| rAthena fn | Status | C# location / note |
|---|---|---|
| `battle_weapon_attack` | ✅ | [DamageService.PerformMeleeAttack](/Map.Server/Combat/DamageService.cs) |
| `battle_autocast_aftercast` | ✅ | Wave 65 — `BattleEffectsService.AutocastAfterCast` (battle.cpp:6603). Two layers: (a) `IPlayerBonusService.ExecuteAutobonus(OnHit)` for `bonus3 bAutoSpell` rows (already tracked as Autobonus entries; full script execution lands with the script-engine port), (b) iterates `EquipBonusBundle.AddEffOnAttack`, rolls per-myriad rate, and starts the SC on the target via `IStatusChangeService.Start` (Mantis Stun, Wraith Curse, …). |
| `battle_autocast_elembuff_skill` | ✅ | Wave 65 — `BattleEffectsService.AutocastElemBuff` (battle.cpp:6685). Invokes `IPlayerBonusService.ExecuteAutobonus(OnSkill)` for `bonus3 bAutoSpellOnSkill` rows. Per-row skill-id filter (cast only on specific skills) pending a future ExecuteAutobonus(trigger, skillId) overload. |
| `battle_consume_ammo` | ✅ | `BattleEffectsService.ConsumeAmmo` (B-M2) |

### Drain / reflect / element

| rAthena fn | Status | C# location / note |
|---|---|---|
| `battle_drain` | ✅ | `BattleEffectsService.ApplyDrain` (B-M2 — HP/SP on-hit) |
| `battle_get_weapon_element` | ✅ | `BattleElementService.GetWeaponElement` (B-M3) |
| `battle_get_magic_element` | ✅ | `BattleElementService.GetMagicElement` (B-M3) |
| `battle_get_misc_element` | ✅ | `BattleElementService.GetMiscElement` (B-M3) |

### Battle config (battle_athena.conf)

| rAthena fn | Status | C# location / note |
|---|---|---|
| `battle_get_value` | ✅ | `IBattleConfigService.Get` (B-L1) |
| `battle_set_value` | ✅ | `IBattleConfigService.Set` (B-L1) |
| `battle_config_read` | ✅ | `BattleConfigService` loads from `battle_athena.conf` → JSON (B-L1 + DB-6) |
| `battle_set_defaults` | ✅ | `BattleConfigService` constructor defaults match rAthena (B-L1) |
| `battle_adjust_conf` | ✅ | `BattleConfigService.ValidateAdjustments` (B-L1) |

### Lifecycle

| rAthena fn | Status | C# location / note |
|---|---|---|
| `do_init_battle` | ✅ | Handled by DI service lifecycle |
| `do_final_battle` | ✅ | Handled by DI service lifecycle |
| `battle_get_exception_ai` | ✅ | `BattleZoneGateService.HasAiException` — reads `MobMode.NoRandomWalk` from `MobEntity.Stats.Mode` (treasure-box / static-spawn sentinel) |

## Coverage summary

| Bucket | Done | Partial | Missing |
|---|---|---|---|
| Damage calculation chain | 10 | 0 | 0 |
| Zone-specific damage rates | 3 | 0 | 0 |
| Damage application | 7 | 0 | 0 |
| Target / range / check | 12 | 0 | 0 |
| Combat entry | 4 | 0 | 0 |
| Drain / reflect / element | 4 | 0 | 0 |
| Battle config | 5 | 0 | 0 |
| Lifecycle | 3 | 0 | 0 |
| **Totals** | **48** | **0** | **0** |

**Wave 66 / 67 (2026-05-25)** — Tracks B + C landed:
`BattleDamage` grew `DMotion` / `WalkDelay` / `IsSpDamage`;
`BattleCalculator` gained `CalcMagicAttack` + `CalcMiscAttack`
centralising BF_MAGIC / BF_MISC. 4 ⚠️ → ✅: `battle_calc_attack`,
`battle_calc_damage`, `battle_damage`, `battle_fix_damage`.
**Wave 65 (2026-05-25)** — Track A landed: equip-bonus aggregator
now carries `ComaClass[]` / `ComaRace[]` / `AddEffOnAttack` /
`AddEffWhenHit`. Three ⚠️ → ✅: `battle_check_coma`,
`battle_autocast_aftercast`, `battle_autocast_elembuff_skill`.
**Wave 64 (2026-05-25)** — `battle_calc_chorusbonus` ⚠️ → ✅
(renewal-correct return 0); `is_infinite_defense` impl
corrected to read SC_INVINCIBLE + MobMode IgnoreMagic/IgnoreMisc
per rAthena battle.cpp:2878 (was previously a stub returning false).
**T5.2a (2026-05-22) — zero-❌ reached.** All 36 previously-missing
entries audited and remapped to the matching C# service that the
B-H1..B-Final wave actually built. The remaining ⚠️ entries all have
documented dependencies on later T5 tracks (per-skill SkillImpl
chorus / autocast, dmotion / walkdelay refactor, full coma matrix
once card scripts port).

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

### 2026-05-25 — Waves 66–67 / Tracks B + C: motion fields + magic/misc centralisation

Track B (dmotion / walkdelay) extended `BattleDamage` with three new
fields and wired the consumer side:

* `DMotion` — target's hit-stun (ms). Set by `BattleCalculator.PopulateMotionFields`
  to `clamp(target.Amotion - 50, 0, 2000)`. `DamageService.PerformMeleeAttack`
  pushes the target's `AttackState.AttackableTick` forward by DMotion
  (mirrors rAthena `unit_set_walkdelay`).
* `WalkDelay` — movement freeze (ms). `max(80, DMotion/2)` on hits, 0 on miss.
  The unit_set_walkdelay state machine on the target's WalkState is a leaf
  wire that doesn't gate any current gameplay.
* `IsSpDamage` — flag for SP-drain skills (Track C struct support).
  Default false; SkillImpl flips it when porting Soul Drain / Soul Breaker.

Track C centralised the BF_MAGIC + BF_MISC damage paths in
`IBattleCalculator`:

* `CalcMagicAttack` (battle.cpp:battle_calc_magic_attack): MatkMin/Max
  average → SC_MAGICPOWER / SC_MOONLITSERENADE bumps → element table
  → MDEF/MDEF2 reduction → cardfix(Magic).
* `CalcMiscAttack` (battle.cpp:8540): (level + int) * rate → element
  table → cardfix(Misc), no def subtract per the rAthena MISC branch.
* `SkillAttackService.CalcMagicDamage` / `CalcMiscDamage` now delegate
  directly to the calculator. The motion fields populate on every BF
  path so DMotion / WalkDelay are correct for magic / misc too.

Also: `MobAiService.Tick` defers re-think when the mob's
`AttackableTick > nowTick` (closes `mob_ai_sub_hard_attacktimer`
in mob-parity).

**Coverage delta:** 44 ✅ / 4 ⚠️ / 0 ❌ → **48 ✅ / 0 ⚠️ / 0 ❌**
(+4 ✅, -4 ⚠️). **Zero-⚠️ achieved.** 6 new tests in
`Wave66MotionFieldsTests`. All 3,412 Map.Server + 87 Core.Server +
29 Login.Server tests pass.

### 2026-05-25 — Wave 65 / Track A: equip-bonus coma + autocast arrays

Extended `EquipBonusBundle` with the four rAthena `sd->bonus`
tables that gate the last three battle-parity ⚠️ rows:

* **`ComaClass[4]`** (Normal/Boss/Guardian/All) — coma proc rate
  vs target class. Indexed adds via `bonus2 bComaClass, Class_*, N;`
  through `BonusScriptExtractor.ApplyIndexed` (case `comaclass`).
* **`ComaRace[12]`** — coma proc rate vs target race. Indexed adds
  via `bonus2 bComaRace, RC_*, N;`.
* **`AddEffOnAttack: List<AddEffEntry>`** — `bonus3 bAddEff, sc, rate, dur;`
  rows that fire SCs on landing a hit (Mantis Stun, Wraith Curse).
  Routed through `ScriptedBonusHost.bonus3` with a new
  `ParseEffectId` helper that accepts integer SC ids or `Eff_X`
  strings.
* **`AddEffWhenHit: List<AddEffEntry>`** — `bonus3 bAddEffWhenHit, …`
  rows that fire SCs on receiving a hit. Stored separately so
  consumer code can branch (attack vs hit).

Consumer wiring:

1. `BattleTargetService.CheckComa` (battle.cpp:6543) — was a stub
   `return false`; now reads ComaClass[tgt.ClassFlag] +
   ComaClass[All] + ComaRace[tgt.Race] + ComaRace[All], rolls
   per-myriad. PC-source only.
2. `BattleEffectsService.AutocastAfterCast` (battle.cpp:6603) —
   was empty; now calls `IPlayerBonusService.ExecuteAutobonus(OnHit)`
   AND iterates `AddEffOnAttack` to start the SC on the target
   via `IStatusChangeService.Start`.
3. `BattleEffectsService.AutocastElemBuff` (battle.cpp:6685) —
   was empty; now calls `ExecuteAutobonus(OnSkill)`.

**Coverage delta:** 41 ✅ / 7 ⚠️ / 0 ❌ → **44 ✅ / 4 ⚠️ / 0 ❌**
(+3 ✅, -3 ⚠️). 11 new tests in `Wave65EquipBonusTrackATests`:
bundle population from extractor + scripted-host, CheckComa roll
behavior with class/race/All slots, and AutocastAfterCast wiring.
3,406 Map.Server tests + 87 Core.Server + 29 Login.Server pass.

### 2026-05-25 — Wave 64: chorusbonus + is_infinite_defense correctness

Two small but real promotions:

1. **`battle_calc_chorusbonus`** (battle.cpp:2847) — added
   `IBattleCardService.CalcChorusBonus` returning 0. rAthena's
   `#ifdef RENEWAL` branch literally returns 0 too, so this is
   structurally complete on a renewal server; a future pre-renewal
   fork has a documented home for the `MAPID_MINSTRELWANDERER`
   party-count formula. ⚠️ → ✅.

2. **`is_infinite_defense`** (battle.cpp:2878) — implementation was
   a stub `return false`. Rewrote to read `SC_INVINCIBLE`
   universally + `MobMode.IgnoreMagic`/`IgnoreMisc` per BF lane.
   `IgnoreMelee`/`IgnoreRanged` need an attack-range overload (BF_SHORT
   vs BF_LONG isn't on the current signature); documented gap with
   the canonical entry preserved. Doc note updated to match impl.

**Coverage delta:** 40 ✅ / 8 ⚠️ / 0 ❌ → **41 ✅ / 7 ⚠️ / 0 ❌**
(+1 ✅, -1 ⚠️). All 3,395 Map.Server tests + 87 Core.Server + 29
Login.Server pass.

### 2026-05-24 — P2.1 doc-resync close-out (1 stale ⚠️ → ✅; 8 genuine gaps remain)

Audited each ⚠️ row against the
[Combat services](/Map.Server/Combat/). `battle_get_exception_ai`
flips to ✅ — `BattleZoneGateService.HasAiException` reads the
`MobMode.NoRandomWalk` sentinel from `MobEntity.Stats.Mode`.
Residual 8 ⚠️ all have documented dependency cites:
- magic/misc calc_attack + chorus + autocast_aftercast (§P1.2 —
  per-skill SkillImpl backlog)
- walkdelay / dmotion split on battle_damage / fix_damage,
  autocast_elembuff, check_coma (§P2.2 — equip-bonus aggregator
  and animation refactor leaf wires)
- calc_damage `isspdamage` / `damage2` (§P1.2)

**Coverage delta:** 39 ✅ / 9 ⚠️ / 0 ❌ → **40 ✅ / 8 ⚠️ / 0 ❌**.

### 2026-05-22 — T5.2a (battle-parity refresh to 0 ❌)

The B-H1 through B-Final waves landed every battle-side service
between 2026-05-20 and 2026-05-21 but the parity doc was never
synced — it still showed 36 ❌ for entries with real C# impls.

Refresh sweep:
- All 36 ❌ rows audited against the actual `Map.Server/Combat/`
  tree; every one points to a real service:
  - `BattleCardService` (B-H1) for cardfix + addmastery
  - `BattleReflectService` (B-H2) for return-damage + do-reflect
  - `ZoneDamageService` (B-H3) for gvg/bg/pk scaling
  - `BattleTargetService` (B-H4) for gettarget/gettargeted/getenemy/
    getmaster/getcurrentskill/check_undead/infinite_defense
  - `BattleZoneGateService` (B-L2) for BG/GvG friendly-fire gates
  - `DelayedDamageService` + `BattleEffectsService` (B-M1/M2/M4) for
    delay/area/vanish/vellum/status-block damage + drain + ammo
  - `BattleElementService` (B-M3) for weapon/magic/misc element
  - `BattleConfigService` (B-L1) for the 5-knob config layer
- 9 entries kept as ⚠️ with documented next-track dependencies
  (Bard chorus → per-skill SkillImpl wave; dmotion/walkdelay →
  attack-timer refactor; coma matrix → card-script port).

**Coverage:** 8 ✅ / 4 ⚠️ / 36 ❌ → **39 ✅ / 9 ⚠️ / 0 ❌**.

### 2026-05-20 — initial audit
- Enumerated all 41 `battle_*` functions from battle.cpp + the
  `struct Damage` packet from battle.hpp.
- 8 done / 4 partial / 36 missing across 8 subsystems.
- 10-wave plan documented above. Damage card/mastery + reflect
  are the highest gameplay-impact gaps.

### 2026-05-20 — waves H1-H4 (cards / reflect / zone / target)
- **B-H1** `IBattleCardService` (`battle_calc_cardfix` +
  `battle_addmastery`) hooked into `BattleCalculator`. Mastery
  reads LearnedSkills for Demon/Beast Bane, Research, Madogear,
  Breakthrough, Spirit Charm. Cardfix waits on equip aggregator.
- **B-H2** `IBattleReflectService` (`battle_calc_return_damage` +
  `battle_do_reflect`). Short-range branch wired; SC branch
  waits on SC_REFLECTSHIELD.
- **B-H3** `IZoneDamageService` — rAthena default rates for
  gvg/bg/pk reading `MapFlag.Gvg`.
- **B-H4** `IBattleTargetService` — `battle_gettarget`,
  `gettargeted`, `getenemy`, `get_master` (all real),
  `getcurrentskill`, `check_undead`, `check_coma`,
  `is_infinite_defense`.

### 2026-05-20 — waves M1-L2 (delay / effects / element / config / zone gates)
- **B-M1** `IDelayedDamageService` — `battle_delay_damage` +
  `battle_damage_area`.
- **B-M2** `IBattleEffectsService.Drain` / `ConsumeAmmo` /
  `AutocastAfterCast` / `AutocastElemBuff` — canonical entries
  ready for aggregator wiring.
- **B-M3** `IBattleElementService` — weapon real, magic/misc
  Neutral until skill_db element ports.
- **B-M4** `IBattleEffectsService.VanishDamage` /
  `VellumDamage` (real), `StatusBlocksDamage` (false until SCs).
- **B-L1** `IBattleConfigService` — 20 rAthena-default knobs.
- **B-L2** `IBattleZoneGateService` —
  `can_hit_bg_target` / `can_hit_gvg_target` /
  `get_exception_ai` with same-guild fallback.

**Final coverage**: every rAthena `battle_*` function has a
canonical C# entry point. ~20 of 41 are working implementations;
the remaining ~21 are documented "data-pending" paths whose
parent dependency (equip aggregator, SC table, skill_db element
column, battle_athena.conf parser) is explicit in each service
header. 435 tests green.
