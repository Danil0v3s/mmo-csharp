# pc.cpp parity · 2026-05-19

`src/map/pc.cpp` (15 989 lines, 157 unique `pc_*` functions) is the
player-character core: lifecycle, stats, skills, equipment, inventory,
options, bonuses, scripts, and dozens of side systems. This audit
groups the function list by subsystem and tracks our C# coverage.

## Status legend

- ✅ implemented — full or near-full parity with rAthena
- ⚠️ partial — exists but has gaps documented inline
- ❌ missing — no C# equivalent

## Subsystem coverage

### Lifecycle (auth / login / cleanup)

| rAthena fn | Status | C# location / note |
|---|---|---|
| `pc_authok` | ⚠️ | char→map auth handoff in [CharServerIpcService](/Map.Server/Services/CharServerIpcService.cs); some post-auth steps missing |
| `pc_authfail` | ⚠️ | map handles via session disconnect — no explicit fail packet path |
| `pc_setnewpc` | ⚠️ | partial via session enter; doesn't push the rAthena ZC_AID/PCB packets in the exact order |
| `pc_reg_received` | ❌ | global/account/char script vars not loaded into session |
| `pc_scdata_received` | ❌ | persisted SC state not re-applied at login |
| `pc_makesavestatus` | ⚠️ | autosave runs but doesn't include the full mmo_charstatus snapshot |
| `pc_should_log_commands` | ✅ | [AtCommandLogger](/Map.Server/Gm/AtCommandLogger.cs) gates on group LogCommands |

### Position / warp / save

| rAthena fn | Status | C# location |
|---|---|---|
| `pc_setpos` | ✅ | [PcSetposService](/Map.Server/Movement/PcSetposService.cs) |
| `pc_setsavepoint` | ✅ | [PcDeathService.SetSavepoint](/Map.Server/Combat/PcDeathService.cs) |
| `pc_lastpoint_special` | ❌ | special savepoint maps (e.g. izlude_d) hardcoded — we don't honor |
| `pc_randomwarp` | ❌ | no random-warp helper; @jump covers the GM side only |
| `pc_memo` | ❌ | warp scroll memo not implemented |
| `pc_cell_basilica` | ❌ | Basilica / Land Protector cell effects |
| `pc_jail` | ❌ | jail map / timer system absent |

### Stat allocation

| rAthena fn | Status | C# location |
|---|---|---|
| `pc_statusup` | ✅ | [StatusChangeHandler](/Map.Server/Handlers/StatusChangeHandler.cs) |
| `pc_traitstatusup` | ❌ | renewal trait stats (POW/STA/WIS/SPL/CON/CRT) — UI unwired |
| `pc_setparam` | ⚠️ | partial via direct stat mutation; no GM `@stat` |
| `pc_readparam` | ❌ | SP_* read facade — needed for script side |
| `pc_maxbaselv` / `pc_maxjoblv` / `pc_maxparameter` | ⚠️ | caps live in ExpTable; no per-job override |
| `pc_is_maxbaselv` / `pc_is_maxjoblv` | ⚠️ | implicit in level commands |
| `pc_updateweightstatus` | ❌ | weight-stage SC application missing |

### EXP / level

| rAthena fn | Status | C# location |
|---|---|---|
| `pc_gainexp` | ✅ | [ExpService](/Map.Server/Status/ExpService.cs) |
| `pc_gainexp_disp` | ⚠️ | ZC_NOTIFY_EXP fires but verbose mode unwired |
| `pc_lostexp` | ❌ | death penalty applied inline in PcDeathService; not exposed as a helper |
| `pc_level_penalty_mod` | ⚠️ | level-diff EXP scaling is hardcoded constant |
| `pc_baselevelchanged` | ❌ | hook for "base level changed" — fires after every level-up |

### Skill

| rAthena fn | Status | C# location |
|---|---|---|
| `pc_skill` | ⚠️ | partial via inventory hydration; no `skill_id, lv, type` adder |
| `pc_skillup` | ✅ | [UpgradeSkillHandler](/Map.Server/Handlers/UpgradeSkillHandler.cs) |
| `pc_calc_skilltree` | ❌ | no job-tree resolution; skills come pre-baked from char-server |
| `pc_clean_skilltree` | ❌ | no reset path |
| `pc_skill_plagiarism` | ❌ | Stalker copy-skill |
| `pc_skill_plagiarism_reset` | ❌ | clear copied skill |
| `pc_checkskill` | ⚠️ | reads `LearnedSkills[id]` — fine for the common case |
| `pc_checkskill_imperial_guard` / `pc_checkskill_summoner` | ❌ | job-specific helpers |
| `pc_validate_skill` | ❌ | per-job skill availability validator |

### Equipment

| rAthena fn | Status | C# location |
|---|---|---|
| `pc_equipitem` | ✅ | [EquipService](/Map.Server/Inventory/EquipService.cs) |
| `pc_unequipitem` | ✅ | EquipService |
| `pc_isequip` | ⚠️ | EquipService.ResolveAllowedPositions covers the bit math; per-job validation missing |
| `pc_isequipped` | ⚠️ | scan inventory; works |
| `pc_setequipindex` | ⚠️ | rebuilt implicitly on each equip op |
| `pc_calcweapontype` | ❌ | derives `weapontype` from equipped weapon — needed for skill `WeaponType` mask |
| `pc_equiplookall` | ❌ | broadcasts every equip slot to AOI |
| `pc_equipswitch_remove` | ❌ | equipswitch (second set) not honored |
| `pc_set_costume_view` | ❌ | costume slots visible-but-no-stats logic missing |
| `pc_check_available_item` | ⚠️ | partial via TradeService bounded-item check |
| `pc_checkequip2` | ⚠️ | equip lookups exist; not a 1:1 helper |
| `pc_insert_card` | ❌ | card insertion via Anvil not yet ported |
| `pc_check_expiration` / `pc_expire_check` | ❌ | rental + char expiration sweep |

### Inventory

| rAthena fn | Status | C# location |
|---|---|---|
| `pc_dropitem` | ✅ | [ItemThrowHandler](/Map.Server/Handlers/Inventory/ItemThrowHandler.cs) |
| `pc_takeitem` | ✅ | [PickupHandler](/Map.Server/Handlers/PickupHandler.cs) + [PickupAction](/Map.Server/Handlers/Actions/PickupAction.cs) |
| `pc_delitem` | ⚠️ | InventoryService removes by index; not as ergonomic as rAthena's variants |
| `pc_checkadditem` | ⚠️ | partial via inventory full check |
| `pc_inventoryblank` | ⚠️ | count via LINQ; no dedicated helper |
| `pc_setinventorydata` | ⚠️ | hydration runs on session enter |
| `pc_putitemtocart` / `pc_cart_delitem` / `pc_getitemfromcart` | ❌ | cart subsystem absent |
| `pc_setcart` | ❌ | cart enable/disable not exposed |
| `pc_inventory_rental_clear` / `pc_inventory_rentals` / `pc_inventory_rental_add` | ❌ | rental item lifecycle |
| `pc_identifyall` | ❌ | mass-identify |
| `pc_itemcd_add` / `pc_itemcd_check` / `pc_itemcd_do` | ❌ | per-item cooldown table |
| `pc_candrop` | ⚠️ | bounded/trade-protected flag check; partial |
| `pc_isautolooting` | ❌ | autoloot flag |

### Zeny / cash

| rAthena fn | Status | C# location |
|---|---|---|
| `pc_payzeny` | ⚠️ | direct CharacterData.Zeny mutation in shop/trade; not a helper |
| `pc_getzeny` | ⚠️ | same — exposed as @zeny |
| `pc_paycash` | ❌ | cash/points/kafra-points payment |

### Options / appearance

| rAthena fn | Status | C# location |
|---|---|---|
| `pc_setoption` | ❌ | option bitmask (riding/cart/falcon/madogear/etc.) |
| `pc_setcart` / `pc_setriding` / `pc_setfalcon` / `pc_setmadogear` | ❌ | option toggles |
| `pc_changelook` | ❌ | appearance broadcast (hair/clothes/head gear visuals) |
| `pc_disguise` | ❌ | turn the PC into a different sprite |
| `pc_setinvincibletimer` / `pc_delinvincibletimer` | ❌ | post-warp invincibility window |

### Orbs (Sphere/Soul/Servant/Abyss/Spirit/Charm)

| rAthena fn | Status | C# location |
|---|---|---|
| `pc_addspiritball` / `pc_delspiritball` | ❌ | Monk/SuraSphere counter |
| `pc_addsoulball` / `pc_delsoulball` | ❌ | Soul Reaper |
| `pc_addservantball` / `pc_delservantball` | ❌ | Servant Weapon |
| `pc_addabyssball` / `pc_delabyssball` | ❌ | Abyss Chaser |
| `pc_addspiritcharm` / `pc_delspiritcharm` | ❌ | Kagerou/Oboro charm |
| `pc_crimson_marker_clear` | ❌ | Rebellion Crimson Marker |

### Bonuses & scripts

| rAthena fn | Status | C# location |
|---|---|---|
| `pc_bonus` / `pc_bonus2-5` | ⚠️ | only via equip aggregator; no script-engine `bonus` opcode |
| `pc_bonus_script` / `pc_bonus_script_clear` / `pc_bonus_script_free_entry` | ❌ | script-temp bonuses |
| `pc_addautobonus` / `pc_delautobonus` / `pc_exeautobonus` | ❌ | auto-cast bonuses (on hit / when hit / on skill) |

### State flags / events / timers

| rAthena fn | Status | C# location |
|---|---|---|
| `pc_addeventtimer` / `pc_deleventtimer` / `pc_cleareventtimer` / `pc_addeventtimercount` | ❌ | script event timers |
| `pc_set_bg_queue_timer` / `pc_delete_bg_queue_timer` | ❌ | battleground queue |
| `pc_close_npc` | ❌ | force-close NPC dialog |
| `pc_set_hate_mob` / `pc_set_costume_view` | ❌ | Taekwon hate/feel |
| `pc_setrestartvalue` | ⚠️ | restart HP/SP partial via PcDeathService.Respawn |

### Marriage / adoption / fame

| rAthena fn | Status | C# location |
|---|---|---|
| `pc_marriage` / `pc_divorce` / `pc_adoption` / `pc_try_adopt` | ❌ | marriage system absent |
| `pc_addfame` | ❌ | fame point accrual |

### Pet / mount

| rAthena fn | Status | C# location |
|---|---|---|
| `pc_setfalcon` / `pc_setriding` / `pc_setmadogear` | ❌ | mount toggles |
| `pc_overheat` | ❌ | Mado overheat |

### Damage / heal / revive

| rAthena fn | Status | C# location |
|---|---|---|
| `pc_damage` | ⚠️ | [DamageService.ApplyDamage](/Map.Server/Combat/DamageService.cs) covers HP delta + death routing |
| `pc_heal` | ⚠️ | direct HP/SP delta; HealCommand wraps for GM use |
| `pc_revive` | ⚠️ | via PcDeathService.Respawn / AliveCommand |
| `pc_revive_item` | ❌ | Token of Siegfried etc. |
| `pc_bleeding` | ❌ | SC_BLEEDING DoT |
| `pc_regen` | ⚠️ | [NaturalHealService](/Map.Server/Status/NaturalHealService.cs) — HP/SP only; AP missing |

### Trade gates

| rAthena fn | Status | C# location |
|---|---|---|
| `pc_can_give_items` | ⚠️ | inline checks in TradeService |
| `pc_can_give_bounded_items` | ⚠️ | inline |
| `pc_can_trade_item` | ⚠️ | inline |
| `pc_can_sell_item` | ⚠️ | inline in ShopService |
| `pc_modifybuyvalue` / `pc_modifysellvalue` | ❌ | Discount/Overcharge skills not honored at shop |

### Script variables (per-player)

| rAthena fn | Status | C# location |
|---|---|---|
| `pc_readreg` / `pc_readreg2` / `pc_readregistry` | ✅ | [IPlayerVarService.ReadNum/ReadStr](/Map.Server/Scripting/Vars/IPlayerVarService.cs) |
| `pc_setreg` / `pc_setreg2` / `pc_setregistry` / `pc_setregistry_str` / `pc_setregstr` | ✅ | [PlayerVarService.WriteNum/WriteStr](/Map.Server/Scripting/Vars/PlayerVarService.cs) |
| `pc_set_reg_load` | ❌ | "regs loaded" flag |

### Macro detector (anti-bot)

| rAthena fn | Status | C# location |
|---|---|---|
| `pc_macro_*` (8 fns) | ❌ | macro-detection subsystem absent — premium-server feature |

### Misc

| rAthena fn | Status | C# location |
|---|---|---|
| `pc_attendance_enabled` / `pc_attendance_claim_reward` | ❌ | daily attendance |
| `pc_show_questinfo` / `pc_show_questinfo_reinit` | ❌ | quest-marker display |
| `pc_show_version` | ❌ | version overhead text |
| `pc_jobchange` | ❌ | job change |
| `pc_steal_item` | ❌ | Steal skill |
| `pc_job_can_entermap` | ❌ | per-job map gate (e.g. doram restrictions) |
| `pc_readdb` | ⚠️ | various DB readers — most already in repo layer |
| `pc_reputation_generate` | ❌ | renewal reputation system |

## Coverage summary

| Bucket | Done | Partial | Missing |
|---|---|---|---|
| Lifecycle | 1 | 4 | 2 |
| Position / warp / save | 2 | 0 | 5 |
| Stat allocation | 1 | 4 | 2 |
| EXP / level | 1 | 2 | 2 |
| Skill | 1 | 2 | 6 |
| Equipment | 2 | 5 | 7 |
| Inventory | 2 | 5 | 8 |
| Zeny / cash | 0 | 2 | 1 |
| Options / appearance | 0 | 0 | 8 |
| Orbs | 0 | 0 | 6 |
| Bonuses / scripts | 0 | 1 | 3 |
| State flags / timers | 0 | 1 | 6 |
| Marriage / fame | 0 | 0 | 5 |
| Damage / heal / revive | 0 | 5 | 2 |
| Trade gates | 0 | 4 | 1 |
| Script vars | 0 | 0 | 3 |
| Macro detector | 0 | 0 | 1 |
| Misc | 0 | 1 | 7 |
| **Totals** | **10** | **36** | **75** |

121 of 157 functions tracked here. Of those, 10 (8%) are full
parity, 36 (30%) are partial, and 75 (62%) are missing. The
remaining ~36 functions are private helpers or thin wrappers we
absorb into call sites.

## Implementation plan

### Phase 1 — Player option / mount surface (high client visibility)

Mount/cart/falcon/madogear toggles + `pc_setoption` + `pc_changelook`
are the most visible missing piece. A player who mounts a Peco today
gets no visual change. Plan:

1. `IPlayerOptionService` with bitfield + ZC_SPRITE_CHANGE / ZC_OPTION_CHANGE broadcast.
2. `pc_setcart`, `pc_setriding`, `pc_setfalcon`, `pc_setmadogear` →
   thin wrappers on the option service.
3. `pc_changelook(LOOK_*)` → broadcast appearance change to AOI.
4. `pc_disguise` → swap class display + re-broadcast.

### Phase 2 — Orbs (combat-visible)

Sphere/soul/servant/abyss/spirit/charm. Wire 6 small services backed
by a counter on `PlayerEntity` + ZC_SPIRITS / ZC_SOULENERGY packets.

### Phase 3 — Cart inventory

`pc_putitemtocart` / `pc_cart_delitem` / `pc_getitemfromcart` plus
cart packet flow. CartInventoryRepository already exists.

### Phase 4 — Item cooldowns + rentals

`pc_itemcd_*` + `pc_inventory_rentals` — both need a per-session
table + periodic timer. Affects consumables and rental gear.

### Phase 5 — Script vars + bonus_script

`pc_setreg*` / `pc_readreg*` — needed for NPC scripts. Then
`pc_bonus_script` for SC scripts.

### Phase 6 — Big-feature ports

`pc_jobchange`, `pc_calc_skilltree`, `pc_steal_item`, marriage/adopt,
fame, jail, attendance, autobonus.

### Phase 7 — Trade-gate cleanup

Replace inline trade/shop gate checks with the canonical helpers so
the bounded/expired/storage-protected logic centralises.

## History

### 2026-05-19 — initial audit
- Enumerated all 157 `pc_*` functions.
- 10 done / 36 partial / 75 missing across 18 subsystems.
- 7-phase plan documented above.

### 2026-05-19 — Phase 1, 2, 4, 5, 6 landing
- **PC-1** `IPlayerOptionService` + `ZC_STATE_CHANGE3` (0x0229) for
  the 32-bit effect-state bitmask. `Option` / `Opt1` / `Opt2` /
  `Karma` fields on `PlayerEntity`.
- **PC-2** `pc_setcart`, `pc_setriding`, `pc_setfalcon`,
  `pc_setmadogear` as method wrappers; broadcasts share the same
  `NotifyOption` pipe. New GM commands `@mount`, `@cart`, `@option`.
- **PC-3** `IPlayerLookService` + `LookType` mirror of rAthena
  `enum _look`. Emits `ZC_SPRITE_CHANGE2` to AOI.
- **PC-4** `IPlayerOrbService` (Spirit / Soul / Servant / Abyss) +
  `ZC_SPIRITS` / `ZC_SOULENERGY` packets. Caps at rAthena defaults
  (15 / 20 / 5 / 5). GM commands `@spiritball`, `@soulball`.
- **PC-5** `InvincibleUntilTick` on `PlayerEntity`; applied on every
  `pc_setpos` (5 000 ms — rAthena `battle.invincible_time`).
  `DamageService.CanDamage` honors it for PvE + PvP both.
- **PC-6** `pc_modifybuyvalue` / `pc_modifysellvalue` ported into
  `ShopService`: Discount (MC_DISCOUNT) / Compulsion (RG_COMPULSION)
  reduce buy price; Overcharge (MC_OVERCHARGE) boosts sell.

Coverage after this pass: ~16 done / ~30 partial / ~75 missing.

### 2026-05-19 — Wave 2 (PC-8 / PC-9 / PC-10 / PC-11)
- **PC-8** `IPlayerSkillService.Grant(skill, lv, kind)` — pc_skill
  wraps `LearnedSkills` mutation. Honors PERMANENT / TEMP_ADDLEVEL /
  PERMANENT_QUEST kinds.
- **PC-9** `IPlayerFameService.AddFame` + `PlayerEntity.Fame` —
  fame counter. Ranking aggregation stays char-server side.
- **PC-10** `IDialogDispatcher.ForceClose` — pc_close_npc engine-side
  cancel. Auto-fired by `PcSetposService` so warps clear stale dialog.
- **PC-11** `PlayerLookExtensions.Disguise / Undisguise` — composes
  on `IPlayerLookService.ChangeLook(LookType.Base, class)`.

### 2026-05-19 — Wave 3 (PC-12)
- **PC-12** `IPlayerVarService` + `PlayerVarService` — pc_setreg /
  pc_readreg / pc_setregistry family. 4 scopes (CharTemp / Char /
  Account / GlobalAccount) backed by the existing EF Core repos
  (char_reg_num/str, acc_reg_num/str, global_acc_reg_num/str).
  Read-through cache + dirty-set flushed on autosave / logout.
  Unblocks NPC scripts that need persistent state.

### 2026-05-19 — Wave 4 (PC-13)
- **PC-13** `IJobChangeService.Change` — pc_jobchange first slice.
  Sets `CharacterData.ClassId`, broadcasts LOOK_BASE, recalcs +
  full-heals, pushes the four HP/SP par-change packets. GM command
  `@jobchange <classId>`. Out of scope (documented): upper/baby
  trees, bard/dancer sex auto-swap, skill-tree reset.

Coverage after this pass: ~21 done / ~28 partial / ~72 missing.

### 2026-05-19 — Wave 5-13 sweep (PC-14 .. PC-22)
Final pass to land canonical entry points for every remaining
`pc_*` function. Where the backend subsystem isn't ported yet, the
implementation is a documented stub (log + return) so call sites
can wire to the rAthena name without grep-rewriting later.

- **PC-14 (Skill helpers)** `IPlayerSkillService` extended with
  `CalcSkillTree`, `CleanSkillTree`, `TryPlagiarize`,
  `PlagiarismReset`, `Validate`, `CheckImperialGuard`,
  `CheckSummoner`. `IExpService` gains `LoseExp` +
  `OnBaseLevelChanged`. `PlayerEntity` gets `SpiritCharm` /
  `SpiritCharmType` / `PlagiarizedSkill*`.
- **PC-15 (Equip helpers)** `IPlayerEquipHelpers` —
  `CalcWeaponType`, `EquipLookAll`, `EquipSwitchRemove`,
  `SetCostumeView`, `InsertCard`, `CheckExpiration`.
- **PC-16 (Inventory helpers)** `IPlayerInventoryHelpers` —
  cart family (Put/Del/Get), `InventoryRental*`, `IdentifyAll`,
  `ItemCooldown*` (working, in-memory), `IsAutolooting`.
- **PC-17 (Bonus engine)** `IPlayerBonusService` — `AddBonusScript`,
  `ClearBonusScripts`, `AddAutobonus`, `DelAutobonus`,
  `ExecuteAutobonus`. `AutobonusTrigger` enum mirrors rAthena.
- **PC-18 (Timer / hate / jail)** `IPlayerTimerService`,
  `IPlayerBgQueueTimerService`, `IPlayerHateService`,
  `IPlayerJailService`.
- **PC-19 (Marriage / adoption)** `IPlayerRelationService` +
  `PlayerEntity.PartnerId / FatherCharId / MotherCharId /
  ChildCharId`. `AdoptResult` enum mirrors rAthena
  `adopt_responses`.
- **PC-20 (Misc)** `IPlayerAttendanceService`,
  `IPlayerQuestMarkerService`, `IPlayerStealService`,
  `IPlayerReputationService`, `IPlayerVersionDisplayService`,
  `IPlayerReviveItemService` (working),
  `IPlayerMacroDetectorService` (premium feature stub).
- **PC-21 (Position helpers)** `IPlayerPositionHelpers` —
  `IsLastpointSpecial`, `RandomWarp` (working), `Memo`,
  `IsBasilicaCell`.
- **PC-22 (Lifecycle / stat)** `IPlayerLifecycleHelpers` —
  `OnRegReceived`, `MakeSaveStatus`, `SetRestartValue`.
  `IPlayerStatHelpers` — `SetParam` / `ReadParam` (working,
  16 SP_* slots), `TraitStatusUp`, `MaxParameter`,
  `MaxBaseLevel`, `MaxJobLevel`, `PayCash`.

**Final coverage**: every rAthena `pc_*` function in the audit has
a canonical C# entry point. Working implementations: ~25 of 157.
Documented-stub (call site wires + log, deferred behavior):
remaining ~120. Genuinely unimplemented (no entry point): 0.

The stub-vs-impl split is documented in each service header so
follow-up work knows exactly what to upgrade as the dependent
subsystem (cart inventory hydration, attendance.yml loader, card
slot column, bonus-script runtime, etc.) ports.
