# rAthena scripting — language & runtime reference

Distilled enumeration of rAthena's NPC scripting system. Source of truth: [rathena/src/map/script.cpp](/Volumes/1TB/Projetos/rathena/src/map/script.cpp) (~28k lines), [script.hpp](/Volumes/1TB/Projetos/rathena/src/map/script.hpp), [script_constants.hpp](/Volumes/1TB/Projetos/rathena/src/map/script_constants.hpp), [doc/script_commands.txt](/Volumes/1TB/Projetos/rathena/doc/script_commands.txt) (canonical command reference), and the corpus under [rathena/npc/](/Volumes/1TB/Projetos/rathena/npc/).

This doc is **reference material** — what rAthena does, not what we will do. The Lua migration scope lives in [README.md](README.md).

---

## 1. File / top-level syntax

NPC `.txt` files contain tab-separated top-level declarations. These are the only legal forms at file scope; script code goes inside `{ ... }` bodies.

```
<map>,<x>,<y>,<f>   script    <name>{::<unique>}   <sprite>,{ <body> }
<map>,<x>,<y>,<f>   script    <name>               <sprite>,<xs>,<ys>,{ <body> }   // with OnTouch area
<map>,<x>,<y>,<f>   warp      <name>               <xs>,<ys>,<dstmap>,<dx>,<dy>
<map>,<x>,<y>,<f>   warp2     <name>               <xs>,<ys>,<dstmap>,<dx>,<dy>    // triggers on hidden players too
<map>,<x>,<y>,<f>   duplicate(<orig>)              <newname>   <sprite>
-                   script    <name>               -1,{ <body> }                   // floating, no map position
-                   shop      <name>               <sprite>,<itemid>:<price>,...
-                   cashshop  <name>               <sprite>,<itemid>:<cashprice>,...
-                   itemshop  <name>               <sprite>,<costitem>:<discount>,<itemid>:<price>,...
-                   pointshop <name>               <sprite>,<costvar>:<discount>,<itemid>:<price>,...
<map>,<x>,<y>,<f>   marketshop <name>              <sprite>,<itemid>:<price>:<stock>,...
function            script    <name>               { <body> }                     // global function
<map>               mapflag   <flag>{,<value>}
<map>,<x>,<y>,<xs>,<ys>  monster   <name>          <mob_id>,<amount>,<respawn1>,<respawn2>{,<event>}
```

Optional state: `script(CLOAKED)`, `script(HIDDEN)`, `script(DISABLED)`. Display name can carry `::UniqueName` for cross-NPC event addressing (`donpcevent "DisplayName::OnFoo"`). Sprite -1 = invisible, 111 = invisible-but-clickable terrain.

Files are discovered through `.conf` import lists (`npc/re/scripts_main.conf` includes `scripts_warps.conf`, `scripts_monsters.conf`, etc.).

## 2. Body syntax

**Control flow:** `if/else`, `while`, `do { } while`, `for`, `switch/case/default/break`, `continue`, `goto <label>`, labels (`L_foo:` or `OnFoo:`), `end`, `return`, `return <expr>`.

**Operators** (C-like precedence): `?:`, `||`, `&&`, `|`, `^`, `&`, `== != < <= > >=`, `<< >>`, `+ - * / %`, prefix `! ~ - ++ --`, postfix `++ --`, compound assignments (`+= -= *= /= %= &= |= ^= >>= <<=`). `+` concatenates strings. Array index `[n]`.

**Comments:** `//` and `/* */`.

**Strings:** double-quoted only. Embedded markup is interpreted by the client, not the parser:
- `^RRGGBB` color codes
- `<ITEM>name<INFO>id</INFO></ITEM>`, `<NAVI>`, `<URL>`, `<QUEST>`, `<MSG>id</MSG>`, `<TIPBOX>`

**Blocks:** `{ }`. Scope is lexical per block, but only `.@` variables are actually scope-local (see §3).

## 3. Variables — the most foreign feature

A variable is `<prefix><name>{$}{[index]}`. The prefix encodes **scope + lifetime + persistence**. Trailing `$` makes it a string.

| Prefix | Scope | Lifetime | Persisted? | Backing store |
|---|---|---|---|---|
| *(none)* | per-character | permanent | yes | `char_reg_num_db` / `char_reg_str_db` |
| `@` | per-character | session | no | memory |
| `#` | per-account (this login server) | permanent | yes | `acc_reg_num_db` / `acc_reg_str_db` |
| `##` | per-account (global, login server) | permanent | yes | `global_acc_reg_*` |
| `$` | global | permanent | yes | `mapreg` table |
| `$@` | global | session | no | memory |
| `.` | per-NPC | until reloadscript | no | NPC struct |
| `.@` | per-call-frame | function/callsub scope | no | stack |
| `'` | per-instance | instance lifetime | conditionally | instance struct |

All scopes support arrays. Arrays are **sparse** — `.@a[100000] = 1` does not allocate 100k slots; missing indices read as `0` / `""`. `getarraysize()` returns highest-set + 1.

**Parameter variables** look like identifiers but bind to player struct fields: `BaseLevel`, `JobLevel`, `Hp`, `MaxHp`, `Sp`, `Zeny`, `Class`, `Sex`, `Str/Agi/Vit/Int/Dex/Luk`, `Karma`, `StatusPoint`, `SkillPoint`, `Weight`, `MaxWeight`, etc. Read with bare name or `readparam()`, write with assignment or `setparam()`.

**Constants** baked in from `script_constants.hpp`: hundreds of `JOB_*`, `SC_*` (status effects), `EQI_*` (equip slots), `MF_*` (map flags), `IT_*` (item types), `ELE_*`, `RC_*`, `Size_*`, `DIR_*`, `bAtk`/`bMatk`/all `b*` bonus tags, `MAX_LEVEL`, `MAX_INVENTORY`, etc. Plus `true`/`false`.

## 4. Event labels (entry points from C++)

| Label | Trigger |
|---|---|
| `OnInit` | once per script load |
| `OnInstanceInit` / `OnInstanceDestroy` | per-instance lifecycle |
| `OnTouch` / `OnTouch_` / `OnTouchNPC` | player/mob enters trigger area |
| `OnWhisper` | player whispers the NPC |
| `OnCommand` | NPC-bound atcommand |
| `OnBuy` / `OnSell` | shop interaction |
| `OnPCLoginEvent` / `OnPCLogoutEvent` | every player login/logout |
| `OnPCDieEvent` / `OnPCKillEvent` / `OnNPCKillEvent` | combat outcomes |
| `OnPCBaseLvUpEvent` / `OnPCJobLvUpEvent` / `OnPCLoadMapEvent` / `OnPCStatCalcEvent` | progression hooks |
| `OnTimer<ms>` | recurring timer (`addtimer` / `initnpctimer`) |
| `OnTimerQuit` | attached player logs out during a timer |
| `OnAgitStart`/`End`/`Init` (+ `…2`/`…3`) | WoE phases |
| `OnGuildBreakEvent`, `OnClock<HHMM>`, `OnMinute<NN>`, `OnHour<NN>`, `OnDay<MMDD>`, `On<Day>`, `OnMonth<NN>` | clock-driven |
| Any user-defined `On…` | via `donpcevent "Npc::OnFoo"` / `doevent "Npc::OnFoo"` |

## 5. Runtime / VM behavior

### State machine
From `script.hpp`: `enum e_script_state { RUN, STOP, END, RERUNLINE, GOTO, RETFUNC, CLOSE }`. The interesting state is `STOP` — the script is parked, waiting for the client.

### Suspension points
| Builtin | Suspends? | Resume condition |
|---|---|---|
| `mes` | no (buffers text) | — |
| `next` | yes | client clicks Next |
| `close` | yes (marks dialog closed) | client clicks Close, then script ends |
| `close2` | yes | client clicks Close, **script continues** |
| `menu "label",L_a,…` | yes | client picks; jumps to label |
| `select(...)` / `prompt(...)` | yes | returns 1-based index |
| `input <var>{,min,max}` | yes | numeric or text input |
| `progressbar "color",<sec>` | yes | timer or movement cancels |
| `sleep <ms>` | yes (requires rid) | timer fires |
| `sleep2 <ms>` | yes (no rid required) | timer fires |
| `addtimer/initnpctimer` | no, but `OnTimer<ms>` is itself an entry |

### Player attachment (`rid`)
Every running script has an optional `rid` = account id of the "active" player. Inventory/dialog/stat commands implicitly target this player. Without rid, those commands no-op or abort. `playerattached()` tests it; `attachrid(<aid>)` / `detachrid` change it; `addrid(<type>, …)` fans out a script over many players.

### Logout mid-dialog
The session is cleared but the script struct may persist briefly. On resume the engine checks `script_rid2sd(...)`; if null, the script terminates. `OnTimerQuit` is the cleanup hook for timer-attached scripts.

### Threading
Single-threaded per map server. All scripts cooperate on the main game loop tick. Suspension yields; nothing is preemptive. **No locks anywhere in script code** — concurrency only at the SQL boundary.

### Stack
Per-call frame is a `script_stack` with `sp` and a `script_data*` array of tagged values (`C_INT`, `C_STR`, `C_NAME` for variable refs, `C_RETINFO`, `C_ARG`, `C_LSTR` for localized strings). `callfunc/callsub` push a retinfo frame.

### Functions
- **Global** (`function script Foo { … }` at top level) — `callfunc("Foo", args…)` from anywhere
- **NPC-local** (`function Foo { … }` inside an NPC body) — only callable within that NPC
- **Labels via callsub** (`callsub L_foo, args…; … L_foo: … return;`) — old style, no formal parameters

Arguments are positional, untyped, retrieved via `getarg(i)` / `getarg(i, default)`, counted with `getargcount()`. Return value via `return <expr>`.

### Error model
Coercion is automatic: `"5" + 3 == 8`, `3 + "5" == "35"`, divide-by-zero clamps. Type mismatch logs and returns 0/`""`. Bad variable refs abort with a console error. No try/catch. SQL errors return -1.

## 6. Builtin catalog (by category)

`script.cpp` registers each command with `BUILDIN_DEF(name, "signature")`. Signature chars: `s` (string), `i` (int), `v` (any), `r` (variable lvalue ref), `l` (label), `?` (start of optional), `*` (variadic). Total: ~1,000+.

| Category | Representative commands |
|---|---|
| **Dialog** | `mes`, `mesf`, `next`, `close`, `close2`, `close3`, `clear`, `menu`, `select`, `prompt`, `input`, `progressbar`, `cutin`, `messagebox` |
| **Player state** | `BaseLevel`, `Hp`, `Zeny`, `Class`, `readparam`, `setparam`, `strcharinfo(0..4)`, `getcharid(0..5)`, `getexp`, `getexpf`, `gethealth`, `heal`, `itemheal`, `percentheal`, `recovery` |
| **Items / inventory** | `getitem`, `getitem2`, `getitem3`, `getitembound`, `delitem`, `delitem2`, `countitem`, `countitem2`, `rentitem`, `groupranditem`, `getinventorylist`, `cleaninventory`, `storagecountitem`, `cartcountitem` |
| **Equipment** | `equip`, `equipitem`, `unequip`, `getequipid`, `getequipname`, `getequiprefinerycnt`, `getequipisidentify`, `successrefitem`, `failedrefitem`, `downrefitem`, `getequipcardid`, `repair`, `nude`, `costume` |
| **Movement / map** | `warp`, `warpchar`, `warpparty`, `warpguild`, `areawarp`, `savepoint`, `getmapxy`, `getmapinfo`, `getmapusers`, `getareausers`, `getareadropitem`, `mapwarp`, `setmapflag`, `removemapflag` |
| **Mobs / NPCs** | `monster`, `areamonster`, `bossmonster`, `clone`, `killmonster`, `killmonsterall`, `mobcount`, `disablenpc`, `enablenpc`, `hideoffnpc`, `hideonnpc`, `cloakoffnpc`, `cloakonnpc`, `setnpcdisplay`, `npctalk`, `npcspeed`, `npcwalkto`, `unitwalk`, `unitkill`, `unitwarp`, `unitskilluseid`, `unitskillusepos` |
| **Timers** | `addtimer`, `deltimer`, `addtimercount`, `initnpctimer`, `startnpctimer`, `stopnpctimer`, `getnpctimer`, `setnpctimer`, `sleep`, `sleep2`, `awake` |
| **Quests / achievements** | `setquest`, `completequest`, `erasequest`, `changequest`, `checkquest`, `isbegin_quest`, `questinfo`, `setquestinfo_level`, `getquestinfo`, `achievementadd`, `achievementremove`, `achievementcomplete`, `achievementexists`, `achievementinfo` |
| **Party / guild / clan** | `getcharid(1\|2\|5)`, `getpartyname`, `getpartymember`, `getguildname`, `getguildmaster`, `getguildmember`, `getguildinfo`, `requestguildinfo`, `clan_join`, `clan_leave`, `clan_master`, `guildchangegm` |
| **Storage / cart / mail** | `openstorage`, `openstorage2`, `guildopenstorage`, `storageclose`, `cartcountitem`, `cartdelitem`, `getmail`, `mail`, `auctionopen`, `rodex_sendmail`, `rodex_sendmail_acc` |
| **Skills / status** | `skill`, `addtoskill`, `getskilllv`, `getgdskilllv`, `resetstatus`, `resetskill`, `resetlvl`, `sc_start`, `sc_start2`, `sc_start4`, `sc_end`, `sc_end_class`, `sc_getscrate`, `getstatus` |
| **Item bonuses** (item scripts) | `bonus`, `bonus2`, `bonus3`, `bonus4`, `bonus5`, `autobonus`, `autobonus2`, `autobonus3`, `bonus_script`, `bonus_script_clear`, `petloot`, `petskillattack`, `petskillsupport` |
| **Battle / WoE** | `pvpoff`, `pvpon`, `gvgoff`, `gvgon`, `agitstart`, `agitend`, `agitcheck`, `attachrid`, `detachrid`, `addrid` |
| **Battleground** | `bg_create`, `bg_destroy`, `bg_join`, `bg_leave`, `bg_warp`, `bg_team_setxy`, `bg_team_setquit`, `bg_monster`, `bg_kick_all`, `waitingroom2bg` |
| **Instances** | `instance_create`, `instance_destroy`, `instance_enter`, `instance_id`, `instance_mapname`, `instance_npcname`, `instance_warpall`, `instance_announce`, `instance_check_party`, `instance_info` |
| **Shops** | `npcshopitem`, `npcshopadditem`, `npcshopdelitem`, `npcshopattach` |
| **Pet / homun / merc / elemental** | `bpet`, `pet`, `birthpet`, `homevolution`, `morphembryo`, `getpetinfo`, `gethominfo`, `getmercinfo`, `mercenary_create`, `mercenary_delete`, `geteleminfo`, `setunitdata`, `getunitdata` |
| **Math / RNG** | `rand`, `min`, `max`, `pow`, `sqrt`, `cbrt`, `log10`, `abs`, `cap_value` |
| **String** | `strlen`, `compare`, `strcmp`, `strpos`, `replacestr`, `substr`, `charisalpha`, `setchar`, `insertchar`, `delchar`, `strtoupper`, `strtolower`, `chr`, `ord`, `sprintf`, `implode`, `explode`, `escape_sql` |
| **Arrays** | `setarray`, `cleararray`, `copyarray`, `deletearray`, `getarraysize`, `getelementofarray`, `inarray`, `count_in_array`, `array_remove`, `array_replace`, `array_find`, `array_pop`, `array_push`, `sort` |
| **Date/time** | `gettime`, `gettimestr`, `gettimetick`, `gettimeformat`, `checkmonth`, `checkweekday` |
| **SQL** | `query_sql`, `query_logsql`, `escape_sql` |
| **Atcommand bridge** | `atcommand`, `charcommand`, `bindatcmd`, `unbindatcmd`, `useatcmd` |
| **Waiting rooms** | `waitingroom`, `delwaitingroom`, `enablewaitingroomevent`, `disablewaitingroomevent`, `getwaitingroomstate`, `warpwaitingpc`, `kickwaitingroomall` |
| **Misc / admin** | `getgmlevel`, `getgroupid`, `kick`, `kickall`, `disconnect`, `announce`, `mapannounce`, `areaannounce`, `kamibroadcast`, `debugmes`, `logmes` |
| **Channels** | `channel_create`, `channel_destroy`, `channel_join`, `channel_leave`, `channel_setopt`, `channel_chat`, `channel_ban` |
| **Hat effects** | `hateffect`, `aura` |

## 7. C++↔script ABI

```cpp
BUILDIN_FUNC(mes) {
    map_session_data *sd;
    if (!script_rid2sd(sd)) return SCRIPT_CMD_SUCCESS;
    clif_scriptmes(*sd, st->oid, script_getstr(st, 2));
    return SCRIPT_CMD_SUCCESS;
}
// registered as: BUILDIN_DEF(mes, "s"),
```

Useful macros for porting:
- `script_getnum(st, i)`, `script_getstr(st, i)` — pull args with coercion
- `script_getref(st, i)` — get a variable reference (for `r` signature)
- `script_pushint`, `script_pushstr`, `script_pushcopy` — push return values
- `script_rid2sd(sd)` — resolve attached player; bails if none
- `st->oid` is the NPC id; `st->rid` is the attached account id; `st->instance_id` for `'var` lookups

## 8. Hardest-to-port concepts (relevant for migration design)

1. **Coroutine dialog model.** `mes` … `next` … `mes` … `select` … `close` is fundamentally a fiber. Lua maps this directly (`coroutine.yield` / `coroutine.resume`).
2. **Sigils-as-scope.** No host language has 9 variable scopes distinguished by prefix character. Needs either a translation pass or a runtime API (`temp("foo")`, `global("foo")`).
3. **Implicit `rid`.** Every dialog/inventory/stat call assumes a player. Either thread a `player` object through everything, or stash it in coroutine-local context.
4. **`query_sql` returning into script arrays.** Direct SQL access from scripts. Either preserve via a binding, or refactor scripts onto a repository surface.
5. **`goto` and labels.** Heavy use in old quests. Event labels (`OnFoo:`) map to functions/methods; control-flow gotos may need a structured-rewrite pass.
6. **Sparse arrays + auto-coerced types.** Doable — `Dictionary<int, T>` for sparse, a `ScriptValue` union or Lua's native dynamic typing for coercion.
7. **Item/equip scripts** run in a separate context (stat-calc) where commands like `bonus` only make sense and dialog commands don't. Two execution modes needed: interactive (has rid, can dialog) vs stat-calc (writes bonuses, no I/O).
8. **`autobonus` strings.** Sub-script-as-string-literal pattern (`autobonus "{ bonus bAtk,30; }",100,5000;`) requires lazy compilation/eval at proc time.
9. **`bindatcmd`.** Scripts can register `@commands`. The atcommand dispatcher must call into the script runtime.

## 9. Key files (rAthena)

| File | What's in it |
|---|---|
| [src/map/script.cpp](/Volumes/1TB/Projetos/rathena/src/map/script.cpp) | Parser, VM, every `BUILDIN_FUNC` |
| [src/map/script.hpp](/Volumes/1TB/Projetos/rathena/src/map/script.hpp) | `script_state`, `script_stack`, `script_data`, `e_script_state`, macros |
| [src/map/script_constants.hpp](/Volumes/1TB/Projetos/rathena/src/map/script_constants.hpp) | All exported numeric constants |
| [src/map/npc.cpp](/Volumes/1TB/Projetos/rathena/src/map/npc.cpp) | NPC loader, event dispatch (the C++ side of "click NPC → start script") |
| [src/map/pc.cpp](/Volumes/1TB/Projetos/rathena/src/map/pc.cpp) | Param variable backing, OnPC*Event triggers |
| [doc/script_commands.txt](/Volumes/1TB/Projetos/rathena/doc/script_commands.txt) | **Canonical reference** — read before porting any builtin |
| [doc/sample/](/Volumes/1TB/Projetos/rathena/doc/sample/) | Minimal example for each feature |
| [npc/](/Volumes/1TB/Projetos/rathena/npc/) | Real corpus — thousands of scripts; the migration test set |
