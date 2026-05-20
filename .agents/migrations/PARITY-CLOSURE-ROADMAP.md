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

## Tier scoreboard (re-evaluated 2026-05-20)

| Tier | Theme | Status | Notes |
|---|---|---|---|
| T1 | Data loaders → SQL + JSON | ✅ **DONE** | 52 `_db` SQL-backed, 19 conf-JSON with schemas, IBattleConfigService overlays at boot |
| T2.1 | Equip-bonus aggregator | ✅ **DONE** | `Map.Server/Inventory/EquipBonusAggregator.cs` — exists from PC-S4 wave |
| T2.2 | Card modifier port | ✅ **DONE** | `BattleCardService.CalcCardFix` reads `PlayerEntity.EquipBonuses`; `EquipBonusBundle` + `BonusScriptExtractor` ship; Hydra-card test exercises +20% vs Demi-Human |
| T2.3 | Per-skill behavior | 🟢 hierarchy + **94 plugins** | Full SkillImpl OOP hierarchy ported from rathena-fork (WeaponSkillImpl / StatusSkillImpl / RecursiveDamageSplashSkillImpl). One file per skill, organized by job-class subdirectory (`Behaviors/Swordman/Bash.cs`, `Behaviors/Wizard/StormGust.cs`, …). Covers: first/second class (59), transcend (24), 3rd class (10), 4th class seed (1). Adding a skill = one new file + one DI line. |
| T2.4 | SC engine completion | 🟡 enum full / behavior ~30 of ~250 + combat hooks | T2.4a + T2.4b done: enum mirrors all 1006 SC ids; first wave of handlers (CC gates / DoT / stat buffs / cast-time SCs) registered; `CastFixSc` honors Suffragium/Memorize/Slowcast/Paralysis/Izayoi/Bragi; `DamageService` reads SteelBody / Kyrie / AutoGuard on every hit. Long-tail SC handlers ride the same registry pattern. |
| T3 | Wire packets | 🟡 113 emitters exist | Per-handler audit needed; the surface is bigger than initially scoped |
| T4 | IPC + persistence | ❌ pending | 73 `IIntifService` stubs — biggest single block of behavior gap |
| T5 | Per-file deep audits | ❌ pending | The pc/battle/skill style tables — 38 files left |
| T6 | Endgame content | ❌ pending | WoE, instances, BG queues, pet evolution, vending autotrade |

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
