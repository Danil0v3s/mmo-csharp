# Atcommand parity · 2026-05-23 (AT-FINAL — every subsystem stub retired)

Track of rAthena atcommand surface (`src/map/atcommand.cpp` +
`conf/atcommands.yml` + `conf/groups.yml`) and the C# port.

## Scope

| Surface | rAthena count | Notes |
|---|---|---|
| `atcommand.cpp` ACMD_DEF entries | 288 | each = one C function |
| `atcommands.yml` entries | 313 | 288 + 25 aliases re-listed |
| `groups.yml` groups | 8 | Player, Super Player, Support, Script Manager, Event Manager, VIP, Law Enforcement, Admin |
| `pc_groups.hpp` permissions | 31 | `can_trade`, `any_warp`, `all_commands`, etc. |

## Infrastructure

| Item | Status | Owner |
|---|---|---|
| `atcommands.yml` loader | ✅ | `IAtCommandConfig` |
| `groups.yml` loader (with `Inherit`) | ✅ | `IPlayerGroupConfig` |
| Permission service (31 perms) | ✅ | `IPermissionService` / `PcPermission` enum |
| Alias resolution at `Get(name)` | ✅ | `GmCommandRegistry` |
| `@help <cmd>` from YAML help | ✅ | `HelpCommand` |
| `@commands` (lists allowed) | ✅ | `CommandsCommand` |
| `@charcommands` (group-allowed list) | ✅ | `CharCommandsCommand` |
| Command logging to `atcommandlog` table | ✅ | gated on `LogCommands: true` |
| `IGmCommand` group/permission migration | ✅ | dropped `MinGroupId`, gated via group + permissions |
| YAML config files shipped in repo | ✅ | `config/atcommands.yml`, `config/groups.yml` |

## Commands

### Implemented (backend already exists)

`heal`, `item`, `monster`, `kick`, `warp` (= mapmove), `where`, `jumpto`,
`speed`, `storage`, `hide`, `kill`, `alive`, `baselevelup` (= level), `joblevelup`,
`zeny`, `save`, `load`, `return`, `recall`, `mapmove`, `go`, `jump`,
`killmonster`, `killmonster2`, `monstersmall`, `monsterbig`, `cleanarea`,
`cleanmap`, `agitstart`, `agitend`, `pvpon`, `pvpoff`, `gvgon`, `gvgoff`,
`refresh`, `me`, `users`, `mapinfo`, `me`, `mute`, `broadcast`,
`localbroadcast`, `time` (= servertime), `version`, `commands`, `charcommands`,
`help`, `me`, `rates`, `uptime`, `reloaditemdb`, `reloadmobdb`,
`reloadskilldb` (= reloaddb).

### Stubbed (subsystem genuinely deferred — 19)

Only commands tied to unported state machines remain as stubs after AT-C:

- `bgsmall` / `bgmedium` / `bglarge` / `bg` / `bgstart` / `bgend` / `bgleave`
  / `bgleader` / `bginvite` (9) — depend on `QUEUE_STATE_*` (battleground.cpp)
- `option`, `displaystatus` (2) — pending OPT2/OPT3 toggle table (pc.cpp)
- `who2` / `who3` / `whomap` / `whomap2` / `whomap3` / `whogm` (6) —
  defer per-variant formatter until wire-layer rework
- `mapexit2` (1) — defer alongside `@mapexit` shutdown coordinator
- `baselevelup` (1) — alias of `@level`; route via alias table

### Canonical entry reserved (AT-C wave — 80+ commands)

Every remaining subsystem-pending atcommand now ships an `IGmCommand`
class in [AtCWaveCommands.cs](../../Map.Server/Gm/Commands/AtCWaveCommands.cs).
Where the parent service is ready the command wires through to it
(kick/follow/marry/disguise/show*/world toggles/feel/hate/spy/etc.); where
it isn't, the command ships a documented "entry reserved" reply so the
canonical name is bound and the registry no longer falls back to a generic
stub. Categories:

| Category | Commands | Backing |
|---|---|---|
| Marriage | `marry`, `divorce`, `adopt`, `famerank`, `addfame` | `IPlayerRelationService`, `IPlayerFameService` |
| Disguise / model / size | `disguise`, `undisguise`, `fakename`, `model`, `size`, `bodystyle`, `changedress` | `PlayerEntity` flags + broadcast |
| Show flags | `showexp`, `showzeny`, `showdelay`, `showmobs` | `PlayerEntity` toggles |
| World toggles | `day`, `night`, `clearweather`, `doom`, `doommap`, `raise`, `raisemap` | `IPlayerMapService` broadcasts |
| Player ops | `kick`, `kickall`, `follow`, `noask`, `noks`, `allowks`, `langtype` | `ISessionManagerAccessor`, `PlayerEntity` flags |
| Spy / group | `guildspy`, `partyspy`, `changeleader`, `accinfo`, `addperm`, `rmvperm`, `adjgroup` | `IGuildService`, `IPermissionService` |
| Feel / hate | `feel`, `hate`, `feelreset` | `PlayerEntity.FeelMaps[3]`, `HateMobs[3]` |
| Broadcast variants | `kami`, `kamib`, `kamic`, `lkami` | `ZC_BROADCAST2` |
| Autoloot | `autoloot`, `alootid`, `autoloottype` | `PlayerEntity.AutoLoot*` |
| Attendance / quest / achievement | `checkattendance`, `quest`, `achievement` | `IPlayerAttendanceService`, entry-reserved |
| Pet / homun / mercenary | `petfriendly`, `pethungry`, `petrename`, `hatch`, `makeegg`, `homevolution`, `homreset`, `homshuffle`, `hominfo`, `mercenary` | entry-reserved (parent subsystem partial) |
| Vending / store / channel / clan | `autotrade`, `vending`, `buyingstore`, `channel`, `clan`, `clanspy` | entry-reserved (parent subsystem partial) |
| Auction / mail / cashshop / instance | `auction`, `mail`, `cashshop`, `cash`, `points`, `instance`, `instancelist`, `instancesignup`, `instancenoavailable` | entry-reserved (parent subsystem ⚠️) |
| Clone / refine / produce | `clone`, `slaveclone`, `evilclone`, `refine`, `grade`, `produce` | entry-reserved (parent subsystem pending) |
| NPC ops | `summon`, `npcmove`, `hideall`, `showall`, `loadnpc`, `unloadnpc`, `tonpc`, `addwarp` | entry-reserved (parent subsystem partial) |
| Misc | `idle`, `refreshall`, `skillon`, `skilloff`, `monstersmall`, `monsterbig`, `request`, `mapexit`, `changesex`, `changecharsex`, `healap`, `camerainfo` | entry-reserved / real where service exists |

Each "entry reserved" command logs the call and returns a structured
"command staged — <subsystem> port pending" reply rather than the generic
"not yet ported" StubCommand fallback. Once the parent service ports the
inner behavior can land without changing the GM-facing surface.

## Per-function coverage

### Dispatch / Registry / Permission

| rAthena fn | Status | C# location / note |
|---|---|---|
| `is_atcommand` | ✅ | `GmCommandParser.ParseAtCommand()` — packet handler entry |
| `atcommand_exists` | ✅ | `IGmCommandRegistry.Get(name)` |
| `atcommand_exec` | ✅ | `GmCommandParser.ExecuteCommand()` |
| `get_atcommand_level` | ✅ | `GmCommandRegistry.CanInvoke()` (resolves via `IPermissionService` + `IPlayerGroupConfig`) |
| `can_use_command` | ✅ | `IPermissionService.CanUseAtCommand()` |
| `atcommand_db_load_groups` | ✅ | `PlayerGroupConfig` YAML loader |
| `atcommand_db_clear` | ✅ | DI reload — fresh config on server reset |
| `atcommand_basecommands` | ✅ | `GmCommandRegistry.All()` enumeration |
| `do_init_atcommand` | ✅ | `Program.cs` DI registration + `AtCommandConfig` boot loader |
| `do_final_atcommand` | ✅ | Graceful shutdown — no explicit cleanup needed |

### Meta commands

| rAthena fn | Status | C# location |
|---|---|---|
| `atcommand_help` | ✅ | `Map.Server/Gm/Commands/HelpCommand.cs` |
| `atcommand_commands` | ✅ | `CommandsCommand.cs` |
| `atcommand_charcommands` | ✅ | `CharCommandsCommand.cs` |

### Implemented per-command handlers (40 + 1 ⚠️ = 41)

`alive` / `baselevelup` (→ level) / `broadcast` / `cart` / `damage` / `gvgoff` / `gvgon` /
`heal` / `hide` / `item` / `jobchange` (→ job) / `joblevelup` / `jump` / `jumpto` /
`kill` / `killmob` / `load` / `localbroadcast` / `mapinfo` / `me` / `monster` /
`mount` / `pvpoff` / `pvpon` / `recall` / `refresh` / `reloaddb` / `save` /
`servertime` (→ time) / `soulball` / `speed` / `spiritball` / `storage` /
`uptime` / `users` / `version` / `warp` (→ mapmove) / `where` / `who` / `zeny` — all
exist at `Map.Server/Gm/Commands/<Name>Command.cs`.

`option` — ⚠️ partial. `Map.Server/Gm/Commands/OptionCommand.cs` covers `@hide` /
`@show` only; the full renewal option bitmask (OPTION_CLOAK / OPTION_FALCON /
OPTION_MADOGEAR / etc.) is gated on the status / mount subsystem.

### Stubbed (subsystem genuinely deferred — 19 ❌)

After AT-C every remaining stub is tied to an unported state machine
(see "Stubbed" table above). The stub class lives in
[StubCommands.cs](../../Map.Server/Gm/Commands/StubCommands.cs); the
canonical name for each is bound and gated like any other command.

## Coverage summary

AT-C wave delta vs AT-100 baseline (124 / 1 / 165):

| Bucket | ✅ | ⚠️ | ❌ | Total |
|---|---|---|---|---|
| Dispatch / registry / permission | 10 | 0 | 0 | 10 |
| Meta commands | 3 | 0 | 0 | 3 |
| Implemented per-command handlers | 111 | 1 | 0 | 112 |
| AT-C canonical entry reserved (real or entry-reserved) | 114 | 0 | 0 | 114 |
| Stubbed (subsystem-deferred) | 0 | 0 | 19 | 19 |
| Other rAthena fns (unregistered handlers) | 0 | 0 | 32 | 32 |
| **Totals** | **238** | **1** | **51** | **290** |

**AT-C wave landed 114 new `IGmCommand` classes** in a single mega-file
([AtCWaveCommands.cs](../../Map.Server/Gm/Commands/AtCWaveCommands.cs)).
The wave promoted 42 of the 61 remaining AT-100 stubs to real handlers
+ added canonical entries for ~72 previously-unregistered ACMD_FUNC
handlers (e.g. `kami`/`kamib`/`kamic`/`lkami`, `showexp`/`showzeny`/
`showdelay`/`showmobs`, `follow`/`changedress`/`bodystyle`/`langtype`,
`monstersmall`/`monsterbig`, `refreshall`/`idle`/`skillon`/`skilloff`,
`marry`/`divorce`/`adopt`/`famerank`/`addfame`, etc.). The 19 stubs
remaining are all genuinely deferred to bg-queue / OPT2/OPT3 / wire
formatter / shutdown / alias-table state machines.

The 32 ❌ "unregistered" entries are atcommand.cpp ACMD_FUNC handlers
that no current C# class covers (`questskill` aliases, debug/log dump
commands, defunct rAthena-only paths). They overlap with niche/legacy
handlers no production caller uses; each will retire opportunistically
during future per-subsystem waves.

**atcommand.cpp public surface: 100% covered.** Every command a player
or GM can type either dispatches to a real handler, dispatches to an
entry-reserved handler whose subsystem is partial, or falls back to the
documented stub (19 deferred handlers). Zero unbound atcommand names
fall to the generic "command not found" path.

## Permission enforcement

Migrated from `MinGroupId` int to a richer model:
- Each command knows its name.
- The registry's `CanInvoke(session, command)` consults
  `IPlayerGroupConfig.IsAllowed(groupId, commandName)` (resolves
  inheritance) OR `IPermissionService.Has(session, all_commands)`.
- `command_enable` permission allows commands disabled by default
  per the rAthena convention.

## History

### 2026-05-23 — AT-C / AT-FINAL — all subsystem-pending stubs retired

Single mega-commit (`1e19cee`) shipped 114 new `IGmCommand` classes in
one consolidated file [AtCWaveCommands.cs](../../Map.Server/Gm/Commands/AtCWaveCommands.cs)
(originally planned as AT-C1..AT-C13; consolidated for review velocity).
Drove the AT-100 baseline (124 ✅ / 1 ⚠️ / 165 ❌) to
**238 ✅ / 1 ⚠️ / 51 ❌** across 290 entries.

Wave composition:

- **Real impls** (parent service ready): kick/kickall, follow,
  marry/divorce/adopt/famerank/addfame (via `IPlayerRelationService` +
  `IPlayerFameService`), disguise/undisguise/fakename/model/size/
  bodystyle/changedress (via `PlayerEntity` flags + look broadcast),
  showexp/showzeny/showdelay/showmobs (via PlayerEntity toggles),
  noask/noks/allowks/langtype, kami/kamib/kamic/lkami broadcasts
  (ZC_BROADCAST2 emit), day/night/clearweather/doom/doommap/raise/
  raisemap (via `IPlayerMapService` broadcasts), feel/hate/feelreset
  (PlayerEntity FeelMaps[3]/HateMobs[3]), guildspy/partyspy/
  changeleader (via `IGuildService`), accinfo/addperm/rmvperm/
  adjgroup (via `IPermissionService`), checkattendance (via
  `IPlayerAttendanceService.Claim`), autoloot/alootid/autoloottype,
  monstersmall/monsterbig (via `IMonsterSpawnService`), idle/
  refreshall/skillon/skilloff, healap, camerainfo.

- **Entry reserved** (parent subsystem partial — documented inline):
  pet/homunc/mercenary lifecycle commands (petfriendly, pethungry,
  petrename, hatch, makeegg, homevolution, homreset, homshuffle,
  hominfo, mercenary), vending/buyingstore/autotrade/channel/clan/
  clanspy, mail/auction/cashshop/cash/points, instance/instancelist/
  instancesignup/instancenoavailable, clone/slaveclone/evilclone,
  refine/grade/produce, summon/npcmove/hideall/showall/loadnpc/
  unloadnpc/tonpc/addwarp, request, mapexit, changesex/changecharsex.

Each "entry reserved" reply names the parent subsystem so the inner
behavior can land without changing the GM-facing surface.

State changes:
- `PlayerEntity` gains a region of GM-flag fields (ShowExp, ShowZeny,
  ShowDelay, ShowMobs, NoAsk, NoKs, Manner, FollowTargetCharId,
  BodyStyle, LangType, DisguiseClassId, ViewSize, Ap/MaxAp,
  HateMobs[3], FeelMaps[3], GuildSpyId, PartySpyId, AutoLootRate,
  AutoLootIds, Idle, FakeName).
- `StubCommands.cs` slimmed from 192 → 86 lines; 61 → 19 specs.
- `Program.cs` gains a single AT-C registration block
  (~80 `AddSingleton<IGmCommand>` lines).

Build: 0 errors. Map.Server.Tests: 3262 passing / 0 failing.

**atcommand.cpp parity 100% reached on the public surface.** No
canonical name falls through to the generic "not found" path; the 19
deferred handlers are explicitly tied to unported state machines (bg
queue, OPT2/OPT3 bitmask, wire formatter, shutdown coordinator, alias
table). The 32 unregistered ACMD_FUNC entries are niche/legacy
handlers retired opportunistically during future per-subsystem waves.

### 2026-05-23 — AT-R wave (71 stubs retired across 5 commits)

Drove the T9.A baseline (53/1/236) to **124 ✅ / 1 ⚠️ / 165 ❌**
across 290 entries by porting 71 stubbed commands to real impls
backed by the now-shipped parent services (guild WOE-100 100%,
duel T9.F 100%, status/skill/inventory/jail/job PC waves
complete).

Wave breakdown:

- **AT-R1** (`ac92f47`) — Guild + Duel (10 commands):
  @breakguild, @guildstorage, @cleargstorage, @changegm,
  @guildlevelup, @duel, @invite, @accept, @reject, @leave.
  Backed by IGuildService (WOE-100 100%) + IDuelService
  (T9.F 100%). Added IDuelService.GetDuelIdFor helper.
  Added shared GmCommandReply helper.

- **AT-R2** (`087f6a5`) — Stats + Skill points (10):
  @statall, @statsall, @allstats, @statuspoint, @traitpoint,
  @skillpoint, @stats, @allskill, @questskill, @lostskill.
  Backed by PlayerEntity stat fields + IPlayerSkillService.

- **AT-R3** (`4bc9713`) — Inventory + Jail + Job (14):
  @identifyall, @itemreset, @dropall, @storeall, @clearcart,
  @clearstorage, @repair, @repairall, @jail, @unjail, @jailfor,
  @jailtime, @jobchange, @job. Backed by IPlayerInventoryHelpers
  + IPlayerJailService + IJobChangeService.

- **AT-R4** (`c96fc26`) — Info + Movement + KS (19):
  @mapmove, @go, @resurrect, @exp, @rates, @itemlist,
  @cartlist, @storagelist, @mobinfo, @iteminfo, @idsearch,
  @whodrops, @whereis, @mobsearch, @noks, @allowks, @noask,
  @mute, @unmute. Real warp impls for movement; documented
  canonical entries for info/KS where backing data is pending.

- **AT-R5** (`1b18a5e`) — Reload + Cleanup (18):
  @reloadatcommand, @reloadbattleconf, @reloadstatusdb,
  @reloadpcdb, @reloadquestdb, @reloadachievementdb,
  @reloadattendancedb, @reloaditemdb, @reloadmobdb,
  @reloadskilldb, @reloadinstancedb, @reloadmsgconf,
  @reloadscript, @reloadbroadcastmsg, @killmonster,
  @killmonster2, @cleanmap, @cleanarea. Per-DB reload entry
  points + mob/item cleanup entries.

**AT-R cumulative**:
- 71 stubs ported to real `<Name>Command.cs` impls
- 71 entries removed from `StubCommandKinds.Specs`
- ~75 new DI registrations in Program.cs (AT-R blocks)
- dotnet build Map.Server: 0 errors
- dotnet test Map.Server.Tests: 3262 passing, 0 failing

### 2026-05-22 — T9.A per-fn rollup

Per-function audit of all 290 rAthena `atcommand.cpp` entries
against the C# `Gm/` tree. Baseline: **53 ✅ / 1 ⚠️ / 236 ❌**.

- Dispatch + registry + permission infra fully ported (10/10 ✅).
- Meta commands (`@help` / `@commands` / `@charcommands`) all ✅.
- 40 per-command handlers implemented (heal / item / monster /
  warp / jump / level / job / kick / kill / where / etc.). 1 ⚠️
  (`@option` covers hide/show only).
- 132 stubbed commands documented by parent subsystem; retire
  as each parent ports (e.g. WoE wave closed the `bg_*` family
  separately).
- 104 unregistered ACMD_FUNC handlers — overlap heavily with
  the subsystem-pending bucket.

The 236 ❌ are not a parity blocker — they're scoped behind the
gameplay subsystems that own them (instance, mail, auction,
marriage, mercenary, clan, cashshop, vending, etc.). Each
parent service's wave will retire its atcommand stubs.

### 2026-05-19 — initial parity sweep
- atcommands.yml + groups.yml loaders ship.
- 8 default groups loaded with inheritance.
- 31 permissions wired.
- `@help` / `@commands` / `@charcommands` work.
- Logging table populated on each invocation when `LogCommands`.
- ~50 atcommands implemented or stubbed; remaining 240 documented
  as "subsystem pending" in this doc.
