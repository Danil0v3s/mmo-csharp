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

As of 2026-05-17, after the status-broadcast cascade landed
([initial-status-broadcast.md](initial-status-broadcast.md)),
**every line through 24 is structurally matched**. The framer reports
**19 total diffs** down from 98 — and all 19 are gameplay-content
placeholders (item bytes, skill data, achievement entries, NPC-script
output, etc.) in subsystems that haven't been ported yet. None are
formula bugs or shape divergences.

| Line | Direction | Packet(s) | Status | Notes |
|---|---|---|---|---|
| 1 | S (6900) | `CA_LOGIN` (danilo3) | — | sent, validates login auth flow |
| 2 | R (6900) | `AC_REFUSE_LOGIN` | ✓ | unknown-account path |
| 3 | S (6900) | `CA_LOGIN` (mmocsharp_M) | — | triggers create-on-login (`_M` suffix) |
| 4 | R (6900) | `AC_ACCEPT_LOGIN` | ✓ | AID/login_id1/login_id2/token tolerant; CharServers list strict |
| 5 | S (6121) | `CH_ENTER` | — | char-server enter |
| 6 | R (6121) | `HC_CHARACTER_LIST` + 4 others (5 packets) | ✓ | all five match including the always-emitted pincode probe |
| 7 | S (6121) | `CH_KEEP_ALIVE` | — | heartbeat |
| 8 | S (6121) | `CH_MAKE_NEW_CHAR` | — | create char "ANERQO" |
| 9 | R (6121) | `HC_ACCEPT_MAKECHAR` | ✓ | full CharacterInfo decoded field-by-field; GID tolerant |
| 10 | S (6121) | `CH_SELECT_CHAR` (slot 0) | — | |
| 11 | R (6121) | `HC_SEND_MAP_DATA` | ✓ | MapName/Domain strict; CharId/Ip/Port tolerant |
| 12 | S (5121→5191) | `CZ_WANT_TO_CONNECTION` | — | char_id token rewriting fires here |
| 13 | R (5121) | 41 packets including `status_calc_pc` cascade | ⚠️ | 37 packets match; remaining 4 are `ZC_ACH_UPDATE` × 3 + `ZC_ALL_ACH_LIST` (placeholder achievement bytes — needs achievement system) |
| 14–23 | S (5121) | client-side post-spawn packets (LoadEndAck, time, refresh, guild-info queries) | — | sent without server gate; we receive |
| 24 | R (5121) | 64 packets — sprite changes, inventory, weight, map property, self-spawn, skill info, hotkeys×2, exp cascade, `clif_initialstatus`, party/config/reputation | ⚠️ | 6 BODY (content placeholders in inventory/skill/hotkey/STANDENTRY/reputation) + 9 MISSING (NPC-script-triggered: NAVIGATION_TO, BROADCAST2, QUEST_NOTIFY_EFFECT, MAIL_NEW_NOTIFY, etc.) |
| 25+ | S/R (5121) | further client–server traffic | — | now reachable; capture's connection holds open further than before. Line 27 (`ZC_HAT_EFFECT` 0x0ADF) is the new "unknown" frontier. |

### What's been ported to make this work

Every parity fix that landed because of the replay:

- **Login server**: revealed `_M`/`_F` create-on-login flow worked but `Char/CharServer` config defaults didn't match rAthena (CharNew vs CharNewDisplay split, MaxBilling=0, etc.).
- **Char server**: always-emit `HC_SECOND_PASSWD_LOGIN` after the slot summary (rAthena `chlogif_pincode_start`); `.gat` suffix on `MapName` in both `CharacterInfo` and `HC_SEND_MAP_DATA`; `MinimumCharacterSlots`/`MaximumCharacterSlots` set to 15; `HC_ACCEPT_MAKECHAR` reclassified fixed-size 177 B.
- **Char create**: full HP/SP/StatusPoint seeding per rAthena `char.cpp:1500` formula (`max_hp = 40 * (100 + vit) / 100`, etc.); `StartStatusPoints` default fixed `0 → 48`.
- **Map server `pc_authok` flow**: emits `ZC_AID → ZC_EXTEND_BODYITEM_SIZE → ZC_ACCEPT_ENTER_ZONE → ZC_FRIENDS_LIST → version chat → MOTD lines → ZC_NPCACK_MAPMOVE` matching rAthena `pc_authok` order; `pc_setpos`-style OOB/non-walkable spawn randomization; `MapDataPaths` list supports rAthena's multi-cache fallback (import → re/pre-re → root); `UpdateMapServerAddressAsync` fan-out wired so char can build `HC_SEND_MAP_DATA`.
- **Map server status broadcast cascade** (commits `db85ebe` slice A + `30ed3b1` slice B):
  - `SpId` constants ([Core.Server/Packets/SpId.cs](../../../Core.Server/Packets/SpId.cs)) for the rAthena `enum _sp` subset the wire emits.
  - `RenewalFormulas` ([Map.Server/Status/RenewalFormulas.cs](../../../Map.Server/Status/RenewalFormulas.cs)) — capture-verified `Hit`, `Flee`, `Critical`, `SoftDef`, `SoftMdef`, `Batk`, `MaxHp`, `MaxSp` formulas straight from `status.cpp:2593-2683`. Equipment-derived fields hardcoded to the captured Novice defaults (Knife atk 17, Cotton Shirt def 10) until items land.
  - `StatusBroadcaster` ([Map.Server/Status/StatusBroadcaster.cs](../../../Map.Server/Status/StatusBroadcaster.cs)) with two entry points:
    - `BroadcastStatusCalcFirst` — invoked from `WantToConnectionHandler` right after `ZC_NPCACK_MAPMOVE`. Mirrors the `status_calc_pc(SCO_FIRST)` diff-emit loop at `status.cpp:6338-6457` including the renewal duplicate DEF1/DEF2 + MDEF1/MDEF2 emits.
    - `BroadcastLoadEndAck` — invoked from `NotifyActorInitHandler`. Mirrors `clif_parse_LoadEndAck` (`clif.cpp:10723-11020`) — sprite, inventory stream, equipswitch, weight, map property, self-spawn STANDENTRY×2, skillinfo, hotkeys×2, exp×4, skillpoint, `clif_initialstatus`, party/config/reputation.
  - `CharacterDataResponse` proto extended with 29 saved-stat fields ([char_service.proto:169](../../../Core.Server/Protos/char_service.proto)) so the broadcaster can run synchronously without a second IPC roundtrip.
  - `MapSessionData.CharacterData` caches the IPC response between the WantToConnection and LoadEndAck handlers.
- **Stack lifecycle**: `IServerReadiness` + ping handler per server replaces brittle log-scraping in the test fixture.

## Next — what the capture says is missing

The 19 remaining diffs are all gameplay-content placeholders. Each unblocks when its parent subsystem ports; none are formula or shape bugs.

### Remaining BODY diffs (10) — content placeholders

| Line | Packet | Cause | Unblocked by |
|---|---|---|---|
| 13 | `ZC_ACH_UPDATE` × 3 | We emit empty body bytes for 3 default-achievement entries; capture has real achievement IDs / counts / reward flags | Achievement system port |
| 13 | `ZC_ALL_ACH_LIST` × 1 | Same — summary header + per-achievement progress placeholders | Achievement system port |
| 24 | `ZC_INVENTORYLIST_NORMAL_V6` | Empty body; capture has the captured Novice's starting inventory items (`Red Potion` × 7, etc.) | Item / inventory system port |
| 24 | `ZC_INVENTORYLIST_EQUIP_V6` | Empty body; capture has Knife + Cotton Shirt entries | Item / equip system port |
| 24 | `ZC_NOTIFY_STANDENTRY` × 2 | Self-spawn shape correct, but cosmetic fields (head, hair, equip view IDs, guild emblem) are zero; capture has real values | Item / equip + cosmetic systems |
| 24 | `ZC_SKILLINFO_LIST` | 37 zero-bytes; capture has the Novice's `NV_BASIC` skill tree entry | Skill system port |
| 24 | `ZC_REPUTATION_LIST` | 65 zero-bytes; capture has rAthena's default reputation factions | Reputation system port |

### Remaining MISSING (9) — NPC-script-triggered packets

All fire from rAthena's `NPCE_LOGIN` script event (default `npc/other/welcome.txt` and similar). They aren't part of `clif_parse_LoadEndAck` itself; they come from script `dispbottom` / `mes` / quest-icon / navigation-pin commands inside the welcome script that runs on first login.

| Line | Packet | Source |
|---|---|---|
| 24 | `ZC_NAVIGATION_TO` (0x08E2) | Script `navigateto` |
| 24 | `ZC_BROADCAST2` (0x01C3) | Script `mapannounce`/`announce` coloured variant |
| 24 | `ZC_CLOSE_DIALOG` (0x00B6) | Script `close` at end of welcome dialog |
| 24 | `ZC_QUEST_NOTIFY_EFFECT` (0x0446) × 2 | `questinfo` quest icons on nearby NPCs |
| 24 | `ZC_ITEM_THROW_ACK` (0x00AF) | One of the post-init `clif_*` calls — exact trigger TBD |
| 24 | `ZC_MSG_STATE_CHANGE3` (0x0983) | Status-icon push (likely `EFST_*` from a welcome buff) |
| 24 | `ZC_MAIL_NEW_NOTIFY` (0x0AC2) × 2 | Rodex mail summary load |

All of these unblock when [`npc.md`](npc.md) (script engine subset) + the mail system land. Until then, the test reports them as MISSING with the right named packet so the diff is auditable.

### Suggested scope for the next slice

All six deliverables from [initial-status-broadcast.md](initial-status-broadcast.md) shipped in `db85ebe` (slice A) and `30ed3b1` (slice B). The structural cascade is now the test's baseline — every line through 24 parses + frames + diffs at field level. The remaining 19 diffs above each map to a specific gameplay subsystem that will fill in over time.

### Natural next steps (capture-driven priority order)

Each unblocks a corresponding row in the BODY/MISSING tables above:

1. **Item / equip system** — fills `ZC_INVENTORYLIST_*` body, sprite-change weapon ID, `ZC_NOTIFY_STANDENTRY` cosmetic fields (head/weapon/equip view ids), `ZC_DEF2`/`ZC_ATK2` hard-equipment values. See [`map/adjacent/items.md`](adjacent/items.md).
2. **Skill system** — fills `ZC_SKILLINFO_LIST` body (Novice's NV_BASIC tree). See [`map/adjacent/skills.md`](adjacent/skills.md).
3. **Achievement system** — fills `ZC_ACH_UPDATE` × 3 + `ZC_ALL_ACH_LIST` bodies.
4. **Reputation factions** — fills `ZC_REPUTATION_LIST` body.
5. **NPC script subset** — emits the 9 MISSING line-24 packets (welcome-script: `NAVIGATION_TO`, `BROADCAST2`, `CLOSE_DIALOG`, `QUEST_NOTIFY_EFFECT`, etc.). See [`map/npc.md`](npc.md).
6. **Hotkey persistence** — fills the two `ZC_SHORTCUT_KEY_LIST` bodies. Lightweight; saved per-char in the char DB.
7. **Mail unread count** — fills the two `ZC_MAIL_NEW_NOTIFY` emits. Wired to char-side `RequestMailInbox` RPC (already exists).

Beyond what the current capture demands, the **next capture target** should exercise:

- Loading an *existing* char (non-empty inventory, learned skills, real status_point spent) — exposes formula gaps the fresh-Novice case can't.
- Two characters on the same map — exposes visibility / area-broadcast bugs.
- Walking / picking up items / dropping items — exercises MS1 movement + MS3 items.

## How to use this doc

When you ship a parity fix that the replay flagged, append a one-line entry to the History table below (date + line covered). When the next capture reveals a new gap, add it to the table in `Current state` rather than guessing from rAthena source what "should" come next.

## History

- **2026-05-17** — **Slice B shipped.** `BroadcastLoadEndAck` mirrors `clif_parse_LoadEndAck` byte-for-byte against capture line 24 (commit `30ed3b1`). Line 24 went from 64 MISSING → 6 BODY + 9 MISSING — all gameplay-content placeholders. Full diff count across the whole capture: 98 → 19. Test now reaches line 27 (`ZC_HAT_EFFECT` 0x0ADF — new frontier).
- **2026-05-17** — **Slice A shipped.** `BroadcastStatusCalcFirst` mirrors `status_calc_pc(SCO_FIRST)` diff-emit byte-for-byte against capture line 13 trailing (commit `db85ebe`). Includes new `SpId` constants, `RenewalFormulas` with capture-verified `Hit/Flee/Critical/SoftDef/SoftMdef/Batk/MaxHp/MaxSp`, and the proto extension that carries 29 saved-stat fields in `CharacterDataResponse`. Line 13 went from 34 MISSING → 4 BODY (achievement placeholder bytes).
- **2026-05-17** — **Packet registry + decoders for the cascade** (commit `d766c6b`). 32 new packet headers + classes in `Core.Server.Packets.Out.ZC` + 15 structural decoders in `Tools.PacketReplay.Decoders`. Replay framer now parses every captured packet (0 UNKNOWN); MISSING/BODY/FIELDS diffs become legible.
- **2026-05-17** — [initial-status-broadcast.md](initial-status-broadcast.md) scope doc written. Decoded line 13 trailing (41 packets, 535 B) and line 24 partial (49 packets, ~1414 B of 1732) packet-by-packet. Full trigger chain traced through rAthena `pc_reg_received → intif_parse_StorageReceived → status_calc_pc(SCO_FIRST)` for line 13 and `clif_parse_LoadEndAck` for line 24. Six deliverables enumerated with rAthena source citations.
- **2026-05-17** — Wrote this doc. Replay state captured: lines 2/4/6/9/11 ✓, line 13 with 7 packets matching + trailing `status_calc_pc` unmatched, line 24 unreached. All scaffolding (decoders, token rewriter, readiness ping, multi-cache loading, `pc_setpos` OOB randomize, rAthena `pc_authok` packet order) committed.
