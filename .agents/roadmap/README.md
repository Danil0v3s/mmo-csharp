# Parity Roadmap — mmo-csharp → rAthena

**Re-baselined 2026-06-01** from a full code-vs-rAthena scan (6 parallel deep audits).
This folder **replaces** the old `.agents/migrations/` roadmap/audit docs (see
[Doc cleanup](#doc-cleanup) below). Each file is **one self-contained development
ticket** — read the ticket, read the cited rAthena source, implement end-to-end,
no stubs, no deferrals.

**Layout (kanban):**

```
README.md      ← this index + ground truth
TIMELINE.md    ← phases / sequencing / dependency order
TEMPLATE.md    ← ticket format
todo/<epic>/   ← all 79 tickets start here, grouped by epic
inprogress/    ← git mv a ticket here when you start it (flat)
done/          ← git mv here when it lands (flat)
```

Tickets move `todo/<epic>/TICKET.md → inprogress/TICKET.md → done/TICKET.md`. The
ticket ID (e.g. `COMBAT-01`) identifies its epic, so `inprogress/` and `done/` stay
flat. See [TIMELINE.md](TIMELINE.md) for the recommended order to pull from `todo/`.

## Honest ground truth (what the old docs got wrong)

The previous tracking claimed "100% parity" across most subsystems. That number
measured **per-function code presence, not working features.** The re-scan found:

| Reality | Evidence |
|---|---|
| **Client packet bridge is ~10% wired** | Only **39** `CZ_*` handlers exist. None for pet / homun / merc / mail / auction / vending / buying-store / cashshop / instance / achievement. Party = join-ack + chat only; Guild = chat only. Most "complete" features are **unreachable from the client.** |
| **Gameplay subsystems are shells** | Quest, Achievement, Mail, Auction, Cashshop, Vending, Buying-store return `0`/`false`/log-only. Pet/Homun/Merc have in-memory bodies but **no spawned entity, no persistence call, no client trigger.** |
| **Persistence IPC is orphaned** | `IntifService` wraps PetSave/HomunSave/QuestSave/MailSend/… — **zero gameplay callers.** Companion/quest/achievement state is lost on logout (`LeaveMap` only calls `SaveCharacterState`). |
| **Combat output is approximate** | A `+10 STR` card does **nothing** (equip/card flat-stat bonuses never reach `CalcPc`). Skill damage is flat `base × DamageRate%` — the entire `battle_calc_attack_skill_ratio` per-skill formula is absent. `RE_LVL_DMOD` (renewal level scaling) is **unimplemented anywhere.** Variable-cast `sqrt` reduction missing. Cast does not interrupt on damage; Safety Wall / Pneuma do not block. `pc_bonus` applies ~29 of ~300 codes. |
| **SC engine has a latent hazard + real magnitude bugs** | **90** SCs have real OnStart bodies plus a redundant later `PresenceMarker` registration; ctor ordering currently favors the real body (so the 4/4 completeness tests pass *today*), but the overwrite guard only detects the shared `_NoOp` by reference — a re-order would silently kill 90 effects undetected (SC-01 = harden the guard + delete the dead re-registrations). Separately, `CalcFlags: All` is genuinely mis-mapped to "+Val1 to all 6 base stats" for ~56 SCs (element-endow / MATK / resist), and several song magnitudes are wrong (Assncros uses `5+5*Val1` instead of `val1<10?val1*2-1:20`, element-endows get a phantom all-stat buff the combat element resolver never reads). |
| **Scripting runtime is ~3% of builtins** | Engine pivoted to **ClearScript/V8** (docs still say Jint). Dialog runtime (`mes/next/menu/close`) **works**, but ~429 of ~483 `ctx.*` methods are no-op `ScriptStub`s. rAthena exposes **~599 script builtins**; ~15–20 are real. **Zero real game NPCs** — only 4 dev-test fixtures (warps/spawns/mapflags/shops were bulk-imported and do work). |
| **Skill bodies: breadth done, depth partial** | All **1,208** skill plugins are DI-registered (no orphan dispatch). But Taekwon (37), Npc (45), Ninja (7) are bare shells (default ratio 100 / 2-cell splash); ~25 skills have no plugin at all. Per-skill ratio porting is the right granularity (rAthena's switch is hardcoded, not data-driven). |

**What IS solid:** Login/Char/Inter gRPC + persistence layer (120 char-side RPCs
with real DB writes), the connect/spawn/walk loop against a live client, the SC
engine *structure* (1,025 types registered), mob AI core FSM, pathfinding, the
declarative content layer (warps/spawns/mapflags/shops), and the skill *dispatch*
plumbing + `SkillBehaviorContext` (40+ services).

## How to use this roadmap

1. Pick a ticket from `todo/` — follow [TIMELINE.md](TIMELINE.md) phase order, or
   grab any **dependency-free** row (deps are noted in each ticket header).
2. `git mv todo/<epic>/<TICKET>.md inprogress/` so the board reflects reality.
3. Read the ticket fully, then open the cited rAthena source.
4. Implement **every** item in the ticket's Scope checklist — the ticket is written
   so you never need to stub or defer. If you find a new gap, add it to the ticket.
5. Add the tests in the Test plan. Run `dotnet build Map.Server && dotnet test`.
6. Flip the ticket Status header, append a one-line History note, add a line to the
   TIMELINE Progress log, then `git mv inprogress/<TICKET>.md done/`.

Recommended sequencing (see [TIMELINE.md](TIMELINE.md) for the full phased plan):
**COMBAT-01** (cards do nothing) → **SC-01/SC-02** (engine) →
**FEATURE-01/02 + PACKET-01..10** (unlock features) → **COMBAT-02/03** (damage
formulas) → **SCRIPT-01..03** (NPC runtime) → the rest in parallel.

## Ticket index

### `combat/` — core damage & stat correctness
| Ticket | Title |
|---|---|
| COMBAT-01 | Equip / card flat-stat & param bonuses reach `CalcPc` |
| COMBAT-02 | Per-skill damage ratio formulas (`battle_calc_attack_skill_ratio`) |
| COMBAT-03 | Renewal base-level damage modifier (`RE_LVL_DMOD`) |
| COMBAT-04 | Base-damage DEX/weapon-lvl/arrow, size-fix table, multi-hit div, dual-wield |
| COMBAT-05 | Defensive (target-side) cardfix + per-skill element resolution |
| COMBAT-06 | `pc_bonus`/`bonus2`/`bonus3` switch-table breadth + flat-bundle consumers |
| COMBAT-07 | Renewal variable-cast `sqrt` formula + equip/card cast bonuses |
| COMBAT-08 | Cast interrupt on damage + `clif_skillcastcancel` + Safety Wall/Pneuma/Land Protector |
| COMBAT-09 | ASPD/amotion formula, job-bonus stats, SC stat-mod recalc ordering |
| COMBAT-10 | Base→final stat layering (equip param `bStr..bLuk` + job bonus + SC mods) |
| COMBAT-12 | Magic skill ratio + constant pipeline (plugin ratio for BF_MAGIC) |
| COMBAT-13 | Asura Strike renewal ×2 when cast with >5 spirit spheres |
| COMBAT-14 | RE_LVL_DMOD per-skill exceptions (INF2_DISABLELVDMG gate, 120/150 divisors, trap TMDMOD) |
| COMBAT-16 | Weapon size-fix table (renewal Knuckle/Whip) + bow arrow_atk |
| COMBAT-17 | Multi-hit div (battle_calc_multi_attack + ACT3 Div wire) |
| COMBAT-18 | Dual-wield left-hand damage (Damage2 + left/right split) |
| COMBAT-19 | Per-skill element resolution (magic/misc + endow overrides) |
| COMBAT-20 | Plant 1-damage + GvG/BG damage reductions |
| COMBAT-21 | Advanced cardfix (debuff, ignore-def, magic/crit-add-race, per-category RE) |
| COMBAT-22 | bonus2 per-skill + indexed tail (skillatk/skillheal/castrate/ignore-def/vanish) |
| COMBAT-23 | pc_bonus single-value tail + 1-arg flag form (speed/healpower/nocastcancel) |
| COMBAT-24 | Per-skill cast/delay tables + SA_ABRACADABRA (deps COMBAT-22) |
| COMBAT-25 | Ground-unit damage intercept (Safety Wall/Pneuma/Land Protector) — split from COMBAT-08 |
| COMBAT-26 | CastEndMap warp skills (Teleport/Warp Portal) — split from COMBAT-08 |
| COMBAT-27 | SC-based no-cast-cancel states (Basilica/Free Cast) in interrupt gate — split from COMBAT-08 |
| COMBAT-28 | ASPD SC + skill contributions (status_calc_aspd / fix_aspd / FREECAST) — split from COMBAT-09 |
| COMBAT-29 | Dual-wield + shield ASPD base terms — split from COMBAT-09 |
| COMBAT-30 | Transcendent ×1.25 / taekwon ×3 MaxHP/SP multiplier — split from COMBAT-09 |
| COMBAT-31 | Break DamageService↔ExpService↔StatusChangeService DI cycle (Map.Server boot) — split from COMBAT-10 |
| COMBAT-32 | Passive-skill absolute base-stat addends + Super Novice all-stat +10 — split from COMBAT-10 |
| COMBAT-33 | Derived-stat SC re-fold on recalc (Angelus Def2 / Provoke Batk%) — split from COMBAT-10 |
| COMBAT-35 | RE_LVL_DMOD per-arm completeness (remaining 120/150 + trap TMDMOD + macro-omitting disable) — split from COMBAT-14 |
| COMBAT-36 | Ammo consumption + no-ammo gate on ranged attacks — split from COMBAT-16 |
| COMBAT-37 | Auto-attack multi_attack: FearBreeze bow + Chain Action revolver — split from COMBAT-17 |
| COMBAT-38 | Per-skill div_ switch arms (Pierce/Backstab/Windcutter/Bowling Bash …) — split from COMBAT-17 |
| COMBAT-39 | Multi-hit plugin GetMultiHitCount sweep (Double Strafe/Triple Attack …) — split from COMBAT-17 |
| COMBAT-40 | Left-hand renewal accumulator fidelity (per-hand mastery/element) — split from COMBAT-18 |
| COMBAT-41 | Bespoke per-skill magic/misc element overrides (Psychic Wave/Adoramus/Hell Inferno …) — split from COMBAT-19 |
| COMBAT-42 | Weapon-skill plant/zone post-ratio + Emperium/INF2-ignore/PK gates — split from COMBAT-20 |
| COMBAT-43 | Cardfix remainder (ignore-def / element-debuff / race2 / distinct magic arrays) — split from COMBAT-21 |
| COMBAT-44 | bonus tail: SkillHeal / HP-SP vanish / race2 / bonus3-5 forms — split from COMBAT-22 |
| COMBAT-45 | pc_bonus consumers: speed/weight/crit/usesp + unbreakable/intravision flags — split from COMBAT-23 |
| COMBAT-46 | SA_ABRACADABRA abra_db random-skill selection — split from COMBAT-24 |
| COMBAT-47 | Land Protector place-gate (UF_NOLP) + skill-path ground-unit intercept — split from COMBAT-25 |
| COMBAT-48 | AL_WARP destination resolution + CZ_SELECT_WARPPOINT handler — split from COMBAT-26 |
| COMBAT-49 | Basilica caster protection (SC_BASILICA cell invulnerability) — split from COMBAT-27 |
| COMBAT-50 | ASPD skill-val terms + FREECAST + exotic fix_aspd SCs — split from COMBAT-28 |
| COMBAT-51 | Transcendent 3rd/4th JOBL_UPPER table + Taekwon-ranker fame population — split from COMBAT-30 |
| COMBAT-52 | die_counter persistence + death-increment wiring (Super Novice +10 gate) — split from COMBAT-32 |
| COMBAT-53 | OnRecalc for bespoke derived-stat SCs + MaxHp/MaxSp SC re-fold — split from COMBAT-33 |
| COMBAT-54 | Per-arm RE_LVL_DMOD for splash/plain 120/150 arms (needs SKILL-17 ratio funnel) — split from COMBAT-35 |
| COMBAT-55 | Ranger trap RE_LVL_TMDMOD damage via trap-unit handlers — split from COMBAT-35 |
| COMBAT-56 | Macro-omitting RE_LVL_DMOD audit (disable scaling per-arm) — split from COMBAT-35 |
| COMBAT-57 | KO_JYUMONJIKIRI SC_JYUMONJIKIRI ratio bonus + double-hit/position-shift — split from COMBAT-35 |
| COMBAT-58 | Ammo consumption on ammo-using skills + out-of-ammo client feedback — split from COMBAT-36 |
| COMBAT-59 | Wire IStatusChangeService into BattleCalculator (break cycle) — SC combat reads live — split from COMBAT-37 |
| COMBAT-60 | Per-skill div_ remainder: splash/SkillImpl arms + miscflag/ctx hook + positive-div multiply — split from COMBAT-38 |
| COMBAT-61 | Full per-hand renewal weapon-attack accumulator split (statusAtk2/patk/crit/res) — split from COMBAT-40 |
| COMBAT-62 | GvG gates: INF2 ignore-reduction + can-hit gate + PK rate + Emperium — split from COMBAT-42 |
| COMBAT-63 | Cardfix remainder: element-debuff + race2 + distinct magic arrays + SubDefEle — split from COMBAT-43 |
| COMBAT-64 | bonus3/4/5 static forms + pc_sub_skillatk_bonus (defender reduction) — split from COMBAT-44 |
| COMBAT-65 | Unbreakable/Intravision consumers + SC speed table — split from COMBAT-45 |
| COMBAT-66 | skill_db UnitFlags loader + production Land Protector unit handler — split from COMBAT-47 |
| COMBAT-67 | Warp Portal ground-unit + deferred consume/cancel-refund + pc_memo set-path — split from COMBAT-48 |
| COMBAT-68 | Renewal Basilica ground-unit + pc_cell_basilica SC_BASILICA_CELL application — split from COMBAT-49 |
| COMBAT-69 | SG_DEVIL max-job-level ASPD clause (Star Gladiator path) — split from COMBAT-50 |
| COMBAT-70 | FREECAST cast-state ASPD recompute trigger — split from COMBAT-50 |
| COMBAT-71 | Remaining status_calc_aspd debuff SCs — split from COMBAT-50 |
| COMBAT-72 | Bespoke derived-stat OnRecalc sweep (remainder + primary-coupled) — split from COMBAT-53 |
| COMBAT-73 | MaxHp/MaxSp SC re-fold (post-CalcPc pass) — split from COMBAT-53 |
| COMBAT-74 | Ranger trap detonation: splash AoE + consume + on-hit SC — split from COMBAT-55 |
| COMBAT-75 | SC_KAGEMUSYA ratio bonus across the Ninja/Kagerou arms — split from COMBAT-57 |
| COMBAT-76 | skill_db ammo columns (per-skill ammotype/qty) + clif_arrow_fail — split from COMBAT-58 |
| COMBAT-77 | Res-ignore (by race + SC_A_TELUM/SC_POTENT_VENOM) on the physical Res reduction — split from COMBAT-61 |
| COMBAT-78 | Skill-crit crit_atk_rate ÷200 variant on the skill-damage path — split from COMBAT-61 |
| COMBAT-79 | Literal per-accumulator split + DEF-at-end reorder (full battle_calc_weapon_attack fidelity) — split from COMBAT-61 |
| COMBAT-80 | can-hit GvG/BG gate (guardian/Emperium/immune) + Emperium GvG branch — split from COMBAT-62 (WoE-gated, coordinate FEATURE-15) |
| COMBAT-81 | Cardfix race2 (bAddRace2/bSubRace2) + status_get_race2 classifier + mob RaceGroups data — split from COMBAT-63 |
| COMBAT-82 | Cardfix remainder: SubDefEle + magic_subsize + flag-matched subele2/subrace3 + arrow arrays — split from COMBAT-63 |
| COMBAT-83 | Flat bonus3/4/5 remainder (drops, vanish-race/flag, SetDefRace, StateNoRecover, AddEffOnSkill) — split from COMBAT-64 |
| COMBAT-84 | SC speed-table tail (exotic SCs + freecast / hiding-walk early branches) — split from COMBAT-65 |
| COMBAT-85 | Generic skill_db UnitFlags column loader (+ SkillUnitFlag bit-order fix) — split from COMBAT-66 (no live consumer yet) |
| COMBAT-86 | AL_WARP deferred requirement-consume + cancel-refund (SKILL_NOCONSUME_REQ) — split from COMBAT-67 |
| COMBAT-87 | Renewal SC_BASILICA effects: offensive element buff + NoAttack caster state — split from COMBAT-68 |
| COMBAT-88 | Cast-lock: block attack/move while casting unless SA_FREECAST — split from COMBAT-70 |
| COMBAT-89 | Bespoke OnRecalc tail (~73 handlers) + primary-coupled sub-class — split from COMBAT-72 |
| COMBAT-90 | MaxHp/MaxSp re-fold tail (remaining ~10 handlers) — split from COMBAT-73 |
| COMBAT-91 | KO_HUUMARANKA/KO_BAKURETSU splash damage path + base-ratio terms + KAGEMUSYA — split from COMBAT-75 |
| COMBAT-92 | Real skill_db Requirements column loader (fold curated ammo/Inf2 overlays) — split from COMBAT-76 |
| COMBAT-93 | Reconcile SkillFailCause with rAthena e_useskill_fail_cause wire values — split from COMBAT-76 |
| COMBAT-94 | Immediate inventory amount-update packet on consume (ammo + items) — split from COMBAT-76 |
| COMBAT-95 | Magic-side MRes reduction curve + ignore (by race + SC_A_VITA) — split from COMBAT-77 |
| COMBAT-96 | Route swing-bypass weapon plugins through the ÷200 skill crit_atk_rate — split from COMBAT-78 |
| COMBAT-97 | PC five-accumulator damage parts (element split + ×2 status + percentAtk) — split from COMBAT-79 |
| COMBAT-98 | race2 cardfix: melee per-group multiply + pet race2 — split from COMBAT-81 |

### `status/` — status-change engine depth
| Ticket | Title |
|---|---|
| SC-01 | De-shadow the 90 `PresenceMarker` overwrites + anti-shadow guard |
| SC-02 | Fix `CalcFlags: All` → 6-base-stat mis-mapping (56 SCs) |
| SC-03 | Bard/Dancer song bespoke Val2/Val3 formulas |
| SC-04 | Consumer-side Val reads (reflect / devotion / shield family) |
| SC-05 | Sorcerer elemental-sphere `_OPTION` buffs |
| SC-06 | Sura combo / Star Emperor stance / Royal Guard banding markers |
| SC-07 | Generator-default magnitude review (159 fallthrough SCs) |
| SC-08 | `status_change_spread`, `nostatus` mapflag, companion `calc_*`, `isimmune` matrix |
| SC-10 | Triage remaining `CalcFlags: All` all-six-stat mis-mappings (~35 SCs) |
| SC-11 | Complete element-endow SCs (Aspersio/Shadow/Ghost/Enchantarms + magic) |
| SC-12 | Energycoat SP-tier reduction + Crescentelbow reflect — split from SC-04 |
| SC-13 | Magicrod magic-absorb + Poisonreact autocast-Envenom — split from SC-04 |
| SC-14 | Aurablade / Gravitation / Parrying combat reads — split from SC-04 |
| SC-15 | Soul Reaper/Linker family consumers — split from SC-04 |
| SC-16 | Sorcerer *_OPTION secondary effects: element change + bolt-autocast + Wind/Petrology mods — split from SC-05 |
| SC-17 | Inspiration debuff-clear + drain tick; Banding real party-count + Def/Atk aggregate — split from SC-06 |
| SC-18 | Convert linear-wrong-magnitude generator-default SCs (a+b·Val1) — split from SC-07 |
| SC-19 | Bespoke/not-a-stat generator-default SCs (Jointbeat bitmask, tick drains, SC chains) — split from SC-07 |
| SC-20 | Bulk-triage the remaining generator-default SCs — split from SC-07 |
| SC-21 | status_isimmune PC card-bonus tolerance matrix — split from SC-08 |
| SC-22 | Companion calc refresh + status_change_refresh wiring + robust nostatus map-id lookup — split from SC-08 |

### `packets/` — client→map packet bridge (unlocks features)
| Ticket | Title |
|---|---|
| PACKET-01 | Party client packets (invite/leave/expel/leader/share-opts/HP-bar) |
| PACKET-02 | Guild client packets (invite/expel/position/break/notice/emblem/storage/alliance) |
| PACKET-03 | Pet client packets (capture/hatch/feed/rename/return/emotion) |
| PACKET-04 | Homunculus client packets (menu/feed/skill-up/name/delete) |
| PACKET-05 | Mercenary client packets |
| PACKET-06 | Mail / RODEX client packet set |
| PACKET-07 | Auction client packets |
| PACKET-08 | Vending / Buying-store / Cashshop client packets |
| PACKET-09 | Instance client packets |
| PACKET-10 | Achievement + Quest UI packets |

### `features/` — gameplay subsystem behavior + persistence
| Ticket | Title |
|---|---|
| FEATURE-01 | Mob-death observer hub (quest + achievement + pet-catch + MVP) |
| FEATURE-02 | Companion/quest/achievement persistence on logout (wire IntifService) |
| FEATURE-03 | Quest service real implementation |
| FEATURE-04 | Achievement service real implementation |
| FEATURE-05 | Mail / RODEX service (attach/zeny/return + IPC) |
| FEATURE-06 | Auction map-side wiring |
| FEATURE-07 | Pet lifecycle (catch roll, egg create, hatch, persistence) |
| FEATURE-08 | Homunculus live entity (spawn + AI + growth + persistence) |
| FEATURE-09 | Mercenary live entity (summon + lifetime tick + persistence) |
| FEATURE-10 | Elemental persistence + lifetime despawn |
| FEATURE-11 | Vending real transfer + autotrade persistence |
| FEATURE-12 | Buying store real implementation |
| FEATURE-13 | Cashshop catalog + buy |
| FEATURE-14 | Instance lifecycle (NPC spawn, timers/auto-destroy, scoping) |
| FEATURE-15 | WoE time-of-week scheduler |
| FEATURE-16 | Fame-ranking subsystem + Taekwon-ranker population — split from COMBAT-51 |

### `scripting/` — NPC scripting runtime + content
| Ticket | Title |
|---|---|
| SCRIPT-01 | Complete dialog primitives (close2/input/prompt/clear/cutin/progressbar/sleep) |
| SCRIPT-02 | Player state-mutation builtins (warp/heal/item/job/sc/skill) |
| SCRIPT-03 | Event-hook dispatch (onInit/onTouch/onTimer/onClock/onPC*) |
| SCRIPT-04 | Quest & achievement script builtins |
| SCRIPT-05 | Party / guild / clan script builtins |
| SCRIPT-06 | Instance scripting builtins |
| SCRIPT-07 | Variable/register system (mapreg SQL, arrays, getd/setd, consolidation) |
| SCRIPT-08 | Timer / effect / clif / NPC-control builtins |
| SCRIPT-09 | Pet/homun/merc/mail/auction/channel/BG script builtins |
| SCRIPT-10 | Bulk NPC script conversion from rAthena `.txt` (transpiler + duplicate) |
| SCRIPT-11 | NPC-chat (`npc_chat`) completion |

### `skills/` — per-skill body depth + cross-cutting
| Ticket | Title |
|---|---|
| SKILL-01 | Route SC procs through apply-rate (not `Random.Shared`) |
| SKILL-02 | Position-targeted staggered AoE timers (`skill_addtimerskill(x,y)`) |
| SKILL-03 | Splash allegiance completeness (slave-mob + PvP/no-FF mapflags) |
| SKILL-04 | Read SC durations/vals from `skill_db` (`GetTime2/3`) in plugins |
| SKILL-05 | Retire/quarantine the dead `DamageRate` ratio path |
| SKILL-06 | Port the ~25 genuinely-missing skills + verify 22 `_ATK` sub-skills |
| SKILL-07 | Family: Taekwon (37 shells) |
| SKILL-08 | Family: Npc (45 mob-skill shells) |
| SKILL-09 | Family: Ninja (7 splash shells) |
| SKILL-10 | Family: Gunslinger (coin/ammo/chain) |
| SKILL-11 | Family: Homunculus / Summoner / Novice shells |
| SKILL-12 | Family polish: Mage/Archer/Thief/Swordman/Merchant/Acolyte (depth) |
| SKILL-13 | Parity-sweep snapshots must fail, not silently rewrite (test integrity) |
| SKILL-14 | Bulk-migrate the remaining ~163 plugin SC-proc rolls onto the apply-rate engine — split from SKILL-01 |
| SKILL-15 | ScDefTable depth: bespoke-formula SCs + min_rate/min_duration + resist-buff adds — split from SKILL-01 |
| SKILL-16 | Route CanDamage through BattleTargetResolver + attack-vs-mechanic-damage split + BG teams — split from SKILL-03 |
| SKILL-17 | Thread SkillBehaviorContext through the SkillAttack funnel for ctx-aware ratios — split from SKILL-05 |
| SKILL-18 | Dash/knockback slide broadcast (ZC_HIGHJUMP) on UnitOps.MovePos — split from SKILL-05 |
| SKILL-19 | Spirit-ball skill-requirement consumption (Asura delspiritball + SpiritBallCost) — split from COMBAT-13 |

### `infra/` — leaf wiring & persistence
| Ticket | Title |
|---|---|
| INFRA-01 | WeaponRefine consume `RefineService` (chance/material/break) |
| INFRA-02 | ChangeMaterial DB (`skill_changematerial_db`) + wire |
| INFRA-03 | ElementalAnalysis (Sorcerer) port |
| INFRA-04 | Sage Autospell skill (`SC_AUTOSPELL` attach + on-hit proc) |
| INFRA-05 | SearchStore `GetAllShops` enumeration |
| INFRA-06 | Party-booking persistence |
| INFRA-07 | MapReg `$globalvar` SQL persistence |
| INFRA-08 | Game-log SQL tables (pick/zeny/mvp/chat/branch/feeding/npc) |
| INFRA-09 | `ScriptedBonusHost` residual host stubs (getskilllv/eaclass/countitem/Class/Zeny/bonus5) |
| INFRA-10 | Navi list generator (deferral decision documented) |
| INFRA-11 | PacketReplayTests Login/Char internal-ping readiness gate — split from COMBAT-31 |

### `mobai/` — mob AI gaps
| Ticket | Title |
|---|---|
| MOBAI-01 | Slave/master coupling in the AI tick |
| MOBAI-02 | MVP behavior (skill priority, hp announce, drop tier) |
| MOBAI-03 | Change-target mode handling (melee/chase/weak/random) |
| MOBAI-04 | Aggro LOS / `battle_check_range` (mob aggros through walls today) |

## Doc cleanup

The following `.agents/migrations/` docs were **removed** on 2026-06-01 as
superseded/contradicted by this scan (they claimed completion that the code does
not have): the meta-roadmaps (`PARITY-REMAINING`, `PARITY-CLOSURE-ROADMAP`,
`ROADMAP`, `GAMEPLAY-ROADMAP`, `CODE-COMPLETENESS-ROADMAP`,
`PARITY-DEFERRAL-ANALYSIS`) and the dated audits (`T6-audit-*`,
`parity-audit-*`, `map/AUDIT-*`, `map/SC-SKILL-AUDIT`, `map/SKILL-AUDIT-DETAIL`,
`map/ns1-audit-*`, `map/ROADMAP`, `map/parity-audit-*`).

**Kept as reference** (rAthena function citations + historical context, but their
"✅ 100%" status columns are NOT authoritative — this roadmap is):
`.agents/migrations/{login,char,inter}/*.md`, `.agents/migrations/map/*-parity.md`,
`.agents/migrations/map/adjacent/*.md`, `.agents/migrations/map/scripting/*.md`,
and the MS1/MS2 subsystem design docs. Treat them as "where is the rAthena code +
how was this wired", not "is it done".
