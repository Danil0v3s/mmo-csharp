# Replay baseline — capturing rAthena to drive parity

**Phase:** cross-cutting harness; informs every MS1+ subsystem
**Source of truth:** a binary capture of a real rAthena session (file: [Map.Server.Tests/Replay/Fixtures/dhxj.log](../../../Map.Server.Tests/Replay/Fixtures/dhxj.log))

The capture is a sequence of `<port>|S|<hex>` / `<port>|R|<hex>` lines covering the full client flow: login → char list → char create → char select → map handoff → first map packets. We replay the `S|` lines against our stack and check each `R|` line matches what our servers actually send.

**Why it matters.** Reading rAthena source for "what should our server send when X happens?" is error-prone — handlers branch on PACKETVER, configs, and runtime state, and behavior often falls out of side effects spread across multiple files. The capture is the ground truth: it shows the exact bytes a real rAthena instance emitted under known conditions. Any divergence from those bytes is a parity bug.

## The replay framework

Lives in [Tools.PacketReplay/](../../../Tools.PacketReplay/) and is driven by [Map.Server.Tests/Replay/](../../../Map.Server.Tests/Replay/).

| Piece | Role |
|---|---|
| [PacketLogFile](../../../Tools.PacketReplay/PacketLogFile.cs) | Parses `port|dir|hex` lines from the fixture |
| [ReplaySession](../../../Tools.PacketReplay/ReplaySession.cs) | TCP runner; port-changes trigger reconnect; two-phase adaptive read |
| [PacketFraming](../../../Tools.PacketReplay/PacketFraming.cs) | Slices byte streams into framed packets via `IPacketSizeRegistry` |
| [Decoders/](../../../Tools.PacketReplay/Decoders/) | One per packet — turns raw bytes into named fields with per-field `Tolerant` flags |
| [Tokens/](../../../Tools.PacketReplay/Tokens/) | Per-packet extractors; on `R|` chunks they learn `(captured value → live value)` substitutions (AID, login_id1/2, char_id) and apply them to subsequent `S|` chunks |
| [PacketComparer](../../../Tools.PacketReplay/PacketComparer.cs) | Frames both sides, decodes where a decoder is registered, surfaces per-field diffs |
| [ServerStackFixture](../../../Map.Server.Tests/Replay/ServerStackFixture.cs) | xUnit fixture: cleans replay DB rows, spawns Login/Char/Map subprocesses, waits for the internal-ping healthcheck to report each ready |
| [PacketReplayTests](../../../Map.Server.Tests/Replay/PacketReplayTests.cs) | The `[Theory]` over `Replay/Fixtures/*.log` |

### Healthcheck packet

[`CZ_INTERNAL_PING (0x7530)` / `ZC_INTERNAL_PONG (0x7531)`](../../../Core.Server/Packets/In/CZ_INTERNAL_PING.cs) — picked from a `0x75xx` range rAthena never uses for clients. Each server registers an [InternalPingHandler](../../../Login.Server/Handlers/InternalPingHandler.cs) that responds with its [IServerReadiness.IsReady](../../../Core.Server/IServerReadiness.cs) flag:

- Login: `State == Running`
- Char: + registered with Login
- Map: + map list registered with Char + IP/port advertised

The fixture polls each server's port until pong reports `Ready=1` instead of scraping logs.

### Token rewriting

The captured rAthena assigned its own AID / login_id1 / login_id2 / char_id; our servers assign different values. Without rewriting, every downstream packet would fail comparison. The [TokenRewriter](../../../Tools.PacketReplay/Tokens/TokenRewriter.cs) walks both sides of every `R|` chunk, learns the captured-vs-live byte sequences from the relevant emitter, and rewrites those byte sequences in every subsequent `S|` chunk before sending.

Current extractors: [AcAcceptLoginTokens](../../../Tools.PacketReplay/Tokens/AcAcceptLoginTokens.cs) (login_id1, AID, login_id2), [HcSendMapDataTokens](../../../Tools.PacketReplay/Tokens/HcSendMapDataTokens.cs) (char_id).

### Tolerance flags

A field marked `Tolerant: true` in its decoder is reported in the diff but doesn't fail the test. Used for fields that are *intrinsically* per-run or per-environment, never for hiding real parity bugs:

- `AID` in any "this is your account id" packet — auto_increment offset differs per DB.
- `char_id` (`GID`) in `HC_ACCEPT_MAKECHAR` — same reason.
- `StartTime` in `ZC_ACCEPT_ENTER_ZONE` — `gettick()`.
- `Seed` in `HC_SECOND_PASSWD_LOGIN` — random per emit.
- `LoginId1`, `LoginId2`, `Token` in `AC_ACCEPT_LOGIN` — per-session random.
- `X`, `Y` in `ZC_ACCEPT_ENTER_ZONE` / `ZC_NPCACK_MAPMOVE` — rAthena `pc_setpos` randomizes when saved coords are OOB or non-walkable; the chosen cell is RNG-dependent.
- `Ip`, `Port` in `HC_SEND_MAP_DATA` — host network differs (capture LAN vs. localhost).

## Current state — what the replay validates

As of 2026-05-17, **6 of 7 capture chunks fully pass**, and the 7th has all 7 expected packets matching with only a trailing run of `status_calc_pc` broadcasts unmatched.

| Line | Direction | Packet(s) | Status | Notes |
|---|---|---|---|---|
| 1 | S (6900) | `CA_LOGIN` (danilo3) | — | sent, validates login auth flow |
| 2 | R (6900) | `AC_REFUSE_LOGIN` | ✓ | unknown-account path |
| 3 | S (6900) | `CA_LOGIN` (mmocsharp_M) | — | triggers create-on-login (`_M` suffix) |
| 4 | R (6900) | `AC_ACCEPT_LOGIN` | ✓ | AID/login_id1/login_id2/token tolerant; CharServers list strict |
| 5 | S (6121) | `CH_ENTER` | — | char-server enter |
| 6 | R (6121) | `HC_CHARACTER_LIST` + `HC_ACCEPT_ENTER` + `HC_CHARLIST_NOTIFY` + `HC_BLOCK_CHARACTER` + `HC_SECOND_PASSWD_LOGIN` (5 packets) | ✓ | all five match including the always-emitted pincode probe |
| 7 | S (6121) | `CH_KEEP_ALIVE` | — | heartbeat |
| 8 | S (6121) | `CH_MAKE_NEW_CHAR` | — | create char "ANERQO" |
| 9 | R (6121) | `HC_ACCEPT_MAKECHAR` | ✓ | full CharacterInfo decoded field-by-field; GID tolerant; HP/SP/StatusPoint formulas match rAthena `char.cpp:1500` |
| 10 | S (6121) | `CH_SELECT_CHAR` (slot 0) | — | |
| 11 | R (6121) | `HC_SEND_MAP_DATA` | ✓ | MapName/Domain strict; CharId/Ip/Port tolerant |
| 12 | S (5121→5191) | `CZ_WANT_TO_CONNECTION` | — | char_id token rewriting fires here |
| 13 | R (5121) | `ZC_AID` + `ZC_EXTEND_BODYITEM_SIZE` + `ZC_ACCEPT_ENTER_ZONE` + `ZC_FRIENDS_LIST` + 2× `ZC_NOTIFY_PLAYERCHAT` (version + MOTD) + `ZC_NPCACK_MAPMOVE` | ⚠️ | all 7 emitted packets match; trailing **535 B of `ZC_PAR_CHANGE` (0x00B0) broadcasts** the capture sends from `status_calc_pc` are not yet emitted by our server |
| 14–23 | S (5121) | client-side post-spawn packets (load-end-ack, time, refresh, guild-info queries) | — | sent without server gate; we receive but don't yet handle most |
| 24 | R (5121) | starts with `ZC_SPRITE_CHANGE2` (0x01D7) — equipment sprite broadcasts (1732 B) | ❌ | unreached; capture's connection closes early on our side once `status_calc_pc` payload doesn't show up |

### What's been ported to make this work

Every parity fix that landed because of the replay:

- **Login server**: revealed `_M`/`_F` create-on-login flow worked but `Char/CharServer` config defaults didn't match rAthena (CharNew vs CharNewDisplay split, MaxBilling=0, etc.).
- **Char server**: always-emit `HC_SECOND_PASSWD_LOGIN` after the slot summary (rAthena `chlogif_pincode_start`); `.gat` suffix on `MapName` in both `CharacterInfo` and `HC_SEND_MAP_DATA`; `MinimumCharacterSlots`/`MaximumCharacterSlots` set to 15; `HC_ACCEPT_MAKECHAR` reclassified fixed-size 177 B.
- **Char create**: full HP/SP/StatusPoint seeding per rAthena `char.cpp:1500` formula (`max_hp = 40 * (100 + vit) / 100`, etc.); `StartStatusPoints` default fixed `0 → 48`.
- **Map server**: emits `ZC_AID → ZC_EXTEND_BODYITEM_SIZE → ZC_ACCEPT_ENTER_ZONE → ZC_FRIENDS_LIST → version chat → MOTD lines → ZC_NPCACK_MAPMOVE` matching rAthena `pc_authok` order; `pc_setpos`-style OOB/non-walkable spawn randomization; `MapDataPaths` list supports rAthena's multi-cache fallback (import → re/pre-re → root); `UpdateMapServerAddressAsync` fan-out wired so char can build `HC_SEND_MAP_DATA`.
- **Stack lifecycle**: `IServerReadiness` + ping handler per server replaces brittle log-scraping in the test fixture.

## Next — what the capture says is missing

The trailing 535 B on line 13 and the entire line 24 are both **`status_calc_pc`** output. rAthena calls this from `pc_authok` after the `clif_changemap`; it recomputes every derived stat and broadcasts a packet per stat that changed.

### Packet inventory for the missing flow

Decoded from the capture's raw bytes around offset 151 on line 13 onward:

| Packet ID | Name | Why it fires |
|---|---|---|
| `0x00B0` | `ZC_PAR_CHANGE` (8 B) | Per-stat broadcast: `SP_WEIGHT`, `SP_MAXWEIGHT`, `SP_SPEED`, `SP_BASELEVEL`, `SP_JOBLEVEL`, `SP_NEXTBASEEXP`, `SP_NEXTJOBEXP`, `SP_HP`, `SP_MAXHP`, `SP_SP`, `SP_MAXSP`, `SP_STR`, `SP_AGI`, `SP_VIT`, `SP_INT`, `SP_DEX`, `SP_LUK`, … |
| `0x0141` | `ZC_COUPLESTATUS` (14 B) | Stat + bonus pair (e.g. SP_STR + StrBonus) |
| `0x00B1` | `ZC_LONGPAR_CHANGE` (8 B) | Large-value stats (Exp, JobExp, Zeny) |
| `0x01D7` | `ZC_SPRITE_CHANGE2` (15 B per slot) | One per equip slot — weapon, head-top/mid/bottom, garment, etc. broadcast to area (currently SELF since no nearby players) |
| `0x0B25` | `ZC_PAR_4JOB_CHANGE` (PACKETVER ≥ 20200916) | Renewal 4-job stat broadcast |

These are *not* gameplay logic — they're a deterministic projection of the saved char-status fields plus rAthena's renewal stat formulas (HP cap from job_basehpsp_db, etc.). For a freshly-created Novice with the captured stat allocation, every value can be derived without combat / skill / item interactions.

### Suggested scope for the next slice

**Full enumeration with rAthena source cites and the exact wire-byte order is in [initial-status-broadcast.md](initial-status-broadcast.md).** Summary:

1. **`SP_*` enum** ([Core.Server/Packets/ParamId.cs](../../../Core.Server/Packets/)) — parameter IDs from rAthena `map.hpp`.
2. **~25 new wire packet classes** — `ZC_PAR_CHANGE`, `ZC_COUPLESTATUS`, `ZC_LONGLONGPAR_CHANGE`, `ZC_STATUS`, `ZC_SPRITE_CHANGE2`, `ZC_NOTIFY_STANDENTRY11`, `ZC_SKILLINFO_LIST`, `ZC_SHORTCUT_KEY_LIST`, `ZC_INVENTORY_*`, etc.
3. **`StatusBroadcaster` service** — `BroadcastStatusCalcFirst` (line 13 cascade) + `BroadcastInitialStatus` (line 24's `clif_initialstatus` block) + `BroadcastLoadEndAckUpdates`.
4. **Renewal stat formulas** — `Hit`, `Flee`, `ASPD`, `Def1/2`, `Mdef1/2`, `Atk1/2`, `Matk1/2`, `Critical`. Subset needed to make a Novice Lv1's captured values match.
5. **Trigger points** — `WantToConnectionHandler` invokes the line-13 cascade synchronously after `ZC_NPCACK_MAPMOVE`; `NotifyActorInitHandler` invokes the line-24 cascade on LoadEndAck.
6. **Decoders** for every new packet so the replay surfaces field-level diffs as we build.

### Why this is the *right* next step

- It's the very next thing the capture demands. No detective work needed about what to build — the bytes tell us.
- It's pure projection from char data (no combat, no skills, no items, no AI). Doable without first standing up any MS1.movement or MS2 systems.
- Once landed, line 13 should pass cleanly and line 24 unblocks. Future captures of more complex flows (entering a populated map, walking near other players) will have an even tighter baseline to validate against.

After this, the natural follow-ups in order of capture coverage:

- **Equipment / inventory packets** for non-Novice fresh creates (clif_inventorylist, ZC_NOTIFY_EQUIP).
- **Quest log on login** (`ZC_ALL_QUEST_LIST` and friends) — irrelevant for a brand-new char but trips up replays of existing chars.
- **Party / Guild presence broadcasts** once the char is in one.
- **Visibility / STANDENTRY for nearby players** once two clients can coexist on the same map.

## How to use this doc

When you ship a parity fix that the replay flagged, append a one-line entry to the History table below (date + line covered). When the next capture reveals a new gap, add it to the table in `Current state` rather than guessing from rAthena source what "should" come next.

## History

- **2026-05-17** — [initial-status-broadcast.md](initial-status-broadcast.md) scope doc written. Decoded line 13 trailing (41 packets, 535 B) and line 24 partial (49 packets, ~1414 B of 1732) packet-by-packet. Full trigger chain traced through rAthena `pc_reg_received → intif_parse_StorageReceived → status_calc_pc(SCO_FIRST)` for line 13 and `clif_parse_LoadEndAck` for line 24. Six deliverables enumerated with rAthena source citations.
- **2026-05-17** — Wrote this doc. Replay state captured: lines 2/4/6/9/11 ✓, line 13 with 7 packets matching + trailing `status_calc_pc` unmatched, line 24 unreached. All scaffolding (decoders, token rewriter, readiness ping, multi-cache loading, `pc_setpos` OOB randomize, rAthena `pc_authok` packet order) committed.
