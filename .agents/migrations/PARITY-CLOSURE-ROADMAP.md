# Parity-closure roadmap · 2026-05-20

Companion to [CODE-COMPLETENESS-ROADMAP.md](CODE-COMPLETENESS-ROADMAP.md).

**Status:** every rAthena map .cpp public function has a canonical C#
entry point. Most are working; ~56 inline "data-pending" markers
point at the remaining shallow spots. This document orders the
close-the-gap work so it can be picked up systematically.

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

### T2.1 — Equip-bonus aggregator

`PlayerEntity.Equip*` already lists equipment; we need a per-PC
aggregator that walks equipment + reads each item's
`bonus`/`bonus2`/`bonus3` script + accumulates the runtime numbers
(card resist by race, atk vs size, drain hp, varcastrate,
fixcastrate, addvarcast, addfixcast, skillcastrate, skillvarcast,
skillfixcast, autospell, addrace, addclass, …).

- Surface as `IEquipBonusAggregator.Get(pc)` returning a
  `EquipBonusBundle` record.
- Re-runs on every equip / unequip / break / strip.
- **Acceptance:** Equipping a Hunter Bow + a Hydra-carded Bow shows
  +20 % damage vs Demi-Human in `BattleCalculator.CalcWeaponAttack`
  test.
- **Dependents unblocked:** `IBattleCardService` real (currently
  pass-through), `IBattleEffectsService.Drain` real,
  `IBattleReflectService` real, cast time bonuses in
  `SkillCastTimingService`.

### T2.2 — Card modifier port

Port `battle_calc_cardfix` (battle.cpp:711) — the SC race/size/
element/class accumulator. Currently `BattleCardService.CalcCardFix`
returns input damage unchanged.

- Reads `EquipBonusBundle` indexed by race/size/element/class.
- Honors NK flags (`SkillNk.NoCardFix` skip).
- **Acceptance:** Damage diff against rAthena replay reference
  within ±2 % for 10 sample skill casts.

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

Wire T1.3's SC table into the cast pipeline. Plugin point:
`StatusChangeService.Apply(SC_TYPE, val1..val4, duration)`.

- Per-SC `IStatusEffect` modules (start, tick, end).
- Cast-time / delay scaling reads SC table
  (`SkillCastTimingService.CastFix` already calls SC overlay —
  empty today; honor `SC_SUFFRAGIUM`, `SC_MEMORIZE`, `SC_SLOWCAST`,
  `SC_PARALYSIS`, `SC_IZAYOI`).
- **Acceptance:** Suffragium halves next cast; Slowcast doubles
  cast; Endure refreshes on hit; Freeze drops movement.

**Tier 2 milestone:** Combat damage matches rAthena replay to within
5 % on the test fixture; all 56 data-pending markers in `Combat/` +
`Skills/` resolve.

---

## Tier 3 — Wire packets (visibility)

Combat correctness without packets is invisible to the client. Port
the outbound `clif_*` emitters next so the player can *see* the
work from Tiers 1–2.

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
