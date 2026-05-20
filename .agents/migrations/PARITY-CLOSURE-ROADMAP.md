# Parity-closure roadmap · 2026-05-20

Companion to [CODE-COMPLETENESS-ROADMAP.md](CODE-COMPLETENESS-ROADMAP.md).

**Status:** every rAthena map .cpp public function has a canonical C#
entry point. Most are working; ~56 inline "data-pending" markers
point at the remaining shallow spots. This document orders the
close-the-gap work so it can be picked up systematically.

## Principle

Port **shared foundations first**, then per-file behavior. The reason:
60 % of the data-pending markers across all services bottom out at
4–5 root dependencies (skill_db YAML, item_db YAML, SC table, equip-
bonus aggregator, outbound packet emitters). Port a foundation once,
every dependent service flips from stub → real.

Each tier below ends with **acceptance criteria** + an estimate of
how many downstream `data-pending` markers it resolves. Pick the
next tier when the current one's acceptance criteria are met.

## Tier 1 — Data loaders (foundation)

These unblock the most consumers. None of them are gameplay-visible
on their own; they fill the catalogs every other system reads from.

### T1.1 — `skill_db.yml` YAML loader  *(biggest single unblock)*

- Read `db/re/skill_db.yml` (~3 500 skills, ~30 columns) into
  `SkillDefinition`.
- Loader writes to `Core.Database` skill_db table via existing
  `SkillDbLoader.FromEntity`.
- Once seeded, `ISkillDb.Get*` returns real values for every skill;
  ~all `SkillCastTimingService` / `SkillRequirementService` /
  `SkillUnitService` data-pending entries flip to real.
- **Acceptance:** `dotnet ef database update` produces a populated
  `skill_db` table; `ISkillDb.Count` reports ~3500; pre-existing
  starter-set tests still pass.
- **Dependents unblocked:** ~12 skill-* services.

### T1.2 — `item_db.yml` YAML loader

- Read `db/re/item_db.yml` (~26 000 items) into `ItemEntity` + the
  `Script` / `OnEquip` / `OnUnequip` columns.
- Includes equip-attributes (job/gender mask, refine cap, view sprite,
  trade restrictions, weight, slots).
- **Acceptance:** `ItemCatalog.GetByNameId(501)` returns Red Potion
  with `HealAmount` populated; `bonus` script column non-empty for
  a sample of carded items.
- **Dependents unblocked:** `ItemDbService` (most gate predicates
  consult item attrs), `IBattleCardService.CalcCardFix`,
  `IBattleEffectsService.Drain`.

### T1.3 — `status_change.yml` + `SC_*` table population

The biggest single port. rAthena ships ~250 SCs (status changes).
We currently have ~5 (Blessing, IncreaseAgi, DecreaseAgi, Poison,
HealOverTime).

- Extend `StatusType` enum with every rAthena `SC_*` (200+ entries).
- Port `status_change_start` / `status_change_end` per-SC behavior
  from `status.cpp:status_change_start` (the giant switch around
  lines 9000–13000).
- Most SCs have 3 fields: duration, stat delta, end-effect — they
  fit one `StatusEffectRegistry` row each. ~30 SCs need real per-
  tick behavior (Poison / Bleeding / Burning / ManaPower / etc.).
- **Acceptance:** Casting Endure applies SC_ENDURE; SC_FREEZE blocks
  movement; Maya Purple Card triggers SC_PRESERVE.
- **Dependents unblocked:** Skill additional/counter effects,
  battle reflect path, autospell rolls, frostjoke proc, status-block
  damage gate, slowcast/suffragium/memorize cast-time scaling.

### T1.4 — Remaining YAML loaders

Once T1.1–T1.3 land, these are mechanical:

- `mob_db.yml` — already partially loaded; complete drops + skills.
- `quest_db.yml` → `QuestService` real.
- `achievement_db.yml` → `AchievementService` real.
- `instance_db.yml` → `InstanceService` real.
- `bg_db.yml` → `BattlegroundService` queue rules.
- `cashshop_db.yml` → `CashShopService.BuyList` real.
- `channels.conf` → `ChannelService.ReadConfig` real.
- `produce_db.yml` + `arrow_db.yml` → `SkillProductionService` real.
- `homunculus_db.yml` + `homun_exp_db.yml` → `HomunculusService` real.
- `mercenary_db.yml` → `MercenaryService` real.
- `pet_db.yml` — already largely loaded; complete evolution +
  autobonus columns.
- `random_options.yml` + `combos.yml` + `enchant.yml` → ItemDb aux.
- `abra_db.yml` + `magic_mushroom_db.yml` + `reading_spellbook.yml`
  + `arrow_db.yml` → `SkillAuxDatabases` real.
- **Acceptance:** Every audit doc that lists "YAML loader pending"
  flips to "loaded; <N> entries".

**Tier 1 milestone:** ~30 of the 56 data-pending markers go away.

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
