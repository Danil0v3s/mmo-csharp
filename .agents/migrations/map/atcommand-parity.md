# Atcommand parity · 2026-05-22 (T9.A — per-fn rollup)

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

### Stubbed (subsystem missing — returns "not yet ported")

`auction`, `mail`, `instance`, `marry`, `divorce`, `clone`, `slaveclone`,
`evilclone`, `mercenary`, `summon`, `disguise`, `undisguise`, `cash`,
`points`, `noks`, `allowks`, `autoloot`, `alootid`, `autoloottype`,
`autotrade`, `channel`, `clan`, `clanspy`, `partyspy`, `guildspy`,
`bg_*`, `duel`, `accept`, `reject`, `invite`, `leave`, `quest`,
`achievement`, `petfriendly`, `pethungry`, `petrename`, `hatch`,
`makeegg`, `produce`, `refine`, `grade`, `request`, `feelreset`,
`feel`, `hate`, `homevolution`, `homreset`, `homshuffle`, `hominfo`,
`addperm`, `rmvperm`, `accinfo`, `addfame`, `addwarp`, `adjgroup`,
`adopt`.

### Backend pending (documented as "feature pending" — large list)

The remaining ~200 commands wait on subsystems still to port:
`instance.cpp`, `duel.cpp`, `mercenary.cpp` ext, `clan.cpp` map side,
`mail.cpp` map side, `auction.cpp` map side, `vending.cpp`,
`buyingstore.cpp`, `bg.cpp` (battlegrounds), `cashshop.cpp`,
`marriage` (in pc.cpp), `quest.cpp` ext, `achievement.cpp` ext.
Each stub points at the parent doc.

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

### Stubbed (subsystem-pending — 132 ❌)

Tracked under their parent subsystem; each stub returns "not yet ported". The
stubs retire as the parent service ports (see e.g. WoE wave closing the
`bg_*` family). Notable groups:

| Subsystem | Stub count | Examples |
|---|---:|---|
| `battleground.cpp` | 9 | `bg*`, `bgstart`, `bgend`, `bginvite` |
| Reload suite | 13 | `reloaditemdb`, `reloadmobdb`, `reloadscript`, … |
| `pet.cpp` ext | 6 | `hatch`, `petfriendly`, `petrename`, `birthpet` |
| `homunculus.cpp` ext | 6 | `homevolution`, `homreset`, `homshuffle`, `hominfo` |
| Marriage (`pc.cpp` ext) | 5 | `marry`, `divorce`, `adopt`, `famerank`, `addfame` |
| `duel.cpp` ext | 5 | `duel`, `invite`, `accept`, `reject`, `leave` |
| Disguise / model (`pc.cpp` ext) | 5 | `disguise`, `undisguise`, `fakename`, `model`, `size` |
| World-state toggles | 7 | `day`, `night`, `clearweather`, `doom`, `doommap`, `raise`, `raisemap` |
| `instance.cpp` | 4 | `instance`, `instancelist`, `instancesignup`, `instancenoavailable` |
| Mob cleanup | 4 | `killmonster`, `killmonster2`, `cleanmap`, `cleanarea` |
| Permission admin | 4 | `addperm`, `rmvperm`, `adjgroup`, `accinfo` |
| Inventory mgmt | 6 | `dropall`, `storeall`, `itemreset`, `clearcart`, `clearstorage`, `cleargstorage` |
| Stat / skill point | 6 | `statall`, `traitpoint`, `statuspoint`, `skillpoint`, `allskill`, `lostskill` |
| Info / search | 11 | `mobinfo`, `iteminfo`, `whodrops`, `whomap`, `whomap2`, `whogm`, `idsearch`, … |
| Channel / clan / cashshop / quest / achievement / vending / buyingstore / autotrade / mercenary / mail / auction | 12 | one per subsystem |
| Other (jail, npc, broadcast variants, mute, feel, hate, refine, grade, produce, repair, identifyall, …) | 34 | per-stub |

### Other rAthena fns not yet wired (104 ❌)

Per-command ACMD_FUNC handlers from atcommand.cpp that haven't been
registered yet (no `[GmCommand]` class). These are roughly 40% of the
288 ACMD_DEF entries; they overlap heavily with the "subsystem-pending"
group above and will retire as each parent ports.

## Coverage summary

| Bucket | ✅ | ⚠️ | ❌ | Total |
|---|---|---|---|---|
| Dispatch / registry / permission | 10 | 0 | 0 | 10 |
| Meta commands | 3 | 0 | 0 | 3 |
| Implemented per-command handlers | 40 | 1 | 0 | 41 |
| Stubbed (subsystem-pending) | 0 | 0 | 132 | 132 |
| Other rAthena fns (unregistered handlers) | 0 | 0 | 104 | 104 |
| **Totals** | **53** | **1** | **236** | **290** |

## Permission enforcement

Migrated from `MinGroupId` int to a richer model:
- Each command knows its name.
- The registry's `CanInvoke(session, command)` consults
  `IPlayerGroupConfig.IsAllowed(groupId, commandName)` (resolves
  inheritance) OR `IPermissionService.Has(session, all_commands)`.
- `command_enable` permission allows commands disabled by default
  per the rAthena convention.

## History

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
