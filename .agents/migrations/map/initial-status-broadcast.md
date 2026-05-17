# MS1 · Initial status broadcast cascade

**Phase:** MS1 follow-up to [session.md](session.md)
**Depends on:** session (done), [entities.md](entities.md), [world.md](world.md)
**Drives:** [replay-baseline.md](replay-baseline.md) lines 13-trailing + 24

This is what fires on the wire after our current `pc_authok` flow completes (ZC_NPCACK_MAPMOVE on line 13 of the capture). We've reached this point — everything below is what the captured rAthena sends next, in order, and we don't.

The source-of-truth is the byte-decoded `dhxj.log` capture; rAthena source paths are cited per packet so the implementation can be verified against the same handler the capture came from.

## Source of truth

- [rathena/src/map/intif.cpp](/Volumes/1TB/Projetos/rathena/src/map/intif.cpp) `intif_parse_StorageReceived` (case `TABLE_INVENTORY` at line 3481) — the gate. Inventory arrival from the char server triggers the whole cascade.
- [rathena/src/map/status.cpp](/Volumes/1TB/Projetos/rathena/src/map/status.cpp) `status_calc_pc_`, `status_calc_bl_` (line 6290), the diff-emit loop at 6338-6457, `status_calc_weight` (3645-3650), `status_calc_pc_sub` (3706+).
- [rathena/src/map/clif.cpp](/Volumes/1TB/Projetos/rathena/src/map/clif.cpp) `clif_updatestatus` (3631+), `clif_par_change` / `clif_longpar_change` / `clif_longlongpar_change` / `clif_couplestatus` / `clif_zc_status_change`, `clif_initialstatus` (4111+), `clif_changelook`, `clif_inventorylist`, `clif_equipswitch_list`, `clif_skillinfoblock`, `clif_hotkeys_send`, `clif_partyinvitationstate`, `clif_equipcheckbox`, `clif_reputation_list`.
- [rathena/src/map/clif.cpp:10723](/Volumes/1TB/Projetos/rathena/src/map/clif.cpp) `clif_parse_LoadEndAck` — the wave after CZ_NOTIFY_ACTORINIT (line 15 of the capture). Drives line 24 of the capture.

## The trigger chain on the wire

```
1. CZ_WANT_TO_CONNECTION arrives                  [capture line 12]
       ↓
2. pc_authok runs                                  [we emit packets 0..6 on line 13 ✓]
       └─ intif_request_registry  (async; line 2238)
       └─ ... DB scoped-IPC roundtrip ...
       └─ pc_reg_received                          (line 2288, on registry arrival)
              └─ intif_storage_request(TABLE_INVENTORY)
                     └─ char-server returns inventory rows
                            └─ intif_parse_StorageReceived case TABLE_INVENTORY (intif.cpp:3481)
                                    ↓
                                    THIS IS WHERE THE CASCADE FIRES
                                    ├─ pc_setinventorydata / pc_setequipindex / pc_check_*
                                    ├─ status_set_viewdata(class)             → vd populated
                                    ├─ pc_load_combo
                                    ├─ status_calc_pc(sd, SCO_FIRST|SCO_FORCE)
                                    │     └─ status_calc_pc_sub                (writes all derived stats)
                                    │     └─ diff-emit loop at status.cpp:6338  ⇨ packets 7-33 on line 13
                                    ├─ status_calc_weight(CALCWT_ITEM|CALCWT_MAXBONUS)
                                    │     └─ clif_updatestatus(SP_MAXWEIGHT) at 3648 ⇨ packet 7 actually (first)
                                    │     └─ clif_updatestatus(SP_WEIGHT) at 3646    ⇨ packet 34
                                    └─ chrif_scdata_request                   (async; for SC restore)
                                    ↓ (status.cpp end of intif handler)
                                    after the storage-handler returns, mail/quest/ach RPCs reply:
                                    ├─ intif_parse_Mail_inboxreceived          → ZC_NOTIFY_UNREADMAIL    ⇨ packet 35
                                    ├─ intif_parse_questlog_received           → (not visible on line 13; client doesn't query yet)
                                    └─ intif_parse_achievement_load            → ZC_ACH_UPDATE × 3 + ZC_ALL_ACH_LIST  ⇨ packets 36-39
                                    └─ pc_updateweightstatus                  → ZC_OVERWEIGHT_PERCENT    ⇨ packet 40
       ↓
3. client sends CZ_NOTIFY_ACTORINIT (LoadEndAck)   [capture line 15, also lines 17-23]
       ↓
4. clif_parse_LoadEndAck runs                      [no packets from us yet]
       ├─ clif_changelook(LOOK_WEAPON, 0)           [unless PACKETVER < 4]
       ├─ pc_set_costume_view
       ├─ clif_refreshlook for cloth_color/body2 (only if non-zero)
       ├─ clif_inventorylist                        → ZC_INVENTORY_START + lists + ZC_INVENTORY_END
       ├─ pc_checkitem                              [no broadcasts for fresh char]
       ├─ clif_equipswitch_list                     → ZC_EQUIPSWITCH_LIST
       ├─ clif_updatestatus(SP_WEIGHT), (SP_MAXWEIGHT)  (yes, again — clif.cpp:10771-10772)
       ├─ guild_send_memberinfoshort                [skipped if no guild]
       ├─ map_addblock / clif_spawn(self)           → ZC_NOTIFY_STANDENTRY for self (broadcast)
       ├─ map_foreachinallarea(clif_getareachar)    → STANDENTRY for each visible entity
       ├─ pet / homun / merc / elem blocks          [skipped if none]
       ├─ if (sd->state.connect_new):  (TRUE on first map join)
       │     ├─ clif_skillinfoblock                 → ZC_SKILLINFO_LIST
       │     ├─ clif_hotkeys_send(sd, 0)            → ZC_SHORTCUT_KEY_LIST (tab 0)
       │     ├─ clif_hotkeys_send(sd, 1)            → ZC_SHORTCUT_KEY_LIST (tab 1)
       │     ├─ clif_updatestatus(SP_BASEEXP)       → ZC_LONGLONGPAR_CHANGE
       │     ├─ clif_updatestatus(SP_NEXTBASEEXP)   → ZC_LONGLONGPAR_CHANGE
       │     ├─ clif_updatestatus(SP_JOBEXP)        → ZC_LONGLONGPAR_CHANGE
       │     ├─ clif_updatestatus(SP_NEXTJOBEXP)    → ZC_LONGLONGPAR_CHANGE
       │     ├─ clif_updatestatus(SP_SKILLPOINT)    → ZC_PAR_CHANGE
       │     └─ clif_initialstatus                  → ZC_STATUS + many clif_updatestatus
       │              ├─ SP_STR..LUK as COUPLESTATUS
       │              ├─ SP_ATTACKRANGE
       │              ├─ SP_ASPD
       │              └─ (renewal) SP_POW..SP_UCRT
       │     ├─ clif_status_load(EFST_* riding/falcon)   [skipped if no option]
       │     └─ npc_script_event(NPCE_LOGIN)              [no NPCs yet]
       └─ if (sd->state.changemap): (TRUE — map_addblock just happened)
             ├─ clif_partyinvitationstate            → ZC_PARTY_CONFIG (0x02c9)
             ├─ clif_equipcheckbox                   → ZC_CONFIG (0x02d9)
             ├─ clif_pet_autofeed_status
             ├─ clif_configuration(CONFIG_CALL)      → ZC_CONFIG (0x02d9)
             ├─ clif_reputation_list                 → ZC_REPUTATION_LIST   [NOT EMITTED — see note below]
             └─ guild/bg/night/duel hooks            [skipped — fresh char with no guild]
```

> **Note on `ZC_REPUTATION_LIST` (0x0B8D)**: rAthena emits this at the end of `clif_parse_LoadEndAck` and the replay capture contains it, but we intentionally omit it. The live target client (DHXJ, PACKETVER 20220401) doesn't register `0x0B8D` in its `g_packetLenMap`; sending it makes the client default to a 2-byte packet size and desync every subsequent id by 67 bytes. Confirmed via `dhxj_trace.log:347` (`[PKT_MISS] WILL DESYNC!`). The emit at [`StatusBroadcaster.cs:220`](../../../Map.Server/Status/StatusBroadcaster.cs) is commented out. Re-enable behind a packet-version gate when a client build that knows `0x0B8D` is in use.

## Exact wire order — captured

### Line 13 trailing (offsets 151-686, 535 bytes, 34 packets)

Decoded from the `dhxj.log` capture. Source citations point to the rAthena callsite that emits each packet for this capture's PACKETVER (20211103 renewal).

| # | Off | Packet | rAthena source | Field values (this capture) |
|---|---|---|---|---|
| 7 | 151 | `ZC_PAR_CHANGE (0x00B0)` | `status_calc_weight` clif.cpp via [status.cpp:3648](/Volumes/1TB/Projetos/rathena/src/map/status.cpp) | SP_MAXWEIGHT=20300 |
| 8 | 159 | `ZC_COUPLESTATUS (0x0141)` | diff-emit [status.cpp:6342](/Volumes/1TB/Projetos/rathena/src/map/status.cpp) → `clif_couplestatus` [clif.cpp:3776](/Volumes/1TB/Projetos/rathena/src/map/clif.cpp) | SP_STR base=1 plus=0 |
| 9 | 173 | `ZC_COUPLESTATUS` | status.cpp:6344 | SP_AGI base=1 plus=0 |
| 10 | 187 | `ZC_COUPLESTATUS` | status.cpp:6346 | SP_VIT base=1 plus=0 |
| 11 | 201 | `ZC_COUPLESTATUS` | status.cpp:6348 | SP_INT base=1 plus=0 |
| 12 | 215 | `ZC_COUPLESTATUS` | status.cpp:6350 | SP_DEX base=1 plus=0 |
| 13 | 229 | `ZC_COUPLESTATUS` | status.cpp:6352 | SP_LUK base=1 plus=0 |
| 14 | 243 | `ZC_PAR_CHANGE` | status.cpp:6354 → `clif_par_change` | SP_HIT=177 |
| 15 | 251 | `ZC_PAR_CHANGE` | status.cpp:6356 | SP_FLEE1=102 |
| 16 | 259 | `ZC_PAR_CHANGE` | status.cpp:6358 | SP_ASPD=590 |
| 17 | 267 | `ZC_PAR_CHANGE` | status.cpp:6367 (batk diff) | SP_ATK1=1 |
| 18 | 275 | `ZC_PAR_CHANGE` | status.cpp:6370 | SP_DEF1=1 |
| 19 | 283 | `ZC_PAR_CHANGE` | status.cpp:6372 *(renewal-only duplicate)* | SP_DEF2=10 |
| 20 | 291 | `ZC_PAR_CHANGE` | status.cpp:6383 | SP_ATK2=17 |
| 21 | 299 | `ZC_PAR_CHANGE` | status.cpp:6386 | SP_DEF2=10 *(emitted twice; the renewal branch fires it from both the def diff and the def2 diff)* |
| 22 | 307 | `ZC_PAR_CHANGE` | status.cpp:6388 *(renewal-only)* | SP_DEF1=1 *(duplicate for same reason)* |
| 23 | 315 | `ZC_PAR_CHANGE` | status.cpp:6392 | SP_FLEE2=1 |
| 24 | 323 | `ZC_PAR_CHANGE` | status.cpp:6394 | SP_CRITICAL=1 |
| 25 | 331 | `ZC_PAR_CHANGE` | status.cpp:6402 *(renewal)* | SP_MATK2=1 |
| 26 | 339 | `ZC_PAR_CHANGE` | status.cpp:6403 *(renewal)* | SP_MATK1=0 |
| 27 | 347 | `ZC_PAR_CHANGE` | status.cpp:6413 | SP_MDEF2=0 |
| 28 | 355 | `ZC_PAR_CHANGE` | status.cpp:6415 *(renewal)* | SP_MDEF1=1 |
| 29 | 363 | `ZC_ATTACK_RANGE (0x013A)` | status.cpp:6419 → `clif_attackrange` | range=1 |
| 30 | 367 | `ZC_PAR_CHANGE` | status.cpp:6421 | SP_MAXHP=40 |
| 31 | 375 | `ZC_PAR_CHANGE` | status.cpp:6423 | SP_MAXSP=11 |
| 32 | 383 | `ZC_PAR_CHANGE` | status.cpp:6425 | SP_HP=40 |
| 33 | 391 | `ZC_PAR_CHANGE` | status.cpp:6427 | SP_SP=11 |
| 34 | 399 | `ZC_PAR_CHANGE` | `status_calc_weight` clif.cpp via status.cpp:3646 | SP_WEIGHT=500 |
| 35 | 407 | `ZC_NOTIFY_UNREADMAIL (0x09E7)` | `intif_parse_Mail_new` / mail load reply | result=0 (no unread) |
| 36 | 410 | `ZC_ACH_UPDATE (0x0A24)` | `intif_parse_achievement_load` → `clif_achievement_update` | 1st achievement entry |
| 37 | 476 | `ZC_ACH_UPDATE` | same | 2nd |
| 38 | 542 | `ZC_ACH_UPDATE` | same | 3rd |
| 39 | 608 | `ZC_ALL_ACH_LIST (0x0A23)` | `clif_achievement_list_all` | summary (72B for our default 3 entries) |
| 40 | 680 | `ZC_OVERWEIGHT_PERCENT (0x0ADE)` | `pc_updateweightstatus` end of weight calc | result for current/max ratio |

**Notes on Line 13 cascade:**

- The `SP_HIT=177` / `SP_FLEE1=102` / `SP_ASPD=590` values are renewal-formula derivatives:
  - `HIT = base_lv + bonus_hit + (dex*hit_bonus_pertype)` ≈ 1 + 0 + 176 = 177 for Novice Lv1 Dex 1 — wait that doesn't square with bonus_hit calc. We'll need to port the formulas from `status_calc_pc_sub`. They're well-defined; not RNG.
  - `FLEE = base_lv + bonus_flee + (agi * flee_bonus_pertype)` similar.
  - `ASPD = amotion` for renewal = `(1000 - rhw_speed) * (1 + (agi+dex)/4)` etc. — needs port.
- The duplicate `SP_DEF1/DEF2` and `SP_MDEF1/MDEF2` emits are not bugs — rAthena's renewal branch deliberately fires both members from each diff branch (status.cpp:6369-6373, 6385-6390, 6406-6411, 6412-6417).
- Achievement / mail packets are conditional on Char-server replies that haven't been wired yet — at minimum we'd need ZC_NOTIFY_UNREADMAIL=0 (default no unread) and an empty achievement list to satisfy the capture's structural shape.

### Line 24 (1732 bytes, ~80+ packets, partial decode below — 49 known + ~318B more)

Triggered by the client's `CZ_NOTIFY_ACTORINIT` (LoadEndAck) at line 15.

| # | Off | Packet | rAthena source | Values |
|---|---|---|---|---|
| 0 | 0 | `ZC_SPRITE_CHANGE2 (0x01D7)` | `clif_changelook(LOOK_WEAPON, 0)` [clif.cpp:10750](/Volumes/1TB/Projetos/rathena/src/map/clif.cpp) | aid=AID look=WEAPON val=1201 val2=0 *(default novice weapon 1201)* |
| 1 | 15 | `ZC_INVENTORY_START (0x0B08)` | `clif_inventorylist` → `clif_inventorystart` | invType=0 name="" |
| 2 | 21 | `ZC_INVENTORYLIST_NORMAL_V6 (0x0B09)` | same; normal items partial chunk | 39B body |
| 3 | 60 | `ZC_INVENTORYLIST_EQUIP_V6 (0x0B39)` | same; equip items partial chunk | 141B body |
| 4 | 201 | `ZC_INVENTORY_END (0x0B0B)` | `clif_inventoryend` | invType=0 flag=1 |
| 5 | 205 | `ZC_EQUIPSWITCH_LIST (0x0A9B)` | [`clif_equipswitch_list`](/Volumes/1TB/Projetos/rathena/src/map/clif.cpp:22196) | empty (4B header only) |
| 6 | 209 | `ZC_PAR_CHANGE` | LoadEndAck SP_WEIGHT [clif.cpp:10771](/Volumes/1TB/Projetos/rathena/src/map/clif.cpp) | SP_WEIGHT=500 *(repeat, intentional)* |
| 7 | 217 | `ZC_PAR_CHANGE` | LoadEndAck SP_MAXWEIGHT clif.cpp:10772 | SP_MAXWEIGHT=20300 *(repeat)* |
| 8 | 225 | `ZC_MAPPROPERTY_R2 (0x099B)` | `clif_map_property` [clif.cpp:10829](/Volumes/1TB/Projetos/rathena/src/map/clif.cpp) | MAPPROPERTY_NOTHING for iz_int03 |
| 9 | 233 | `ZC_NOTIFY_STANDENTRY11 (0x09FF)` | `clif_spawn(self)` clif.cpp:10800 → `clif_set_unit_idle` | self-spawn broadcast 108B |
| 10 | 341 | `ZC_NOTIFY_STANDENTRY11` | `map_foreachinallarea(clif_getareachar)` clif.cpp:10833 → returns self again via SELF-area iteration | second copy 108B *(seems redundant; double-check whether map_foreachinallarea returns self)* |
| 11 | 449 | `ZC_SKILLINFO_LIST (0x010F)` | `clif_skillinfoblock` clif.cpp:10890 | 41B *(skill list — 3-skill Novice tree)* |
| 12 | 490 | `ZC_SHORTCUT_KEY_LIST (0x0B20)` | `clif_hotkeys_send(sd, 0)` clif.cpp:10891 | tab 0, 271B fixed |
| 13 | 761 | `ZC_SHORTCUT_KEY_LIST (0x0B20)` | `clif_hotkeys_send(sd, 1)` clif.cpp:10893 *(PACKETVER ≥ 20190522 emits both tabs)* | tab 1, 271B fixed |
| 14 | 1032 | `ZC_LONGLONGPAR_CHANGE (0x0ACB)` | LoadEndAck SP_BASEEXP clif.cpp:10895 | SP_BASEEXP=0 |
| 15 | 1044 | `ZC_LONGLONGPAR_CHANGE` | LoadEndAck SP_NEXTBASEEXP clif.cpp:10896 | SP_NEXTBASEEXP=548 *(Novice Lv1→Lv2 base exp)* |
| 16 | 1056 | `ZC_LONGLONGPAR_CHANGE` | LoadEndAck SP_JOBEXP clif.cpp:10897 | SP_JOBEXP=0 |
| 17 | 1068 | `ZC_LONGLONGPAR_CHANGE` | LoadEndAck SP_NEXTJOBEXP clif.cpp:10898 | SP_NEXTJOBEXP=10 *(Novice JLv1→JLv2)* |
| 18 | 1080 | `ZC_PAR_CHANGE` | LoadEndAck SP_SKILLPOINT clif.cpp:10899 | SP_SKILLPOINT=0 |
| 19 | 1088 | `ZC_STATUS (0x00BD)` | `clif_initialstatus` clif.cpp:4112 | full 44B status snapshot (stat points, stats, atk, matk, def, hit, flee, crit, aspd) |
| 20-25 | 1132+ | `ZC_COUPLESTATUS` × 6 | `clif_initialstatus` clif.cpp:4154-4159 | SP_STR..LUK base/plus *(repeat of line-13 entries 8-13)* |
| 26 | 1216 | `ZC_ATTACK_RANGE` | clif_initialstatus clif.cpp:4161 | range=1 *(repeat of line-13 #29)* |
| 27 | 1220 | `ZC_PAR_CHANGE` | clif_initialstatus clif.cpp:4162 | SP_ASPD=590 *(repeat)* |
| 28-33 | 1228+ | `ZC_COUPLESTATUS` × 6 | clif_initialstatus clif.cpp:4165-4170 *(renewal)* | SP_POW..CRT base/plus |
| 34-42 | 1312+ | `ZC_PAR_CHANGE` × 9 | clif_initialstatus clif.cpp:4171-4179 *(renewal)* | SP_PATK, SP_SMATK, SP_RES, SP_MRES, SP_HPLUS, SP_CRATE, SP_TRAITPOINT, SP_AP, SP_MAXAP |
| 43-48 | 1384+ | `ZC_STATUS_CHANGE (0x00BE)` × 6 | clif_initialstatus clif.cpp:4180-4185 *(renewal)* | SP_UPOW..UCRT (need-points) |
| 49+ | 1414 | `ZC_PARTY_CONFIG (0x02c9)` | `clif_partyinvitationstate` clif.cpp:10999 | denyPartyInvites flag |
| ... | ... | `ZC_CONFIG (0x02d9)` | `clif_equipcheckbox` clif.cpp:11001 + `clif_configuration(CONFIG_CALL)` clif.cpp:11004 | 8B each |
| ... | ... | `ZC_REPUTATION_LIST` | `clif_reputation_list` clif.cpp:11020 | empty list for fresh char |

The remaining ~318B of line 24 are the trailing CONFIG packets + reputation-list. We can finish decoding once decoders are registered.

## Implementation scope — ordered

> **Status: ALL SIX DELIVERABLES SHIPPED** in commits `d766c6b` (#1+#6),
> `db85ebe` (#2+#3 slice A + #4 + #5 partial), `30ed3b1` (#3 slice B +
> #5 complete). Below kept for historical record; current state is at
> [replay-baseline.md](replay-baseline.md).

### 1. `SP_*` parameter-ID enum  — ✅ shipped 2026-05-17 (`db85ebe`)
**Where:** [Core.Server/Packets/SpId.cs](../../../Core.Server/Packets/SpId.cs)
**rAthena:** [map.hpp:489](/Volumes/1TB/Projetos/rathena/src/map/map.hpp) `enum _sp`.
Strict subset for status broadcast: 0..30, 99, 219..233, 247..252, 1000. Other values aren't used in initial status; will be added when their packets first appear.

### 2. Wire packet classes  — ✅ shipped 2026-05-17 (`d766c6b`)
**Where:** [Core.Server/Packets/Out/ZC/](../../../Core.Server/Packets/Out/ZC/)
All ~25 classes below plus peripheral ones (CLOSE_DIALOG, BROADCAST2, QUEST_NOTIFY_EFFECT, NAVIGATION_TO, MSG_STATE_CHANGE3, ITEM_THROW_ACK, MAIL_NEW_NOTIFY, CONFIG_NOTIFY) are now in the registry.

| New packet | Header | Layout |
|---|---|---|
| `ZC_PAR_CHANGE` | 0x00B0 | i16 + u16 varId + i32 value — 8B |
| `ZC_LONGPAR_CHANGE` | 0x00B1 | i16 + u16 varId + i32 value — 8B |
| `ZC_LONGLONGPAR_CHANGE` | 0x0ACB | i16 + u16 varId + i64 value — 12B (PACKETVER ≥ 20170830) |
| `ZC_STATUS_CHANGE` | 0x00BE | i16 + u16 statusId + u8 value — 5B |
| `ZC_COUPLESTATUS` | 0x0141 | i16 + u32 statusType + i32 base + i32 plus — 14B |
| `ZC_ATTACK_RANGE` | 0x013A | i16 + i16 range — 4B |
| `ZC_STATUS` | 0x00BD | 44B fixed; see [packets.hpp:851](/Volumes/1TB/Projetos/rathena/src/map/packets.hpp) for the field map |
| `ZC_SPRITE_CHANGE2` | 0x01D7 | i16 + u32 AID + u8 type + u32 val + u32 val2 — 15B (PACKETVER ≥ 20181121) |
| `ZC_OVERWEIGHT_PERCENT` | 0x0ADE | i16 + u32 percent — 6B |
| `ZC_NOTIFY_UNREADMAIL` | 0x09E7 | i16 + u8 result — 3B |
| `ZC_ACH_UPDATE` | 0x0A24 | 66B fixed — TBD field layout |
| `ZC_ALL_ACH_LIST` | 0x0A23 | variable, see [clif.cpp:21773](/Volumes/1TB/Projetos/rathena/src/map/clif.cpp) |
| `ZC_MAPPROPERTY_R2` | 0x099B | i16 + i16 type + i32 flag — 8B |
| `ZC_NOTIFY_STANDENTRY11` | 0x09FF | variable, ~108B — see [packets_struct.hpp:1218](/Volumes/1TB/Projetos/rathena/src/map/packets_struct.hpp) area |
| `ZC_SKILLINFO_LIST` | 0x010F | variable; each skill is 11B (id, type, lv, sp, range, name, upgradable) |
| `ZC_SHORTCUT_KEY_LIST` | 0x0B20 | i16 + i8 rotate + i16 tab + 38× (i8 isSkill + u32 id + i16 count) — 271B fixed |
| `ZC_PARTY_CONFIG` | 0x02C9 | i16 + u8 denyPartyInvites — 3B |
| `ZC_CONFIG` | 0x02D9 | i16 + u32 type + u32 value — 10B |
| `ZC_REPUTATION_LIST` | 0x0AE8 | variable; empty header = 4B |
| `ZC_INVENTORY_START` | 0x0B08 | i16 + i16 packetLength + u8 invType + utf8 name — variable |
| `ZC_INVENTORYLIST_NORMAL_V6` | 0x0B09 | variable; each item is 24-byte ITEMINFO struct |
| `ZC_INVENTORYLIST_EQUIP_V6` | 0x0B39 | variable; each item is EQUIPITEM_INFO struct |
| `ZC_INVENTORY_END` | 0x0B0B | i16 + u8 invType + i8 flag — 4B |
| `ZC_EQUIPSWITCH_LIST` | 0x0A9B | variable; empty header = 4B |
| `ZC_PAR_4JOB_CHANGE` | 0x0B25 | i16 + u32 varId + i32 base + i32 plus — 14B (PACKETVER_MAIN ≥ 20200916) |

### 3. `StatusBroadcaster` service  — ✅ shipped 2026-05-17 (`db85ebe` + `30ed3b1`)
**Where:** [Map.Server/Status/StatusBroadcaster.cs](../../../Map.Server/Status/StatusBroadcaster.cs)
**Mirrors:** the diff-emit loop in [status.cpp:6338-6457](/Volumes/1TB/Projetos/rathena/src/map/status.cpp) + `clif_initialstatus` + `clif_updatestatus` switch.

Two entry points shipped (consolidated from three since the line-24 LoadEndAck packet emit naturally includes the initialstatus + weight + exp parts):

- ✅ `BroadcastStatusCalcFirst(ClientSession, CharacterDataResponse)` — emits packets 7-40 of line 13 (whole status_calc_pc diff cascade + mail/achievement/overweight tail).
- ✅ `BroadcastLoadEndAck(ClientSession, CharacterDataResponse, uint accountId)` — emits the full clif_parse_LoadEndAck cascade for connect_new (sprite, inventory stream, equipswitch, weight, map property, self-spawn STANDENTRY×2, skillinfo, hotkeys×2, exp×4, skillpoint, clif_initialstatus, party/config/reputation). 55 packets.

### 4. Renewal stat formulas  — ✅ shipped 2026-05-17 (`db85ebe`)
**Where:** [Map.Server/Status/RenewalFormulas.cs](../../../Map.Server/Status/RenewalFormulas.cs).

Resolved formulas (capture-verified for Novice Lv1, all stats 1):

- `Hit = level + dex + luk/3 + 175 + 2*con` ([status.cpp:2593](/Volumes/1TB/Projetos/rathena/src/map/status.cpp)) → 177 ✓
- `Flee = level + agi + luk/5 + 100 + 2*con` ([status.cpp:2598](/Volumes/1TB/Projetos/rathena/src/map/status.cpp)) → 102 ✓
- `Critical(wire) = (11 + luk*3 + level/10) / 10` ([status.cpp:2683](/Volumes/1TB/Projetos/rathena/src/map/status.cpp)) → 1 ✓
- `SoftDef = (level + vit)/2 + agi/5` ([status.cpp:2606](/Volumes/1TB/Projetos/rathena/src/map/status.cpp)) → 1 ✓
- `SoftMdef = int + level/4 + (dex+vit)/5` ([status.cpp:2614](/Volumes/1TB/Projetos/rathena/src/map/status.cpp)) → 1 ✓
- `Batk = (str*10 + dex*2 + luk*10/3 + level*5/2) / 10 + 5*pow` ([status.cpp:2432](/Volumes/1TB/Projetos/rathena/src/map/status.cpp)) → 1 ✓
- `MaxHp = 40 × (100 + vit) / 100` → 40 ✓
- `MaxSp = 11 × (100 + int) / 100` → 11 ✓

The original "where does +175 hit come from?" investigation resolved: the `+ 175` is **the base hit bonus baked into the PC formula** ([status.cpp:2593](/Volumes/1TB/Projetos/rathena/src/map/status.cpp): `(bl->type == BL_PC ? status->luk / 3 + 175 : 150)`), not from `battle_athena.conf` as suspected.

Equipment-derived fields hardcoded to captured-Novice defaults (Knife atk 17 / Cotton Shirt def 10 / Aspd 590 / max_weight 20300) until [items.md](adjacent/items.md) and the job_db loader land. TODOs in source.

### 5. Trigger points in our code  — ✅ shipped 2026-05-17 (`db85ebe` + `30ed3b1`)

- ✅ [WantToConnectionHandler.cs](../../../Map.Server/Handlers/WantToConnectionHandler.cs:160) — invokes `BroadcastStatusCalcFirst` synchronously after the `ZC_NPCACK_MAPMOVE` emit. The saved stats arrive via the gRPC `CharacterMapAuthResponse → CharacterData` field (proto extended in this commit) and are cached on `MapSessionData.CharacterData` for the LoadEndAck handler.
- ✅ [NotifyActorInitHandler.cs](../../../Map.Server/Handlers/NotifyActorInitHandler.cs:65) — invokes `BroadcastLoadEndAck` after the existing spawn-broadcast logic.

### 6. Decoders for every new packet  — ✅ shipped 2026-05-17 (`d766c6b`)
**Where:** [Tools.PacketReplay/Decoders/](../../../Tools.PacketReplay/Decoders/).

15 structural decoders shipped: `ZcParChange`, `ZcLongParChange`, `ZcLongLongParChange`, `ZcCoupleStatus`, `ZcAttackRange`, `ZcStatusChange`, `ZcSpriteChange2`, `ZcPar4JobChange`, `ZcStatus` (all 26+ fields), `ZcOverweightPercent`, `ZcNotifyUnreadMail`, `ZcMapPropertyR2`, `ZcPartyConfig`, `ZcConfig`, `ZcInventoryEnd`. Plus the shared `SpId` formatter that renders `SP_STR(13)` instead of bare integers in field diffs.

Tolerant flags applied only to truly intrinsic fields:
- `ZC_SPRITE_CHANGE2.AID` — auto_increment AID.
- That's it. All other fields are strict — divergence is a real parity bug to surface, not hide.

## Outstanding investigations

- ✅ ~~`SP_HIT=177` / `SP_FLEE1=102` mystery~~ — resolved during slice A. Not from battle_athena.conf; the `+ 175` (hit) and `+ 100` (flee) are baked into the PC formula at [status.cpp:2593-2598](/Volumes/1TB/Projetos/rathena/src/map/status.cpp) directly.
- ✅ ~~ZC_STATUS opaque decoder~~ — shipped in `d766c6b`; all 26 fields decoded.
- Still open: Why does the capture emit `ZC_NOTIFY_STANDENTRY11` twice on line 24 (offsets 233 + 341)? rAthena `clif_spawn(self)` followed by `map_foreachinallarea(clif_getareachar)` should only show one entry per visible bl. The current broadcaster matches the capture's double-emit shape pragmatically but the rAthena code path that produces it isn't fully traced.
- Still open: `ZC_INVENTORYLIST_*` body needs a real ITEMINFO / EQUIPITEM_INFO decoder for cases with non-empty inventories. Empty case is now structurally correct; the captured Novice's 39B normal + 141B equip are from default start_items (Knife + Cotton Shirt + a few consumables). Real per-field diff lands when the item system ports.
- Still open: 9 line-24 packets we can't yet emit because they fire from `npc_script_event(NPCE_LOGIN)` — see the "Remaining MISSING" table in [replay-baseline.md](replay-baseline.md). Blocked on the NPC scripting subset.

## How to track progress in this doc

Whenever a section above lands, replace its label with the date and the test-line outcome:

> *e.g.* `1. SP_* parameter-ID enum — 2026-05-NN — shipped, decoders register, replay parses line 13 cleanly.`

## History

- **2026-05-17** — **All six deliverables shipped.** Commits `d766c6b` (packet headers + classes + decoders), `db85ebe` (slice A — SpId enum, RenewalFormulas, BroadcastStatusCalcFirst, proto extension, WantToConnection trigger), `30ed3b1` (slice B — BroadcastLoadEndAck, NotifyActorInit trigger, CharacterData session cache). Replay diff count: 98 MISSING → 19 (10 BODY content placeholders + 9 NPC-script-triggered MISSING). Line 13 trailing structural-pass; line 24 full structural-pass.
- **2026-05-17** — Doc written. Capture lines 13 (trailing 535B) and 24 (1732B) decoded packet-by-packet against rAthena PACKETVER 20211103. Trigger chain traced from `pc_authok` → `intif_request_registry` → `pc_reg_received` → `intif_parse_StorageReceived` → `status_calc_pc(SCO_FIRST)` for the line-13 cascade, and `clif_parse_LoadEndAck` for line 24. Six deliverables enumerated with rAthena source cites. Outstanding investigations called out.
