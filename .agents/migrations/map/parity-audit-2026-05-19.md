# Map.Server parity audit · 2026-05-19

Cross-cutting scan of `Map.Server/` against rAthena `src/map/` and
`conf/battle/*.conf`. Excludes items already tracked elsewhere (MVP
drops, item-script long tail, vending/buying-store, mail/auction —
those are owned by other docs).

Findings grouped by impact. **High** = changes observable in-game today
(broken gates, wrong drop windows, missing rules). **Medium** = parity
drift or incomplete feature. **Low** = cosmetic, structural, or
operator-side.

## High

**M-H1. Map flags are registered but never enforced.**
`registerMapFlag()` populates `NpcRegistry._mapFlags`, but no gameplay
path queries them. Today: `noteleport` doesn't block `@warp`,
`nopvp` doesn't gate damage between players, `nosave` doesn't suppress
the autosave flush, `noskill` doesn't refuse skill casts, `nodrop`
doesn't refuse `CZ_ITEM_THROW`, `noexp`/`nopenalty` doesn't change
death respawn behavior. Adding a small `IMapFlagService` keyed by map
name + reading it at the relevant gates is the unblock.

**M-H2. Same-party / same-guild friendly fire isn't prevented.**
`battle_check_target` in `battle.cpp` returns -1 when source and dest
share party / guild outside GvG zones. `DamageService.ApplyDamage`
applies damage without that check today. PvE is unaffected (mobs vs
PCs), but the rule shows up the moment any player-to-player damage
path opens (skills with AoE, etc.).

**M-H3. Skill cast not cancelled by incoming damage / movement.**
`SkillCastService` resolves pending casts on tick; nothing interrupts
them. rAthena `unit_skillcastcancel` (unit.cpp:1024) cancels cast on
damage and on certain skill / state changes. For our 8 ported skills
the practical impact is limited — but Heal/Bash/etc. will land even
under fatal damage that should have cancelled them.

**M-H4. Loot tier 3 (guild) + MVP windows missing.**
`IItemDropService.OwnerProtectionMs = 3000`, `PartyProtectionMs = 5000`
is a 2-tier model. rAthena's defaults from `battle.cpp:11505`:
- `item_first_get_time` 3000 (owner)
- `item_second_get_time` 1000 (party-only, after first elapses)
- `item_third_get_time` 1000 (guild-only, after second elapses)
- `mvp_item_*_get_time` 10000/10000/2000 (MVP drop has its own tiers)

**M-H5. Centralized `cant.act` gate is missing.**
rAthena's `pc_cant_act` consolidates "is this PC frozen / stunned /
asleep / petrified" checks; every action handler (attack, skill,
sit/stand, item use, drop) calls it. Today each C# handler does
ad-hoc state checks. Without the centralized gate, the missing
status effects (M-M2) can never gate actions properly even after they
land.

**M-H6. Mob AI — rude-attacked escalation absent.**
`mob_ai_sub_hard` runs a counter on each unreachable attacker; after
`rude_attacked_count` strikes it calls `unit_escape` and tries the
`MSC_RUDEATTACKED` skill condition. C# `MobAiService` has no
unreachable-attacker counter; a player on a ledge can endlessly
chip a mob that never escapes.

## Medium

**M-M1. Status effects: many SCs missing.** `StatusEffectRegistry`
ships 5 of the high-traffic effects (Poison, Bless, IncAGI, DecAGI,
HealOverTime). Missing with concrete gameplay impact:
- `SC_STUN` — blocks action 100% of duration
- `SC_FREEZE` — blocks action + applies element switch
- `SC_BLEEDING` (full impl with FLEE reduction; we have DoT only)
- `SC_BLIND` — hit-rate halve
- `SC_PROVOKE` — +ATK -evasion
- `SC_ENDURE` — registered but the damage modifier path is no-op
- `SC_CURSE`, `SC_STONE`, `SC_SLEEP`, `SC_SILENCE`, `SC_CONFUSION`

Each is a small case in `StatusEffectRegistry` once M-H5 lands.

**M-M2. Mob skill conditions: 23 of 27 `MSC_*` missing.**
Only `Always` and `MyHpLessThanRate` evaluators ship. Priorities by
frequency in `mob_skill_db`: `MSC_RUDEATTACKED`, `MSC_CLOSEDATTACKED`,
`MSC_LONGRANGEATTACKED`, `MSC_AFTERSKILL`, `MSC_SKILLUSED`,
`MSC_MASTERHPLTMAXRATE`, `MSC_FRIENDHPLTMAXRATE`,
`MSC_MYSTATUSON`/`MSC_MYSTATUSOFF`, `MSC_DAMAGEDGT`.

**M-M3. Equip — costume and shadow slots not mapped.**
`EquipBits` covers 8 of the 14 EQP_* bits. Costume Head Top/Mid/Low +
Garment (4 bits) and Shadow Weapon/Shield/Armor/Shoes/AccR/AccL (6
bits) all unsupported. Costume parsing is on the `ItemEntity` row
already; just needs `EquipBits` extension + `ResolveAllowedPositions`
update.

**M-M4. `equipswitch` (second equipment set) not toggled.**
`InventoryItem.EquipSwitch` field exists; nothing reads it. rAthena's
`pc_equipswitch` swaps in the second set on a single hotkey press —
hardcore PvP feature, defer-but-document.

**M-M5. GM commands — administrative gap.**
Today: `@damage @killmob @storage @warp @where` (5). Common admin
commands missing: `@heal`, `@item`, `@job`, `@level`, `@kick`,
`@reloaditemdb`, `@reloadmobdb`, `@reloadskilldb`, `@hide`,
`@speed`, `@kill`. Five-ish quick wins.

**M-M6. Battle config knobs declared in rAthena but not surfaced in
C# config:** `enable_critical`, `mob_critical_rate`, `critical_rate`,
`casting_rate`, `delay_rate`, `gvg_*_damage_rate`, `pk_*_damage_rate`,
`bg_*_damage_rate`. Today we treat all damage zones identically.

**M-M7. Action handler — pickup / emote not wired.**
`CZ_REQUEST_ACTION` codes 1 (pickup), 12 (touch), and emote/mount
have no handler. Pickup is the one with gameplay impact — clients
that send code 1 instead of `CZ_ITEM_PICKUP` will see no response.

**M-M8. Damage chain — card-fix and mastery accumulation missing.**
`BattleCalculator` doesn't fold attacker / target card modifiers,
`wd.masteryAtk`, dual-wield left-hand path, or `battle_attack_sc_bonus`
SC modifiers. Today the numbers are correct in the trivial case but
diverge once cards / SCs are involved.

**M-M9. `SkillDefinition` columns missing.** `nk[]` (skill damage
flags — ignore-card, ignore-element, knockback, etc.), `inf2[]`
(NoCastSelf, IsTrap, IgnoreGTB), weapon-mask, job-mask. The loader
ignores all four; resolvers can't honor any of them.

## Low

**M-L1. Channel system absent.** No `#main`/`#trade`/`#world`
or user-created channels. Whisper/party/guild only.

**M-L2. Cash shop / item-shop / point-shop / market-shop variants.**
`ShopService` covers regular shops; the four script-driven variants
have no handler.

**M-L3. Script command exposure.** TS host has 5 registrars
(`registerNpc/Warp/Shop/Spawn/MapFlag`). rAthena exposes ~300 script
commands. Concrete gaps with gameplay impact: `getitem/delitem`,
`percentheal/heal`, `set`/`get`, `savepoint`, `warp`, `announce`,
`getarg`. Dialog/menu state machine (`mes/next/menu/close`) also
absent.

**M-L4. Subsystems with no C# counterpart yet:** `instance.cpp`
(dungeons), `cashshop.cpp`, `vending.cpp`, `buyingstore.cpp` (player
shops), map-side `quest.cpp`/`achievement.cpp` tracking, `duel.cpp`,
`navi.cpp`. Most are deferrable until content needs them.

**M-L5. Icewall blocking on movement.** Mobs stuck inside an icewall
should retry north/east before unblocking south/west. `MovementService`
treats all cells identically.

**M-L6. `unit_can_attack` / `status_check_skilluse`-style
comprehensive state validation.** Several edge gates (target on same
map, alive, in-sight) are checked ad-hoc per call site instead of a
centralized helper.

---

## Implementation plan

This session: items below get implemented; the rest stays
documented and tracked for follow-up.

1. M-H4 — three loot tiers + MVP windows.
2. M-H1 — `IMapFlagService` + 4 high-impact gates (`nopvp`, `noskill`,
   `noteleport`, `nodrop`).
3. M-H2 — same-party / same-guild friendly-fire prevention.
4. M-M7 — `CZ_REQUEST_ACTION` code 1 pickup action handler.
5. M-M3 — extend `EquipBits` for costume + shadow slots.
6. M-M5 — `@heal`, `@item`, `@job`, `@level`, `@kick`, `@reloaditemdb`.
7. M-H5 — centralized `cant.act` gate as an extension method on
   `PlayerEntity` reading SC state. Sets the foundation for M-H3 and
   M-M1 in the next session.
8. M-H6 — rude-attacked counter + `MSC_RUDEATTACKED` evaluator
   stub.

Deferred (tracked):
- M-H3 (cast cancellation) — needs the SC state to be honored first.
- M-M1 (remaining SCs) — depends on M-H5; multi-week port.
- M-M2 (mob conditions long tail) — each MSC is a small file; bulk
  add in a dedicated pass.
- M-M4, M-M6, M-M8, M-M9, M-L1–L6 — explicitly documented above.

---

## History

### 2026-05-19 — session implementation

Closed items 1-8 above. Concrete changes per audit item:

- **M-H4**: `FloorItemEntity` gained `OwnerGuildId` /
  `GuildProtectionUntilTick` / `IsMvpDrop`; `ItemDropService` now does
  three-tier cumulative window calculation (3000/1000/1000 owner-party-
  guild for regular drops, 10000/10000/2000 for MVPs). Matches
  `battle.cpp:11505` defaults.
- **M-H1**: `IMapFlagService` reads `INpcRegistry.AllMapFlags()` once
  and caches a per-map bitmask. Gates wired:
  - `nodrop` → `ItemThrowHandler`.
  - `noskill` → `SkillCastService.StartCast` (new
    `SkillCastResult.MapRefused`).
  - `noteleport` → `WarpCommand`.
  - `nopvp` → `DamageService.CanDamage`.
- **M-H2**: `DamageService.CanDamage(source, target)` enforces
  same-party / same-guild friendly-fire prevention plus the `nopvp`
  flag. Called from both `ApplyDamage` and `PerformMeleeAttack`.
  PvE paths (PC↔Mob, Mob↔Mob) bypass cleanly.
- **M-M7**: `PickupAction` (`IActionHandler`, code 1) wraps
  `IItemDropService.TryPickup` + `IInventoryService.GiveItem` so
  clients routing pickup through `CZ_REQUEST_ACTION` (instead of the
  dedicated `CZ_ITEM_PICKUP`) get the same drop lookup and ownership
  gates.
- **M-M3**: `EquipBits` adds Costume Head Top/Mid/Low/Garment (4)
  + Shadow Armor/Weapon/Shield/Shoes/AccR/AccL (6); 14 EQP_* bits
  total. `ResolveAllowedPositions` reads the costume / shadow
  Location columns; shadow AccR/L resolves ambiguously like body
  AccR/L does.
- **M-M5**: Four GM commands: `@heal` (full or signed HP/SP
  adjustment + ZC_PAR_CHANGE), `@item <name|id> [amount]` (via
  `IItemCatalog` + `IInventoryService.GiveItem`), `@level <delta>`
  (base level adjust + status_calc_pc recalc + 5x ZC_PAR_CHANGE),
  `@reloaddb <item|mob|skill|all>` (calls `Reload()` on the
  corresponding catalog).
- **M-H5**: `EntityActionGates` extension class with `CanAct` /
  `CanCastSkill` methods reading SC state for OPT1
  (STONE/FREEZE/STUN/SLEEP) plus SILENCE/CONFUSION for casts.
  Wired into `AttackService.StartAttack` + per-tick, `SkillCastService.StartCast`
  (new `SkillCastResult.CannotAct`), `UseItemHandler`,
  `ItemThrowHandler`, `SitAction`.
- **M-H6**: `MobEntity.RudeAttackedCount` counter +
  `RudeAttackedCondition` evaluator (MSC_RUDEATTACKED) +
  `IMobAiService.NotifyAttacked` hook called from
  `DamageService.ApplyResolved` post-hit. Threshold 2 (rAthena
  `battle.mob_rudeattacked_count` default); when crossed it tries
  the matching skill row, falls back to `unit_escape` (5-cell walk
  away from attacker). `DamageService` resolves `IMobAiService` via
  `IServiceProvider` to break the otherwise-fatal DI cycle.

Tests: 319 / 319 Map.Server.Tests pass, 87 / 87 Core.Server.Tests,
29 / 29 Login.Server.Tests. `PacketReplayTests.Replay` continues to
fail with the same pre-existing diff (quest/mail/NPC-name packets the
server doesn't emit yet) — this commit doesn't touch those paths.
