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
