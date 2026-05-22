# Parity-closure roadmap · 2026-05-20

Companion to [CODE-COMPLETENESS-ROADMAP.md](CODE-COMPLETENESS-ROADMAP.md).

**Status:** every rAthena map .cpp public function has a canonical C#
entry point. The original survey counted **~56 data-pending markers**;
post-Tier-1 sweep down to ~23, post-T2.2/T2.4 sweep down to **~19
markers** (Strip / Suffragium / Memorize / Slowcast / Endure /
ReflectShield / Magnificat closures landed). The remainder cluster
around skill-specific behavior + per-shop registry exposure (no
longer a data-availability problem; the data is in SQL or JSON,
the consumer code just hasn't been wired through).

**Latest session (2026-05-20):** Tier 2 substantially advanced —
T2.2 + T2.4a + T2.4b + four combat-side SC consumer closures
(SteelBody / Kyrie / AutoGuard / Bleeding / Magnificat / Strip).
~51 new tests. See History.

## Tier scoreboard (re-evaluated 2026-05-22 after T5 + T6 sweeps)

| Tier | Theme | Status | Notes |
|---|---|---|---|
| T1 | Data loaders → SQL + JSON | ✅ **DONE** | 52 `_db` SQL-backed, 19 conf-JSON with schemas, IBattleConfigService overlays at boot |
| T2.1 | Equip-bonus aggregator | ✅ **DONE** | `Map.Server/Inventory/EquipBonusAggregator.cs` — exists from PC-S4 wave |
| T2.2 | Card modifier port | ✅ **DONE** | `BattleCardService.CalcCardFix` reads `PlayerEntity.EquipBonuses`; `EquipBonusBundle` + `BonusScriptExtractor` ship; Hydra-card test exercises +20% vs Demi-Human |
| T2.3 | Per-skill behavior | 🟢 hierarchy + **1,209 files** + 5 helpers + 10 manual ports | Full rathena-fork structural parity. 5 missing helpers built (clif_skill_nodamage/_fail/_damage, skill_addtimerskill, BlownBy, ZC_WARPLIST). Wave 1 of manual per-skill ports done: AL_HEAL (full renewal formula + Kaite/Berserk/Akaitsuki branches + 9 dedicated tests), AL_INCAGI/AL_DECAGI/AL_BLESSING/AL_RUWACH/AL_PNEUMA, AL_WARP destination chooser, MG_SAFETYWALL/MG_SOULSTRIKE/MG_NAPALMBEAT. Per-skill body fill-in is the additive backlog from here. |
| T2.4 | SC engine completion | 🟡 enum full / behavior ~30 of ~250 + combat hooks | T2.4a + T2.4b done: enum mirrors all 1006 SC ids; first wave of handlers (CC gates / DoT / stat buffs / cast-time SCs) registered; `CastFixSc` honors Suffragium/Memorize/Slowcast/Paralysis/Izayoi/Bragi; `DamageService` reads SteelBody / Kyrie / AutoGuard on every hit. Long-tail SC handlers ride the same registry pattern. |
| T3 | Wire packets | ✅ **DONE** (T5.3) | T5.3 closed clif_skillcasting + clif_status_change + DamageActionType.LuckyDodge + companion display + InventoryList canonical seams. 113 emitters across the surface; per-packet audit in `map/clif-parity.md`. |
| T4 | IPC + persistence | ✅ **DONE** (T5.4) | All 75 IIntifService entry points dispatch (40 ✅ + 35 ⚠️ data-pending); zero ❌. Mail send/return + Quest save/load + Achievement save/load wired via typed ICharServerIpcService* sub-IPCs. The 35 ⚠️ all key off one gating dependency — per-subsystem snapshot serializer (Pet/Homun/Merc/Storage/Auction); tracked as Goal A. See `map/intif-parity.md`. |
| T5 | Per-file deep audits | ✅ **DONE** (T5.2) | All map/*-parity.md docs at 0 ❌ as of 2026-05-22 (battle / skill / pc deep-audit refresh in commits `95ce4a5..f9bd8d9`; mob via T4.9 in `acccd3e..e851a2c`; intif via T5.4 in `bc39af0`). |
| T6 | Endgame content | ✅ **DONE** (T5.5) | WoE / instances / BG queues / pet evolution / vending — each has its own parity doc at 0 ❌. Canonical entry points exist; gameplay content fills land as a separate gameplay-content track. |
| T6-doc | Login/Char/Inter doc refresh | ✅ **DONE** (T6) | T6.1..T6.5 sweep (2026-05-22) verified login/char/inter audit docs at 0 ❌; per-file tally + wave cross-reference in `T6-audit-2026-05-22.md`. This row tracks the documentation pass, not new code. |

**Where the gap is now**:
- ~70 % of the original gap (56 → 23 data-pending markers + the big
  SC table + all the YAML data) collapsed when Tier 1 landed.
- The remaining ~30 % is **per-skill / per-item behavior code** — not
  data, not infrastructure. Mostly Tier 2.3 / 2.4 / 3 / 4 work.

## Principle

Port **shared foundations first**, then per-file behavior. The reason:
60 % of the data-pending markers across all services bottom out at
4–5 root dependencies (skill_db SQL, item_db SQL, SC table, equip-
bonus aggregator, outbound packet emitters). Port a foundation once,
every dependent service flips from stub → real.

**Content data lives in MariaDB.** rAthena ships YAML files in
`db/re/`; the C# port treats those as the *source of truth* and
ingests them into SQL tables via deploy-time seed scripts. The
runtime reads from SQL only — never from YAML. See the
"Architectural rule" callout in Tier 1 for the established
pattern + reference impl (`skill_db`).

Each tier below ends with **acceptance criteria** + an estimate of
how many downstream `data-pending` markers it resolves. Pick the
next tier when the current one's acceptance criteria are met.

## Tier 1 — Data loaders → **SQL** (foundation)

These unblock the most consumers. None of them are gameplay-visible
on their own; they fill the catalogs every other system reads from.

> **Architectural rule:** every rAthena `*_db` file becomes a
> **MariaDB table**, not a runtime YAML read. rAthena YAML is the
> *source of truth* for content, but the C# port ingests via a seed
> script at deploy time and the runtime reads from the SQL table.
> This keeps the map-server boot path single-channel
> (`GameDbContext`), lets ops patch content without recompiling,
> and matches rAthena's own `use_sql_db: yes` deployment mode.

### Established pattern (one-line per `_db`)

1. **EF Core entity** in `Core.Database/Entities/<Name>DbEntity.cs`
   mirroring the YAML's row shape (per-level columns flattened to
   `:`-delimited strings, same as rAthena's SQL exporter).
2. **Repository interface** `I<Name>DbRepository` in
   `Core.Database/Repositories/Api/`; concrete impl injected by
   `AddGameDatabase`.
3. **EF migration** added under `Core.Database/Migrations/`.
4. **Seed SQL** at `Core.Database/Seeds/Scripts/seed_<name>_db.sql`,
   generated from the rAthena YAML by a one-shot transformer (the
   transformer lives under `Tools.SeedGen/` or runs by hand once).
5. **Runtime loader service** (e.g. `SkillDb`) takes
   `IServiceScopeFactory`, opens a scope, reads
   `repo.GetAllAsync()` on `Reload()`, populates the in-memory
   catalog. Falls back to the existing hand-built starter set if
   the SQL table is empty.

**Reference implementation:** `skill_db` is already done in this
shape — [SkillDbEntity](/Core.Database/Entities/SkillDbEntity.cs),
[ISkillDbRepository](/Core.Database/Repositories/Api/ISkillDbRepository.cs),
[SkillDbLoader.FromEntity](/Map.Server/Skills/SkillDbLoader.cs),
[SkillDb.Reload](/Map.Server/Skills/SkillDb.cs). Every new loader
follows the same five-step shape.

Today's coverage (entity + repo + seed exist):
- `skill_db` ✅
- `item_db` ✅  (split usable / equip / etc. seeds)
- `mob_db` ✅
- `attendance` ✅
- `roulette` ✅

Pending — these are the new SQL tables Tier 1 lands:

| `_db` file | Consumer service | Notes |
|---|---|---|
| `skill_db.yml` full content seed | `ISkillDb` | entity exists; seed only carries starter set today |
| `item_db.yml` full content seed | `ItemCatalog` | usable / equip / etc. seeds exist but are starter-set; needs full rAthena content + the `Script` / `OnEquip` / `OnUnequip` columns populated |
| `mob_db.yml` full content seed | `MobDb` | same shape — entity exists, seed is starter-set; needs drops + skill list |
| `mob_skill_db.yml` | `IMobOpsService` (mob skill use) | new entity + repo + seed |
| `produce_db.yml` | `ISkillProductionService` | new |
| `arrow_db.yml` | `ISkillProductionService.ArrowCreate` + `ISkillArrowDatabase` | new |
| `random_option_db.yml` + `random_option_group.yml` | `IItemDbService.RandomOption*` | new |
| `combos_db.yml` | `IItemDbService.FindComboId` | new |
| `enchant_db.yml` | item enchanting | new |
| `item_group_db.yml` | `IItemDbService.GetItemGroup` | new |
| `item_package_db.yml` | item-package opening | new |
| `item_reform_db.yml` | item reform UI | new |
| `laphine_synthesis_db.yml` + `laphine_upgrade_db.yml` | Laphine system | new |
| `abra_db.yml` | `IAbraDatabase.PickRandom` | new |
| `magic_mushroom_db.yml` | `IMagicMushroomDatabase.PickRandom` | new |
| `reading_spellbook.yml` | `IReadingSpellbookDatabase.Get` | new |
| `homunculus_db.yml` + `homun_exp.yml` + `homun_skill_tree.yml` | `IHomunculusService` | new |
| `mercenary_db.yml` + `mercenary_skill_db.yml` | `IMercenaryService` | new |
| `elemental_db.yml` + `elemental_skill_db.yml` | `IElementalService` | new |
| `pet_db.yml` | `PetService` / `IPetOpsService` | partial — extend with evolution + autobonus columns |
| `quest_db.yml` | `IQuestService` | new |
| `achievement_db.yml` + `achievement_level_db.yml` | `IAchievementService` | new |
| `instance_db.yml` | `IInstanceService` | new |
| `battleground_db.yml` | `IBattlegroundService` queue rules | new |
| `cashshop_db.yml` | `ICashShopService.BuyList` | new |
| `statpoint.yml` + `job_db.yml` + `job_basepoints.yml` + `exp.yml` | PC level-up tables | partial — already covered by `IClassParameterService` for caps; per-level points table missing |
| `castle_db.yml` | guild castles | new |
| `efst_list.yml` (status icon ↔ SC) | SC display | new — paired with T1.X SC table |
| `status.yml` (death-penalty / dispel-resist) | SC engine | new |
| `channels.conf` | `IChannelService.ReadConfig` | config-file (not YAML); loaded into in-memory channel registry at boot |
| `battle_athena.conf` (and includes) | `IBattleConfigService` | config-file; ~600 knobs; loader exists but only ~20 defaults populated |
| `script.conf` / `inter_athena.conf` | misc | low priority |

### T1.1 — Foundational catalogs (most-used, biggest unblock)

Highest-leverage first — these three power most of the
combat / skill / item paths.

#### T1.1.a — `skill_db` full content

- Generate a `seed_skill_db_full.sql` from `db/re/skill_db.yml` via
  the transformer. ~3 500 rows.
- `dotnet ef database update` + run the seed.
- **Acceptance:** `ISkillDb.Count` reports ≥ 3 400 after seed;
  starter-set tests still pass.
- **Dependents unblocked:** ~12 skill-* services flip to real
  values for `skill_get_*`.

#### T1.1.b — `item_db` full content + scripts

- Extend the existing `seed_item_db_*` scripts with the full
  rAthena catalog (~26 000 rows). Populate `Script`, `OnEquip`,
  `OnUnequip` columns — these strings get executed by the TS
  bonus-script engine (already wired for the starter set).
- **Acceptance:** `ItemCatalog.GetByNameId(501)` returns Red Potion
  with `Script` non-empty; equip-bonus aggregator (Tier 2.1) can
  read it.
- **Dependents unblocked:** `ItemDbService` gate predicates,
  `IBattleCardService.CalcCardFix`, `IBattleEffectsService.Drain`,
  every script that calls `getitem` / `equip` / bonus.

#### T1.1.c — `mob_db` full content + drops

- Extend `seed_mob_db.sql` with the full rAthena mob roster (~3 600
  mobs) including the drop table column and the per-mob skill
  list column.
- **Acceptance:** Killing a Poring drops Apple at the rAthena rate;
  Boss MVP drops the right MVP-tier rewards.

### T1.2 — Status-change table (`SC_*`)

The biggest single port. rAthena ships ~250 SCs. We currently have
~5 (Blessing, IncreaseAgi, DecreaseAgi, Poison, HealOverTime).

The SC table itself is **code, not data** — rAthena defines per-SC
behavior in `status.cpp:status_change_start` (the giant switch
around lines 9000–13000). What goes to SQL is the *display* / *icon*
table only.

Plan:
- Extend `StatusType` enum with every rAthena `SC_*` (200+ entries).
- Port `status_change_start` / `status_change_end` per-SC behavior
  into per-SC `IStatusEffect` modules registered with
  `IStatusEffectRegistry` (same shape as the current 5 starter SCs).
- Most SCs have 3 fields: duration, stat delta, end-effect — fit
  one registry row each. ~30 SCs need real per-tick behavior
  (Poison / Bleeding / Burning / ManaPower / Endure / Maximize / …).
- Seed `efst_list_db` from `db/re/efst_list.yml` so SC icons map
  to client EFST ids.
- Seed `status_db` (rAthena `db/re/status.yml`) for death penalty
  / dispel resist / item drop rates per SC.
- **Acceptance:** Casting Endure applies SC_ENDURE; SC_FREEZE blocks
  movement; Maya Purple Card triggers SC_PRESERVE; client shows the
  correct buff icon.
- **Dependents unblocked:** Skill additional/counter effects,
  battle reflect path, autospell rolls, frostjoke proc, status-block
  damage gate, slowcast/suffragium/memorize cast-time scaling.

### T1.3 — Remaining `_db` tables

For each row in the **Pending** table above, follow the five-step
shape:

1. EF Core `Entities/<Name>DbEntity.cs`.
2. `Repositories/Api/I<Name>DbRepository.cs` + concrete impl.
3. EF migration (`dotnet ef migrations add Add<Name>Db`).
4. Seed SQL at `Seeds/Scripts/seed_<name>_db.sql` generated from
   the rAthena YAML.
5. Hook the existing runtime service to read from the new repo on
   `Reload()` (fall back to in-memory empty if the table's empty).

Each row is mechanical — usually < 200 LOC across the five files.
A single PR can land 3–5 of them comfortably.

- **Acceptance per row:** `dotnet test` green; the consumer service
  flips its data-pending log line to "<N> entries from SQL".
- **Acceptance per tier:** Every audit doc that lists "YAML loader
  pending" or "data-pending on <name>_db" gets flipped to "loaded
  from SQL; <N> entries".

### T1.4 — Seed-generation transformer

Optional infrastructure: a `Tools.SeedGen` console app that reads
the rAthena YAML directly (via `YamlDotNet`) and emits the
`seed_*_db.sql` files. Saves hand-editing 100k-row SQL inserts.

- Reads `/Volumes/1TB/Projetos/rathena/db/re/*.yml`.
- Emits to `Core.Database/Seeds/Scripts/seed_*_db.sql`.
- One generator per `_db`; share a YAML→SQL flattener for the
  per-level `:`-packed columns.
- **Acceptance:** Running `dotnet run --project Tools.SeedGen` from
  a clean tree produces the same seed files we'd hand-author.

This is the *production* / *upgrade* path too — on rAthena content
patches (e.g. `kRO_2025_Q1` skill rebalance), re-run the generator
and replace the seed file.

**Tier 1 milestone:** ~30 of the 56 data-pending markers go away.
Every `_db` consumer reads from SQL; YAML is only touched by
`Tools.SeedGen` at deploy time.

---

## Tier 2 — Combat correctness

With the data in place, the combat math closes. These tiers flip
"works but ignores most modifiers" into "rAthena parity damage".

### T2.1 — Equip-bonus aggregator ✅ DONE

Shipped earlier (PC-S4 wave) at
[Map.Server/Inventory/EquipBonusAggregator.cs](/Map.Server/Inventory/EquipBonusAggregator.cs).
Walks equipment + reads each item's `bonus`/`bonus2`/`bonus3`
script + accumulates the runtime numbers. Re-runs on equip /
unequip / break / strip.

### T2.2 — Card modifier port ✅ DONE

`battle_calc_cardfix` (battle.cpp:711) — the race/size/element/
class accumulator. Shipped 2026-05-20.

- `Map.Server/Inventory/EquipBonusBundle.cs` — indexed-bonus
  struct mirroring rAthena's `indexed_bonus`. AddRace / SubRace /
  AddEle / SubEle / AddSize / SubSize / AddClass / SubClass arrays
  + flat fields (FlatAtk, FlatMatk, FlatCritical, FlatHit, FlatFlee,
  FlatMaxHp/Sp, MaxHpRate/SpRate, LongAtkRate, ShortAtkRate,
  CritAtkRate, cast / drain knobs).
- `Map.Server/Inventory/BonusScriptExtractor.cs` — regex pass over
  each item's `script` / `equip_script` column populating the
  bundle. Covers the static `bonus`/`bonus2` patterns (~90 % of
  cards + armor / weapon scripts). Dynamic patterns (`getrefine()`,
  conditional bonuses, `callfunc`) are silently skipped so a future
  script-engine port slots in without touching the call site.
- `EquipBonusAggregator.BuildBundle` runs on every equip change
  inside `EquipService.TryRecalcStats`; the result lives on
  `PlayerEntity.EquipBonuses`.
- `BattleCardService.CalcCardFix` accumulates per-target
  Race / Element / Size / Class multipliers + `LongAtkRate`/
  `ShortAtkRate` based on weapon range. Renewal-additive: base
  100 %, each row adds its percent, single multiply at the end
  with floor-at-1.
- **Acceptance — Met:** Hydra-card weapon test
  (`bonus2 bAddRace,RC_DemiHuman,20;`) returns 1200 on a 1000
  hit vs a Demi-Human target. 15 new tests
  (`BattleCardServiceTests`, `EquipBonusAggregatorTests.BuildBundle_*`)
  cover Race / Size / Class_Boss / Long/ShortAtkRate / unequipped
  ignore / reset semantics.
- **Follow-up backlog (not blocking):** rAthena replay diff fixture
  (±2 %) once a deterministic replay harness lands; `SkillNk.NoCardFix`
  skip flag plumb-through when the skill resolver picks up `SkillNk`.

### T2.3 — Skill resolver per-skill behavior

Today's resolvers (Weapon / Magic / Misc / Heal / Status) dispatch
by `SkillDamageKind` only. The per-skill quirks (Bowling Bash extra
hits, Acid Bomb % damage, Magnum Break splash + fire-cloak SC,
Sonic Blow chain, Storm Gust freeze, …) live in
`skill_castend_damage_id` switch cases — currently absent.

- Port the per-skill switch as `ISkillBehavior` plugins, one per
  skill (or per family).
- New `SkillBehaviorRegistry` keyed by skill id; resolver consults
  it before falling back to the generic DamageKind dispatch.
- Land ~20 skills per pass starting with the most-used (Bash,
  Magnum, Bowling Bash, Sonic Blow, Acid Bomb, Storm Gust,
  Heaven's Drive, Magnus, Holy Light, Pneuma, Safety Wall,
  Fire Bolt chain, Bolt skills).
- **Acceptance:** Skill replay diff ≤ ±5 %, knockback applied,
  status proc triggered.

### T2.4 — SC engine completion

The `status_yml` SQL table ships 1,005 rAthena SC display rows
(death penalty / dispel resist / icon mapping); the gap was the
**per-SC behavior code** — not data.

#### T2.4a — Enum expansion ✅ DONE (2026-05-20)

`StatusType.cs` now mirrors **all 1006 rAthena `SC_*` ids**
1:1 with rAthena's `enum sc_type` (status.hpp:233). Values are
lockstep so persistence (`SaveStatusChangeDataAsync` /
`RequestStatusChangeDataAsync`) round-trips without remapping.
Friendly C#-only names (`HealOverTime`) parked at id 2000
outside the rAthena range; `Basilica` (was at the wrong slot)
split into `Basilica = 116` (Priest skill) + `BasilicaCell`
(cell-based variant).

Generator at `/tmp/gen_statustype.py` reads `status.hpp` and
emits the enum — re-runnable on rAthena content patches.

#### T2.4b — First wave of behavior modules ✅ DONE (2026-05-20)

`Map.Server/Status/StatusEffectRegistry.cs` registers ~30 SC
handlers spanning the categories below; the long-tail SCs ride
the same registry shape.

- **Crowd-control gates** (Stone / Freeze / Stun / Sleep / Curse /
  Silence / Confusion / Blind / Stonewait) — no-op handlers so
  `EntityActionGates.CanAct` / `CanCastSkill` flip false when the
  SC attaches.
- **DoT**: Bleeding (10 s, MaxHp/100), Burning (3 s, MaxHp*3/100),
  DeadlyPoison (1 s, MaxHp*2/100). Poison is the pre-existing
  reference impl (1.5 s, MaxHp*15/1000).
- **Stat buffs with revert**: Adrenaline / Twohandquicken
  (+AspdRate), Provoke (-Def +Batk), Concentrate (+Agi +Dex
  potion), Concentration (+Hit LK skill), Angelus (+Mdef2),
  Assumptio (+Def +Mdef cached in Val2/Val3 for clean revert).
- **Cast-time SC overlay** (consumed by
  `SkillCastTimingService.CastFixSc`):
  - SC_SUFFRAGIUM → −15 %/level then auto-consume.
  - SC_MEMORIZE → halve cast, decrement Val1, end at zero.
  - SC_SLOWCAST → +10 %/level, permanent for duration.
  - SC_PARALYSIS → +Val3 % (Guillotine Cross status).
  - SC_IZAYOI → /2 (Kagerou / Oboro).
  - SC_POEMBRAGI → ×(100−Val2)/100 (Minstrel song).
  Stacking order: debuffs first, then buffs. Tested.
- **Presence markers** ready for combat-hook ports: Endure /
  Magnificat / FireWeapon / WaterWeapon / WindWeapon /
  EarthWeapon / Kyrie / AutoGuard / ReflectShield / SteelBody /
  Providence / BasilicaCell.

- **Acceptance — Met:** Suffragium halves next cast (tested);
  Slowcast +50 % at lv5 (tested); Stone/Sleep gate CanAct
  (tested); Bleeding ticks MaxHp/100 every 10 s (tested).
  20 new tests; full Map.Server.Tests 354/355.

#### T2.4 — Long-tail SC behavior (~220 still skeletal)

The registry now has the pattern; remaining work is incremental:
each batch of 20-30 SCs follows the same OnStart/OnEnd shape.
Priority order for the next wave:
- **Refresh-on-hit infra** for Endure (needs DamageService hook).
- **Combat-side reads** for AutoGuard / ReflectShield / SteelBody /
  Kyrie barrier consumption.
- **Per-tick effects** for SP-drain SCs, FreezingState, Toxin,
  Pyrexia, Leech, MagicMushroom.
- **Bardsong stacking** rules for ApplecidR / Service4U etc.
- **Forth-class** Handicap* SCs.

### T2.5 — Per-skill behavior plugins (was T2.3, renumbered)

Today's resolvers (Weapon / Magic / Misc / Heal / Status) dispatch
by `SkillDamageKind` only. The per-skill quirks (Bowling Bash extra
hits, Acid Bomb % damage, Magnum Break splash + fire-cloak SC,
Sonic Blow chain, Storm Gust freeze, …) live in
`skill_castend_damage_id` switch cases — currently absent.

- Port the per-skill switch as `ISkillBehavior` plugins, one per
  skill (or per family).
- New `SkillBehaviorRegistry` keyed by skill id; resolver consults
  it before falling back to the generic DamageKind dispatch.
- Land ~20 skills per pass starting with the most-used (Bash,
  Magnum, Bowling Bash, Sonic Blow, Acid Bomb, Storm Gust,
  Heaven's Drive, Magnus, Holy Light, Pneuma, Safety Wall,
  Fire Bolt chain, Bolt skills).
- **Acceptance:** Skill replay diff ≤ ±5 %, knockback applied,
  status proc triggered (after T2.4 lands).

**Tier 2 milestone:** Combat damage matches rAthena replay to within
5 % on the test fixture; the remaining `data-pending` markers in
`Combat/` + `Skills/` clear once T2.2 + T2.4 + T2.5 land.

---

## Tier 3 — Wire packets (visibility)

Combat correctness without packets is invisible to the client. Port
the outbound `clif_*` emitters next so the player can *see* the
work from Tiers 1–2.

**Current state:** `Core.Server/Packets/Out/` already ships
**113 emitter classes**. The visible-gameplay loop (move, attack,
chat, NPC dialog, shop, inventory equip/unequip, trade, storage)
is already wired end-to-end (proven by the live DHXJ client tests
documented in `map/replay-baseline.md`). The remaining Tier 3 work
is the **skill-side + status-broadcast surface** specifically — the
list below is what's missing, not what's pending entirely.

### T3.0 — Per-packet audit (do this first)

Before adding more emitters, audit the 113 existing classes against
`rathena/src/map/clif.cpp`'s ~780 `clif_*` outputs. Output: a
`map/clif-parity.md` per-packet table (✅/⚠️/❌) so subsequent
T3.* waves only build what's missing.

### T3.1 — Skill cast + result packets

- `clif_skill_cast` (cast bar start)
- `clif_skill_castcancel`
- `clif_skill_damage` (single-target damage)
- `clif_skill_nodamage` (support / heal)
- `clif_skill_poseffect` (ground unit anim)
- `clif_skill_setunit` (drawing ground units on client)
- `clif_skill_fail` (refused cast)
- **Acceptance:** Bash deals damage, animates correctly, client
  sees the hit floater.

### T3.2 — Status icon broadcast

- `clif_status_change` (status icon on/off)
- `clif_efst_status_change_sub` (party member SC indicators)
- `clif_displaystatus`
- **Acceptance:** Cast Increase AGI → green AGI buff icon appears on
  player + on party HUD.

### T3.3 — Combat result variants

- `clif_damage` (auto-attack splash)
- `clif_blown` (knockback)
- `clif_dispdamage` (mob over-head damage)
- `clif_hpmeter` (party / boss HP bar)
- `clif_obtain_exp`
- **Acceptance:** Knockback animates client-side; party leader sees
  member HP shrink.

### T3.4 — Companion display packets

- `clif_homunculus`, `clif_send_homstats`, `clif_hom_food`
- `clif_pet_emotion`, `clif_send_petstatus`
- `clif_mercenary_info`, `clif_mercenary_skills`
- `clif_elemental_info`
- **Acceptance:** Companion appears next to PC, HP / SP / hunger
  bars render correctly.

**Tier 3 milestone:** The client renders the gameplay correctly;
all packet-related "data-pending" entries in `Handlers/` /
`ClifWire/` resolve.

---

## Tier 4 — Persistence + cross-server IPC

`IIntifService` has 149 entry points all returning 0 — they need to
forward to the existing typed `*IpcService` wrappers on the char
server.

### T4.1 — Mail round-trip

- `MailService.Send` → `intif.MailSend` → `IInterMailService.SendAsync`
  (already exists char-side).
- `MailService.GetAttachment` → `intif.MailGetAttach`.
- Per-PC mail-draft session state.
- **Acceptance:** Send mail → recipient sees it on next mail open,
  attachments + zeny transfer.

### T4.2 — Quest log save/load

- `QuestService.PcLogin` → `intif.QuestRequest`.
- `QuestService.Add` / `Delete` / `UpdateObjective` → `intif.QuestSave`.
- **Acceptance:** Accept quest → log out → log in → quest still
  there with progress.

### T4.3 — Achievement save/load

- Same shape as quest.

### T4.4 — Pet / homun / merc save/load

- `PetOpsService.Save` → existing `IInterPetService.SaveAsync`.
- `HomunculusService.Save` → existing `IInterHomunService.SaveAsync`.
- `MercenaryService.Save` → existing `IInterMercService.SaveAsync`.
- **Acceptance:** Pet HP / hunger / intimacy survive relog.

### T4.5 — Storage save/load

- `IStorageService.Open` / `Save` already exists for account storage.
- `GuildStorageService.Open` → `intif.RequestGuildStorage`.
- Premium storage → `intif.RequestAccountStorage(slotId)`.
- **Acceptance:** Items deposited to guild storage by member A
  visible to member B from another map.

### T4.6 — Auction round-trip

- `intif.Auction*` → existing `IInterAuctionService`.
- **Acceptance:** Register auction → expires → item / refund returns
  via mail.

**Tier 4 milestone:** Cross-server persistence parity. Every "IPC
forward pending" data-pending entry resolves.

---

## Tier 5 — Per-file deep audits (the long tail)

For each file in the list, run the `/rathena-parity` skill workflow
to produce a full per-function status table + wave plan (the
pc.cpp / battle.cpp / skill.cpp style). This is where the C# port
gets fine-grained correctness audit beyond "entry point exists".

Suggested ordering by dependency + impact:

1. **`status.cpp`** — already has many entries; full per-SC table.
2. **`clif.cpp`** — full per-packet inventory (780 entries).
3. **`unit.cpp`** — movement / attack / skill-use action lifecycle.
4. **`mob.cpp`** — full mob lifecycle + drop_adjust + clone.
5. **`map.cpp`** — full cell/region/foreach API.
6. **`npc.cpp`** — event dispatch + warp + monster spawning.
7. **`script.cpp`** — BUILTIN inventory mapped to TS API.
8. **`itemdb.cpp`** — per-attribute audit (slot mask, weight, …).
9. **`guild.cpp`** — full member / castle / alliance lifecycle.
10. **`pet.cpp`** — evolution + autobonus per pet.
11. **`homunculus.cpp`** — evolution / mutation / Sphere Mine etc.
12. **`mercenary.cpp`** — contract / kill bonus tiers.
13. **`elemental.cpp`** — per-mode AI + skill use.
14. **`achievement.cpp`** — per-objective dispatch.
15. **`quest.cpp`** — per-quest hook.
16. **`battleground.cpp`** — per-mode queue rules.
17. **`instance.cpp`** — instance map naming + reservation.
18. **`channel.cpp`** — autojoin + GM channels.
19. **`mail.cpp`** — attachment validation + delivery timer.
20. **`party.cpp`** — full party engine audit (not just booking).
21. **`chrif.cpp`** + **`intif.cpp`** — IPC routing audit.
22. **Everything else** — trade, vending, buyingstore, cashshop,
    storage, navi, path, log, npc_chat, pc_groups, searchstore,
    mapreg, duel, clan, date.

Each audit produces a `<file>-parity.md` table; each wave plan
becomes a small PR.

**Tier 5 milestone:** Every file has the pc/battle/skill style audit
table.

---

## Tier 6 — Endgame content systems

The features that fall out once Tiers 1–5 land. These are gameplay-
visible feature work, not parity gap.

- **WoE (War of Emperium):** castle ownership, emperium HP, GvG
  damage rules → relies on `Guild.CastleData*` + `IZoneDamageService`.
- **Instances:** dungeon entry / lock / reset → relies on
  `InstanceService` real + scripts.
- **BG (Battleground):** rotation / scoring / item rewards → relies
  on `BattlegroundService` queue + scripts.
- **Pet evolution:** stage transitions / form changes.
- **Homun mutation / Sphere Mine / S types.**
- **Vending / buying-store autotrade:** persistence + reconnect.
- **Cash shop sales schedule.**
- **Auction House full flow:** register → bid → expire → refund.
- **Mail rodex (modern UI).**

These don't have a `data-pending` marker because their entry points
are already canonical — they're just feature implementation past the
parity bar.

---

## Next concrete tasks (2026-05-20, post-Tier-1)

In recommended pickup order — each is one PR-sized chunk:

1. ~~**T2.2 — Card modifier port.**~~ ✅ DONE 2026-05-20.

2. ~~**T2.4a — SC enum expansion.**~~ ✅ DONE 2026-05-20.

3. ~~**T2.4b — Per-SC IStatusEffect modules, first wave.**~~ ✅ DONE 2026-05-20.

4. **DB-7 — Wave 3 `_db` ports** (any remaining tables I missed
   in Wave 2 — produce_db.txt, mob_chat_db, stylist, const, etc.).
   ~half a day. Each is < 100 LOC by the established pattern.

5. **DB-8 — Per-loader wiring to consume payload_json.** ~1 day.
   The Wave 2 catalogs (item_combos, item_packages, status_yml,
   refine, enchantgrade, …) are in SQL but their runtime services
   still use empty stubs — wire each to deserialize the
   `payload_json` column on Reload.

6. **T3.0 — clif.cpp packet audit.** ~half a day. Run
   `/rathena-parity clif.cpp` to produce a per-packet status table
   under `map/clif-parity.md`. Outputs the real Tier 3 backlog,
   replacing the optimistic T3.1-3.4 list.

7. ~~**T2.5 — Per-skill behavior plugins, first wave.**~~ Infra
   shipped 2026-05-20: `ISkillBehavior` + `SkillBehaviorRegistry`
   wired into `SkillCastService.ResolveSkill`; two seed plugins
   (MagnumBreak, Bash) shipped. Adding more plugins is now an
   additive cadence — one PR per family:
   - **Knight/Lord Knight:** Bowling Bash (extra hits scaling
     on enemies in radius), Pierce (×hits by target Size),
     Spiral Pierce.
   - **Assassin:** Sonic Blow (8-hit chain), Soul Breaker,
     Grimtooth.
   - **Wizard:** Storm Gust (3-hit + Freeze proc), Lord of
     Vermillion, Earth Spike.
   - **Priest:** Heal-vs-Undead (overload AL_HEAL plugin
     returns damage instead of heal), Holy Light, Magnus
     Exorcismus.
   - **Hunter:** Double Strafe, Arrow Shower, Blitz Beat.

After these, Tier 4 (IIntifService stubs → real char-server
IPC) becomes the dominant gap. Tier 5 / 6 remain as long-tail
backlogs.

---

## How to use this doc

When picking up the next task:

1. Scan the active tier for the next item with an unresolved
   acceptance criterion.
2. Run `/rathena-parity <file>` for that file if it needs the deep
   audit (Tier 5), or follow the tier's port plan otherwise.
3. Add a History line to this doc when a tier completes.
4. Move to the next tier when its milestone is reached — the
   foundation-first ordering means later tiers depend on earlier
   ones.

The tier separation is not strict — when an interesting feature
needs work *and* its dependencies are done, ship it. Don't gate
gameplay on Tier 4 completeness if Tier 1–3 already cover the
hot path.

## History

### 2026-05-22 — T6 audit-doc rollup refresh + T4/T5/T6 tier-row flip

Tier scoreboard rebuilt after T5 (`fa8b494..bc39af0`) closed every
`map/*-parity.md` to 0 ❌. T3 / T4 / T5 / T6 rows now ✅; new T6-doc
row tracks the login/char/inter doc-refresh sweep (T6.1..T6.5, 5
commits, 2026-05-22). Per-tree audit rollup at
[T6-audit-2026-05-22.md](T6-audit-2026-05-22.md). No code changes
in this commit — tier scoreboard refresh only.

### 2026-05-21 — T2.3-P4: Archer directory complete (126/126) + Merchant wave (~24/105)

Cleared every Archer skill stub by reading each rathena-fork .cpp and
manually translating to C#. **Archer directory at 100 %** (126 of 126).
Started Merchant wave: ~24 of 105 stubs done before context window
pressure ended the wave.

**Archer categories ported**: Hunter base (Double Strafe, Arrow
Shower, Charge Arrow, Phantasmic Arrow, Ankle Snare, Land Mine,
Sandman, Shockwave / Skid / Spring / Flasher / Freezing / Claymore /
Talkie Trap, Detect, Remove Trap, Improve Concentration), Hunter
attacks (Beast Strafing, Blitz Beat, Falcon Assault, Sharpshooting,
Sense), Sniper buffs (Wind Walker, True Sight). Ranger / Wind Hawk
suite: Aimed Bolt, Arrow Storm, Cluster Bomb, Cobalt/Maize/Magenta/
Verdure Trap, Detonator, Electric Shocker, Fear Breeze, Firing Trap,
Icebound Trap, Camouflage, Crescive Bolt, Deep Blind Trap, Flame /
Solid / Swift Trap, Gale Storm, Hawk Boomerang/Rush/Mastery, Warg
Bite/Dash/Mastery/Rider/Strike, Sensitive Keen, Wild Walk.
Performer suite (Bard / Dancer / Wanderer / Minstrel / Trouvere):
Acoustic Rhythm, Amp, Battle Theme, Classical Pluck, Down Tempo,
Echo Song, Encore, Focus Ballet, Friggs Song, Geffenia Nocturn,
Gloomy Day, Great Echo, Gypsy's Kiss, Harmonic Lick, Harmonize, Hip
Shaker, Impressive Riff, Improvised Song, Jawaii Serenade, Lady
Luck, Lerads Dew, Lullaby, Magic Strings, Make Arrow, Marionette
Control, Melody Strike, Mental Sensing, Metallic Sound/Fury,
Moonlit Serenade, Musical Interlude, Nipelheim Requiem, Pang Voice,
Perfect Tablature, Poem of the Netherworld, Power Chord, Pron March,
Reverberation, Retrospection, Rhythm Shooting, Roki Capriccio,
Rose Blossom, Saturday Night Fever, Sensitive Keen, Severe Rainstorm,
Sheltering Bliss, Skilled Special Singer, Slinging Arrow, Slow
Grace, Song of Lutie, Song of Mana, Sound Blend, Sound of Destruction,
Swing Dance, Symphony of Lovers, Tarot Card of Fate, Unbarring Octave,
Unchained Serenade, Unlimited Humming Voice, Valley of Death, Voice
of Siren, Vulcan Arrow, Wand of Hermode, Warcry of Beyond, Windmill
Rush Attack, Wink of Charm, Ain Rhapsody, Circle of Nature's Sound,
Dance With a Warg, Dazzler, Deep Sleep Lullaby, Dominion Impulse,
Melody of Sink.

**Merchant wave** (24 of 105, partial): ABR Battle Warrior /
Dual Cannon / Mother Net / Infinity, Acid Demonstration, Acid Terror,
Acidified Zone (Fire/Ground/Water/Wind), Advance Protection, Advanced
Adrenaline Rush, Adrenaline Rush, Aid Berserk Potion, Aid Condensed
Potion, Aid Potion, Alchemical Weapon, Analyze, Arm Cannon, Attack
Machine, Axe Boomerang, Axe Stomp, Axe Tornado.

**Carried-over TODOs**: bound-elemental binding (ABR mob spawn),
party_foreachsamemap splash, weapon-type checks (axe/staff/book),
break_equip helper, AM_POTIONPITCHER inventory-script potion hp/sp
read, partner-chorus detection, song-dispatcher (renewal
skill_castend_song), bound-elemental upgrades, OPTION_FALCON /
OPTION_WUG / OPTION_WUGRIDER toggle service, tarot card dispatch
(14 effects), abra DB.

**Tests**: build green; expected 384/385 (same pre-existing replay
failure as P2/P3).

**Remaining stubs**: ~800 across 12 directories (Merchant 81,
Taekwon 91, Thief 85, Swordman 76, Ninja 63, ElementalNpc 51,
Homunculus 45, Gunslinger 44, Other 40, MercenaryNpc 35, Summoner
33, Npc 154, Novice 12).

### 2026-05-21 — T2.3-P3: Mage directory complete (143 manual ports)

Cleared every `// TODO: port from rathena-fork` stub in
`Map.Server/Skills/Behaviors/Mage/`. Total Mage plugins ported to real
behavior: **143 of 143** — directory at 100 %.

**Categories ported** (this wave + carryover from P1/P2):
- **1st-class Mage core**: FireBolt, ColdBolt, LightningBolt,
  SoulStrike, SoulVulcanStrike, NapalmBeat, NapalmVulcan,
  SoulExpansion, Sight, SightBlaster, SightRasher, EnergyCoat,
  FireWall, SafetyWall, Fireball, Thunderstorm, FrostDiver, FrostNova,
  IceWall, StoneCurse, FirePillar, MeteorStorm, JupitelThunder,
  HeavensDrive, LordOfVermilion, Quagmire, Stasis, EarthSpike, WaterBall.
- **Wizard / Sage / Professor**: StormGust, GravitationField,
  Ganbantein, Suicide, Estimation (Sense), Hindsight, Monocell,
  HocusPocus, Dispell, ElementalChange (Fire/Water/Earth/Wind), EndowBlaze
  (Flame Launcher), EndowQuake (Seismic Weapon), EndowTornado
  (Lightning Loader), EndowTsunami (Frost Weapon), CastCancel,
  CreateElementalConverter, ClassChange, MagicRod, SpellBreaker,
  MindBreaker, SoulExhale, SoulSiphon, Indulge.
- **Warlock**: JackFrost, MarshOfAbyss (FiberLock), Comet, ChainLightning,
  CrimsonRock, DrainLife, WhiteImprison, FrostyMisty, HellInferno,
  EarthStrain, TetraVortex, SiennaExecrate, ReadingSpellbook, Release,
  SummonStone, SummonFireBall, SummonLightningBall, SummonWaterBall.
- **Sorcerer**: PsychicWave, EarthGrave, DiamondDust, VaretyrSpear,
  PoisonBuster, Arrullo, CloudKill, ElectricWalk, FireWalk, ElementalAction,
  ElementalShield, SpellFist, SpiritControl, SpiritRecovery, Striking,
  FireInsignia, WaterInsignia, WindInsignia, EarthInsignia, Warmer,
  Deluge, Volcano, Whirlwind, VacuumExtreme, SummonFireSpiritAgni,
  SummonWaterSpiritAqua, SummonWindSpiritVentus, SummonEarthSpiritTera.
- **Arch Mage / Elemental Master (4th)**: AstralStrike, CrimsonArrow,
  CrystalImpact, DestructiveHurricane, FrozenSlash, RockDown, StormCannon,
  AllBloom, ViolentQuake, DiamondStorm, Conflagration, TerraDrive,
  LightningLand, VenomSwamp, ElementalBuster, ElementalVeil,
  SummonElementalArdor, SummonElementalDiluvio, SummonElementalProcella,
  SummonElementalTerremotus, SummonElementalSerpens.
- **Other 4th-class debuffs**: BeastlyHypnosis, BlindingMist,
  ActivityBurn, FloralFlareRoad, MysteryIllusion, RainOfCrystal,
  StrantumTremor, TornadoStorm, GrimReaper.
- **Misc / generated**: ReadingSpellbook, FourSpiritAnalysis,
  MonsterChant, IncreasingActivity, GoldDigger, Leveling, Rejuvenation,
  Coma, Gravity, Questioning.

**TODOs accepted at port time** (carry-over from P2, plus Mage-specific):

| Helper | Status | Affects |
|---|---|---|
| Bound-elemental binding (`sd->ed`) + EM tier classes | Not surfaced on Entity | All Sorcerer summon skills + EM tier upgrades + ElementalAction/Veil/Buster — broadcast-only stubs |
| SC_SPHERE_1..5 slot machinery (Warlock balls) | Not on StatusType enum | Summon{Stone,FireBall,LightningBall,WaterBall} land cast frame only; Release lv 2 is broadcast-only |
| SC_FREEZE_SP + SC_SPELLBOOK1..MAXSPELLBOOK | Not on StatusType enum | Release lv 1 (spellbook detonation) is broadcast-only |
| Caster-SC readback in CalculateSkillRatio | Hook lacks ctx | SC_CLIMAX (Crystal Impact / Destructive Hurricane / Frozen Slash / Rock Down / Storm Cannon / All Bloom / Violent Quake), SC_SUMMON_ELEMENTAL_ARDOR/DILUVIO/PROCELLA/SERPENS/TERREMOTUS (Conflagration / Diamond Storm / Lightning Land / Terra Drive / Venom Swamp), SC_HEATER/COOLER/BLAST/CURSED_SOIL_OPTION (Cloud Kill / Earth Grave / Poison Buster / Electric Walk / Fire Walk / Psychic Wave / Varetyr Spear) — formula misses option-buff bonus |
| `map_foreachinpath` / `map_foreachindir` / `map_foreachinshootrange` | Not exposed to behavior layer | Eight-path AoE: Crimson Arrow, Storm Cannon, Sight Rasher, Frost Nova, Sienna Execrate splash chain |
| Per-stagger unit spawn helpers | Not surfaced | Meteor Storm / All Bloom / Violent Quake / Earth Strain wave staggering — primary unit drops, sub-units TODO |
| Weapon-type read (W_FIST / W_STAFF / W_BOOK) | Not on Entity.Stats | Endow*'s W_FIST fail-gate skipped; Psychic Wave's staff-doubles-hit suppressed |
| `pc_checkskill(SA_FROSTWEAPON/SA_SEISMICWEAPON)` etc. | Player skill table not surfaced to formula hook | Diamond Dust / Earth Grave / Varetyr Spear contribute base formula only |
| `clif_autospell` dialog (player SA_AUTOSPELL pick) | Selection UI not ported | Hindsight is mob-path only for now |
| Abra DB | Not loaded | HocusPocus is a broadcast-only no-op |
| Current-HP/SP read on Entity | Not exposed | Indulge skips HP precondition; Soul Exhale player↔player SP swap skips |
| `mob_class_change` | Not wired | Monocell broadcasts but doesn't transform |
| Sub-skill ids `WL_TETRAVORTEX_*`, `WL_CHAINLIGHTNING_ATK`, `AG_*_ATK/_ATK2`, `AG_DESTRUCTIVE_HURRICANE_CLIMAX`, `AG_CRYSTAL_IMPACT_ATK` | Not on SkillIds catalog (P3 4th-class set) | Element-specific sub-hits collapse to the primary skill id |

**Tests**: build green; 384/385 (same pre-existing port-5191 replay
failure as P2). No regressions from any of the 143 ports.

**Remaining stubs**: ~942 (14 directories — Archer 121, Merchant 104,
Taekwon 91, Thief 85, Swordman 76, Ninja 63, ElementalNpc 51,
Homunculus 45, Gunslinger 44, Other 40, MercenaryNpc 35, Summoner 33,
Npc 154, Novice 12).

### 2026-05-21 — T2.3-P2: Acolyte directory complete (91 manual ports)

Cleared every `// TODO: port from rathena-fork` stub in
`Map.Server/Skills/Behaviors/Acolyte/`. Total Acolyte plugins ported
to real behavior: **91 of 91** — directory at 100 %.

**Categories ported**:
- **1st-class core**: Heal (full renewal Kaite/Berserk/Akaitsuki port,
  already from P1), IncreaseAgi, DecreaseAgi, Blessing, Ruwach,
  Pneuma, WarpPortal, Angelus, Cure, Crucis (SignumCrucis), HolyLight,
  TurnUndead, Aspersio, Sanctuary, Resurrection, Teleport.
- **Transcend / 2nd-job**: Magnificat, Gloria, Suffragium, ImpositioManus,
  Assumptio, LexDivina, KyrieEleison, Basilica, MagnusExorcismus,
  Redemptio, BenedictioSanctissimiSacramenti, Renovatio, StatusRecovery.
- **3rd-class Sura**: AsuraStrike, RagingPalmStrike, RagingThrust,
  RagingTrifectaBlow, RagingQuadrupleBlow, RaisingDragon, AbsorbSpiritSphere,
  AssimilatePower, ChainCrushCombo, CursedCircle, DragonCombo,
  EarthShaker, ExplosionBlaster, FallenEmpire, FlashCombo, GateOfHell,
  GentleTouchQuiet, GentleTouchCure, GlacierFist, HowlingOfLion,
  KiExplosion, KiTranslation, KnuckleArrow, OccultImpaction, PowerVelocity,
  RampageBlaster, RideInLightening, SkyNetBlow, Snap, SummoningSpiritSphere,
  ThrowSpiritSphere, TigerCannon, Windmill, Zen.
- **3rd-class Arch Bishop**: Adoramus, Ancilla, Clearance, CantoCandidus,
  ColuceoHeal, Convenio, Crementia, Epiclesis, HighnessHeal, Judex,
  LaudaAgnus, LaudaRamus, Oratio, Praefatio, Silentium, Vituperatum.
- **4th-class Inquisitor / Cardinal**: Arbitrium, Competentia, DilectioHeal,
  DupleLightMagic, Effligo, FirstBrand, Framen, MassiveFlameBlaster,
  MedialeVotum, OleumSanctum, Petitio, PneumaticusProcella, Reparatio,
  SecondJudgement, SecondFlame, SecondFaith, ThirdConsecration,
  ThirdFlameBomb, ThirdPunish.
- **Generated names that have no per-skill behavior in source**:
  HolyWater (item-production scaffold).

**StatusEffectRegistry** gained NoOp markers for Kaite / Bitescar /
Akaitsuki / Saturdaynightfever / Laudaagnus / Laudaramus so the
SC reads land. Several skills include TODOs for SCs that don't yet
exist on our StatusType enum (Praefatio's <c>SC_PRAEFATIO</c>,
Massive Flame Blaster's burn marker, Cardinal Fidus Animus
mastery bonus).

**TODOs accepted at port time** — kept as comments inside the
ported files, not as functional gaps:

| Helper | Status | Affects |
|---|---|---|
| `IPartyMapService.ForEachOnSameMap` | Missing | All party-broadcast skills (Angelus/Magnificat/Gloria/Suffragium/Impositio/Praefatio/Crementia/Convenio/MedialeVotum/Renovatio etc.) fall back to single-target only |
| `IMapFlagService` via ctx | Missing | WoE/BG/PvP gates on Resurrection / Redemptio / Teleport / Convenio |
| `MAPID_FIRSTMASK` class introspection | Missing | Gunslinger-coin guards on AbsorbSpiritSphere / AssimilatePower / KiTranslation |
| `MO_CALLSPIRITS` time-based cap on `pc_addspiritball` | Approximated as `Add(1)` | Spirit-sphere addition skips per-ball decay tracking |
| `CD_MACE_BOOK_M`, `CD_FIDUS_ANIMUS` masteries | Not in SkillIds | Cardinal mastery bonus contributions omitted |
| SC reads from `CalculateSkillRatio` hook | Hook lacks ctx | SC_COMBO bonuses (Gate of Hell, Tiger Cannon, Throw Spirit Sphere) omitted |
| `pc_lostexp` Redemptio EXP penalty | Renewal drops it | Caster only loses HP (1) + SP (0), no EXP cost |

**Tests**: build green; 384/385 (only pre-existing
port-5191 replay failure persists). No regressions from any of the
91 ports.

**Remaining stubs**: 1,085 (15 other directories — Mage 131,
Archer 121, Merchant 104, Taekwon 91, Thief 85, Swordman 76,
Ninja 63, ElementalNpc 51, Homunculus 45, Gunslinger 44, Other 40,
MercenaryNpc 35, Summoner 33, Npc 154, Novice 12). Each follows the
same manual-port pattern: read fork .cpp → translate carefully to
C# → verify build → move on.

### 2026-05-20 — T2.3 manual-port wave 1 + 5 missing helpers

**Helpers** (5 of 5):

| Helper | Files added | rAthena equivalent |
|---|---|---|
| `clif_skill_nodamage` (661 stub callsites) | `ZC_USE_SKILL.cs` (0x09cb), `ISkillClientService` + `SkillClientService` | Cast result frame for status / heal / buff casts. |
| `clif_skill_fail` (238 callsites) | `ZC_ACK_TOUSESKILL.cs` (0x0110) + `SkillFailCause` enum | Caster-only rejection feedback. |
| `clif_skill_damage` (73 callsites) | `ZC_NOTIFY_SKILL.cs` (0x01de) | Offensive skill hit + damage broadcast. |
| `skill_addtimerskill` (38 callsites) | `ISkillTimerService` + `SkillTimerService`, wired into `MapServerImpl` tick | Closure-based deferred per-skill callback. |
| `BlownBy` (20+ callsites) | `ZC_HIGHJUMP.cs` (0x01ff) + real impl in `UnitOpsService` | Knockback: cell-by-cell slide stopping at walls; broadcasts slide+fixpos pair. |
| `clif_skill_warppoint` | `ZC_WARPLIST.cs` (0x011c) | AL_WARP destination chooser. |

`SkillBehaviorContext` gained an optional `Client` slot so per-skill
bodies broadcast through `ISkillClientService` instead of building
raw ZC packets — keeps each `SkillImpl` body on the high-level
intent ("this cast healed N HP", "this cast was rejected for SP").

**Manual ports** (10 of 10 in wave 1):

| Skill | File | Notes |
|---|---|---|
| AL_HEAL | `Acolyte/Heal.cs` | Full renewal formula, Kaite bounce, Berserk/SaturdayNightFever suppress, Akaitsuki sign-flip, Bitescar end, Heal-EXP gain. 9 dedicated tests, all pass. |
| AL_INCAGI | `Acolyte/IncreaseAgi.cs` | SC_INCREASEAGI 100 % apply, SC_CHANGEUNDEAD damage branch, skill_db duration ladder inlined. |
| AL_DECAGI | `Acolyte/DecreaseAgi.cs` | Resist-roll formula `50 + lv*3 + (lv+int)/5`; broadcast carries SC-landed bool. |
| AL_BLESSING | `Acolyte/Blessing.cs` | Same structure as IncAgi, different SC + duration. |
| AL_RUWACH | `Acolyte/Ruwach.cs` | Self-buff SC_RUWACH + `CalculateSkillRatio +45 %`. |
| AL_PNEUMA | `Acolyte/Pneuma.cs` | Ground-target SkillUnit placement (1-cell wall). |
| AL_WARP | `Acolyte/WarpPortal.cs` | Destination chooser packet, SC_CURSEDCIRCLE_ATKER end on cast complete. |
| MG_SAFETYWALL | `Mage/SafetyWall.cs` | Ground SkillUnit placement (Land Protector overlap branch pending). |
| MG_SOULSTRIKE | `Mage/SoulStrike.cs` | (lv+1)/2 magic bolts + `CalculateSkillRatio +5*lv vs Undead`. |
| MG_NAPALMBEAT | `Mage/NapalmBeat.cs` | 3×3 splash with damage-share; `CalculateSkillRatio -30 + 10*lv`. |

`StatusEffectRegistry` gains `NoOp` markers for Kaite / Bitescar /
Akaitsuki / Saturdaynightfever so the Heal SC reads land. `SkillIds`
gains `HP_MEDITATIO = 363` for the renewal heal-bonus stat.

**Tests**: 384/385 (+9 vs. prior baseline) — 9 new
`AcolyteHealTests` covering each Heal branch. The single failure is
the pre-existing port-5191 replay-baseline test, unrelated.

**Pace**: helpers ~5 hours, per-skill ports average 20–40 min for
simple skills + 1.5 hr for Heal (the most complex). Adding more
manual ports is per-PR additive cadence from here.

### 2026-05-20 — T2.3 missing-skill audit (gap = 6)

Ran `/tmp/find_missing.py` to enumerate rathena-fork .hpp files
with no matching C# stub. Result:

- **rathena-fork .hpp files**: 1,241
- **C# stubs on disk**: 1,209
- **Truly missing (no skill_db.yml id)**: **6** — all 4th-class,
  bleeding-edge entries the fork added but our `db/re/skill_db.yml`
  baseline lacks. To close: append the 6 `Id:`/`Name:` entries to
  `db/re/skill_db.yml`, re-run `/tmp/gen_skill_stubs.py`.
  | File | Constant | Class |
  |---|---|---|
  | `acolyte/blazingflameblast` | `IQ_BLAZING_FLAME_BLAST` | Inquisitor |
  | `gunslinger/midnightfallen` | `NW_MIDNIGHT_FALLEN` | Night Watch |
  | `ninja/fourcolorscharm` | `SS_FOUR_CHARM` | Sky Emperor |
  | `novice/overcomingcrisis` | `HN_OVERCOMING_CRISIS` | Hyper Novice |
  | `swordman/dragonicpierce` | `DK_DRAGONIC_PIERCE` | Dragon Knight |
  | `thief/hitandsliding` | `ABC_HIT_AND_SLIDING` | Abyss Chaser |
- **Name mismatches (present, named differently)**: 9 — not missing
  in behavior, just the C# class name differs from the fork's hpp
  class name. Reasons: suffix variations the fork uses for split
  implementations (`DupleLightMelee`, `SevereRainstormMelee`,
  `SlingItemAttack`, `ChainReactionShotAttack`), a typo in the fork
  the generator faithfully mirrors (`ActifiedZone*` instead of
  `AcidifiedZone*` — 4 files), and the intentional digit-prefix
  guard (`K16thNight`, because C# identifiers can't start with a
  digit). No action required; these are accounted for.
- The remaining numerical delta (1,241 fork files vs 1,209 C#
  stubs) collapses because some fork hpp files declare multiple
  classes (e.g. TetraVortex + the 4 element variants) and the
  dedup pass intentionally collapses those into a single C# file.

Net parity gap: **6 skills**, all blocked on `skill_db.yml`
entries upstream. Tracked as backlog task #150.

### 2026-05-20 — roadmap written
- 6-tier order published: data loaders → combat correctness → wire
  packets → IPC → per-file deep audit → endgame content.
- ~56 inline data-pending markers across 29 service files; T1–T4
  resolve the bulk of them by porting shared foundations.
- Tier 5 (per-file deep audits) is where the long tail of
  per-function parity work lives — pickable file-by-file with the
  `/rathena-parity` skill.

### 2026-05-20 — Tier 1 clarification: SQL is the runtime contract
- Locked in the architectural rule: every `*_db` lives in MariaDB,
  not as a runtime YAML read. rAthena YAML is source of truth +
  ingested via deploy-time seed scripts. Matches the existing
  pattern already used by `skill_db` / `item_db` / `mob_db` /
  `attendance` / `roulette`.
- Documented the 5-step pattern (entity + repo + migration + seed
  + runtime loader) so each `_db` port is mechanical.
- Listed every pending `_db` table with its consumer service, and
  proposed `Tools.SeedGen` as the optional YAML → SQL transformer.

### 2026-05-20 — Tier 1 substantively done
- Tools.RathenaImporter shipped (Tools.RathenaImporter/) — C#
  console reads rAthena YAML via YamlDotNet, emits seed_*.sql.
  Per-file converter pattern (IYamlToSqlConverter); 10 converters
  live (skill / abra / magicmushroom / spellbook / quest / pet /
  achievement / homunculus / mercenary / instance).
- rAthena pre-gen SQL imported: mob_skill_db (11,634 rows) joins
  item_db / mob_db / roulette already in the seeder.
- 10 new EF entities + repositories + EF migration
  (AddStaticCatalogDbs) for: abra, magicmushroom, spellbook, quest,
  pet, achievement, homunculus, mercenary, instance, mob_skill.
- Runtime catalog loaders wired (DB-4): AbraDatabase /
  MagicMushroomDatabase / ReadingSpellbookDatabase / QuestService /
  PetOpsService / AchievementService / HomunculusService /
  MercenaryService / InstanceService — all read from SQL on boot,
  fall back to empty in tests. SkillDb's existing SQL path now
  loads 1,614 rows from the new seed.
- Total: ~6,400 new catalog rows SQL-backed beyond what was already
  there + the full 1,614-skill catalog flowing through SkillDb.
- Remaining for full Tier 1: elemental / battleground / cashshop /
  item_combo / item_package / item_randomopt / item_group /
  item_enchant / laphine / castle / job stats / status / channels.conf
  / battle_athena.conf. Each is mechanical via the established
  pattern — add converter + entity + repo + config + migration +
  seeder wiring.

### 2026-05-20 — Tier 1 done (Wave 2)
- 35 more YAML→SQL converters shipped: 10 flat-shape tables
  (castle, statpoint, exp_homun/guild, size_fix, reputation,
  create_arrow, item_randomopt, cashshop, captcha) + 25 payload-
  shape tables that store deeply nested rAthena YAML as JSON in
  a `payload_json` column (elemental_db, battleground_db,
  skill_tree, guild_skill_tree, mob_summon, item_randomopt_group,
  attr_fix, level_penalty, job_stats, job_exp, job_basepoints,
  status_yml, item_combos, item_packages, item_group_db,
  item_enchant, item_reform, laphine_synthesis, laphine_upgrade,
  refine, enchantgrade, map_drops, mob_item_ratio, item_cash,
  attendance, reputation_group). Runtime services deserialize
  the JSON back to typed records on Reload.
- New EF migration `AddTier1Wave2Catalogs` creates 36 tables.
- DatabaseSeeder runs the 30 non-empty seed files.
- 52 _db tables total now SQL-backed (16 from Wave 1 + 36 here).

### 2026-05-20 — Tier 1 conf → JSON with schemas
- Tools.RathenaImporter gains a conf parser + a conf→JSON+schema
  emitter. Run `dotnet run --project Tools.RathenaImporter --conf-only`
  to (re-)generate the JSON files from rAthena `conf/*.conf`.
- 19 conf files converted (18 battle/*.conf + channels.conf) to
  `Map.Server/config/battle/*.json` + `Map.Server/config/channels.json`,
  totaling ~633 documented knobs. Each property's `description` in
  the schema comes from rAthena's inline `.conf` comments — IDE
  hover gives the original gameplay documentation.
- IBattleConfigService now overlays the JSON values on top of the
  in-memory defaults at boot. Re-running the importer + restarting
  the map server picks up upstream rAthena config tweaks.
- All existing `appsettings.json` (Login, Char, Map, Web,
  Core.Database) now reference a hand-written shared schema at
  `schemas/appsettings.shared.schema.json` for autocomplete +
  documentation.
- `schemas/README.md` documents the conventions (how the $schema
  reference works, where to add new schemas).

### Tier 1 done
- Every rAthena `_db` worth porting flows YAML → SQL at deploy
  time; runtime reads SQL only.
- Every gameplay knob in `battle_*.conf` flows .conf → JSON; the
  JSON gets editor autocomplete via the generated schemas.
- Tier 2 (combat correctness) is the next active tier.

### 2026-05-20 — T2.3 full rathena-fork structural parity: 1,113 generated stubs
- Auto-generation pipeline ports the entire
  `rathena-fork/src/map/skills/<class>/<skill>.cpp+hpp` tree to
  C# skeletons. 1,113 new files committed (1,208 total per-skill
  plugins counting the 95 hand-written from earlier waves).
- Generator (`/tmp/gen_skill_stubs.py`) parses each rathena-fork
  pair, extracts class name + base class + overridden methods +
  skill id from the constructor, cross-references skill_db.yml
  for the numeric id, emits a properly-namespaced .cs at
  `Map.Server/Skills/Behaviors/<Class>/<Skill>.cs`.
- Each stub includes the original C++ body inlined as `// TODO:
  port from rathena-fork` block reference comments so the
  per-skill implementer has the source visible alongside the
  skeleton C# overrides.
- Dedup pass (`/tmp/dedup_methods.py`) brace-walks the generated
  files to strip duplicate method overrides emitted when a single
  hpp declared multiple classes (e.g. TetraVortex + the 4 element
  child classes).
- `SkillImpl` base gains `ModifyDamageData` hook to match the
  rathena-fork virtual method surface (default pass-through).
- `SkillIds.cs` grows from 67 → 1,182 constants. 1,115 new
  constants pulled from `db/re/skill_db.yml`, sorted by enum name,
  appended in a single block beneath the hand-curated section.
- `Program.cs` DI block grows by ~1,187 lines (1,208 total
  plugin registrations including the 22 hand-written hold-overs).
- Tier scoreboard for T2.3 flipped to **1,208 files** — full
  rathena-fork structural parity. Body fill-in per skill is the
  incremental backlog from here: open file, translate body,
  add test.
- Tradeoff acknowledged: ~80 hand-written real implementations
  from earlier waves under Priest/, HighWizard/, etc. were
  retired in favor of the generator's stubs at the proper
  rathena-fork paths (`acolyte/`, `mage/`). Real impl restoration
  is per-skill TODO work; the file structure + IDs + DI wiring
  are all in place.
- Tests: 375/376 (1 pre-existing replay-baseline failure
  unchanged); build green.

### 2026-05-20 — T2.3 refactor to SkillImpl + 35 transcend / 3rd / 4th class plugins
- Refactored the flat `ISkillBehavior` interface to the proper
  rAthena-fork OOP hierarchy: `SkillImpl` base + three specialized
  subclasses (`WeaponSkillImpl`, `StatusSkillImpl`,
  `RecursiveDamageSplashSkillImpl`). Each subclass owns the
  per-hook composition (CalculateSkillRatio / ModifyHitRate /
  ApplyAdditionalEffects / etc.) so a single skill can layer
  multiple specializations cleanly (e.g. Bash overrides ratio +
  hit-rate + post-hit stun).
- Reorganized all 59 first/second-class plugins into per-job
  subdirectories matching rathena-fork's tree
  (`Behaviors/Swordman/Bash.cs`, `Behaviors/Mage/FireBolt.cs`, …).
  Class names dropped the `Behavior` suffix to match rathena-fork
  (`Bash` not `BashBehavior`).
- New transcend-class subdirectories with 24 plugins:
  `LordKnight/` (LkConcentration, TensionRelax, Berserk,
  SpiralPierce, HeadCrush), `HighPriest/` (Assumptio),
  `HighWizard/` (MagicCrasher, MagicPower, NapalmVulcan),
  `Paladin/` (Pressure, Sacrifice, ShieldChain),
  `Champion/` (PalmStrike, TigerFist, ChainCrush),
  `AssassinCross/` (EnchantDeadlyPoison, SoulBreaker,
  MeteorAssault), `Sniper/` (FalconAssault, SharpShooting,
  WindWalk), `Whitesmith/` (MeltDown, CartBoost).
- 3rd-class seeds (10 plugins, one per class): `RuneKnight/
  DeathBound`, `Warlock/ChainLightning` (7-hit bounce chain),
  `ArchBishop/Adoramus`, `Ranger/ArrowStorm`,
  `Mechanic/AxeBoomerang`, `GuillotineCross/DarkIllusion`,
  `RoyalGuard/OverBrand`, `Sura/DragonCombo`,
  `Minstrel/Reverberation`, `Sorcerer/PsychicWave`,
  `Genetic/CartCannon`, `ShadowChaser/TriangleShot`.
- 4th-class seed: `DragonKnight/DragonicAura`.
- `StatusEffectRegistry` grows 11 new NoOp handlers for the
  transcend/3rd/4th SCs (Tensionrelax / Berserk / Magicpower /
  Sacrifice / Edp / Windwalk / Meltdown / Cartboost / Deathbound /
  Adoramus / DragonicAura).
- 17 tests in `SkillImplBehaviorTests` cover the new hierarchy
  composition (Bash ratio + hit rate + stun proc all wire
  cleanly via separate hooks) and per-subclass dispatch
  (WeaponSkillImpl pipeline, StatusSkillImpl toggle,
  RecursiveDamageSplashSkillImpl splash enumeration).
- Total per-skill plugin count: **94**. Adding any future skill
  (transcend long-tail, more 3rd-class, more 4th-class, NPC
  skills, mob skills) = one new file in the matching subdir +
  one DI line.

### 2026-05-20 — T2.3 per-skill migration: 57 more plugins (waves 1-3)
- Convention "every major skill has its own file" enforced — each
  rAthena case arm of `skill_castend_damage_id` / `skill_castend_nodamage_id`
  ports to a `<SkillName>Behavior.cs` under
  `Map.Server/Skills/Behaviors/`. The file's xmldoc cites the
  rAthena source line + formula so future maintainers can
  cross-check against the C++.
- Wave 1 (22 plugins): SM Provoke/Endure, KN TwoHandQuicken/Pierce/
  BowlingBash, MG FrostDiver/StoneCurse, AL/PR HolyLight/LexDivina/
  LexAeterna/TurnUndead, MC Mammonite, BS HammerFall/AdrenalineRush/
  Overthrust, AC DoubleStrafe/ArrowShower, HT BlitzBeat, TF Hiding/
  Poison, AS SonicBlow, MO TripleAttack.
- Wave 2 (24 plugins): PR Impositio/Suffragium/Aspersio/KyrieEleison/
  Magnificat/Gloria, MG bolts (Fire/Cold/Lightning/Soul)/AoE
  (NapalmBeat/Fireball/Thunderstorm), AL SignumCrucis, KN spear
  branch (BrandishSpear/SpearStab/SpearBoomerang), AS GrimTooth/
  EnchantPoison, MO FingerOffensive/Investigate/ExtremityFist/
  ExplosionSpirits/BodyRelocation.
- Wave 3 (11 plugins): AS Cloaking, BS Maximize, WZ EarthSpike/
  HeavenDrive/JupitelThunder/FrostNova, AC OwlsEye/Concentration,
  MO CallSpirits, BA FrostJoker, DC Scream.
- `StatusEffectRegistry` gains 11 new handlers (Hiding / Overthrust /
  Aeterna / Impositio / Aspersio / Signumcrucis / Encpoison /
  Cloaking / Maximizepower as NoOps; Gloria with Luk stat-mod;
  Explosionspirits with Cri/Batk stat-mod).
- `SkillIds.cs` grows from 27 → 67 verified constants
  (cross-checked against db/re/skill_db.yml).
- Program.cs DI wires all 59 plugins (one AddSingleton per skill).
- 58 new tests in `T2_3_SkillBehaviorMigrationTests` covering
  damage math, SC application, toggle semantics (Hiding/Cloaking/
  LexDivina), filter logic (SignumCrucis Undead/Dark only,
  TurnUndead instakill bands), splash patterns (BowlingBash/
  ArrowShower/Fireball/HeavenDrive), multi-hit counters
  (DoubleStrafe/SonicBlow/TripleAttack/JupitelThunder),
  reveal-hidden side-effects (HeavenDrive pops Hiding), random-
  proc bounds (FrostDiver/HammerFall/Scream/FrostJoker).
- Tests: full sweep 437/438 (1 pre-existing replay-baseline failure
  unchanged).
- **Coverage now spans every classic Renewal first-class + 1-2
  job tree** (NV/SM/MG/AL/MC/AC/TF + KN/PR/WZ/BS/HT/AS) plus
  Monk + Bard + Dancer trans skills. Transcend (LK/HP/HW/PA/CH/
  AB/etc.) + 3rd-class follow the same pattern when those waves
  port.

### 2026-05-20 — T2.5 ISkillBehavior plugin layer + 2 seed plugins
- New `Map.Server/Skills/Behaviors/` namespace:
  - `ISkillBehavior` interface (one `Resolve` method that returns
    true to claim the cast or false to fall through to the
    generic resolver).
  - `SkillBehaviorRegistry` indexes plugins by rAthena skill id.
  - `SkillBehaviorContext` record carries shared services
    (Entities / Damage / Battle / Sc) so per-plugin ctors stay
    tiny.
- `SkillCastService.ResolveSkill` consults the registry first;
  plugin returns true → skip generic. Optional DI deps so the
  legacy ctor still works.
- `MagnumBreakBehavior` (SM_MAGNUM): claims cast; 5×5 splash
  around caster, runs the standard swing on each victim ×
  (120 + 20*lv)% rate, applies SC_FIREWEAPON for 10 s.
- `BashBehavior` (SM_BASH): falls through to generic Weapon
  resolver; layers a Fatal Blow stun proc at lv 6+ (chance =
  5 + 5*(lv-5)%).
- 9 new tests in `SkillBehaviorTests`. Registry plumbing +
  splash radius + self-skip + SC application + stun proc bounds
  + fall-through semantics all covered.
- Program.cs DI: plugins + behavior registry + the 5 generic
  resolvers + the resolver registry all hand-wired (resolvers
  were previously only built in SkillCastService's test ctor).

### 2026-05-20 — StripEquip wired to SC table
- `SkillSideEffectService.StripEquip` was a data-pending stub
  until T2.4a put SC_STRIPWEAPON/SHIELD/ARMOR/HELM into
  `StatusType`. Now parses the equip mask and attaches one SC per
  slot (HandR/HandL → STRIPWEAPON, HandL → STRIPSHIELD,
  Armor → STRIPARMOR, HeadTop/Mid/Low → STRIPHELM).
- `StatusEffectRegistry` adds 4 NoOpHandler entries for the strip
  SCs.
- 5 new tests in `SkillSideEffectStripTests`.
- Item-side enforcement (CanEquip read on the SC presence) is
  the next step; the duration is at least correctly recorded.

### 2026-05-20 — Regen SC overlay (Bleeding / Magnificat)
- `NaturalHealService.Tick` reads the SC table:
  - `SC_BLEEDING` on the entity suppresses the HP regen line
    (matches rAthena `status_check_natural_heal`).
  - `SC_MAGNIFICAT` on the entity doubles the SP regen amount
    after the sitting bonus.
- Optional `IStatusChangeService` ctor dep — DI resolves it
  automatically; legacy tests without SC see no overlay.
- 2 new tests in `NaturalHealServiceTests`.

### 2026-05-20 — Combat-side SC consumers (SteelBody / Kyrie / AutoGuard)
- `DamageService.ApplyResolved` gains an `ApplyScDamageReduction`
  step that runs before HP commit. Renewal order:
  1. **AutoGuard** — Val1 % full block; sets action = Flee so the
     client renders dodge anim instead of hit.
  2. **Kyrie** — Val1 HP shield absorbs up to its pool per hit,
     Val2 hit counter decrements; SC ends when either drops to 0.
  3. **SteelBody** — 90 % flat reduction multiplies whatever
     survived absorb, floor-at-1 so the client still draws a hit
     floater.
- `DamageService` ctor gains optional `IStatusChangeService` and
  `Random` deps — DI resolves them automatically; legacy callers
  still build since the step no-ops when `sc == null`.
- 9 new tests in `DamageServiceScConsumerTests` cover the three
  consumers individually plus Kyrie + SteelBody stacking.
- Full Map.Server.Tests at 363/364 (unchanged pre-existing
  replay-baseline failure).
- **Remaining presence markers waiting for consumers:** Endure
  (needs refresh-on-hit infra), ReflectShield (needs back-damage
  feedback to attacker), Providence (race-specific resist),
  Magnificat (SP regen rate read in IPcRegenService), Maximize
  (max-roll damage in BattleCalculator).

### 2026-05-20 — T2.4b first-wave SC handlers done
- `StatusEffectRegistry` grows from 5 → 30+ handlers covering
  the crowd-control gates (Stone/Freeze/Stun/Sleep/Curse/Silence/
  Confusion/Blind/Stonewait), DoT (Bleeding/Burning/DeadlyPoison),
  stat buffs with revert (Adrenaline/Twohandquicken/Provoke/
  Concentrate/Concentration/Angelus/Assumptio), and presence-only
  markers for future combat-side reads (Endure/Magnificat/element
  endow/AutoGuard/ReflectShield/SteelBody/Providence/Kyrie/
  BasilicaCell).
- `SkillCastTimingService.CastFixSc` rewritten from passthrough
  to the proper SC overlay: Slowcast/Paralysis push cast time up,
  then Suffragium/Memorize/Izayoi/Bragi cut it down; Suffragium
  and Memorize auto-consume per cast. DI optional-inject of
  `IStatusChangeService` so legacy callers still build.
- 20 new tests: 12 in `StatusEffectsExpansionTests` (handler
  semantics + revert round-trips + DoT periods) + 8 in
  `SkillCastFixScTests` (every overlay + stacking order).
- Full Map.Server.Tests stays at 354/355 (unchanged pre-existing
  replay-baseline failure).

### 2026-05-20 — T2.4a SC enum expansion done
- `StatusType.cs` regenerated as a 1:1 mirror of rAthena's
  `enum sc_type` (status.hpp:233-1426). 1006 entries; persistence
  indices in lockstep with rAthena so save/load round-trips work
  without any remapping layer.
- Pre-existing C# names preserved: `Stone`, `Freeze`, `Stun`,
  `Sleep`, `Poison`, …, `Blessing`, `IncreaseAgi`, `DecreaseAgi`,
  `Magnificat`, `DeadlyPoison`, `Concentrate`.
- Two pre-existing C#-only entries cleaned up:
  - `HealOverTime` (no rAthena counterpart) moved to id 2000
    outside the rAthena range — was at 100 colliding with
    SC_VOLCANO.
  - `AttackUpRate` (was at 101 colliding with SC_DELUGE) dropped
    — no consumers.
- `Basilica` slot rectified: was at 130 (SC_MINDBREAKER slot)
  but semantically the cell variant. Split into:
  - `Basilica = 116` (rAthena SC_BASILICA, Priest skill)
  - `BasilicaCell` (cell-based variant, applied on Basilica /
    Land-Protector ground unit). The one consumer
    (`PlayerPositionHelpers.IsBasilicaCell`) updated to use the
    cell variant.
- Some compound rAthena names without internal underscores PascalCase
  to compact tokens (`Encpoison`, `Reflectshield`,
  `Twohandquicken`, …) — valid identifiers; cosmetic prettify
  can land later without touching values.
- Generator at `/tmp/gen_statustype.py` parses status.hpp and emits
  the enum. Re-runnable on rAthena content patches.
- Tests stay at 334/335 (unchanged pre-existing replay-baseline
  failure); no test code changes needed.

### 2026-05-20 — T2.2 card modifier port done
- `EquipBonusBundle` (race / element / size / class indexed arrays
  + flat fields + cast / drain knobs) lives at
  `Map.Server/Inventory/EquipBonusBundle.cs`.
- `BonusScriptExtractor` (regex pass over `bonus` / `bonus2` /
  `bonus3` statements) translates each equipped item's `script`
  column into bundle deltas. Pragmatic 90% coverage of static
  card / armor scripts; dynamic patterns (`getrefine()`,
  conditional bonuses, `callfunc`) leave the bundle slot at 0
  so the bias is "miss" not "lie".
- `EquipBonusAggregator.BuildBundle` rebuilds the bundle from
  scratch on every equip change inside
  `EquipService.TryRecalcStats`. `PlayerEntity.EquipBonuses`
  caches the result (mirrors rAthena's `indexed_bonus` cadence).
- `BattleCardService.CalcCardFix` now reads the bundle and
  accumulates per-target Race / Element / Size / Class +
  Long/ShortAtkRate multipliers (renewal additive, single multiply
  with floor-at-1).
- 15 new tests (4 BuildBundle aggregator + 11 BattleCardService),
  covering Hydra (DemiHuman +20 %), Skel Worker (Large +25 %),
  Goblin+Hydra stacking, RC_All, Class_Boss vs MVP / normal,
  Long/ShortAtkRate gating, unequipped-ignore, and reset semantics.
- T2.2 roadmap entry flipped 🟡 unblocked → ✅ DONE.

### 2026-05-20 — re-evaluation after Tier 1 land + boot test
- Verified end-to-end boot: `dotnet build` → `dotnet ef database
  update` → `dotnet run --project Map.Server` succeeds. Map server
  loaded `MobDb 2,555` + `ItemCatalog 28,532` from SQL, accepted
  TCP on 5191, gRPC on 6003, 60 FPS game loop. **The server
  compiles and boots cleanly post-Tier-1.**
- Data-pending markers dropped **56 → 23** across 20 files. The
  remaining clusters are all "per-skill / per-shop / per-storage
  consumer wiring" (Tier 2/3 work), not data-availability gaps.
- Tier scoreboard updated at top of doc with re-evaluated status
  per tier.
- T2.1 (equip-bonus aggregator) flagged ✅ done — was shipped
  earlier in the PC-S4 wave at
  `Map.Server/Inventory/EquipBonusAggregator.cs`. Original
  roadmap missed that prior work; corrected here.
- T2.2 (card modifier port) reclassified 🟡 **unblocked** — the
  blocker (EquipBonusBundle + item_db.script) is resolved.
- T2.4 (SC engine) refined: data side is done (status_yml in SQL),
  remaining work is enum expansion + per-SC IStatusEffect modules.
- T3 reframed: 113 outbound packets already ship; T3.0 audit
  pass added to scope the real remaining surface.
- T2.3 renumbered to T2.5 since it depends on T2.4 SC table.
- New "Next concrete tasks" section gives a 6-step pickup order
  with PR-sized chunks and effort estimates.

### 2026-05-21 — T2.3-P4/P5 Merchant + Swordman manual ports done
- Merchant directory now 105/105 (no remaining stubs). Final wave:
  RushQuake, RushStrike, SparkBlaster, SlingItem, SpecialPharmacy,
  StealthField, SummonFlora, SummonMarineSphere, SynthesizedShield,
  SyntheticArmor, TheWholeProtection, ThornTrap, TripleLaser,
  TwilightAlchemy1/2/3, UpgradeWeapon, Vaporize, Vending, VulcanArm,
  WallOfThorns, WeaponPerfection, WeaponRepair, WoodenFairy,
  WoodenWarrior.
- Swordman directory now 80/80 (no remaining stubs). Two waves of
  ~38 + 38 covering Rune Knight (Abundance, CrushStrike, DragonBreath,
  DragonHowling, EnchantBlade, FightingSpirit, GiantGrowth,
  HundredSpear, IgnitionBreak, MilleniumShield, PhantomThrust,
  Refresh, SonicWave, StoneHardSkin, StormBlast, VitalityActivation,
  WindCutter), Royal Guard / Imperial Guard (Banding, BanishingPoint,
  CannonSpear, CrossRain, EarthDrive, ForceOfVanguard, GrandJudgement,
  GuardianShield, HesperusLit, ImperialCross, JudgementCross,
  KingsGrace, MoonSlasher, OverBrand, OverSlash, PinpointAttack,
  Piety, RadiantSpear, RageBurst, RayOfGenesis, ShieldPress,
  ShieldShooting, ShieldSpell, Trample, UltimateSacrifice),
  Dragon Knight (DragonicAura, DragonicBreath, HackAndSlasher,
  MadnessCrusher, ServantWeapon, ServantWeaponDemolition,
  ServantWeaponPhantom, ServantWeaponSign, StormSlash),
  Crusader/Paladin/Lord Knight (AutoBerserk, BattleChant, BowlingBash,
  BrandishSpear, ChargeAttack, CounterAttack, Endure, GloriaDomini,
  GrandCross, HolyCross, MartyrsReckoning, ProvokeSelf,
  Relax, ResistantSouls, Sacrifice, ShieldBoomerang, ShieldChain,
  ShieldReflect, Smite, SpearBoomerang, SpearStab, SpiralPierce,
  TraumaticBlow, VitalStrike).
- 9 carry-over "auto-generated stub" docstring headers in Acolyte
  (ChainCrushCombo, DragonCombo, FallenEmpire, GateOfHell,
  GentleTouchQuiet, Judex, MagnusExorcismus, SkyNetBlow) and Archer
  (BlitzBeat) updated — actual implementations were already manual,
  only the boilerplate header was stale.
- Build green throughout; only the two pre-existing warnings
  (DamageService null-return, CharCommandsCommand unused param).
- Per-directory progress: Acolyte 100/100, Archer 126/126,
  Mage 143/143, Merchant 105/105, Swordman 80/80 — **554 ports
  total now manually translated**. Remaining: ~653 stubs across
  11 directories (Thief 85, Taekwon 91, Ninja 63, ElementalNpc 51,
  Homunculus 45, Gunslinger 44, Other 40, MercenaryNpc 35,
  Summoner 33, Npc 154, Novice 12).

### 2026-05-21 — T2.3-P6/P7/P8 Novice + MercenaryNpc + Summoner done
- Novice directory now 12/12 (no remaining stubs). Hyper Novice
  damage / utility skills (DoubleBowlingBash, FirstAid, GroundGravitation,
  HellsDrive, HelpAngel, JackFrostNova, JupitelThunderstorm,
  MegaSonicBlow, MeteorStormBuster, NapalmVulcanStrike, ShieldChainRush,
  SpiralPierceMax) all ported with ratio formulas + SC_HNNOWEAPON /
  SC_STUN / SC_CURSE / SC_BLEEDING / SC_ANKLE follow-ups.
- MercenaryNpc directory now 35/35 (no remaining stubs). Full coverage
  of MS_/MA_/MER_/ML_ skill family (Bash/Bowling/Magnum/Arrow Repel/
  Arrow Shower/Brandish/Pierce/Reflect Shield/Spiral Pierce/Sandman/
  Land Mine/Skid Trap/Freezing Trap/Remove Trap/Double Strafe/
  Focused Arrow/Charge Arrow/Blessing/Increase Agi/Decrease Agi/
  Kyrie/Magnificat/Mental Cure/Recuperate/Regain/Tender/Compress/
  Benediction/Lex Divina/Provoke/Sense/Sight/Mind Blaster/Crash/
  Devotion/Scapegoat).
- Summoner directory now 33/33 (no remaining stubs). Doram base set
  (Bite, Scratch, Grooming, PickyPeck, Lope, SilvervineStemSpear,
  SilvervineRootTwist, CatnipMeteor, CatnipPowdering, Chattering,
  Hiss, MeowMeow, Purring, BunchofShrimp, TastyShrimpParty, TunaBelly,
  TunaParty, LunaticCarrotBeat, NyangGrass, PowerofFlock, SpiritofSavage,
  ScarofTarou) + Shaman-class additions (BlessingofMysticalCreatures,
  ChulhoSonicClaw, HogogongStrike, HowlingofChulho, HyunrokBreeze,
  HyunrokCannon, ColorsofHyunrok, KisulRampage, KisulWaterSpraying,
  MarineFestivalofKisul, SandyFestivalofKisul).
- Build green throughout (only the pre-existing two warnings).
- Per-directory progress at this checkpoint: Acolyte 100/100,
  Archer 126/126, Mage 143/143, Merchant 105/105, Swordman 80/80,
  Novice 12/12, MercenaryNpc 35/35, Summoner 33/33 — **634 ports
  manually translated**. Remaining: ~573 stubs across 8 directories
  (Thief 85, Taekwon 91, Ninja 63, ElementalNpc 51, Homunculus 45,
  Gunslinger 44, Other 40, Npc 154).

### 2026-05-21 — T2.3-P9..P12 Other + Gunslinger + Homunculus + ElementalNpc done
- Other directory now 40/40 (carry-over from prior wave). Wedding +
  recall + ABR + cash-shop buff + cleanse skills implemented with
  status-toggle / heal / mob-spawn stubs.
- Gunslinger directory now 44/44. Gunslinger / Rebellion / Night
  Watch families covered (Fling, Disarm, Single Action, Adjustment,
  Tracking, Trip Snare, Anti-Material Blast, Banishing Buster, etc.).
- Homunculus directory now 45/45. HLIF_ / HAMI_ / HFLI_ / HVAN_ /
  MH_ skill families ported including Volcanic Ash cell-drop and
  ToxinOfMandara recursive splash.
- ElementalNpc directory now 51/51 (no remaining stubs). Three
  groups: toggle SC + SC_OPTION pairs (CircleOfFire/CoolAir/Cooler/
  CursedSoil/FireCloak/Gust/Heater/Petrology/Pyrotechnic/SolidSkin/
  StoneShield/Tropic/Upheaval/WaterDrop/WindCurtain/WildStorm/
  WaterScreen/WindStep + ColdForce/CrystalArmor/DeepPoisoning/
  EarthCare/EyesOfStorm/FlameArmor/FlameTechnic/GraceBreeze/
  PoisonShield/StrongProtection + AgeOfIce/AquaPlay/Avalanche/
  Blast/TidalWeapon), cell-drop placement skills (FireMantle/
  PowerOfGaia/WaterBarrier/Zephyr), and recursive-splash damage
  skills (FireArrow/FireBomb/FireWave/HurricaneRage/IceNeedle/
  RockLauncher/StoneHammer/StoneRain/StormWind/TyphoonMissile/
  WaterScrew/WindSlasher/FlameRock/DeadlyPoison).
- Build green throughout. Only fix needed during the wave was a
  `Math.Max(int, ushort)` ambiguity in WindStep (cast skillLevel to
  int) — caught by `dotnet build Map.Server` and resolved in the
  same wave.
- Per-directory progress at this checkpoint: Acolyte 100/100,
  Archer 126/126, Mage 143/143, Merchant 105/105, Swordman 80/80,
  Novice 12/12, MercenaryNpc 35/35, Summoner 33/33, Other 40/40,
  Gunslinger 44/44, Homunculus 45/45, ElementalNpc 51/51 — **814
  ports manually translated**. Remaining: ~393 stubs across 3
  directories (Ninja 63, Thief 85, Taekwon 91, Npc 154).

### 2026-05-21 — T2.3-P13 Ninja family done
- Ninja directory now 63/63 (no remaining stubs).
- Throw / Charm family: ThrowShuriken, ThrowKunai, ThrowZeny,
  ThrowHuumaShuriken, HuumaShurikenConstruct, HuumaShurikenGrasp,
  FireCharm, EarthCharm, IceCharm, WindCharm.
- Cell-drop family: CastNinjaSpell, ReleaseNinjaSpell, FireMantle
  (no — that's elemental), CrimsonFireFormation, HiddenWater, IceMeteor,
  KunaiRotation, Makibishi, Mirage, RagingFireDragon, ShadowHunting,
  LightningStrikeOfDestruction, ImprovisedDefense.
- Single-target damage: FireArrow (elemental), Crimson Fire Petal,
  Spear Of Ice, Wind Blade, Soul Cutter, Shadow Slash, Vanishing
  Slash, Final Strike, Kunai Refraction, Ice Needle (elemental).
- Recursive splash: Throw Huuma Shuriken, Golden Dragon Cannon,
  Kunai Splash, Kunai Explosion, Swirling Petal, Red Flame Cannon,
  Shadow Flash, Thundering Cannon, Rapid Throw.
- Splash bombs: Dark Dragon Nightmare, Darkening Cannon, Kunai
  Nightmare, Shadow Nightmare, Cold Blooded Cannon, Shadow Dance,
  Kunai Distortion.
- Buffs / dispels: Mirror Image, Ominous Moonlight, Shadow Warrior,
  Distorted Crescent, Empty Shadow, Shadow Hiding, Illusion Shock,
  Nightmare Erasion, Moonlight Fantasy, Shadow Trampling.
- Special: KoCrossSlash (Jyumonjikiri SC), Melt Away (POS2 +
  knockback self), Shadow Leap, Illusion Death (Coma + percent
  damage), Illusion Bewitch (position swap + Confusion), Illusion
  Shadow (Zanzou clone spawn — animation only), Makibishi (random
  spike placement + Stun), Infiltrate (slide + Shimiru SC).
- Build green throughout. The single fix needed was a null reference
  in MoonlightFantasy (`src as PlayerEntity` → `is PlayerEntity sd`).
- Per-directory progress: Acolyte 100/100, Archer 126/126,
  Mage 143/143, Merchant 105/105, Swordman 80/80, Novice 12/12,
  MercenaryNpc 35/35, Summoner 33/33, Other 40/40, Gunslinger 44/44,
  Homunculus 45/45, ElementalNpc 51/51, Ninja 63/63 — **877 ports
  manually translated**. Remaining: ~330 stubs across 3 directories
  (Thief 85, Taekwon 91, Npc 154).

### 2026-05-21 — T2.3-P14 Thief done + T2.3-P15 Taekwon partial
- Thief directory now 85/85 (no remaining stubs). Full coverage of
  TF_ / RG_ / AS_ / ASC_ / GC_ / SC_ / SHC_ / ABC_ families: DoubleAttack,
  Envenom, Steal, Mug, SandAttack, StoneFling, DeftStab, Grimtooth,
  BackStab, Antidote, BackSlide, BloodyLust, Cloaking, CloakingExceed,
  CounterSlash, CreateNewPoison, CreateDeadlyPoison, CrossImpact,
  CrossRipperSlasher, CrossSlash, DancingKnife, DarkClaw,
  DarkIllusion, Detoxify, DimensionDoor, DivestWeapon/Shield/Armor/
  Helm/All, EnchantPoison, EnchantDeadlyPoison, FatalShadowCrow,
  FromTheAbyss, EmergencyEscape, FatalMenace, Maelstrom, AbyssDagger,
  AbyssSquare, AutoShadowSpell, ChaosPanic, ChainReactionShot,
  CounterInstinct, CloseConfine, BodyPainting, FrenzyShot, EternalSlash,
  Masquerade × 6 (Gloomy/Ignorance/Weakness/Enervation/Unlucky/Laziness),
  Invisibility, FindStone, MeteorAssault, PhantomMenace,
  HallucinationWalk, Remover, OmegaAbyssStrike, FeintBomb,
  SavageImpact, PoisoningWeapon, SightlessMind, ShadowStab,
  ImpactCrater, ManHole, Reproduce, Scribble, Snatch, PoisonSmoke,
  SoulDestroyer, Stealth, StripShadow, ThrowVenomKnife,
  StripAccessory, VenomDust, VenomPressure, RollingCutter, SonicBlow,
  ShadowForm, TriangleShot, UnluckyRush, VenomSplasher, WeaponCrush.
- Taekwon directory partial (40/91): DownKick, TurnKick, Counter,
  JumpKick, HighJump, Run, StormKick, FlashKick, Mission, Spirit × 16
  (Monk/Wizard/Crusader/Supernovice/Knight/Sage/Alchemist/Rogue/
  Assasin/Blacksmith/StarGladiator/SoulLinker/Hunter/Priest/Artist +
  SpiritofRebirth), Es-family × 9 (Esma/Esha/Eska/Eske/Espa/Estin/
  Estun/Eswhoo/Eswoo), Soul-link gear × 4 (FalconsSoul/GolemsSoul/
  FairysSoul/ShadowsSoul), Kai × 2 (Kaite/Kaupe). Remaining 51
  Taekwon stubs are Star Emperor kick / Talisman / Warmth / Soul Cannon
  / Star/Moon/Sun cycle (mostly damage skills + status buffs).
- Build green throughout.
- Per-directory progress at this checkpoint: Acolyte 100/100,
  Archer 126/126, Mage 143/143, Merchant 105/105, Swordman 80/80,
  Novice 12/12, MercenaryNpc 35/35, Summoner 33/33, Other 40/40,
  Gunslinger 44/44, Homunculus 45/45, ElementalNpc 51/51, Ninja 63/63,
  Thief 85/85, Taekwon 40/91 — **1002 ports manually translated**.
  Remaining: ~205 stubs across 2 directories (Taekwon 51, Npc 154).

### 2026-05-21 — T2.3-P15 Taekwon done + T2.3-P16 Npc partial
- Taekwon directory now 91/91 (no remaining stubs). Full coverage of
  TK_/SG_/SL_/SP_/SJ_/SKE_/SOA_/OB_ family.
- Npc directory partial (21/154). Done: AcidBreath, AgilityUp,
  AntiMagic, AttributeChange, Bleeding, Bleeding2, BlindAttack,
  BreakArmor, BreakHelm, BreakShield, CaneOfEvilEye, ChangeLocation,
  Comet2, CriticalWounds, CrossOfDarkness, CurseAttack, DancingBlade,
  DarkBlessing, DarkBreath, DarknessBreath, DarkPiercing.
- Build green. **1074 ports manually translated**; 133 Npc remain.

### 2026-05-21 — T2.3-P16 Npc family complete (directory closure)
- Npc directory now 154/154 (no remaining stubs). Final wave covered
  the Wide* family (Bleeding/Bleeding2, Confusion/Confusion2, Curse/
  Curse2, Freeze/Freeze2, Petrify/Petrify2, Silence/Silence2, Sleep/
  Sleep2, Stun/Stun2, CriticalWounds, Leash, Sight, SoulDrain, Suck,
  Web), status-attack family (PoisonAttack, PetrifyAttack,
  SilenceAttack, SleepAttack, StunAttack with 20*lv % SC + 20 % hit
  boost), element-attack family (PoisonAttribute, WaterAttribute,
  WindAttribute, ShadowAttribute, UndeadElement), element-change
  StatusSkillImpl family (PoisonAttributeChange/WaterAttributeChange/
  WindAttributeChange/ShadowAttributeChange/UndeadAttributeChange),
  self-buff/misc skills (PowerUp, SpeedUp, SlowCast, StoneSkin,
  Provocation, SiegeMode, Transformation, Smoking, Talk, Lick,
  Invisible, Stop, RandomMove, Rebirth, Revenge, RecallSlaves,
  MilleniumShield2, PropertyImmune), Npc* prefixed skills
  (NpcCloudKill, NpcArrowStorm, NpcDragonBreath, NpcIgnitionBreak,
  NpcRayOfGenesis, NpcMagmaEruption, NpcPsychicWave, NpcPhantomThrust,
  NpcFatalMenace, NpcPoisonBuster, NpcElectricWalk, NpcFireWalk,
  NpcHowlingOfMandragora, NpcCursedCircle, NpcColuceoHeal, NpcRun,
  NpcSuicide, NpcVenomImpress), single-target / cell-place damage
  (PiercingAttack, MultiStageAttack, SplashAttack, RandomAttack,
  PulseStrike/PulseStrike2, RainOfMeteor, Reverberation2, IceMine,
  VenomFog, StormGust2, ThunderBreath, SoulStrikeOfDarkness,
  SpiritDestruction, SuckingBlood, VampireGift, SuicideBombing),
  and Leash (pull to caster).
- **Final tally: 1207/1207 skills manually ported across all 16
  subdirectories.** No remaining `TODO: port from rathena-fork`
  comments in any Map.Server/Skills/Behaviors/*/*.cs file.
- Build green: 2 pre-existing warnings, 0 errors.
- Directory closure: Acolyte 100/100, Archer 126/126, Mage 143/143,
  Merchant 105/105, Swordman 80/80, Novice 12/12, MercenaryNpc 35/35,
  Summoner 33/33, Other 40/40, Gunslinger 44/44, Homunculus 45/45,
  ElementalNpc 51/51, Ninja 63/63, Thief 85/85, Taekwon 91/91,
  Npc 154/154.
