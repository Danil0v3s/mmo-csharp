# Parity Roadmap — mmo-csharp → rAthena (vertical rebuild)

**Restructured 2026-06-03.** The previous board sliced every feature **by layer**
(a `FEATURE-` service ticket, a separate `PACKET-` ticket, separate persistence/data
follow-ups). Completing one never produced anything a player could use, and every gap
spawned another deferral card, so the backlog grew while the game didn't. This board
**replaces that with vertical slices**: one ticket per *player-observable capability*,
owning every layer it needs — data → persistence → service → IPC → client packets →
client behaviour — end to end.

> **A ticket is done when a player can do the thing against the live client and it
> survives logout.** Not when "the service method exists".

The old layer-sliced tickets (112 todo + 119 done) are preserved under
[`_archive/`](_archive/) — they hold the **line-level rAthena citations and the
history of what code already landed**. Each vertical ticket cites the relevant archived
tickets so you keep that detail. The archived `done/` work is real code in the repo; the
vertical tickets "reopen" each capability as a single self-contained spec so the
implementer owns the whole outcome (verify-and-extend the landed parts, build the rest).

## Layout (kanban)

```
README.md      ← this index + ground truth
TIMELINE.md    ← phase order + the explicit pick-order the loop follows
TEMPLATE.md    ← vertical-ticket format (enforces end-to-end scope)
todo/<epic>/   ← vertical tickets, grouped by epic
inprogress/    ← git mv here when you start one (flat)
done/          ← git mv here when the player-outcome is true end-to-end (flat)
_archive/      ← the old layer-sliced board (rAthena citations + landed-code history)
```

## Honest ground truth (unchanged — this is why vertical matters)

| Reality | Evidence |
|---|---|
| **Client packet bridge is ~10% wired** | Only **39** `CZ_*` handlers. None for pet / homun / merc / mail / auction / vending / buying-store / cashshop / instance / achievement. Party = join-ack + chat; Guild = chat only. Most "complete" services are **unreachable from the client** — which is exactly why a service-only ticket delivers nothing. |
| **Gameplay services were shells, now partial** | The archived `FEATURE-01..15` landed real *service-layer* logic (quest/achievement/mail/auction/vending/buying/cashshop transfer + pet/homun/merc/elemental entity slices + instance lifecycle + WoE scheduler). But **none are reachable or persistent end-to-end** — no client packets, persistence IPC mostly un-called. |
| **Persistence IPC is orphaned** | `IntifService` wraps PetSave/HomunSave/QuestSave/MailSend/… — few gameplay callers. Companion/quest state is still lost on logout for most features. |
| **Combat output is approximate but mostly landed** | The archived `COMBAT-01..96` ported most of the renewal damage/stat/cast/ASPD pipeline (cards reach `CalcPc`, per-skill ratios, `RE_LVL_DMOD`, cardfix, cast interrupt). A formula **tail** remains (race2, GvG can-hit, speed/cast edges) — bundled here, deferred (combat last). |
| **SC engine: structure solid, magnitudes partial** | 1,025 SC types registered; archived `SC-01..08` fixed the worst mis-mappings. A magnitude/consumer **tail** remains — bundled here. |
| **Scripting runtime ~3% of builtins** | Dialog works; ~429 of ~483 `ctx.*` are no-op stubs; **zero real game NPCs**. Truly last. |
| **Skill bodies: breadth done, depth partial** | All 1,208 plugins DI-registered; Taekwon/Npc/Ninja families are bare shells. Per-family verticals here. |

**What IS solid:** Login/Char/Inter gRPC + persistence layer (120 char-side RPCs with
real DB writes), the connect/spawn/walk loop against a live client, the SC engine
*structure*, mob AI core FSM, pathfinding, the declarative content layer
(warps/spawns/mapflags/shops), and skill *dispatch* plumbing.

## How to use this roadmap

1. Pick the next ticket in [TIMELINE.md](TIMELINE.md) pick-order (or any
   dependency-free row).
2. `git mv todo/<epic>/<TICKET>.md inprogress/`.
3. Read the ticket + the cited rAthena source + the cited `_archive/` tickets (for
   line-level refs and what already landed).
4. Build **every layer** in the Scope checklist — the ticket is written so the player
   outcome is reachable without stubbing or deferring a layer.
5. Add the cross-layer tests. `dotnet build Map.Server && dotnet test`.
6. Flip Status, append History, add a TIMELINE Progress-log line, `git mv` to `done/`.

**Follow-ups are only for genuinely NEW capabilities you discover — never for a layer
this ticket already needed.** "Service landed, packets later" is not done.

## Ticket index

### `gameplay/` — playable capabilities (Phase 2, highest leverage)
| Ticket | Capability (player outcome) | Absorbs (archive) |
|---|---|---|
| GP-PARTY | ✅ Party end-to-end (create/invite/leave/expel/leader/share + HP-bar/dot sync; reason→GP-PARTY-EXPEL-REASON, instant-HP→GP-PARTY-INSTANT-HP) | PACKET-01 |
| GP-PARTY-EXPEL-REASON | Kicked-vs-left party withdraw reason byte — split from GP-PARTY |
| GP-PARTY-INSTANT-HP | Instant party HP-bar on damage/heal (vs the ~1s sync) — split from GP-PARTY |
| GP-GUILD | Guild works end-to-end (invite/expel/position/break/notice/emblem/storage/alliance) | PACKET-02 |
| GP-MAIL | ✅ RODEX mail end-to-end (open/list/read/claim/delete + compose/attach/send; rental→GP-MAIL-RENTAL, partial-claim→GP-MAIL-PARTIAL-CLAIM) | FEATURE-05/25, PACKET-06 |
| GP-MAIL-RENTAL | Rental-item expiry on mail attachments (needs expire_time proto field) — split from GP-MAIL |
| GP-MAIL-PARTIAL-CLAIM | Separated zeny-only/item-only mail claims (needs char partial-settle) — split from GP-MAIL |
| GP-AUCTION | ✅ Auction house end-to-end (stage/register/escrow/search-by-category/bid/outbid-refund/buy-now/cancel + full-item-fidelity mail delivery to winner/seller + expiry-timer sweep) | FEATURE-06/26, PACKET-07 |
| GP-PET | ✅ Pet end-to-end (tame/hatch-with-saved-state/feed/menu/rename/emote/loot/return/relog-rehatch) — auto-skill+loyalty→scripting, see follow-ups | FEATURE-07/27/28, PACKET-03 |
| GP-PET-LOGIN-RESUMMON | Pet left out at logout re-appears on login (status.pet_id auto-hatch) — split from GP-PET |
| GP-PET-LOYALTY-BONUS | Loyal pets grant their support bonus (pet script commands, SCR-DOMAIN) — split from GP-PET |
| GP-PET-CATCH-GATES | nopetcapture mapflag + hide-check + inventory-blank capture guards — split from GP-PET |
| GP-PET-RENAME-NAMEPKT | Pet over-head name refreshes live on rename (BL_PET 0x0095) — split from GP-PET |
| GP-PET-LOOT-OVERFLOW | Pet loot that won't fit drops on the ground + 10s re-loot cooldown — split from GP-PET |
| GP-PET-AUTOSKILL | Pet casts its attack skill (pet_attackskill) — needs the petskillattack script builtin (SCR-DOMAIN) — split from GP-PET |
| GP-HOMUN | Homunculus works end-to-end (summon/feed/AI/combat/growth/hunger/skill) | FEATURE-08/29/30/31, PACKET-04 |
| GP-MERC | Mercenary works end-to-end (summon/AI/combat/lifetime) | FEATURE-09/32/33, PACKET-05 |
| GP-ELEM | Elemental works end-to-end (summon/AI/lifetime/persist) | FEATURE-10/34 |
| GP-QUEST | ✅ Quests end-to-end (accept/track/live-count/complete/UI/login-snapshot/toggle/immediate-save + any-mob filters; instance-loc→GP-QUEST-FILTER-INSTANCE, label→GP-QUEST-FILTER-DISPLAY) | FEATURE-03/20/21/22, PACKET-10 |
| GP-QUEST-FILTER-INSTANCE | Quest Location filter honours instance source map (needs GP-INSTANCE) — split from GP-QUEST |
| GP-QUEST-FILTER-DISPLAY | Descriptive label for filtered quest objectives (clif_quest_string) — split from GP-QUEST |
| GP-ACHIEVE | ✅ Achievements end-to-end (mob-kill trigger/progress/manual-reward-claim/title-equip+name-block/login-window/persistence) — non-battle trigger completion→GP-ACHIEVE-NONBATTLE-TRIGGERS, reward script→GP-ACHIEVE-REWARD-SCRIPT (both SCR-PLAYER) | FEATURE-04, PACKET-10 |
| GP-ACHIEVE-NONBATTLE-TRIGGERS | Level/job/zeny/… achievements complete from script conditions (needs SCR-PLAYER) — split from GP-ACHIEVE |
| GP-ACHIEVE-REWARD-SCRIPT | Scripted achievement rewards run on claim (needs SCR-PLAYER) — split from GP-ACHIEVE |
| GP-VEND | ✅ Vending end-to-end (open/own-list/stall-sign/browse/buy/sold-out-close) — overweight→GP-VEND-OVERWEIGHT, autotrade→GP-AUTOTRADE-RUNTIME | FEATURE-11/35, PACKET-08 |
| GP-VEND-OVERWEIGHT | Buyer over the weight limit is refused (needs weight system exposed) — split from GP-VEND |
| GP-AUTOTRADE-RUNTIME | Offline autotrade vendors/buyers stay open + sellable across logout (headless on-map presence) — shared by GP-VEND + GP-BUYSTORE |
| GP-BUYSTORE | ✅ Buying store end-to-end (open/escrow/own-list/stall-sign/click-view/sell-in-trade/close-refund) — search→INF-SEARCHSTORE, autotrade→GP-AUTOTRADE-RUNTIME | FEATURE-12/36, PACKET-08 |
| GP-CASHSHOP | ✅ Cash shop end-to-end (catalog/open/browse/buy/kafra-cash-split/sale-price + login sale banner + account-bound point persistence; slot-code→GP-CASHSHOP-SLOT-WEIGHT-CODE, sale-scheduler→GP-CASHSHOP-SALE-BANNER) | FEATURE-13/37/38/39, PACKET-08 |
| GP-CASHSHOP-SLOT-WEIGHT-CODE | Cash-shop buy reports slot-full (5) vs over-weight (4) distinctly — split from GP-CASHSHOP |
| GP-CASHSHOP-SALE-BANNER | Timed limited-time-sale scheduling + live banner/countdown/refresh (@sale + sales table) — split from GP-CASHSHOP |
| GP-INSTANCE | Instances work end-to-end (create/enter/populated maps/lifecycle/UI) | FEATURE-14, INFRA-12, PACKET-09 |
| GP-WOE | WoE works end-to-end (scheduler✅ + castles + Emperium + guardians) | FEATURE-15, COMBAT-80 |
| GP-MVPFAME | MVP rewards + fame ranking + Taekwon ranker end-to-end | FEATURE-16/18/19 |

### `combat/` — damage/stat formula tail (deferred — combat last)
| Ticket | Outcome | Absorbs |
|---|---|---|
| CB-WEAPON | Weapon/melee damage matches rAthena at all the remaining edges | COMBAT-41/54/60/97 |
| CB-DEFENSE | Cardfix / resist / race2 defensive completeness | COMBAT-98/99/100..104 |
| CB-GATES | GvG/BG/PK/can-hit/Emperium combat gates | COMBAT-80 |
| CB-SPEED | ASPD / cast / movement-speed formula tail | COMBAT-105/106/110 |
| CB-SKILLDB | skill_db columns (unit-flags/crit/weapon-state) + consumers | COMBAT-107/109/111/112/113/114 |

### `status/` — SC engine depth (deferred)
| Ticket | Outcome | Absorbs |
|---|---|---|
| SC-MAGNITUDE | SC magnitudes correct (CalcFlags mis-map + generator-default review) | SC-10/11/18/19/20 |
| SC-CONSUMERS | Starved SC consumer reads wired | SC-12/13/14/15 |
| SC-FAMILIES | Sorcerer/Star-Emperor/Soul/Bard bespoke effects | SC-16/17 |
| SC-INSPIRATION-RECALC | ✅ SC_INSPIRATION fixed: val2→Watk+Matk (was Batk), MaxHp→4*Val1% (was flat), +OnRecalc/OnRecalcPool | — |
| SC-DERIVED-RECALC | ✅ All bespoke handlers re-apply Def/Hit/Flee/Cri/Batk/Watk/Matk/Patk/Smatk/Res/… mods after a CalcPc recalc (55 handlers across 5 batches; audit clean) | — |
| SC-IMMUNE | ✅ Effect-resist cards (bResEff in GetScDef) + weapon-swap status_change_refresh + cached nostatus map-id lookup; companion calc→SC-COMPANION-CALC | SC-21/22 |
| SC-COMPANION-CALC | Homun/merc/elem recompute derived stats (MaxHp) on level-up — split from SC-IMMUNE, blocked on GP-HOMUN/MERC/ELEM |

### `skills/` — per-family body depth (deferred)
| Ticket | Outcome | Absorbs |
|---|---|---|
| SK-ENGINE | Plugins read skill_db durations/vals + apply-rate everywhere | SKILL-04/14/15/17 |
| SK-AOE | Position-staggered AoE timers (Meteor/comet trains) | SKILL-02 |
| SK-MISSING | The ~25 missing skills + 22 `_ATK` sub-skills | SKILL-06 |
| SK-TAEKWON | Taekwon family (37) | SKILL-07 |
| SK-NPC | Npc mob-skill family (45) | SKILL-08 |
| SK-NINJA | Ninja family (7) | SKILL-09 |
| SK-GUNSLINGER | Gunslinger family (coin/ammo/chain) | SKILL-10 |
| SK-HOMUN | Homunculus/Summoner/Novice families | SKILL-11 |
| SK-CLASSIC | 1st/2nd class depth polish + dash broadcast | SKILL-12/16/18 |

### `scripting/` — NPC runtime + content (truly last)
| Ticket | Outcome | Absorbs |
|---|---|---|
| SCR-DIALOG | Dialog primitives complete (input/close2/cutin/progressbar) | SCRIPT-01 |
| SCR-PLAYER | Player-state builtins (warp/heal/item/job/sc/skill) | SCRIPT-02 |
| SCR-EVENTS | Event-hook dispatch (onInit/onTouch/onTimer/onClock/onPC*) | SCRIPT-03 |
| SCR-VARS | Variable/register system + mapreg SQL | SCRIPT-07, INFRA-07 |
| SCR-CONTROL | Timer/effect/clif/NPC-control builtins | SCRIPT-08/11 |
| SCR-DOMAIN | Quest/party/guild/instance/companion script builtins | SCRIPT-04/05/06/09 |
| SCR-BULK | Bulk NPC conversion + real town NPCs (kafra/tool dealer) | SCRIPT-10 |

### `infra/` — small vertical features + leaf wiring (parallel)
| Ticket | Outcome | Absorbs |
|---|---|---|
| INF-REFINE | Refine NPC works (chance/material/break) | INFRA-01 |
| INF-CRAFT | Change-material + elemental-analysis crafting works | INFRA-02/03 |
| INF-AUTOSPELL | Sage Autospell works (attach + on-hit proc) | INFRA-04 |
| INF-SEARCHSTORE | Search-store enumeration works | INFRA-05 |
| INF-PARTYBOOK | Party booking persists | INFRA-06 |
| INF-GAMELOG | Game-log SQL tables written | INFRA-08 |
| INF-BONUSHOST | ScriptedBonusHost residual builtins | INFRA-09 |
| INF-REPLAY | Replay-harness readiness gate (test infra) | INFRA-11 |

### `mobai/` — mob AI (parallel)
| Ticket | Outcome | Absorbs |
|---|---|---|
| AI-MVP | ✅ MVP bosses behave like MVPs — mob_skill_db loader feeds the skill picker (priority/hp-announce-chat/summon/heal/teleport) + MVP drop tier; this unblocks ALL mob skills, not just MVPs. Boss active-radius+HP-bar→AI-BOSS-ACTIVE-HP | MOBAI-02 |
| AI-BOSS-ACTIVE-HP | ✅ Boss/mob active-window (mob_active_time/boss_active_time — stay hard after PC leaves view) + floating mob HP bar (ZC_HP_INFO) — split from AI-MVP |
| AI-PERF | ✅ Cell-grid range scans (verified) + player-mastered slave target-inherit (any type) + changechase direct-set (no CanChangeTarget gate); visibility sub-gate→AI-CHANGECHASE-VIS | MOBAI-05/06/07 |
| AI-CHANGECHASE-VIS | ✅ Changechase honours the target visibility (status_check_skilluse — hide/cloak + LOS) gate — split from AI-PERF |

## Selection order

The loop / a contributor walks [TIMELINE.md](TIMELINE.md) phases in order and takes the
first ticket whose `Depends on:` are all in `done/`. **Phase 2 (gameplay) first** — it's
the biggest player-facing win and where "done ≠ playable" hurt most. Combat is **last**,
scripting **truly last** (standing directive). Infra + mob AI run in parallel.
