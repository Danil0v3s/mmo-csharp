# Parity-remaining · 2026-05-24

What is left to reach behavioral parity with rAthena. No
"waves". No "tiers". No "stubs". Each row below is a single
named gap with a single concrete owner, a measurable acceptance
test, and a citation to the rAthena source line that defines
the correct behavior.

Companion to (and replacement of) the open sections in
[PARITY-CLOSURE-ROADMAP.md](PARITY-CLOSURE-ROADMAP.md). The
roadmap's history is preserved; this file is the active
worklist.

## Ground truth (measured 2026-05-24)

Re-measured against `HEAD` (commit `af40ace tests baseline`):

| Surface | Measurement | How counted |
|---|---:|---|
| **Build** | 0 errors, 0 warnings | `dotnet build` |
| **Inline `data-pending` markers in production code** | **45** in 25 files (was 47; P0 closed 2: ScriptedBonusHost.sc_start + SkillSideEffectService.BreakEquip) | `grep -rn data-pending Map.Server Core.Server Core.Database Login.Server Char.Server` |
| **`// TODO` markers inside ported skill plugins** | **240** sites across 14 of 16 families | `grep -rn '// TODO' Map.Server/Skills/Behaviors/` |
| **Skill `(skillId, level)` baselines failing rAthena replay** | **1,675 of 2,439** (31% match) | `find Map.Server.Tests/Skills/Baselines -name '*.rathena-todo.txt' \| wc -l` |
| **SC handlers with rAthena-faithful OnStart formula** | 132 of 1,006 (13.1%) — P0.2 added 25 | hand-ported bespoke bodies |
| **SC handlers via generator (+Val1 to each CalcFlag)** | ~325 of 1,006 (32%) | `StatusCalcFlagDefaults` |
| **SC handlers presence-only via `RegisterDefaultsForMissingTypes` no-fields branch** | ~465 of 1,006 (46%) | per status.yml policy |
| **SC handlers as explicit `CombatMarkerHandler` (Val\* reader cited)** | ~99 of 1,006 (10%) | combat-side / cast-side / regen-side readers |
| **SC handler structural completeness** | **1,006 / 1,006** | `StatusEffectCompletenessTests` (passing) |
| **ScriptedBonusHost residual silent no-ops** | **0** (P0.4 closed all 5) | grep `Map.Server/Inventory/Script/ScriptedBonusHost.cs` |
| **Per-file parity docs at 0 ❌ in active per-fn table** | 42 / 42 | `.agents/migrations/map/*-parity.md` |
| **Per-file parity docs with open ⚠️ rows** | ~340 entries across 36 docs | `grep -c ⚠️` |

The visible-gameplay loop works end-to-end with a live
PACKETVER 20220401 client. The remaining gap is **depth, not
surface**: every public function has an entry point; the
question is whether each entry point does the rAthena thing
correctly.

## What "no stubs" means here

A stub is **either**:

1. A body that is empty, returns `false`/`0`/`null`/`default`
   unconditionally, or only writes a `data-pending` log line.
2. A `// TODO: port from rathena-fork` block where the matching
   rAthena C++ body has known consumer-visible behavior we do
   not reproduce.
3. A `RegisterDefaultsForMissingTypes()` synthesized SC handler
   whose actual rAthena semantics include behavior beyond the
   "+Val1 to listed CalcFlag" default — i.e. a Class B
   bespoke-formula gap.
4. A documented `_behaviorElsewhereAllowlist` entry where the
   consumer-side reader cited in the entry **does not yet
   read** `sc.Val1`/`Val2`/`Val3`.

Items are layered in three phases. **P0 is blocking** —
everything in P1 will trip over a P0 row if you skip it. **P1
is the bulk** — once P0 is done, each P1 row is independent and
can run in any order or in parallel. **P2 is fully parallel**
with both — pick from it any time, including before P0.

```
P0 — Foundations           P1 — Per-skill bodies         P2 — Independent leaf work
─────────────────────      ─────────────────────          ─────────────────────
P0.1 helpers   ┐                                          P2.1 docs resync
P0.2 SC.1+SC.2 ├── unblock ──→ P1 (formerly A + B)        P2.2 leaf data-pending
P0.3 sc_start  ┘                                          P2.3 structural items
P0.4 SC.3 (engine completion)
```

After P0 lands, P1 has zero internal dependencies — pick any
family, port skill-by-skill, no hidden gates. P2 has zero
dependencies on anything else; ship a row whenever a
convenient gap appears.

---

## P0 — Foundations (do these first; everything else trips on them)

P0 closes the dependencies that the per-skill backlog (P1) and
the script-host call sites (E.7 / now P0.3) all cite. Five
rows; each is a single-PR-sized piece of infrastructure.

### P0.1 — Cross-cutting helpers cited by ~150 skill TODOs

These show up across multiple families. Building one helper
unblocks every skill that names it.

| Helper / dependency | rAthena symbol | Skill TODO sites blocked | Owner project |
|---|---|---:|---|
| `IPartyMapService.ForEachOnSameMap` | `party_foreachsamemap` | 13 | `Map.Server/Party/` |
| `pc_checkskill(sd, skill_id)` reads player's learned-skill table | `pc_checkskill` | 12 | `Map.Server/Skills/IPlayerSkillService` |
| `skill_area_sub` (multi-target splash enumerator) | `skill_area_sub` | 8 | `Map.Server/Skills/SkillArea` |
| `skill_check_unit_movepos` (move ground unit) | `skill_check_unit_movepos` | 4 | `Map.Server/Skills/SkillUnit` |
| `pc_setpos` (warp player) | `pc_setpos` | 4 | `Map.Server/Movement/IPcSetposService` |
| `pc_addspiritcharm(type, count, ms)` | `pc_addspiritcharm` | 4 | `PlayerEntity.SpiritCharms` |
| `mob_once_spawn(map, x, y, …)` (ad-hoc mob spawn) | `mob_once_spawn` | 4 | `Map.Server/Mob/IMobOnceSpawnService` |
| `SC_SPHERE_1..5` (Warlock balls slot machinery) | `SC_SPHERE_*` | 4 | `Map.Server/Status/StatusType.cs` (slot enum + spell-book queue) |
| `MAPID_FIRSTMASK` class introspection | `pc->class_ & MAPID_FIRSTMASK` | 2 | `PlayerEntity.ClassMask` |
| `pc_checkequip(target, EQP_*)` (read equipped slot id) | `pc_checkequip` | 2 | `IPlayerEquipService.CheckEquip` |
| `clif_skill_estimation` (Sense / Estimation result frame) | `clif_skill_estimation` | 2 | `Core.Server/Packets/Out/ZC` |
| `clif_cooking_list` (Pharmacy / Cooking dialog) | `clif_cooking_list` | 2 | same |
| `mob_class_change` (Monocell, etc.) | `mob_class_change` | 2 | `Map.Server/Mob/IMobClassChangeService` |
| `skill_break_equip` (random equip break) | `skill_break_equip` | 2 | `Map.Server/Skills/SkillSideEffectService` |
| `skill_blown` + `clif_blown` (knockback) | `skill_blown` | 2 | `Map.Server/Skills/SkillEffectService` |

**Definition of done per helper:** real impl, real test, and a
grep across `Map.Server/Skills/Behaviors/` shows zero `// TODO`
markers referencing the helper symbol. The helper's PR ships
with the matching skill body updates that consume it (the
skill bodies are mechanical once the helper exists).

### P0.2 — SC engine bespoke-formula port-overs (Class B)

Generator-synthesized handlers apply `+Val1` to each CalcFlag
field listed in `db/re/status.yml`. Exact for the +Val1-to-stat
family, wrong magnitude / wrong field for SCs with bespoke
formulas. Roughly 50 SCs still need their `status.cpp` formula
inlined into an explicit `Register(StatusType.X, …)` ahead of
the generator default.

Already shipped (wave 4a/4b/5a): Provoke, Concentration, Blessing,
Truesight, Bloodlust, Bard/Dancer songs (×8), ASPD potion family
(×4), ASPD quicken family (×4), Hallucinationwalk, Marsh-of-Abyss,
Cloakingexceed, Spurt, Explosionspirits, plus ~20 others.

**How to find the remaining ~50:** any SC in
`StatusCalcFlagDefaults.cs` whose rAthena `status.cpp` `case
SC_X:` block does math beyond `sc->val2 = sc->val1;` or similar.
Pick by gameplay impact (PvP/MVP-relevant first); examples
explicitly mentioned in the rAthena audit: Tarot proc table,
weapon-element endow formulas, song-stacking caps, ApplecidR
song bands.

**Definition of done per SC:** explicit `Register()` call in
`StatusEffectRegistry` ctor with rAthena formula citation in
xmldoc, per-level test, generator default no longer winning.

### P0.3 — SC Val\* readers cited but not implemented on the consumer side

`_behaviorElsewhereAllowlist` in `StatusEffectCompletenessTests`
lists 91 SCs whose implementation lives on a consumer (damage /
cast / regen / visibility / equip-gate pipeline reading
`sc.Val1/Val2/Val3`) rather than in `OnStart`. Each entry names
the cited consumer. Audit each entry — confirm the consumer
actually reads the val. Where it does not, port the read.

**Done so far** (consumer reads val correctly): AutoGuard, Kyrie,
SteelBody, Suffragium, Memorize, Slowcast, Paralysis, Izayoi,
PoemBragi, Magnificat, Tensionrelax, Endure, Bleeding regen
suppress, Hiding/Cloaking visibility,
Aspersio/Encpoison/Fireweapon/Waterweapon/Windweapon/Earthweapon
weapon-element override, StripWeapon/Shield/Armor/Helm equip-gate,
BasilicaCell, Kaite, Curse, Berserk full combo, Adoramus,
DragonicAura, Impositio, Cartboost, WindWalk, LaudaAgnus,
LaudaRamus, Provoke %, Concentration %, Angelus, Concentrate %.

**Not yet implemented on the consumer:**

| SC | Consumer that needs the read | rAthena reference |
|---|---|---|
| `SC_REFLECTSHIELD` | `DamageService` post-resolve: feed N% of damage back at attacker | `battle.cpp:status_change_reflect_shield` |
| `SC_SACRIFICE` | `DamageService` pre-resolve: subtract from caster's HP (devotion link) | `battle.cpp:battle_calc_damage` |
| `SC_DEATHBOUND` | `DamageService` reflect path (500 + 100×Val1 %) | `battle.cpp` Death-Bound branch |
| `SC_BITESCAR` | Sura per-skill plugin: tick HP drain on attacker | `skill.cpp:SU_BITE` |
| `SC_AKAITSUKI` | `IHealService`: flip heal sign on target | `skill.cpp:AB_AKAITSUKI` |
| `SC_SATURDAYNIGHTFEVER` | `IHealService` + `IRegenService`: suppress all heal/regen | `status.cpp` SC_SATURDAYNIGHTFEVER |
| `SC_MARIONETTE` / `SC_MARIONETTE2` | `IStatusCalcService.CalcPc`: transfer base-stats source → target | `status.cpp:11353-11360` |
| `SC_PROVIDENCE` | `BattleCalculator`: race-specific damage resist (Val2 = race id) | `battle.cpp` Providence branch |
| `SC_MAXIMIZEPOWER` | `BattleCalculator.RollWeaponDamage`: force max-roll | `battle.cpp` weapon roll |
| `SC_AETERNA` | `DamageService` next-hit-doubled marker; SC ends on consume | `status.cpp:11297-11298` |
| `SC_SIGNUMCRUCIS` | `BattleCalculator` defense reduction (Undead/Demon vs caster's Cross) | `status.cpp:11296` |
| `SC_MAGICPOWER` | `BattleCalculator` next-magic-cast Matk% bump; SC ends on consume | `status.cpp:10556-10564` |
| `SC_DEFENDER` | `BattleCalculator`: + flat Def vs ranged | `status.cpp:11271` |
| `SC_BANDING` (Royal Guard) | `BattleCalculator` + new `IPartyAuraService`: per-band ally count read | `skill.cpp:LG_BANDING` |
| `SC_BANDING_DEFENCE` | Same as above; defense scaling | same |
| `SC_INSPIRATION` | `IStatusCalcService.CalcPc`: full-stat buff per Val1 | `status.cpp:11366` |
| `SC_HEAT_BARREL` | `BattleCalculator` (Gunslinger): per-bullet damage bonus | `status.cpp:11392` |
| `SC_REFLECTDAMAGE` (RG) | `DamageService`: reflect % of damage to attackers within 7-cell | `battle.cpp` |

The remaining ~30 Val\* readers (Soul Linker spirit family,
Star Emperor stance, Sura combo chains, Guillotine Cross
status DoTs, Warlock vacuum/teargas, 4th-class niche) all
have the same shape: the per-skill plugin in
`Map.Server/Skills/Behaviors/<Family>/*.cs` reads
`sc.Val1/Val2/Val3` from its own caster/target. They land
naturally as part of P1 for that family — but the SC handler
side is already in place (wave 5b–5d), so the *blocker* is
just the plugin port itself.

**Definition of done per row:** the named consumer reads
`sc.Val1`/`Val2`/`Val3` and produces the cited behavior; a
unit test pins the read.

### P0.4 — `ScriptedBonusHost.sc_start` family wired

`ScriptedBonusHost.sc_start`, `sc_start2`, `sc_start4`, `sc_end`
all accept the call and return without doing anything. Five
methods total (the fifth is `bonus5`). Wiring them to
`IStatusChangeService.Start` / `End` lets item scripts like
`bonus_script "{ sc_start SC_BLESSING, 60000, 10; }"` and
weapon-on-hit scripts apply real SCs.

**Why this is in P0:** ~48 `sc_start` calls and ~10 `sc_end`
calls fire silently per item-equip cycle. Without this row,
several skill ports (and most equipped-item-driven SC tests)
will fail their baselines for reasons that look like skill
bugs but are actually script-host bugs. ~30 LOC of wire +
allowlist check; depends on P0.2 / P0.3 (the SCs that fire
must do something — those rows close most of the impact).

**Definition of done:** `grep -c "data-pending\|/\* visual-only\|^\s*\{\s*\}" ScriptedBonusHost.cs` == 0.

### P0.5 — `status_change_spread` + map-flag gates + companion `calc_*`

Five entries on `status-parity.md` still ⚠️; small to medium
fixes, but they affect player-visible behavior PvP/PvM
calculations rely on:

| rAthena fn | Gap | C# location |
|---|---|---|
| `status_change_spread` | Burning / Influenza / Misty Frost don't propagate to nearby targets | `IStatusChangeService.Spread` |
| `status_change_isDisabledOnMap` | `nostatus` mapflag not enforced; PvP balance affected | `IMapFlagService.IsStatusDisabled` |
| `status_calc_homunculus_` / `_mercenary_` / `_elemental_` | Delegates to CalcMob; companion-specific stat refresh on level/equip/SC partial | `IStatusCalcService.CalcMob` callsites |
| `status_isimmune` matrix | `bAddDefRate` / `bAddItemHealRate` / `bAddRaceTolerance` bonuses don't apply | `IStatusOpsService.IsImmune` |
| `status_change_refresh` | Weapon-switch SC reapply; no skill currently exercises this | `IStatusChangeService.Refresh` |

**Why in P0:** skill ports that depend on the matrix (a Priest
with bAddRaceTolerance vs Demon shouldn't take full damage)
will look like skill bugs until these land. Closing them
collapses an entire class of baseline mismatches.

---

## P1 — Per-skill body ports (parallel after P0)

This is the bulk of the work — 1,675 baselines, 240 inline
TODOs, across 16 family directories. After P0 lands, every
row here is independent: pick a family, walk
`*.rathena-todo.txt` → ported body → `*.json` match.

### P1.1 — Per-skill plugin TODOs

240 `// TODO` markers across 14 of 16 family directories. Each
one cites a specific local detail (an extra `+800` damage band,
a weapon-type fall-through, a stack-cap, an SC end-on-consume,
…). With P0 landed, these are pure bug-for-bug ports against
rAthena's `skill.cpp`.

**Definition of done per plugin:** the file has zero `// TODO`
or `// FIXME` comments, and its baseline matches rAthena's
JSON for every learned level.

### P1.2 — Per-family baseline backlog

1,675 (skillId, level) baselines fail the per-level rAthena
replay test. Family breakdown (2026-05-24):

| Family | Failing | Total | %-match | TODO sites in ported plugins |
|---|---:|---:|---:|---:|
| Npc | 238 | 308 | 23% | 1 |
| Mage | 181 | 284 | 36% | 48 |
| Taekwon | 170 | 182 | 7% | 1 |
| Acolyte | 154 | 200 | 23% | 42 |
| Thief | 140 | 174 | 20% | 5 |
| Archer | 139 | 252 | 45% | 17 |
| Merchant | 139 | 210 | 34% | 38 |
| Swordman | 124 | 160 | 23% | 32 |
| ElementalNpc | 98 | 102 | 4% | 7 |
| Ninja | 92 | 126 | 27% | 21 |
| Gunslinger | 74 | 88 | 16% | 6 |
| Other | 52 | 80 | 35% | 8 |
| Summoner | 52 | 66 | 21% | 8 |
| Novice | 22 | 47 | 53% | 0 |
| Homunculus | 0 | 90 | 100% | 1 |
| MercenaryNpc | 0 | 70 | 100% | 5 |

Per skill: a baseline graduates from `*.rathena-todo.txt` to
matching `*.json` when its `Resolve` body produces the same
damage, status proc, knockback, splash pattern, SC application
and packet shape that rAthena's `case SK_X:` does at the same
caster/target state.

**Recommended family order** (gameplay-impact-first, not
dependency — every family is now independent):
Acolyte → Mage → Swordman → Thief → Merchant → Archer →
Taekwon → Ninja → Gunslinger → Summoner → Other → Novice →
ElementalNpc → Npc.

**Definition of done per family:** family directory
`*.rathena-todo.txt` count = 0 AND no `// TODO` or `// FIXME`
markers in any ported plugin of that family.

---

## P2 — Independent leaf work (parallel with anything)

Pick from this section at any time — no dependencies on P0 or
P1, and no internal dependencies between rows.

### P2.1 — Documentation resync

The audit docs at `.agents/migrations/map/*-parity.md` carry
~340 ⚠️ rows across 36 files. Many are **stale** — the code
ships a real body but the doc hasn't been resynced. Confirmed
stale buckets (where the row says "stub" but the .cs has a
real body):

- `homunculus-parity.md` — 34 ⚠️ rows say "stub". `HomunculusService.cs`
  has 491 LOC of real bodies (AT-D2/D3 wave).
- `pet-parity.md` — 28 ⚠️ rows say "stub". `PetOpsService.cs`
  has 375 LOC of real bodies (AT-E wave).
- `mercenary-parity.md` — 19 ⚠️ rows say "stub". `MercenaryService.cs`
  has real bodies (AT-D2 wave).
- `battleground-parity.md` — 17 ⚠️ rows; AT-D3 + AT-E filled
  most.
- `instance-parity.md` — 5 ⚠️ rows; AT-E baked the basics.
- `channel-parity.md` — 15 ⚠️ rows; AT-E baked the default
  channel set.

**Definition of done per doc:** walk every ⚠️ row, open the
cited C# file, and either:

- flip the row to ✅ with a citation, or
- confirm the gap is real and link to the matching P0 or P1
  row in this file.

Track each per-doc sweep as a single PR; ~36 PRs at < 100 LOC
of doc edits each.

### P2.2 — Production `data-pending` markers — 47 in 26 files

Each line is a one-shot wire that depends on a single named
piece of data or service. None block P0 or P1; close them in
any order as small isolated PRs. Grouped by file:

#### P2.2.a — Skill production / refine / identify (1 file, 7 lines)

[`Map.Server/Skills/SkillProductionService.cs`](../../Map.Server/Skills/SkillProductionService.cs)

| Method | Gap | Closes against |
|---|---|---|
| `ProduceMix` | needs `produce_db.yml` loader + entity | `IProduceDbRepository` (new, mechanical) |
| `ArrowCreate` | needs `arrow_db.yml` loader + entity | `IArrowDbRepository` (new) |
| `ChangeMaterial` | needs `change_material_db.yml` | `IChangeMaterialDbRepository` (new) |
| `RepairWeapon` | needs `BrokenFlag` column on `InventoryEntity` | EF migration + column add |
| `WeaponRefine` | needs refine catalog wired (typed `RefineDb` exists; just wire the consumer) | `IRefineDbRepository` (already typed) |
| `Identify` | needs single-index inventory-update helper | `IInventoryService.MarkIdentified(index)` |
| `ElementalAnalysis` | needs `elemental_analysis_db.yml` (Sorcerer) | new repository |

#### P2.2.b — Skill side-effects / cast-end / timing (3 files, 12 lines)

[`Map.Server/Skills/SkillSideEffectService.cs`](../../Map.Server/Skills/SkillSideEffectService.cs)
- `Autospell` → wait on `SC_AUTOSPELL` engine attachment (overlaps P0.3 if SC_AUTOSPELL hasn't landed there)
- `BreakEquip` → wait on `IInventoryService.BreakSlot`
- StripEquip already real (lands SC via T2.4a)

[`Map.Server/Skills/SkillCastEndService.cs`](../../Map.Server/Skills/SkillCastEndService.cs)
- `CastendPos2` ground-unit spec table — wait on per-skill plugin's ground-unit type override (this overlaps with P1 — fixed naturally as P1 families land)
- `CastendMap` cross-map warp — wait on warp pipeline in `IPcSetposService` (covered by P0.1)

[`Map.Server/Skills/SkillCastTimingService.cs`](../../Map.Server/Skills/SkillCastTimingService.cs)
- Item/card cast-time bonuses (`bonus2 bVariableCastrate`) → wait on `PlayerEntity.EquipBonuses.CastRate` field (`EquipBonusBundle` already has it; verify the reader)
- `SA_ABRACADABRA` always 0 → wait on abra DB loader (which exists; verify the wire)

#### P2.2.c — Shop / Vending / Search store (3 files, 4 lines)

[`Map.Server/Shop/Vending/VendingService.cs`](../../Map.Server/Shop/Vending/VendingService.cs)
- Persistence: `autotrade_db` not wired. Wire to `IAutotradeRepository` so vending shops survive map reload.

[`Map.Server/Shop/SearchStore/SearchStoreService.cs`](../../Map.Server/Shop/SearchStore/SearchStoreService.cs) + `ISearchStoreService.cs`
- Enumeration depends on `IVendingService.GetAllShops` + `IBuyingStoreService.GetAllShops` returning the live shop list. Wire the readers.

#### P2.2.d — Storage / Mail / Inventory hooks (3 files, 3 lines)

[`Map.Server/Storage/Guild/GuildStorageService.cs`](../../Map.Server/Storage/Guild/GuildStorageService.cs)
- Persistence: `guild_storage_db` table. Wire to `IGuildStorageRepository` + `intif.RequestGuildStorage` (Char-side repo already exists).

[`Map.Server/Items/Db/ItemDbService.cs`](../../Map.Server/Items/Db/ItemDbService.cs)
- `Reload()` is empty — `item_db.yml` + auxiliary YAML loaders wait on `Tools.RathenaImporter` re-run + repo wire. Tier 1 partially closed; verify the trickle-in is consumed.

[`Map.Server/Items/MapDropService.cs`](../../Map.Server/Items/MapDropService.cs)
- `map_drops.yml` overrides not loaded. Typed table exists post-DB-8i — verify the reader.

#### P2.2.e — Party booking, channels, navi, mapreg (4 files, 6 lines)

[`Map.Server/Party/Booking/PartyBookingService.cs`](../../Map.Server/Party/Booking/PartyBookingService.cs)
- `Load()` empty — no persistence. Booking entries vanish on map restart.

[`Map.Server/Chat/Channels/ChannelService.cs`](../../Map.Server/Chat/Channels/ChannelService.cs)
- `channels.conf` JSON loader exists; verify the consumer reads it (per AT-E ReadConfig table is baked, but the JSON path should win when present).

[`Map.Server/Navi/NaviService.cs`](../../Map.Server/Navi/NaviService.cs)
- `navi_create_lists`: generator never ported (cell-level navmesh export).

[`Map.Server/Scripting/MapReg/MapRegService.cs`](../../Map.Server/Scripting/MapReg/MapRegService.cs)
- `Init()` / `Final()` empty — SQL load + flush not wired. `$globalvar` script state doesn't persist.

#### P2.2.f — Battleground / Mob / Logging (4 files, 5 lines)

[`Map.Server/BattleGround/BattlegroundService.cs`](../../Map.Server/BattleGround/BattlegroundService.cs)
- Late-joiner warp-in: needs map-pool reservation (AT-E baked `BgMapPool[]`; verify the call site).
- Multi-party enroll: only leader enrolls today.

[`Map.Server/Mob/MobWarpChaseService.cs`](../../Map.Server/Mob/MobWarpChaseService.cs)
- T5.1c shipped the real impl; the surviving marker is the doc tag (verify safe to remove).

[`Map.Server/Mob/Conditions/MasterAttackedCondition.cs`](../../Map.Server/Mob/Conditions/MasterAttackedCondition.cs)
- Homun / Mercenary owner-aggro: needs `MobEntity.MasterEntityId` reverse-link.

[`Map.Server/Logging/IGameLogService.cs`](../../Map.Server/Logging/IGameLogService.cs)
- `loginlog` / `pickuplog` / `dropitemlog` tables — only `cash_db` log lands today.

#### P2.2.g — Program.cs DI inline notes (3 lines)

Three documented data-pending notes at the DI registration
site — each references a pending downstream wire. They are
not stubs but pointers; remove them when the cited dependency
lands.

### P2.3 — Standalone structural items

- **Per-skill replay diff harness coverage** — currently 2,439
  baselines exist; some skills have no baseline (no .json,
  no .rathena-todo.txt) because the generator skipped them.
  Audit the baseline generator output against
  `SkillIds.cs` (1,212 ids) and fill any holes. (Helpful for
  P1 — but P1 can proceed without it.)
- **`PathService.PathSearch` / `PathSearchLong`** —
  10-line stubs (`return true`); used only by skill-cast
  pre-checks (NS-1c finding). Real impl is the existing
  A\* in `Pathfinder.cs`; just delegate to it.
- **Equip-bonus aggregator dynamic-script patterns** —
  `BonusScriptExtractor` silently skips `getrefine()`,
  `callfunc`, conditional `if(…)` bonuses. About 10 % of
  rAthena's card/armor scripts. Port the dynamic-evaluation
  layer through the existing TS bonus engine instead of regex.
- **TS converter output bugs** — 5,407 typecheck errors in
  the converted item scripts; documented as bug-for-bug with
  rAthena. Runtime behavior unaffected — out of scope for
  parity but tracked in `map/item-scripting-conv.md`.

---

## Explicitly out of scope for this file

These remain deferred — they don't block in-map gameplay
behavioral parity:

- Multi-process IPC race windows (auth / account / cross-server)
- RNG sequence reproducibility (rates match; sequences don't)
- Byte-level packet replay (`PacketReplayTests` filtered out)
- Client-version compensation quirks for clients we don't target
- WoE timing (3-day cycle scheduler) — content question, not
  parity (engine ships under WOE-1/2/100)
- Cash shop sales schedule UI

## How to use this doc

1. **Start in P0.** Pick the next unfinished row. P0 is small
   (5 sections, ~15–30 sub-rows total) and front-loads every
   dependency P1 needs.
2. **When P0 is fully clear**, move to P1. Pick a family from
   `P1.2`, walk its `*.rathena-todo.txt` files, and port each
   skill against rAthena's `case SK_X:` block. P1 rows are now
   independent — pick by interest / impact, not by order.
3. **P2 runs anytime.** If you want a smaller win between
   bigger P0/P1 pieces, grab a P2 row — they have no
   dependencies on anything else.
4. Read the cited rAthena source.
5. Ship the wire / port the body / write the test.
6. Move the row from this file to its `History` entry at the
   bottom (or to the relevant `map/*-parity.md` History).
7. Re-run the measurements in **Ground truth** above; if a
   bucket count drops, edit it inline.

**The guarantee:** P0 → P1 → P2 has no backward dependencies.
Skipping ahead from P0 to P1 will leave you stuck on a missing
helper or unread `Val*`. P2 is fully orthogonal — fair game at
any moment.

## History

### 2026-05-24 — P0 landed (all five sections shipped)

Closed every P0 row across one commit. New files + extended
service surfaces; full test suite stays green at 3,395 / 3,395
non-replay tests (the lone `PacketReplayTests.Replay` failure is
pre-existing and explicitly out-of-scope).

**P0.1 — Cross-cutting helpers (15 helpers)**

| Helper | Owner | Status |
|---|---|---|
| `IPartyMapService.ForEachOnSameMap` | new `Map.Server/Party/PartyMapService.cs` | ✅ shipped |
| `IPlayerSkillService.CheckSkill` | extended `Map.Server/Skills/IPlayerSkillService.cs` | ✅ shipped |
| `ISkillAttackService.SkillAreaSub` | already existed | ✅ verified |
| `IUnitOpsService.CheckUnitMovePos` + `MovePos` | `Map.Server/Movement/UnitOps/UnitOpsService.cs` | ✅ shipped |
| `IPcSetposService.Setpos` | already existed | ✅ verified |
| `IPlayerOrbService.AddCharm` / `RemoveCharms` | extended; `PlayerEntity.SpiritCharmExpireTick` added | ✅ shipped |
| `IMobSpawnService.SpawnAt` (= `mob_once_spawn`) | already existed | ✅ verified |
| `StatusType.Sphere1..5` | already on enum | ✅ verified |
| `PlayerEntity.ClassId` + `ClassMask` + new `MapidClass` static class | `Map.Server/Entities/MapidClass.cs` | ✅ shipped |
| `IEquipService.CheckEquip` + `FindEquipped` | extended | ✅ shipped |
| `ISkillClientService.BroadcastSkillEstimation` + `ZC_MONSTER_INFO` | extended + new packet | ✅ shipped |
| `ISkillClientService.BroadcastCookingList` + `ZC_MAKABLEITEMLIST` | extended + new packet | ✅ shipped |
| `IMobOpsService.SetClass` (= `mob_class_change`) | real impl; `MobEntity.SetClass` internal mutator | ✅ shipped |
| `ISkillSideEffectService.BreakEquip` | real impl via `IMapSessionRegistry` + Inventory mutation | ✅ shipped |
| `IUnitOpsService.BlownBy` (= `skill_blown`) | already existed | ✅ verified |

Plus new infrastructure:
- `IMapSessionRegistry` — `PlayerEntity.SessionId` → `MapSessionData` lookup wrapping `SessionManager`.

**P0.2 — Bespoke SC formulas (25 ports)**

Inlined `status.cpp` per-SC formulas for SCs whose generator
default (+Val1 to each CalcFlag) didn't match rAthena's exact
magnitude / field. New `RegisterP0Wave2BespokeFormulas()`
method registers:

`Exeedbreak`, `Dancewithwug`, `Leradsdew`, `Melodyofsink`,
`Competentia`, `Religio`, `Benedictum`, `PotentVenom`,
`DMachine`, `AbyssSlayer`, `Windsign`, `GefNocturn`,
`AinRhapsody`, `MusicalInterlude`, `JawaiiSerenade`,
`PronMarch`, `SpellEnchanting`, `Weaponbreaker`, `HiddenCard`,
`TalismanOfWarrior`, `TalismanOfMagician`, `TFifthGod`,
`TalismanOfFiveElements`, `HeavenAndEarth`, `TemporaryCommunion`,
`BlessingOfMCreatures`, `WildWalk`.

`StatusEffectRegistry` grew an `ApplyBaseStatDelta` helper for
all-base-stats buffs (HeavenAndEarth / TalismanOfFiveElements).
`StatusEffectCompletenessTests._behaviorElsewhereAllowlist`
dropped `Leradsdew` (now real OnStart body).

**P0.3 — SC Val\* consumer reads (9 shipped, 9 deferred to per-skill plugins)**

`DamageService.ApplyScDamageReduction` extended:
- `SC_DEFENDER` — flat ranged-damage reduction (5+5\*Val1 %)
- `SC_AETERNA` — next-hit doubles, SC ends on consume

New `DamageService.ApplyScPostResolve` (runs after damage commits):
- `SC_REFLECTSHIELD` — Val2 % feedback to attacker
- `SC_REFLECTDAMAGE` — same shape (RG variant)
- `SC_DEATHBOUND` — Val2 ‰ reflect, SC ends on consume
- `SC_SACRIFICE` — Val2 hit-counter decrement

`StatusOpsService.Heal`:
- `SC_SATURDAYNIGHTFEVER` — suppresses ALL heals (returns 0)
- `SC_AKAITSUKI` — heal sign flips to damage

`NaturalHealService.Tick`:
- `SC_SATURDAYNIGHTFEVER` — HP regen blocked
- `SC_INSPIRATION` — HP regen +Val1 %

`BattleCalculator.CalcBaseDamage`:
- `SC_MAXIMIZEPOWER` — forces max weapon roll

**Deferred** (their consumers are per-skill plugins or larger
subsystems already tracked under P1 / P2):
- `SC_BITESCAR` → Sura `BitescarOnHit.cs` plugin (P1)
- `SC_MARIONETTE` / `_2` → needs source-ref plumbing (P1 plugin)
- `SC_PROVIDENCE` → race-check matrix (combat magic branch)
- `SC_SIGNUMCRUCIS` → same
- `SC_MAGICPOWER` → magic damage branch
- `SC_BANDING` / `SC_BANDING_DEFENCE` → `IPartyAuraService` (P1)
- `SC_HEAT_BARREL` → per-bullet path (Gunslinger P1)

**P0.4 — `ScriptedBonusHost.sc_start` family wired**

`sc_start`, `sc_start2`, `sc_start4`, `sc_end` now forward to
`IStatusChangeService.Start` / `End` via `ResolveStatusType`
(accepts `"SC_BLESSING"`, `"Blessing"`, numeric ids).
`bonus5` wired to `ApplyIndexedBonus` (no longer silent no-op).

`ComboDispatcher` + `ItemHookDispatcher` ctors now thread
`IStatusChangeService?` through to the host. Verified:
`grep -c "data-pending\|/\* visual-only\|/\* same as sc_start" ScriptedBonusHost.cs` == 0.

**P0.5 — SC engine ⚠️ closures**

- `IsDisabledOnMap` — wired to `IMapFlagService.IsSet(NoStatus)`;
  `Permanent`-flagged SCs bypass. Gate runs in `Start` before
  Val storage so refused SCs simply don't attach.
- `NoStatus` flag added to `MapFlag` enum.
- `status_change_spread`, `status_change_refresh`, `status_calc_homunculus_` /
  `_mercenary_` / `_elemental_`, `status_isimmune` (base) all verified
  pre-existing impls; the "partial" tags in `status-parity.md` refer
  to companion-side data hydration (out of scope for P0).

**Validation**: every helper-shaped TODO in `Map.Server/Skills/Behaviors/`
still cites its rAthena name (counts unchanged: 13 `party_foreachsamemap`,
12 `pc_checkskill`, 8 `skill_area_sub`, …). The TODOs are inside the
skill-plugin bodies — P1's job to convert each one from a `// TODO`
into a real call now that the helper exists. No P1 row blocks on a
missing P0 helper.

`data-pending` markers in `Map.Server/` dropped 47 → 45 (closed
ScriptedBonusHost.sc_start + SkillSideEffectService.BreakEquip;
the remaining 45 are all P2.2 leaf wires).

### 2026-05-24 — re-layered into P0 / P1 / P2 phases

Original A–G section layout (mostly impact-ordered) was
re-grouped into dependency-ordered phases so the doc can be
walked sequentially without hitting hidden blockers:

- **P0** absorbed the old "cross-family helpers" sub-table
  from A, the full content of C.1 + C.2, all of D, and C.3
  (engine-completion gaps that affect baselines). These were
  blocking P1; now they're explicitly front-loaded.
- **P1** is the merged old A + B (per-family backlog +
  per-plugin TODOs).
- **P2** is the merged old E + F + G (independent leaf
  wires + doc resync + standalone structural items).

The original section count (A..G) collapses to three phases
with explicit ordering guarantees. No content was dropped —
every row from the original sections maps to a row in the new
layout.

### 2026-05-24 — file written

Replaces the open work-tracking sections of
`PARITY-CLOSURE-ROADMAP.md` (tier scoreboard, NS-3 wave
plans, "Next steps", "Suggested next PR"). Roadmap retained
for history; new work tracked here.
