# Atcommand parity · 2026-05-19

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

## Permission enforcement

Migrated from `MinGroupId` int to a richer model:
- Each command knows its name.
- The registry's `CanInvoke(session, command)` consults
  `IPlayerGroupConfig.IsAllowed(groupId, commandName)` (resolves
  inheritance) OR `IPermissionService.Has(session, all_commands)`.
- `command_enable` permission allows commands disabled by default
  per the rAthena convention.

## History

### 2026-05-19 — initial parity sweep
- atcommands.yml + groups.yml loaders ship.
- 8 default groups loaded with inheritance.
- 31 permissions wired.
- `@help` / `@commands` / `@charcommands` work.
- Logging table populated on each invocation when `LogCommands`.
- ~50 atcommands implemented or stubbed; remaining 240 documented
  as "subsystem pending" in this doc.
