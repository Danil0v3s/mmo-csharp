# Gameplay roadmap · 2026-05-20

The interop roadmap (`ROADMAP.md`) is complete — Login / Char / IPC
ship at 100% parity. Map server has a wide service surface
(175 DI registrations, ~406 C# files) but a lot of it is
canonical entry points whose backend data hasn't ported. This
doc reorganises the remaining work around **vertical slices** —
each slice ships an end-to-end playable experience.

## Where we actually stand (live-validated)

DHXJ client (PACKETVER 20220401) → Login → Char select →
Map handoff → spawn at Prontera → walk → all works. From there:

| Loop | Status | Notes |
|---|---|---|
| **Login + char select + spawn** | ✅ | live-validated end-to-end |
| **Walking + view broadcast** | ✅ | AOI + visibility correct |
| **Auto-attack mobs** | ✅ | base damage + flee + crit + element fix |
| **Mob death → EXP → level up** | ✅ | party-share path works |
| **Loot from floor** | ✅ | three-tier ownership windows |
| **NPC click → dialog → close** | ✅ | for NPCs authored in `scripts/dist/main.js` |
| **Shop buy / sell** | ✅ | Discount/Overcharge wired |
| **Storage open / move / close** | ✅ | full kafra surface |
| **Equip / unequip + stat sync** | ✅ | costume + shadow slots map cleanly |
| **Drop / throw item** | ✅ | nodrop mapflag honored |
| **Public chat + @-commands** | ✅ | 30+ GM commands, ~200 stubbed |
| **Whisper / party / guild chat** | ✅ | end-to-end wire |
| **Player trade** | ✅ | atomic exchange state machine |
| **Sit / stand** | ✅ | regen bonus applied |
| **Use consumable** | ✅ | through `IItemUseService` |
| **Warp portals** | ✅ | same-map + cross-map |
| **PvP within nopvp/gvg flags** | ✅ | M-H1/M-H2 enforced |
| **Heartbeat + reconnect** | ✅ | session lifecycle |

That's about **20 gameplay loops** that fire correctly today. A
real player on a Mac with the DHXJ client can autologin, walk
into prontera, hit a poring, loot a Jellopy, sell it, save at the
inn — without surprises.

## What's a hollow shell

The wide service surface from pc.cpp / battle.cpp / atcommand.cpp
audits exposes the rAthena canonical entry points but several
have NO backend data, so the visible behavior is shallow:

| Subsystem | Service shape | Behavior gap |
|---|---|---|
| **Skill resolvers** | 5 strategies (Weapon/Magic/Heal/Status/Misc) | Only ~8 skills (Bash, Fire Bolt, Heal, Bless, etc.) actually resolve; rest hit the default path |
| **Status changes** | `IStatusChangeService` + registry | 5 of ~80 SCs registered (Poison, Bless, IncAGI, DecAGI, HoT) |
| **Card bonus aggregator** | `IBattleCardService.CalcCardFix` exists | Returns input unchanged — no `pc_bonus` opcode runtime |
| **Mob skill use** | `IMobSkillCondition` strategies | 3 of 27 conditions evaluate (Always, MyHpLT, RudeAttacked); most mobs swing only |
| **Cart inventory** | move ops work in-memory | No cart packet wire-up, no DB hydration on session enter |
| **Quest tracking** | service shape exists | No `quest_db` loaded, no `ZC_QUEST_*` packets, no quest NPCs |
| **Mail / Auction / Vending / BuyingStore** | char-server data exists | No map-side packet flow |
| **Achievement / Reputation** | service shape exists | No data, no UI |
| **Marriage / Adoption / Fame** | service shape exists | No `@marry` ceremony NPC; rank list isn't aggregated |
| **Instance / Duel / WoE / BG** | none | Each is a top-level subsystem (~1k lines) |
| **rAthena script engine** | TypeScript replacement | Our engine is real but content is limited to what we author; rAthena's `.txt` scripts don't run |

The honest summary: **everything in the "20 loops" table works
because the data backing it (mob_db, item_db, map cache, skill_db
for the 8 ported skills) IS loaded.** The hollow shells are
hollow because the *data* (skill_db at scale, quest_db,
attendance.yml content, mob_skill_db wiring, vending packets)
isn't connected, not because the C# layer is broken.

## The three structural blockers

Three subsystems unblock a disproportionate amount of gameplay.
Everything else is small and parallelisable; these three are
load-bearing.

### Blocker 1 — Skill resolver coverage

Most player progression happens through skills. We have ~8
resolvers. To reach "play a Knight through level 50," we need
~30 skills (Sword Mastery passive, Two-Hand Sword, Bash,
Magnum Break, Endure, Provoke, Bowling Bash, Spear Boomerang,
Spear Stab, Two-Hand Quicken, Brandish Spear, etc.).

**Unblock path:** port `skill.cpp:skill_castend_*` per-skill
branches. Each branch is small (~30 lines); the work is volume,
not complexity. Strategy-pattern resolver registry is already
in place — adding a skill is one `ISkillResolver` class.

### Blocker 2 — `pc_bonus` opcode runtime + equip aggregator

Cards, refines, weapon properties, and "bonus" scripts on items
all feed through the rAthena `pc_bonus`/`bonus2`/`bonus3` opcode
system. Without it, an item that says `bonus bStr,5;` does nothing
when equipped. This is the silent reason combat numbers feel
"off" — we apply weapon ATK and refine but not the card stat
modifiers.

**Unblock path:** port `script.cpp` opcodes for the bonus family
(~40 opcodes — bStr, bAgi, bAddRace, bAddEle, bAtkPercent,
bAtkRate, bMaxHp, bMaxSp, etc.) routing to fields on
`BattleStats`. Extend `BattleStats` to carry the
`indexed_bonus` aggregator arrays. Hook into
`EquipBonusAggregator` so re-equip recomputes the totals.

### Blocker 3 — Status effects beyond the starter 5

Combat depth comes from SCs — Stun shuts you down, Bleeding
drains HP, Endure breaks stun-lock, Provoke + Inc AGI buff
allies, Curse / Silence / Confusion are big debuffs. Currently
only 5 of ~80 SCs port. Mobs that try to apply Stun via skill
just deal raw damage.

**Unblock path:** register 20-30 SC handlers in
`StatusEffectRegistry`. Each handler is a small class
(OnStart / OnTick / OnEnd). Pattern matches what we did for
the 5 already shipped.

## Vertical slices (the path going forward)

Each slice ships an end-to-end playable experience. After each
slice the live client can do something new. Slices stack — they
don't conflict, so they can be reordered by priority but each
one stands alone.

### VS-1 — "Novice → Swordman first-class" (≈1 week)

Goal: A fresh character can clear the Novice Training Ground,
reach base level 10, change to Swordman, equip a Sword, and use
Bash on a Poring.

Required:
- TypeScript NPC content: Novice Trainer, Job Changer (Sword
  Master), Prontera Inn save NPC, basic Tool Dealer
- Skill resolver for `SM_BASH` (already partially ported), plus
  `NV_BASIC`, `SM_SWORD` (passive — no resolver needed),
  `SM_HP_RECOVERY`, `SM_PROVOKE`, `SM_ENDURE`
- SC handlers: `SC_PROVOKE`, `SC_ENDURE`
- Job change happy-path through `IJobChangeService.Change(7)` —
  already works mechanically; needs the NPC trigger

Test: live client login → walk to Sword Master NPC → take job
quest → fight in training ground → return → change job → train
Bash → hit poring with Bash for visibly higher damage.

### VS-2 — "Knight playthrough" (≈1-2 weeks)

Goal: A Swordman can level to job 50, change to Knight, ride a
Peco, hit a Bowling Bash for AoE.

Required:
- Skill resolvers: `SM_MAGNUM`, `SM_BASH` lv 10, `KN_TWOHANDQUICKEN`,
  `KN_BOWLINGBASH`, `KN_SPEARMASTERY`, `KN_PIERCE`, `KN_BRANDISHSPEAR`,
  `KN_RIDING` (already works via PC option service), `KN_AUTOCOUNTER`
- SC handlers: `SC_TWOHANDQUICKEN`, `SC_MAGNUMBREAK`,
  `SC_AUTOCOUNTER`
- AoE resolver pattern (one cell + ring of cells) — the
  IDelayedDamageService.DamageArea helper already exists
- Job change to Knight (class id 7)
- Knight skill tree validation in `IPlayerSkillService.Validate`

Test: live client warp to job NPC → become Knight → equip
two-handed sword → mount Peco via `@mount` → cast Bowling Bash
on a mob group → see splash damage on adjacent cells.

### VS-3 — "Bonus opcode runtime + cards meaningful" (≈1 week)

Goal: Equip a sword with a Skel Worker card → see +5 ATK.
Equip a card combo → see the right bonus apply.

Required:
- `pc_bonus` opcode handler in script engine (we have ClearScript
  V8 host already)
- Extend `BattleStats` with `IndexedBonus`: race / element /
  size / class arrays (long[] of size N each)
- Wire `IBattleCardService.CalcCardFix` to read the aggregator
- Equip on-load triggers card script execution
- Test items: at least 10 common cards
  (Skel Worker, Andre, Vadon, Drainliar, ...)

Test: `@item 4054 1` (Skel Worker card) → `@item 1101 1`
(Sword) → slot card via `pc_insert_card` → equip → swing on a
Plant-race mob → see card damage modifier.

### VS-4 — "Party + chat polish" (≈1 week)

Goal: Two players form a party, share EXP, see each other's HP
bars, chat in /p.

Required:
- Party invite / accept packets (server-side party service
  already exists)
- ZC_NOTIFY_HP_TO_GROUPM packet for shared HP bars
- Party member list refresh on join / leave / disconnect
- Party EXP share already works through `IPartyShareService`;
  needs UI verification with two clients

Test: two clients on same map → `/p` invite → accept → both kill
a mob → both gain shared EXP → leave → return to solo split.

### VS-5 — "Quest engine MVP" (≈1-2 weeks)

Goal: Talk to the kafra-side quest NPC → take a "kill 10
porings" quest → see counter advance → return → get reward.

Required:
- `quest_db.yml` loader (rAthena format)
- ZC_QUEST_LIST / ZC_QUEST_NOTIFY_EFFECT / ZC_QUEST_UPDATE_INFO
  packets
- Quest state on `PlayerEntity` (Active quest ids + per-quest
  counters)
- Persistence via `quest` table (entity exists)
- Mob-kill hook in `IExpService.GainExp` increments the counter
- TS NPC scripts pick up quest state via the existing
  `PlayerContext`

Test: live client → quest NPC dialog → take quest → kill mob →
see counter advance in client UI → return → claim reward.

### VS-6 — "Player economy" (≈2 weeks)

Goal: Set up a vending shop, browse other vendors, send mail
with item attachment.

Required:
- Vending packet flow (CZ_REQ_OPENSTORE_FAR / ZC_STORE_ENTRY)
- BuyingStore packet flow (analogous)
- Mail packets map-side (ZC_MAIL_NEW_NOTIFY already in repo)
- Auction packets map-side
- Char-server already has the data side for mail/auction

Test: two clients → one opens vending stall → other walks up,
buys an item → first receives zeny. Send mail with attached
item, recipient logs in, opens mail, takes attachment.

### VS-7 — "Mob skill use" (≈1 week)

Goal: A Munak casts Stone Curse on you. Drainliar applies
Bleeding. A Verit casts on group.

Required:
- Mob skill cast path: `mob_skill_db.yml` already loaded;
  resolver dispatch already in registry
- More MSC_* condition evaluators (we have 3, need ~20):
  MSC_FRIENDHPLTMAXRATE, MSC_AFTERSKILL, MSC_SKILLUSED,
  MSC_CASTTARGETED, MSC_CLOSEDATTACKED, MSC_LONGRANGEATTACKED
- SC handlers for the mob skills (Stone Curse, Bleeding, etc.)

Test: walk into a map with a Munak mob → it casts NPC_STONE
on you → you turn to stone → freed by Cure or timer.

### VS-8 — "Instances" (≈3-4 weeks — largest)

Goal: Party of 4 enters Endless Tower, fights through floors,
gets MVP loot.

Required:
- `instance.cpp` port (instance management, party allocation,
  per-instance map duplication)
- Map dynamic loading (currently we load 2 fixed maps; need
  on-demand instance maps)
- ZC_INSTANCE_CREATE / DESTROY / NOTIFY packets
- Instance state persistence

This is the biggest single piece of remaining work and unblocks
endgame.

### VS-9 — "WoE / BG" (≈3-4 weeks)

Goal: Guild War of Emperium runs on a schedule, players siege
castles.

Required:
- Battleground core (team membership, queue, score)
- WoE script (castle owner, agit_start / agit_end events)
- Guild treasure room
- Emperium mob with guild-only ownership

Together with VS-8, this is the "endgame" tier — only relevant
once VS-1..VS-7 are in.

## Suggested ordering

The right path is:

1. **VS-1** (Novice → Swordman) — proves the content loop end-to-end
2. **VS-3** (bonus opcode runtime) — biggest structural fix; unblocks
   real combat numbers for the rest of the slices
3. **VS-2** (Knight playthrough) — second progression tier
4. **VS-7** (mob skill use) — adds combat depth
5. **VS-4** (party + chat polish) — group play
6. **VS-5** (quest engine) — content depth
7. **VS-6** (player economy) — economy depth
8. **VS-8** (instances) — endgame
9. **VS-9** (WoE / BG) — competitive endgame

That ordering yields a continuously-improving playable build —
each slice ships a thing the player can do that wasn't possible
before. VS-3 is intentionally before VS-2 because the bonus
runtime makes Knight gear actually feel like Knight gear.

## What this leaves out (and why)

- **macro detector** — premium-server feature, not required for
  any gameplay loop
- **achievement system** — nice-to-have but doesn't unblock play
- **navi.cpp** — auto-navigation is a UI convenience; client
  has its own pathing fallback
- **searchstore** — vending-store search, ships alongside VS-6
- **mapreg / global script vars** — needed for some scripts but
  rAthena-script-engine-shaped work; we author equivalents in TS
- **most of mob.cpp's exotic AI modes** — slave, looter, assist
  modes can ship piecemeal as content needs them

## Tracking

Each VS gets its own doc under
`.agents/migrations/gameplay/VS-N-<name>.md` when it starts.
Same audit shape as pc-parity.md / battle-parity.md — table of
required items / status / wave plan / history. The
`rathena-parity` skill (`.claude/skills/rathena-parity/`) drives
the per-file C# parity passes; this roadmap drives the
*gameplay-experience* axis.
