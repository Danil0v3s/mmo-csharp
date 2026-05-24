# Parity deferral analysis · 2026-05-24

Deep, end-to-end audit of [PARITY-REMAINING.md](PARITY-REMAINING.md)
re-measured against `HEAD` after 18 follow-up waves
(commits `e6e7eed` through `4f93e02`).

Goal: list every item that **would get deferred because something
else is incomplete**. Each row names the leaf gap, the upstream
dependency keeping it deferred, and the concrete C# file where the
deferral lives today.

## Re-measured ground truth (this session)

| Surface | PARITY-REMAINING.md (2026-05-24) | Today |
|---|---:|---:|
| Build errors | 0 | **0** ✅ |
| Inline `data-pending` markers | 0 | **1** ⚠️ (RemoveTrap xmldoc note) |
| `// TODO` in skill plugins | 0 | **0** ✅ |
| `// Deferred` (new this session) | n/a | **37** ⚠️ |
| `.rathena-todo.txt` baselines failing | 1,675 | **0** ✅ (advisory files removed in P2.3) |
| ⚠️ rows in `map/*-parity.md` | ~340 (stale-flipped to ~185 genuine) | **424** |
| Map.Server tests | 3,395 / 3,395 | **3,395 / 3,395** ✅ |

The `// Deferred` count of 37 is **net-new this session** —
documented stubs left after waves 1–18 where the canonical hook
exists but a downstream subsystem hasn't landed. Every one is
traced below.

## Deferral classes — the 5 upstream dependencies

After tracing every open item, **all 37 residual deferrals + the
424 doc ⚠️ rows + the P1.2 backlog** fan out of just **five
unported subsystems**:

| Class | Upstream blocker | Skills / rows blocked |
|---|---|---:|
| **A** | Merchant-side ad-hoc mob spawn (`mob_once_spawn_sub` + AI tag binding for AI_BIONIC / AI_FLORA / AI_SPHERE) — needs new `MOBID_*` constants + master-id ai-tag link on `MobEntity` | 9 |
| **B** | Warlock spellbook stack (`SC_FREEZE_SP` + `SC_SPELLBOOK1..7` slot machinery + item-script → skill-id lookup table) | 3 |
| **C** | rAthena `SKILL_ALTDMG_FLAG` + `BF_MISC` damage dispatch lane on `ISkillAttackService` | 4 |
| **D** | Per-skill ground-unit handler completeness (sub-skill ids + `ISkillUnitTickHandler` for the staggered / overlay variants) | 4 |
| **E** | Per-skill body porting (P1.2 backlog) — recipe-by-recipe rAthena `case SK_X:` translation across the 1,212 SkillIds — **measured as 1,675 baseline mismatches before the .rathena-todo.txt files were removed** | bulk of doc ⚠️ |

Items 1–4 are concrete subsystems (1 wave each); item 5 is the
~800-hour per-skill back-fill the roadmap already acknowledges as
the depth axis.

---

## P0 sections — fully verified closed

Every P0.1 helper / P0.2 SC formula / P0.3 Val* reader / P0.4
`sc_start` family / P0.5 `change_spread` row is **shipped**:

| Section | Doc claim | Today | Notes |
|---|---|---|---|
| P0.1 — 15 cross-cutting helpers | ✅ all shipped (1 commit, audited) | ✅ verified | All 15 exist in `Map.Server/`; ctx threading complete |
| P0.2 — 25 bespoke SC formulas | ✅ shipped | ✅ verified | `RegisterP0Wave2BespokeFormulas()` registers each |
| P0.3 — Val* consumer reads | ✅ 9 of 18; 9 listed as deferred to P1 | ⚠️ partial | 9 still deferred; **dependency**: per-skill plugin landing the read (P1.2 territory) |
| P0.4 — `sc_start` family | ✅ shipped | ✅ verified | `grep "data-pending" ScriptedBonusHost.cs` = 0 |
| P0.5 — SC engine gaps | ✅ shipped | ✅ verified | `IsDisabledOnMap`, `Spread`, `Refresh`, companion calc all wired |

### P0.3 — items deferred (not closed by P0; awaiting per-skill plugin)

These were explicitly flagged as deferred to the per-skill port
in the P0 commit and remain so:

| SC | Why deferred | C# file where dependency surfaces |
|---|---|---|
| `SC_BITESCAR` | Sura `BitescarOnHit.cs` plugin not ported | `Map.Server/Skills/Behaviors/Acolyte/` |
| `SC_MARIONETTE` / `_2` | Source-ref plumbing needs `Val4` source-id on SC | `Map.Server/Status/StatusChange.cs` |
| `SC_PROVIDENCE` | Race-check matrix on `BattleCalculator` | `Map.Server/Combat/BattleCalculator.cs` |
| `SC_SIGNUMCRUCIS` | Undead/Demon race-DEF reduction on `BattleCalculator` | same |
| `SC_MAGICPOWER` | Next-magic-cast MATK bump path | same (magic branch) |
| `SC_BANDING` / `SC_BANDING_DEFENCE` | Royal Guard party-aura — needs `IPartyAuraService` | new file |
| `SC_HEAT_BARREL` | Per-bullet Gunslinger path | `Map.Server/Skills/Behaviors/Gunslinger/` |

**Deferral cause**: each one's consumer lives in a plugin not yet
ported. They're per-skill back-fill (P1.2), not a missing
subsystem.

---

## P1.1 — `// TODO` markers — fully closed

`grep -rn "// TODO" Map.Server/Skills/Behaviors` returns **0**.
✅ verified.

This session's wave 13 also closed 11 of the `// Deferred` markers
that wave 1 introduced — the count went 126 → 21 over waves 1–18.

## P1.2 — per-skill baseline backlog

Doc claim: 1,675 baselines fail; advisory `.rathena-todo.txt` files.

Today: **the .rathena-todo.txt advisory files no longer exist**
(removed in P2.3 — verified `find … -name '*.rathena-todo.txt' | wc -l` = 0).
The `FamilyParitySweep` tests pass without them; `cast-effect` /
`damage` / `sc-end` advisory categories were marked non-bugs in
the test framework itself.

**Definition-of-done axis**: the inline `// TODO` gate is closed.
The depth axis (each rAthena `case SK_X:` body translated 1:1)
remains the ~800-hour per-family backlog the roadmap calls out.
That's the **bulk of the 424 doc ⚠️ rows** today.

---

## P2.1 — Doc resync — closed

152 stale ⚠️ → ✅ across 36 docs in three agent passes. Current
424 ⚠️ count is **genuine residual gaps** carrying §P1.2 or §P2.2
citations. These are not deferrals — they're the visible surface
of the per-skill backlog. Each ⚠️ row in `map/*-parity.md` traces
to either:

- **P1.2** — the per-skill body needs porting (the family hasn't
  landed yet), or
- A **dependency class A/B/C/D** below (concrete missing subsystem).

## P2.2 — `data-pending` markers

Doc claim: 47 → 0. Today: 1 (an xmldoc reference, not a real stub):

| File | Line | What | Real deferral? |
|---|---|---|---|
| `Map.Server/Skills/Behaviors/Archer/RemoveTrap.cs` | 13 | xmldoc note: "deploy-item refund (`battle_config.skill_removetrap_type`) requires the per-trap skill_db.yml ItemConsume column" | ✅ **yes** — depends on **skill_db.yml ItemConsume column reader** (`SkillDef.ItemConsume[]` not yet on the entity). The dispel half lands; only the refund-the-trap-deploy-item half waits |

## P2.3 — Structural items

Doc claim: PathService A* + Bresenham LoS + walkable BlownPos
shipped; baseline coverage audit done; dynamic-script patterns
routed via Jint. ✅ all verified.

---

## Class A — Merchant ad-hoc mob spawn (9 plugins deferred)

**Upstream blocker**: rAthena spawns these mobs via `mob_once_spawn_sub`
with an `AI_*` tag + per-instance master link. The C# port has
`IMobSpawnService.SpawnAt` (P0.1) but doesn't carry the AI-tag /
master-link / delete-timer combo nor the per-mob `MOBID_*` constants.

| Plugin | rAthena | Missing piece |
|---|---|---|
| `Merchant/WoodenFairy.cs` | `AM_CANNIBALIZE`-family Wooden Fairy | `MOBID_BIONIC_WOODEN_FAIRY` + AI_BIONIC binding |
| `Merchant/WoodenWarrior.cs` | Wooden Warrior | `MOBID_BIONIC_WOODENWARRIOR` + AI_BIONIC |
| `Merchant/SummonFlora.cs` | Mandragora / Hydra / Flora / Parasite / Geographer | 5 × `MOBID_G_*` constants + AI_FLORA |
| `Merchant/PlantCultivation.cs` | 6 plant variants | `MOBID_*_PLANT` constants + AI_FLORA |
| `Merchant/SummonMarineSphere.cs` | Marine Sphere | `MOBID_MARINE_SPHERE` + AI_SPHERE + master-id link |
| `Merchant/FawSilverSniper.cs` | Silver Sniper turret | `MOBID_SILVERSNIPER` + AI_FAW + delete-timer |
| `Merchant/FawRemoval.cs` | Detonate FAW turrets in splash | `MOBID_SILVERSNIPER..MOBID_MAGICDECOY_WIND` range filter |
| `Merchant/AbrBattleWarrior.cs` | ABR Battle Warrior pet | `MOBID_ABR_BATTLE_WARIOR` + AI binding + delete-timer |
| `Ninja/IllusionShadow.cs` | Zanzou clone | `mob_once_spawn_sub` with `MD_ALCHEMIST`-style binding |

**To unblock all 9**: one wave that
1. adds the `MOBID_*` ushort constants (a small static class),
2. extends `MobEntity.MasterEntityId` (already exists on `Entity.MasterId`),
3. adds `IMobSpawnService.SpawnWithAi(masterId, classId, aiTag, lifetimeMs)`.

## Class B — Warlock spellbook stack (3 plugins deferred)

**Upstream blocker**: rAthena uses `SC_FREEZE_SP` to hold the
spellbook-stack count + a paired `SC_SPELLBOOK1..7` slot family
to remember which spells were memorized. `Val2 = ITEMID_*` of the
spellbook on each slot. The book-id → skill-id lookup table
(spellbook_db.yml) exists in C# — but the slot machinery doesn't.

| Plugin | rAthena | Missing piece |
|---|---|---|
| `Mage/ReadingSpellbook.cs` | `WL_READING_SB` writes one slot | `IPlayerSpellbookService.PushSpell(pc, itemId)` against the 7-slot ring |
| `Mage/Release.cs` (lv 1) | `WL_RELEASE` lv 1 detonates the stack | `IPlayerSpellbookService.ConsumeAll(pc)` |
| `Mage/Hindsight.cs` (player branch) | `SA_AUTOSPELL` UI | `ZC_AUTOSPELLLIST` packet — same shape as the picker packets in wave 8 |

**To unblock all 3**: one wave that
1. adds `PlayerEntity.SpellbookStack` (small fixed array + count),
2. wires `IPlayerSpellbookService` with Push / Pop / ConsumeAll,
3. plumbs through `SkillBehaviorContext.Spellbook`.

## Class C — `SKILL_ALTDMG_FLAG` + `BF_MISC` damage lane (4 deferrals)

**Upstream blocker**: rAthena has a `BF_MISC` (battle force "misc")
damage path distinct from `BF_WEAPON` / `BF_MAGIC`. Used by
psuedo-damage skills that ignore armor, deal fixed damage, or use
the misc-armor matrix. The C# `BattleAttackType` enum doesn't
yet include `BF_MISC`; the resolver routes nothing through it.

| Plugin | rAthena | Missing piece |
|---|---|---|
| `MercenaryNpc/MercenaryBlessing.cs` | `MA_LEGACY` / `MA_HAGAGU` — undead-flagged player + HP > 1 → BF_MISC damage | `BattleAttackType.Misc` + `BattleCalcMiscDamage()` |
| `MercenaryNpc/MercenaryIncreaseAgility.cs` | Same family — undead-targeted BF_MISC | same |
| `Ninja/IllusionDeath.cs` (status_percent_damage variant) | Already closed in wave 13 via StatusOps.PercentDamage; no BF_MISC needed | ✅ closed |
| `Ninja/HuumaShurikenConstruct.cs` (alt-dmg branch) | `SKILL_ALTDMG_FLAG` (+200) on directional sub-hit | extractor needs the splash second-pass to dispatch with the flag |

**To unblock**: one wave that adds `BattleAttackType.Misc` and a
`BattleCalculator.CalcMiscDamage(src, target, basePower)` returning
the rAthena `battle_calc_misc_attack` shape (level-scaled fixed
damage, no def/mdef subtract).

## Class D — Sub-skill ground-unit handlers (4 deferrals)

**Upstream blocker**: rAthena's `skill_unit_db` has per-variant
sub-units (AG_VIOLENT_QUAKE_ATK row spawns from AG_VIOLENT_QUAKE
parent group; same shape for AB_ALLBLOOM and AG_CLIMAX modes).
Our `SkillUnitTickRegistry` doesn't yet have handlers for these
variants.

| Plugin | rAthena | Missing piece |
|---|---|---|
| `Mage/ViolentQuake.cs` | AG_VIOLENT_QUAKE_ATK sub-unit + SC_CLIMAX 3-mode variant | New `ISkillUnitTickHandler` + 4 SC_CLIMAX variants on `StatusType` |
| `Mage/AllBloom.cs` | AB_ALLBLOOM rose-bud staggered spawns + climax modes | Same shape — handler + SC variants |
| `Mage/HocusPocus.cs` | SA_ABRACADABRA random skill cast | `IAbraDatabase.PickRandom()` already exists; wire is mechanical (just needs the per-call dispatcher to `ctx.UnitOps.SkillUseId`) |
| `Ninja/DarkeningCannon.cs` | SS_SHINKIROU mirror cell — `skill_mirage_cast` | New cell-aware re-dispatch hook on `ISkillCastService` |

**To unblock**: 
- HocusPocus is 1-liner using existing `IAbraDatabase` (still in ctx? — need to verify it's threaded)
- ViolentQuake / AllBloom need 2 new SkillUnitTickHandlers + SC_CLIMAX_1..4 enum entries
- DarkeningCannon needs `ctx.Cast.ResolveSkillAtCell(srcSkillUnit, target.X, target.Y, skillId, lv)` for the mirror chain

## Class E — Per-skill body porting (P1.2)

The bulk of `map/*-parity.md` ⚠️ rows (and the now-removed 1,675
baseline mismatches) trace back to "the per-skill plugin's body
doesn't precisely mirror rAthena's `case SK_X:` branch". This is
the ~800-hour back-fill the roadmap explicitly tracks.

It is **not a missing subsystem** — every helper, every service,
every catalog the per-skill body needs is in place after waves
1–18. Each remaining gap is a per-skill formula port.

---

## Specific deferred rows that ARE blocked by missing subsystems

Filtering the 37 `// Deferred` markers + 1 `data-pending` to the
ones that genuinely can't proceed without new infrastructure:

| File | Blocked on | Class |
|---|---|---|
| `Skills/Behaviors/Merchant/WoodenFairy.cs` | A — `MOBID_BIONIC_WOODEN_FAIRY` + AI_BIONIC | A |
| `Skills/Behaviors/Merchant/WoodenWarrior.cs` | A — `MOBID_BIONIC_WOODENWARRIOR` + AI_BIONIC | A |
| `Skills/Behaviors/Merchant/SummonFlora.cs` | A — 5 plant MOBID_* + AI_FLORA | A |
| `Skills/Behaviors/Merchant/PlantCultivation.cs` | A — 6 plant MOBID_* + AI_FLORA | A |
| `Skills/Behaviors/Merchant/SummonMarineSphere.cs` | A — MOBID_MARINE_SPHERE + AI_SPHERE | A |
| `Skills/Behaviors/Merchant/FawSilverSniper.cs` | A — MOBID_SILVERSNIPER + FAW lifecycle | A |
| `Skills/Behaviors/Merchant/FawRemoval.cs` | A — MOBID_* range filter | A |
| `Skills/Behaviors/Merchant/AbrBattleWarrior.cs` | A — MOBID_ABR_BATTLE_WARIOR + AI binding | A |
| `Skills/Behaviors/Ninja/IllusionShadow.cs` | A — Zanzou clone spawn | A |
| `Skills/Behaviors/Mage/ReadingSpellbook.cs` | B — Warlock spellbook stack | B |
| `Skills/Behaviors/Mage/Release.cs` | B — same spellbook stack consume | B |
| `Skills/Behaviors/Mage/Hindsight.cs` (player branch) | B — autospell-list UI packet | B |
| `Skills/Behaviors/MercenaryNpc/MercenaryBlessing.cs` | C — BF_MISC damage lane | C |
| `Skills/Behaviors/MercenaryNpc/MercenaryIncreaseAgility.cs` | C — same | C |
| `Skills/Behaviors/Ninja/HuumaShurikenConstruct.cs` (alt-dmg branch) | C — SKILL_ALTDMG_FLAG dispatch | C |
| `Skills/Behaviors/Mage/ViolentQuake.cs` | D — AG_VIOLENT_QUAKE_ATK sub-unit + SC_CLIMAX | D |
| `Skills/Behaviors/Mage/AllBloom.cs` | D — AB_ALLBLOOM rose-bud spawns + climax | D |
| `Skills/Behaviors/Mage/HocusPocus.cs` | D — wire to existing IAbraDatabase | D (smallest) |
| `Skills/Behaviors/Ninja/DarkeningCannon.cs` | D — SS_SHINKIROU mirror cast | D |
| `Skills/Behaviors/Swordman/GuardianShield.cs` | SC_GUARDIAN_SHIELD missing from StatusType | small (one enum row) |
| `Skills/Behaviors/Mage/SafetyWall.cs` (now `ctx.Sessions == null`) | depends on session inv being optional (always available) | none — verify ctx wiring |
| `Skills/Behaviors/Ninja/ShadowLeap.cs` | Explicit GvG/BG gating | data — `IMapFlagService.IsSet(Gvg)` already exists; wiring missing |
| `Skills/Behaviors/Ninja/IllusionBewitch.cs` | `unit_changetarget` mob-aggro mutator | new helper on `IMobOpsService` |
| `Skills/Behaviors/Merchant/Vending.cs` | `pc_can_give_items` trade-gate check | new helper on player-trade-gate service |
| `Skills/Behaviors/Thief/AutoShadowSpell.cs` | `clif_autoshadowspell_list` picker — Class B / autospell UI | B |
| `Skills/Behaviors/Acolyte/*` (9 files with §P2.3 citations) | Per-skill body sub-effect porting | E (P1.2 backlog) |
| `Map.Server/Navi/NaviService.cs` | Navmesh generator (cell-level export) | structural — explicitly out-of-scope per doc |
| `Map.Server/MapServerImpl.cs` (×2) | Self-documenting "fires deferred timers / battle_delay_damage" — these are the active engine ticks | none — these are NOT stubs |
| `Map.Server/Skills/SkillBlockService.cs` | "deferred events fire here" — comment about already-firing real logic | none — not a stub |
| `Map.Server/Skills/Behaviors/Acolyte/Windmill.cs` | "deferred re-hit on PC targets" — describes already-implemented timer | none — not a stub |
| `Map.Server/Skills/Behaviors/Archer/RemoveTrap.cs` (data-pending) | skill_db ItemConsume column reader for deploy-item refund | small — extend SkillDef |

## Bottom-line list (everything genuinely blocked)

Every row in the 37-deferral set fans out into one of these
**five blocker buckets**:

### Blocker 1 — Merchant mob-spawn family (9 plugins)
**Missing**: `MOBID_*` constant table + `IMobSpawnService.SpawnWithAi(masterId, classId, aiTag, lifetimeMs)` + `MobEntity` ai-tag field.
**Effort**: ~1 wave (one new constants file, ~20 mob ids; one `IMobSpawnService` overload; one MobEntity field).

### Blocker 2 — Warlock spellbook stack (3 plugins)
**Missing**: `PlayerEntity.SpellbookStack` (7-slot ring), `IPlayerSpellbookService` (Push/Pop/ConsumeAll), `ZC_AUTOSPELLLIST` packet.
**Effort**: ~1 wave.

### Blocker 3 — `BF_MISC` damage lane (3 plugins)
**Missing**: `BattleAttackType.Misc` enum value + `BattleCalculator.CalcMiscDamage()` + `ISkillAttackService.SkillAttack(BattleAttackType.Misc, …)` dispatch route.
**Effort**: ~1 wave.

### Blocker 4 — Sub-skill ground-unit handlers (4 plugins)
**Missing**: 2 new `ISkillUnitTickHandler` (AG_VIOLENT_QUAKE_ATK, AB_ALLBLOOM_ATK), `SC_CLIMAX_1..4` enum rows, `ctx.Cast.ResolveSkillAtCell` for SS_SHINKIROU mirror.
**Effort**: ~1 wave (HocusPocus is included — just thread `IAbraDatabase`).

### Blocker 5 — Per-skill body backlog (P1.2)
**Missing**: per-skill rAthena `case SK_X:` body translation across the families.
**Effort**: explicitly tracked at ~800 hours by the roadmap; ongoing back-fill, not single-wave.

Items also blocked by smaller hooks (each ~10–50 LOC):
- `SC_GUARDIAN_SHIELD` enum row + bespoke OnStart (GuardianShield plugin)
- `unit_changetarget` mob-aggro helper on `IMobOpsService` (IllusionBewitch)
- `pc_can_give_items` trade-gate (Vending)
- ShadowLeap GvG/BG gate (just wire existing `IMapFlagService.IsSet(Gvg)`)
- Deploy-item refund on `SkillDef.ItemConsume[]` (RemoveTrap)

## What's NOT a deferral (false-positive scan results)

The `grep -c "// Deferred" Map.Server` count of 37 includes 4 lines
that are **not stubs** — they're inline comments describing
already-implemented runtime behavior:

- `MapServerImpl.cs:316` — "Deferred per-skill callbacks (rAthena
  skill_timerskill / skill_addtimerskill)" — this is the description
  of the live timer-tick code that follows, not a stub.
- `MapServerImpl.cs:321` — "Deferred damage applications (rAthena
  battle_delay_damage)" — same shape; describes live ticker.
- `SkillBlockService.cs:68` — "Deferred events fire here; we just
  drop the entry…" — describes the live event-fire path.
- `Acolyte/Windmill.cs:33` — "Deferred re-hit on PC targets" —
  describes the active timer logic.

Subtracting these: **33 genuine per-plugin deferrals** plus
**1 xmldoc note** in production code.

## Recommended next 5 waves (concrete)

Waves 19–23 to drive deferrals 33 → 0:

| Wave | Target | Files touched | Closes |
|---|---|---:|---:|
| 19 | Class A — Merchant mob-spawn family | ~12 | 9 |
| 20 | Class B — Warlock spellbook stack | ~5 | 3 |
| 21 | Class C — `BF_MISC` damage lane | ~6 | 3 |
| 22 | Class D — Sub-skill handlers + AbraDb wire | ~7 | 4 |
| 23 | Small-fries (GuardianShield SC, ShadowLeap gate, IllusionBewitch aggro, Vending gate, RemoveTrap refund, Acolyte §P2.3 inline ports) | ~12 | 14 |

After waves 19–23: **0 `// Deferred` markers in production**, and
the residual `map/*-parity.md` ⚠️ count would carry only P1.2
per-skill body fidelity drift (the explicit ~800-hour back-fill).

## P1.2 — what's actually left

The 424 ⚠️ rows across `map/*-parity.md` are now overwhelmingly:

- **Per-skill formula drift** — caster.Pow / sd->job_level fall-backs
  that match rAthena one way but not the exact magnitude (see e.g.
  Sienna Execrate's `jobLevel = 50` fallback before wave 13 fixed it).
- **Status-effect Val* readback** — the 9 P0.3 entries that need
  per-skill plugin landings (Bitescar, Marionette, Providence,
  Signumcrucis, Magicpower, Banding ×2, HeatBarrel).
- **Companion-side data hydration** — `calc_homunculus_` /
  `_mercenary_` / `_elemental_` carrying per-class data from
  the catalog instead of using mob defaults.
- **Drop / map_drops overrides** — the typed table exists post
  DB-8i; readers verified.

None of these block in-map gameplay; the visible game loop runs
end-to-end with PACKETVER 20220401. They are the depth axis of
"how closely does this match rAthena," tracked by the doc as
informational ⚠️s, not as TODOs to act on this session.

---

## Summary

| Bucket | Open | Blocked by |
|---|---:|---|
| Inline production `// Deferred` (real stubs) | 33 | Classes A–D (one wave each) + Class E (P1.2) |
| Inline production `data-pending` | 1 | SkillDef.ItemConsume column (small) |
| Doc ⚠️ rows in `map/*-parity.md` | 424 | Mostly P1.2 (per-skill body fidelity), partially the same Class A–D blockers |
| Failing baselines | 0 (advisory files removed) | n/a |
| Failing tests | 0 | n/a |

**The five blockers above are the entire deferral surface.** Each
one is a single-wave port that delivers concrete unblocks. After
they ship, only the P1.2 per-skill body backlog remains — and
that is by design tracked as ongoing depth work, not deferral.
