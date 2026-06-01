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
