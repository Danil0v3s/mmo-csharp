# Parity Roadmap — Timeline & Sequencing

Companion to [README.md](README.md). The README is the **what** (79 tickets); this
is the **when/in-what-order**. Phases are ordered by dependency + player-visible
leverage. Tickets within a phase are mostly parallel unless a dependency is noted.

**Workflow:** every ticket starts in [`todo/`](todo/) (grouped by epic). When you
start one, `git mv` it to [`inprogress/`](inprogress/); when it lands (build green,
tests added, Status header flipped, History line appended), `git mv` it to
[`done/`](done/). The `todo/` epic subfolders encode the area; `inprogress/` and
`done/` are flat (the ticket ID is self-identifying).

```
todo/<epic>/TICKET.md  ──start──▶  inprogress/TICKET.md  ──land──▶  done/TICKET.md
```

## Dependency cheat-sheet

```
COMBAT-01 ─▶ COMBAT-06            (shared flat-bonus consumer wiring)
COMBAT-03 ─▶ SKILL-12            (RE_LVL_DMOD composed into per-skill ratios)
COMBAT-09 ◀▶ COMBAT-01           (CalcPc recalc ordering touches the same path)
SKILL-04  ─▶ SKILL-07..12        (ISkillDb on context unblocks duration reads)
FEATURE-01 ─▶ FEATURE-03/04/07   (mob-death observer feeds quest/ach/pet-catch)
FEATURE-02 ─▶ FEATURE-03/04/07/08/09/10  (persistence wiring underlies all companion/quest state)
PACKET-0x ◀▶ FEATURE-0x          (packet bridge + service behavior land in lockstep)
SCRIPT-01/02/03 ─▶ SCRIPT-10     (a kafra needs working dialog + warp/save/storage)
SCRIPT-04 ─▶ FEATURE-03/04       · SCRIPT-06 ─▶ FEATURE-14 · SCRIPT-07 ◀▶ INFRA-07
```

---

## Phase 0 — Engine correctness foundation  *(do first; everything reads these)*

The numbers a player sees are wrong until these land. Small, high-leverage.

| Ticket | Why first |
|---|---|
| **COMBAT-01** | A `+10 STR` card / `+30 HIT` gear currently does nothing — every stat downstream is wrong. Dependency-free. |
| **COMBAT-09** | `CalcPc` zeroes derived stats each recalc and drops job-bonus/SC deltas; pairs with COMBAT-01 (same path). |
| **SC-01** | Harden the PresenceMarker overwrite guard (latent: a re-order silently kills 90 effects). Cheap insurance. |
| **SC-02** | `CalcFlags: All` mis-maps element-endow / MATK / resist SCs to "+Val1 to 6 base stats" — real wrong magnitudes. |

**Exit:** equip a stat card → stat window + auto-attack damage change correctly; SC completeness test still 4/4 with the new anti-shadow assertion.

## Phase 1 — Combat fidelity  *(parallel after Phase 0)*

Makes damage/cast match rAthena. Mostly independent rows.

- **COMBAT-02** per-skill damage ratio (`battle_calc_attack_skill_ratio`) + constant-addition
- **COMBAT-03** `RE_LVL_DMOD` renewal level scaling *(unblocks SKILL-12)*
- **COMBAT-04** base-damage DEX/weapon-lvl/arrow, size-fix table, multi-hit div, dual-wield
- **COMBAT-05** defensive (target-side) cardfix + per-skill element resolution
- **COMBAT-06** `pc_bonus`/`bonus2`/`bonus3` switch-table breadth *(after COMBAT-01)*
- **COMBAT-07** renewal variable-cast `sqrt` + equip/card cast bonuses (bundle already populated — pure wiring)
- **COMBAT-08** cast-interrupt-on-damage + `clif_skillcastcancel` packet + Safety Wall/Pneuma/Land Protector intercept
- **SKILL-01** route SC procs through apply-rate (resist/luck) instead of `Random.Shared`
- **SKILL-03** splash allegiance (slave-mob + PvP/no-FF mapflags)
- **SKILL-05** retire the dead `DamageRate` ratio path (latent trap)
- **SC-03..SC-08** remaining SC magnitude/consumer/spread work

**Exit:** Bash/Magnum/Asura/Double-Strafe magnitudes match rAthena at representative levels; cast bar interrupts on hit; a Priest's resist cards reduce incoming damage.

## Phase 2 — Feature unlock  *(the biggest player-facing win; packets + behavior in lockstep)*

Today most features are unreachable (only 39 `CZ_` handlers) and the services are
shells. Pick a feature, land its PACKET + FEATURE pair together. **Land
FEATURE-01 + FEATURE-02 first** — the mob-death observer + persistence underpin the rest.

1. **FEATURE-01** mob-death observer hub  ·  **FEATURE-02** companion/quest/achievement persistence
2. Quest line: **FEATURE-03** + **PACKET-10**
3. Achievement line: **FEATURE-04** + **PACKET-10**
4. Mail/RODEX: **FEATURE-05** + **PACKET-06**
5. Party: **FEATURE** (model exists) + **PACKET-01**
6. Guild: **PACKET-02** (+ **FEATURE-15** WoE scheduler)
7. Pet: **FEATURE-07** + **PACKET-03**
8. Homunculus: **FEATURE-08** (new live entity) + **PACKET-04**
9. Mercenary: **FEATURE-09** + **PACKET-05**
10. Elemental: **FEATURE-10**
11. Vending/Buying/Cashshop: **FEATURE-11/12/13** + **PACKET-08**
12. Auction: **FEATURE-06** + **PACKET-07**
13. Instance: **FEATURE-14** + **PACKET-09**

**Exit:** a player can open mail, accept/leave a party, hatch & feed a pet, summon a homunculus that fights, and complete a quest — all end-to-end against the live client, surviving logout.

## Phase 3 — NPC scripting runtime  *(parallel with Phase 2; gated chain at the end)*

Engine + dialog work today; ~3% of builtins are real and there are zero game NPCs.

- **SCRIPT-01** dialog primitives (input/close2/prompt/cutin/progressbar)
- **SCRIPT-02** player-state builtins (warp/heal/item/job/sc/skill) — the big one
- **SCRIPT-03** event-hook dispatch (onInit/onTouch/onTimer/onClock/onPC*)
- **SCRIPT-07** variable/register system + mapreg SQL *(overlaps INFRA-07)*
- **SCRIPT-08** timer/effect/clif/NPC-control builtins
- **SCRIPT-04/05/06** quest/party-guild/instance builtins *(gated on the matching FEATURE-*)*
- **SCRIPT-11** npc-chat
- **SCRIPT-09** companion/mail/channel/BG builtins *(lowest priority)*
- **SCRIPT-10** bulk NPC conversion + `registerDuplicate` *(XL; hard-blocked on 01/02/03)*

**Exit:** a converted kafra (warp/save/storage/menu) works in prontera; the rAthena town-NPC corpus can be transpiled and placed.

## Phase 4 — Skill body depth  *(parallel; after SKILL-04 + COMBAT-03)*

- **SKILL-04** add `ISkillDb` to context, replace hardcoded SC durations/Vals *(unblocks the rest)*
- **SKILL-02** position-staggered AoE timers (MeteorStorm/comet trains)
- **SKILL-06** port the genuinely-missing skills + verify `_ATK` sub-skill invocation
- **SKILL-07** Taekwon (37 shells) · **SKILL-08** Npc (45) · **SKILL-09** Ninja (7) · **SKILL-10** Gunslinger · **SKILL-11** Homun/Summoner/Novice
- **SKILL-12** Mage/Archer/Thief/Swordman/Merchant/Acolyte depth-polish

**Exit:** no bare shell plugins (default ratio 100 / 2-cell splash); every learned-level skill matches rAthena's `case` output.

## Phase 5 — Infra, persistence & mob AI polish  *(parallel anytime)*

- **INFRA-01..09** refine / change-material / elemental-analysis / sage-autospell / search-store / party-booking / mapreg-SQL / game-log / bonus-host residuals
- **INFRA-10** navi generator — decide won't-do-for-runtime (documented)
- **MOBAI-01** slave/master · **MOBAI-02** MVP behavior · **MOBAI-03** change-target modes · **MOBAI-04** aggro LOS/range (stops aggro-through-walls)

**Exit:** refine/produce/search-store work; `$globalvar` survives restart; logs persist; mobs respect line-of-sight; MVPs behave like MVPs.

---

## At-a-glance ordering

```
Phase 0  ▓▓                         engine correctness (4 tickets)
Phase 1   ▓▓▓▓▓▓▓▓▓▓▓               combat fidelity     (~14)
Phase 2     ▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓  feature unlock      (25, lockstep)
Phase 3        ▓▓▓▓▓▓▓▓▓▓▓          scripting runtime   (11)
Phase 4            ▓▓▓▓▓▓▓▓         skill depth         (12)
Phase 5         ▓▓▓▓▓▓              infra + mob AI      (14)
            └─ overlapping; 2/3/5 run in parallel once 0/1 stabilize ─┘
```

## Progress log

Update this as columns shift (date · ticket · todo→inprogress / inprogress→done).

- **2026-06-01** — Roadmap created (79 tickets, all in `todo/`). Build break fixed (`PlayerEntity.CanEquipTick` wired into `EquipItemHandler`). Baseline: 0 tickets in progress, 0 done.
- **2026-06-01** — **COMBAT-01** inprogress→done. Equip/card **flat-derived** bonuses (Hit/Flee/Cri/Batk/Matk/MaxHp/MaxSp/Aspd) now reach `CalcPc` idempotently on all recalc paths + build on map enter. Param-stat half (`bStr..bLuk`) needed a base→final split → filed **COMBAT-10** (new, in `todo/combat/`; coupled with COMBAT-09). 3553/3553 Map.Server tests green.
- **2026-06-01** — **SC-01** inprogress→done. SC registry is now order-independent: `PresenceMarker` reuses shared `_NoOp`, and `Register` refuses to let a marker overwrite a real OnStart body (OR-merges the flag). Removed 128 dead duplicate markers (Marionette pair kept). New `StatusEffectShadowGuardTests` (16). 3569/3569 green. No follow-ups.
- **2026-06-01** — **SC-02** inprogress→done. The `CalcFlags: All` mis-mapping fixed for the 7 named SCs: weapon endows now set the weapon element (no phantom all-stat buff), Incmatkrate = MATK%, Siegfried/Nibelungen bespoke (deleted shadowing all-six bodies), Berserk verified. 3586/3586 green. Filed SC-10 (bulk triage of ~35 remaining) + SC-11 (more endow SCs).
- **2026-06-01** — **COMBAT-02** inprogress→done. Added the skill constant-addition stage (`SkillImpl.CalculateSkillConstantAddition`, applied after ratio in `WeaponSkillImpl`); Asura now adds its `250+150*lv` constant; Magnum ratio fixed to rAthena inner/outer; Bash/Double-Strafe verified; no-double-count guard. 3594/3594 green. Filed COMBAT-12 (magic ratio) + COMBAT-13 (Asura >5-sphere ×2).
- **2026-06-01** — **COMBAT-03** inprogress→done. Renewal RE_LVL_DMOD now applied: weapon ratio (`ReLvlDivisor` virtual, default 100) + magic (RE_LVL_MDMOD) + misc, scaling damage by baseLevel/100 above lv99. 3602/3602 green. Filed COMBAT-14 (per-skill exceptions: data gate + 120/150 + trap TMDMOD).
- **2026-06-01** — **COMBAT-04** inprogress→done. PC base-damage now uses the renewal DEX-derived atkmin (`dex*(80+weaponLv*20)/100`, weapon level plumbed end-to-end) instead of a flat max. 3613/3613 green. XL ticket — filed COMBAT-16 (size-fix+bow), COMBAT-17 (multi-hit div), COMBAT-18 (dual-wield) for the other 3 axes.
- **2026-06-01** — **COMBAT-05** inprogress→done. Defender-side cardfix: a player's bSubRace/bSubEle/bSubSize/bSubClass resist cards now reduce incoming damage (incl. from mobs — the old src-not-PC early-out skipped it). 3620/3620 green. XL — filed COMBAT-19 (skill element), COMBAT-20 (plant+GvG/BG), COMBAT-21 (advanced cardfix).
- **2026-06-01** — **COMBAT-06** inprogress→done. Item-script rate bonuses now work: bAtkRate (weapon %), bMatkRate (magic %), bDef/bMdef flat + bDefRate/bMdefRate % (CalcPc). 3629/3629 green. XL umbrella — filed COMBAT-22 (bonus2 per-skill tail) + COMBAT-23 (single-value + flag form) for the rest.
- **2026-06-01** — **COMBAT-07** inprogress→done. Renewal variable-cast DEX/INT sqrt reduction + global equip/card cast bonuses (var/fix cast rate + flat ms + delay rate) now applied in SkillCastTimingService. 3637/3637 green. Filed COMBAT-24 (per-skill cast tables + SA_ABRACADABRA).
- **2026-06-01** — **COMBAT-08** inprogress→done (axis 1). Damage now interrupts casts: `DamageService` cancels a cancellable-on-hit cast on the surviving-hit path (`unit_skillcastcancel` type 2, gated on `SkillDb.GetCastCancel` + `EquipBonusBundle.NoCastCancel`) and unconditionally on death (type 0). Replaced the `BroadcastSkillCastCancel` log stub with a real `ZC_SKILL_CAST_CANCEL` (0x01b9) AOI emit. 3641/3641 green. L ticket — filed COMBAT-25 (ground-unit intercept: Safety Wall/Pneuma/Land Protector), COMBAT-26 (CastEndMap warp), COMBAT-27 (SC no-cancel states incl. Basilica) for the other 3 axes; `bNoCastCancel` parse owned by COMBAT-23.
- **2026-06-01** — **COMBAT-09** inprogress→done (axes 3+4). Replaced the wrong `*540/590` amotion heuristic with the renewal `status_base_amotion_pc` ASPD formula (AGI/DEX sqrt curve, ranged /7 vs melee /5 divisor, `min(aspd_base,200)`, RE %-modifier from bAspdRate, `2000−aspd·10` conversion, bAspd flat, cap [95,4000]); `adelay=2·amotion`; renewal `dmotion=cap(800−4·agi,400,800)`. Threaded JobId/WeaponType into the 3 CalcPc call sites (fixes amotion≈40ms when job_aspd cache wired). Fixed MaxHP fold order to flat-before-rate. 3656/3656 green. Discovered axes 1 (SC re-fold) + 2 (job-bonus) need COMBAT-10's base/final split → moved to COMBAT-10; filed COMBAT-28 (SC/skill ASPD terms), COMBAT-29 (dual-wield/shield base), COMBAT-30 (trans ×1.25 MaxHP).
- **2026-06-01** — **SKILL-01** inprogress→done (engine slice). Built the status-change resist pipeline: `ScStartFlag` enum, renewal `status_get_sc_def` port (`StatusChangeService.GetScDef` + `ScDefTable`: stat resist, level-diff `(max(0,Δlv))²/5·100`, Curse LUK-0 immunity, boss/MVP gate, Aegis rounding, separate rate/duration reduction), rate-aware `Start(rate,…,flag)` that resists+rolls (seedable Random), legacy no-rate `Start` now a guaranteed wrapper. Migrated 3 representative procs (MeteorStorm/Adoramus/Bash). `Skill01ScDefTests` (12). 3668/3668 green. Unblocks SKILL-07/08/09/12. Filed SKILL-14 (bulk-migrate the remaining ~163 plugin proc rolls), SKILL-15 (ScDefTable depth: bespoke arms + min_rate/min_duration + SCRESIST/Siegfried/item-reseff).
- **2026-06-01** — **SKILL-03** inprogress→done. Splash allegiance fixed via the shared `BattleTargetResolver` (battle_check_target port): summoned-slave master substitution (player's slave + siblings → Party) + PvP/GvG/BG mapflags (field-map strangers are Neutral not enemies; pvp_noparty/gvg_noparty/pvp_noguild friendly-fire; GvG guildmates always allies). Added 5 MapFlag members + parser. MapForeachInRangeServiceTests → 13. 3677/3677 green. Unblocks SKILL-08/12. Filed SKILL-16 (route CanDamage's attack path through the resolver + attack-vs-mechanic-damage split, since the heal-flip applies friendly damage via ApplyDamage).
- **2026-06-01** — **SKILL-05** inprogress→done. Retired the legacy `DamageRate` second ratio authority: added the single `WeaponSkillImpl.ComputeSkillDamage` entry point (ratio→RE_LVL_DMOD→constant) shared by CastendDamageId + the SkillAttackService/WeaponSkillResolver funnels; plugin ratio now wins for any skill with a plugin, `DamageRate` is the no-plugin fallback only. Resolver leak-guards + doc rewrites + fallback-row annotations. SkillRatioConsistencyTests (3). 3680/3680 green. Filed SKILL-17 (ctx-aware ratio via funnel), SKILL-18 (Asura/MovePos dash slide broadcast).
- **2026-06-01** — **SC-03** inprogress→done. Corrected the Bard/Dancer song magnitudes (Assncros val1*2-1 cap 20; Whistle Flee2 (val1+1)/2 not ×10; Appleidun renewal HP-rate (5+2·v1)+casterVit/10+MusicalLesson/2) and deleted the 7 shadowed Wave4a duplicate registrations (one body per song; Drumbattle kept). Added SkillIds.BA_MUSICALLESSON. BardDancerSongFormulaTests (12). 3692/3692 green. No follow-ups.
- **2026-06-01** — **SC-04** inprogress→done. Wired 3 starved SC consumer reads: Kaupe (DamageService dodge — roll Val2% full block + Val3 count), Kaahi (on-hit heal Val2 HP charging Val3 SP, no-revive gate), Richmankim (+Val2% mob-kill EXP in ExpService). SC04ConsumerValReadTests (7). 3699/3699 green. Filed SC-12 (Energycoat/Crescentelbow), SC-13 (Magicrod/Poisonreact), SC-14 (Aurablade/Gravitation/Parrying), SC-15 (Soul family) for the remaining starved set; Longing→COMBAT-28.
- **2026-06-01** — **SC-05** inprogress→done. Rewrote the 12 Sorcerer *_OPTION SCs from the generator's phantom +Val1 to the fixed rAthena Val2: equip-Atk (Pyrotechnic/Heater/Tropic → WatkMin/Max), MATK (Aquaplay/Cooler/ChillyAir/Blast → MatkMin/Max), HP-rate% (Petrology/CursedSoil); presence-only correct Val2 for WildStorm/WindStep/WindCurtain. Removed 12 from StatusCalcFlagDefaults (generator floor→330), deleted the dup Pyrotechnic. SorcererOptionFormulaTests (16). 3711/3711 green. Filed SC-16 (element-change + bolt-autocast + Wind/Petrology secondary effects).
- **2026-06-01** — **SC-06** inprogress→done. Star Emperor stance + RG magnitudes fixed from generator +Val1 to rAthena: Sunstance Val2=2+Val1 ATK% (Batk+Watk), Starstance Val2=4+2*Val1 ASPD, Inspiration Val2=40*Val1 (ATK/MATK)+Val3=6*Val1 (all-stat)+MaxHp, Banding best-effort count (no faked Def). Lunar/Universe stances + Nen verified already-correct. SC06StanceFormulaTests (7). 3718/3718 green. Filed SC-17 (Inspiration debuff-clear/drain + Banding real count).
- **2026-06-01** — **SC-07** inprogress→done (XL slice). Built the generator-default enumeration (`StatusEffectRegistry.GeneratedStatModDefaultTypes` — the ~159 +Val1 magnitude-review worklist) + `GeneratorDefaultAuditTests` guard; converted Fear to its rAthena 20% Hit/Flee reduction (verified Quagmire/Marshofabyss already correct). 3721/3721 green. Remaining triage decomposed: SC-18 (linear-wrong), SC-19 (bespoke/not-a-stat), SC-20 (bulk remainder).
- **2026-06-01** — **SC-08** inprogress→done. SC-engine half of the P0.5 leaves: flagged all 18 SCF_SPREADEFFECT SCs (GetEffectiveFlags now OR-merges the table's SpreadEffect bit), wired the Deadly Infect spread trigger into DamageService (roll 30+10*Val1% both directions), added Hermode/DeadlyDefeasance to IsImmune. SC08SpreadImmuneTests (5). 3726/3726 green. Filed SC-21 (card-bonus tolerance matrix), SC-22 (companion refresh + status_change_refresh wiring + robust map-id lookup).
- **2026-06-01** — **COMBAT-10** inprogress→done. Ported the rAthena base→final param layering (status.cpp:4205-4266): new PcBaseParams (persisted base) on PlayerEntity; CalcPc folds base + equip param + job_bonus into the 12 stats via a delta-vs-snapshot (idempotent + primary-stat SC mods like Blessing/AGI-Up survive recalc); misc/atk/matk/maxhp/aspd derive from the final stats. All 7 recalc-input builders read BaseParams (fixed StatusOpsService's Pow..Crt=0 wipe); enter/alloc/setstat/trait-up write base. Combat10BaseFinalLayeringTests (7) + updated COMBAT-01 boundary; unit suite 3732 green. Discovered + filed COMBAT-31 (pre-existing DI cycle blocking Map.Server boot/replay harness), COMBAT-32 (passive-skill base addends + SuperNovice +10), COMBAT-33 (derived-stat SC re-fold).
- **2026-06-01** — **COMBAT-31** inprogress→done. Broke three accumulated DI cycles blocking Map.Server boot (ExpService→StatusChangeService; DamageService↔StatusChangeService; SkillBehaviorRegistry↔SkillAttackService-via-HolyLight) by making the runtime-only edges Lazy<> + DI factories. Map.Server now boots + binds 5191 in ~6s (was: dies at construction). Unit suite 3732 green; 4 test rigs updated. Replay E2E now boots past Map but hits the Login internal-ping readiness handshake → filed INFRA-11.
- **2026-06-01** — **COMBAT-12** inprogress→done. Magic-skill damage now routes through the plugin's CalculateSkillRatio (rAthena battle_calc_attack_skill_ratio is shared by BF_WEAPON+BF_MAGIC): CalcMagicDamage uses the plugin ratio as authority over skill_db.DamageRate when overridden (reflection-gated so DamageRate-reliant plugins are safe), + an ATK_ADD constant param on CalcMagicAttack. SoulStrike's +5*lv vs-undead bonus (dead hook) fixed by routing bolts through SkillAttack(BF_MAGIC); revives AL_HOLYLIGHT +25; added MG_SOULSTRIKE to SkillDb fallback. Combat12MagicRatioTests (4); suite 3736 green. (Ghost element → COMBAT-19.)
- **2026-06-02** — **COMBAT-13** inprogress→done. Asura Strike now doubles its ratio when cast with >5 spirit spheres (renewal battle.cpp:4843): CastendDamageId reads SpiritBall and threads the >5 bit as miscflag through the weapon pipeline; miscflag-aware CalculateSkillRatio applies ×2 before the 500000 cap. Combat13AsuraSphereTests (4); suite 3740 green. Filed SKILL-19 (spirit-ball requirement consumption).
- **2026-06-02** — **COMBAT-14** inprogress→done. Found the INF2_DISABLELVDMG premise fictional in this rAthena (per-arm RE_LVL_DMOD is the real mechanism); shipped the clean subset — ReLvlDivisor 120/150 overrides for the three weapon plugins routing through ComputeSkillDamage (PhantomThrust/FallenEmpire 150, FeintBomb 120). Combat14ReLvlDivisorTests (4); suite 3744 green. Filed COMBAT-35 (remaining 9 divisor plugins + Ranger trap TMDMOD + macro-omitting-arm disable audit).
- **2026-06-02** — **COMBAT-29** inprogress→done. Dual-wield + shield ASPD base: `CalcPc` adds `+ aspd_base[Shield=99]` when a shield is worn (new `EquipSummary.HasShield` + `PcBaseInputs`/`BattleStats.HasShield` through all 4 builders) else `+ aspd_base[wt2]/4` reusing COMBAT-18's `LeftWeaponType`; new `IJobAspdCacheService.GetBaseAspdExactByJobId` (exact-or-0) avoids the no-row default. Combat29AspdShieldDualWieldTests (2); unit suite 3845 (1 fail = pre-existing INFRA-11 replay gate). No follow-ups.
- **2026-06-02** — **COMBAT-39** inprogress→done. Multi-hit hit-count sweep: new `SkillHitCounts` table (signed skill_db `HitCount` for all 60 multi-hit WeaponSkillImpl skills, script-generated from db/re/skill_db.yml incl. 5 per-level tables); `WeaponSkillImpl.GetMultiHitCount` now defaults to `abs(SkillHitCounts.Get(SkillId,lv))` so Triple Attack renders 3, Vulcan Arrow 9, Sonic Blow 8, etc. (display only, HP unchanged); removed SonicBlow's redundant override. The sign feeds COMBAT-60's positive-div multiply. Combat39HitCountTests (11) + updated Combat17/38 expectations; unit suite 3909 (1 fail = pre-existing INFRA-11 replay gate). SkillImpl/splash base-count remainder noted on COMBAT-60.
- **2026-06-02** — **COMBAT-38** inprogress→done. Per-skill div_ arms: found the div logic already written in each plugin's `ModifyDamageData` but the hook was dead (never invoked). Wired it into `WeaponSkillImpl.CastendDamageId` + the `SkillAttackService` funnel, activating the WeaponSkillImpl arms (KN_PIERCE size+1, KN_BOWLINGBASH 2HSword→2, SC_FATALMENACE dagger+1, RA_WUGSTRIKE, RagingQuadrupleBlow, ThrowSpiritSphere, FrenzyShot) for the display div (HP delta unchanged). Combat38PerSkillDivTests (7); unit suite 3890 (1 fail = pre-existing INFRA-11 replay gate). Filed COMBAT-60 (splash/SkillImpl arms — WindCutter/BackStab/AxeStomp/OverSlash — + miscflag/ctx hook + positive-div per-hit multiply).
- **2026-06-02** — **COMBAT-37** inprogress→done. Auto-attack multi_attack FearBreeze + Chain Action: `BattleCalculator.CalcMultiAttack` split into three `Hits==1`-gated branches (rAthena order) — `TryFearBreeze` (bow+SC_FEARBREEZE+ammo>1, tier-ladder roll val5≤4%→5..≤13%→2, ammo-capped div, val4=div-1), the COMBAT-17 double-attack, and `TryChainAction` (revolver+GS_CHAINACTION or SC_E_CHAIN, 5*lv%→2 hits + SC_QD_SHOT_READY 1500ms). Added `IAmmoService.GetEquippedAmmoAmount` (live div cap) + injected IAmmoService into BattleCalculator. Combat37MultiAttackTests (9); unit suite 3883 (1 fail = pre-existing INFRA-11 replay gate). Filed COMBAT-59 (wire `_sc` into BattleCalculator so FearBreeze + the dormant SC-combat set — EDP/MagicPower/Signum/Kagemusya — activate in production).
- **2026-06-02** — **COMBAT-36** inprogress→done. Ranged ammo gate + consumption: new `IAmmoService`/`AmmoService` (HasUsableAmmo gate + ConsumeAmmo one-round-per-swing, drop stack + clear equip bit at 0 via the RemovedInventoryIds sync path; RequiredAmmoSubtype validates Bow→Arrow / guns→Bullet per renewal battle.cpp:10401) wired (optional) into `AttackService.Tick`: no/wrong ammo → reschedule without swinging (ATK_NONE), else swing then consume (battle_consume_ammo). Combat36AmmoConsumptionTests (5); unit suite 3874 (1 fail = pre-existing INFRA-11 replay gate). Filed COMBAT-58 (skill-path ammo consume + out-of-ammo client feedback).
- **2026-06-02** — **COMBAT-35** inprogress→done. RE_LVL_DMOD(120) applied to the two arms with a live divisor path: PinpointAttack + KoCrossSlash (WeaponSkillImpl → `ComputeSkillDamage`), stale docstring TODOs cleared. Discovered the ticket's premise is false for the other arms — the splash (`RecursiveDamageSplashSkillImpl`) + plain-`SkillImpl` plugins' `CalculateSkillRatio` is NOT consumed by the damage funnel (`SkillAttackService.WeaponDamage` uses the skill_db DamageRate for non-WeaponSkillImpl plugins), so a divisor override there is a no-op pending SKILL-17; and the Ranger traps have no damage path. Combat35ReLvlDivisorTests (5); unit suite 3869 (1 fail = pre-existing INFRA-11 replay gate). Filed COMBAT-54 (splash/plain divisors, blocked on SKILL-17), COMBAT-55 (trap TMDMOD damage units), COMBAT-56 (macro-omitting scaling audit), COMBAT-57 (KoCrossSlash SC ratio + double-hit).
- **2026-06-02** — **COMBAT-33** inprogress→done. Derived-stat SC re-fold on recalc: new `StatusEffectHandler.OnRecalc` callback + `IStatusChangeService.ReapplyDerivedStatMods` (iterates active SCs → each OnRecalc), called from `StatusCalcService.CalcPc` after the equip fold (reuses the COMBAT-28 `Lazy<IStatusChangeService>`). Generic coverage of the generator-default SCB_* set (`ApplyCalcFlagDelta` gains a `derivedOnly` filter via `IsRecalcReappliedField`, skipping primary/AspdRate/MaxHp so COMBAT-10's primary delta isn't doubled) + explicit OnRecalc for Angelus/Provoke/Concentration. Angelus (+Def2) and Provoke (Batk%/Def%) now survive equip/level recalcs. Combat33DerivedStatRefoldTests (5); unit suite 3864 (1 fail = pre-existing INFRA-11 replay gate). Filed COMBAT-53 (bespoke derived-stat handler sweep + MaxHp/MaxSp re-fold).
- **2026-06-02** — **COMBAT-32** inprogress→done. Passive-skill base-stat addends + Super Novice all-stat +10: new `StatusCalcService.ApplyPassiveBaseStatAddends` folds the status.cpp:4221-4241 absolute base addends (Super Novice +10 all stats gated on job-id set {23,4045,4190,4191} + joblv≥70 + die_counter==0; BS_HILTBINDING +1 STR; SA_DRAGONOLOGY +(lv+1)/2 INT; AC_OWL +lv DEX; RA_RESEARCHTRAP +lv INT; SU_POWEROFLAND +20 INT) into the `paramBase[]` span so they ride the COMBAT-10 idempotent delta-fold. Added 4 `SkillIds` constants + `PlayerEntity.DieCounter`. Combat32PassiveBaseStatTests (9); unit suite 3859 (1 fail = pre-existing INFRA-11 replay gate). Filed COMBAT-52 (die_counter death-increment + persistence).
- **2026-06-02** — **COMBAT-30** inprogress→done. Transcendent ×1.25 / Taekwon-ranker ×3 MaxHP+MaxSP: `JobAegisMapper.IsTranscendent` (job-id band 4001-4022) + `TaekwonJobId`, applied in `StatusCalcService.CalcPc` after the VIT/INT scale and before the equip flat/rate fold (status.cpp:3479) — trans gets ×1.25, a `JobId==4046 + BaseLevel>=90 + PlayerEntity.IsTaekwonRanker` (new flag) Taekwon gets ×3. Also fixed the latent `ClassMask`-never-populated bug (the `ClassId` setter now derives the mask). Combat30TranscendentMaxHpTests (5); unit suite 3850 (1 fail = pre-existing INFRA-11 replay gate). Filed COMBAT-51 (trans-3rd/4th JOBL_UPPER table + Taekwon fame-rank population).
- **2026-06-02** — **COMBAT-28** inprogress→done. ASPD SC contributions: `StatusCalcService` reads the live SC list (via `Lazy<IStatusChangeService>` to break the recalc DI cycle) and folds `status_calc_aspd(fixed)` (Quagmire-gated Quicken family + Madness/Berserk/AssnCros/potions → `(fixedSc+val)·agi/200`), `status_calc_aspd(false)` (Steel Body/Defender/Gospel/DontForgetMe → %-modifier), and `status_calc_fix_aspd` (Heat Barrel). Two-Hand Quicken/Berserk speed up; Quagmire slows. Combat28AspdScTests (5); unit suite 3843 (1 fail = pre-existing INFRA-11 replay gate). Filed COMBAT-50 (skill val + FREECAST + exotic fix_aspd SCs).
- **2026-06-02** — **COMBAT-27** inprogress→done. SC/GvG-gated no-cast-cancel: ported `unit_skillcastcancel` into `DamageService.InterruptCastOnDamage` — `NoCastCancel2` exempts unconditionally; `NoCastCancel`/SC_UNLIMITEDHUMMINGVOICE exempt only off GvG/BG maps (new `IsGvgOrBgMap`). Split `NoCastCancel2` out of COMBAT-23's collapsed flag. SC_BASILICA found fictional-as-cast-cancel (it's damage-immunity → COMBAT-49); ticket's GvG wording was inverted vs rAthena. Combat27NoCastCancelTests (4); unit suite 3838 (1 fail = pre-existing INFRA-11 replay gate). Filed COMBAT-49.
- **2026-06-02** — **COMBAT-26** inprogress→done. CastEndMap warp skills: `SkillCastEndService.CastEndMap` (was `return false`) implements AL_TELEPORT — "Random" → `IPlayerPositionHelpers.RandomWarp`, "SavePoint" → `IPcDeathService.WarpToSavepoint`, gated on the `noteleport` map flag (PCs only). Injected the warp seam as optional ctor deps. Combat26CastEndMapTests (4); unit suite 3834 (1 fail = pre-existing INFRA-11 replay gate). Filed COMBAT-48 (AL_WARP memo resolution + CZ_SELECT_WARPPOINT handler).
- **2026-06-02** — **COMBAT-25** inprogress→done. Defensive ground-unit intercept: `DamageService.TryGroundUnitBlock` (from `PerformMeleeAttack`) blocks a melee swing on a Safety Wall cell (consuming the new `SkillUnitGroup.Val2` block pool, `DelUnitGroup` when spent) and a ranged swing on a Pneuma cell; the opposite lane passes through. Combat25GroundUnitBlockTests (3); unit suite 3830 (1 fail = pre-existing INFRA-11 replay gate). Filed COMBAT-47 (Land Protector place-gate via UF_NOLP + skill-path intercept).
- **2026-06-02** — **COMBAT-24** inprogress→done. Per-skill cast tables: added flat-ms `SkillVarCast`/`SkillFixCast` maps (bonus2 bSkillVariableCast/bSkillFixedCast) + extractor, and a new `SkillCastTimingService.ApplyPerSkillCast` that folds them + COMBAT-22's per-skill % cast-rates keyed on the skill, after the global bonuses in `VfCastFix`. SA_ABRACADABRA now casts instantly (0 cast) with 0 after-cast delay. Combat24PerSkillCastTests (6); unit suite 3827 (1 fail = pre-existing INFRA-11 replay gate). Filed COMBAT-46 (abra_db random-skill selection).
- **2026-06-02** — **COMBAT-23** inprogress→done. pc_bonus single-value tail + 1-arg flag form: added the flag-form regex (`bNoCastCancel` + `bUnbreakable*`/`bIntravision`) + the single-value tail (`bHealPower`, `bHPrecovRate`/`bSPrecovRate`, `bSpeedRate`(min -val), `bCriticalRate`, `bUseSPrate`, `bAddMaxWeight`) to `EquipBonusBundle`/extractor/V8 host. Wired `HealPower` → renewal heal formula, recov-rates → `NaturalHealService`. Combat23PcBonusTailTests (7); unit suite 3821 (1 fail = pre-existing INFRA-11 replay gate). Fixed a pre-existing ElementTable static-seed race between element test classes. Filed COMBAT-45 (speed/crit/usesp/maxweight/healpower2/unbreakable consumers).
- **2026-06-02** — **COMBAT-22** inprogress→done. bonus2 per-skill maps: `bSkillAtk` (skillId→%) with a reflection-built skill-name→id resolver (name/quoted/numeric; bonus2 regex widened) feeds the weapon lane (`ComputeSkillDamage`) + magic lane (`CalcMagicAttack`) post-DEF; per-skill `bVariableCastrate`/`bFixedCastrate` stored inversed as data for COMBAT-24. Combat22SkillAtkTests (6); unit suite 3814 (1 fail = pre-existing INFRA-11 replay gate). MagicAddRace/IgnoreDefRace already landed in COMBAT-21. Filed COMBAT-44 (SkillHeal + HP/SP vanish + race2 + bonus3/4/5 + sub-skillatk).
- **2026-06-02** — **COMBAT-21** inprogress→done. Advanced cardfix: `CalcCardFix` rewritten to rAthena's per-category multiplicative `APPLY_CARDFIX` (offensive + defensive sections each accumulate a 1000-base `cardfix` and apply once → stacked categories now ×1.20×1.15=×1.38, not additive ×1.35). Added `MagicAddRace`/`CritAddRace` tables + extractor; BF_MAGIC race uses `MagicAddRace`; `TryCritical` folds `CritAddRace` (×10) before the cri-gate. Updated 3 card tests to the multiplicative numbers. Combat21CardfixTests (6); unit suite 3808 (1 fail = pre-existing INFRA-11 replay gate). Filed COMBAT-43 (ignore-def + element-debuff + race2 + distinct magic arrays + flag lists).
- **2026-06-02** — **COMBAT-20** inprogress→done. Plant 1-damage (`is_infinite_defense` + `battle_calc_attack_plant`) + GvG/BG zone scaling (`battle_calc_gvg/bg_damage`) now apply as a shared final stage (`BattleCalculator.ApplyPlantAndZone`) on the weapon auto-attack (`PerformMeleeAttack`) and magic/misc (`CalcMagic/MiscAttack`). Rewrote the dead, buggy `ZoneDamageService` (was 25/75 + double-multiplied) into a faithful skill-vs-normal rate selector via `IBattleConfigService` (gvg/bg lane 60, range 80). Combat20PlantGvgTests (14); unit suite 3802 (1 fail = pre-existing INFRA-11 replay gate). Filed COMBAT-42 (weapon-skill plant/zone post-ratio + Emperium/INF2-ignore/can-hit/PK/SC_INVINCIBLE).
- **2026-06-02** — **COMBAT-19** inprogress→done. Per-skill element resolution: the dead-stub `BattleElementService.GetMagic/GetMiscElement` now ports `battle_get_magic/misc_element` (declared skill element + ELE_WEAPON/ENDOWED/RANDOM sentinels, added to `BattleElement` 12/13/14 + `SkillDbLoader`); injected as optional `IBattleElementService` into `BattleCalculator` (legacy weapon-element fallback when null) and used in `CalcMagicAttack`/`CalcMiscAttack`; resolved element threads into `CalcCardFix` (new `attackElement` arg) for the defender `bSubEle` resist. Combat19SkillElementTests (9); unit suite 3788 (1 fail = pre-existing INFRA-11 replay gate). Filed COMBAT-41 (bespoke per-skill element overrides).
- **2026-06-02** — **COMBAT-18** inprogress→done. Dual-wield left-hand: off-hand weapon (lhw) captured by `EquipBonusAggregator` (weapon-only off-hand slot) → `BattleStats.LeftWatk*` through all 4 recalc-input builders; `BattleCalculator.ComputeHandDamage` shares the renewal pipeline across both hands; `ApplyLeftRightSplit` ports `battle_calc_attack_left_right_hands` exactly (katar TF_DOUBLE fraction + AS_RIGHT/AS_LEFT + KO_RIGHT/KO_LEFT, floors, hand gates); `Damage2` threads `PerformMeleeAttack` → `ApplyResolved` → `BroadcastAct` → `ZC_NOTIFY_ACT3.Damage2`. Combat18DualWieldTests (8); unit suite 3779 (1 fail = pre-existing INFRA-11 replay gate). Filed COMBAT-40 (per-hand mastery/element + full renewal accumulator fidelity).
- **2026-06-02** — **COMBAT-17** inprogress→done. Multi-hit div: auto-attack double-attack (TF_DOUBLE+dagger / new `EquipBonusBundle.DoubleRate` (SP_DOUBLE_RATE max-merge) / SC_KAGEMUSYA, renewal `max(7*lv,double_rate)` rate) sets `Hits=2` + doubles per-hit damage in `BattleCalculator.CalcMultiAttack`; `WeaponSkillImpl.GetMultiHitCount` (Sonic Blow → 8) feeds the skill div; `hits` threads `PerformMeleeAttack`/skill funnel → `ApplyResolved` → `BroadcastAct` → `ZC_NOTIFY_ACT3.Div`. Combat17MultiHitTests (10); unit suite 3771 green (1 fail = pre-existing INFRA-11 replay E2E gate). Filed COMBAT-37 (FearBreeze/ChainAction), COMBAT-38 (per-skill div_ arms), COMBAT-39 (multi-hit plugin sweep); spear-on-Peco found fictional.
- **2026-06-02** — **COMBAT-16** inprogress→done. Renewal weapon size-fix (Knuckle/Whip ×Large=75) + weapon-type resolution + bow arrow_atk. Fixed the always-0 WeaponType (item_db SubType is a NAME; CalcWeaponType never ran) via WeaponTypeCodes resolved in EquipBonusAggregator → player.WeaponType (also un-breaks renewal ASPD lookup); aggregator folds equipped ammo ATK for Bow/gun swings. Combat16WeaponSizeBowTests (17); suite 3761 green. Filed COMBAT-36 (ammo consumption + no-ammo gate).
