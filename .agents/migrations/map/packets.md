# MS1 · Client packet inventory

**Phase:** cross-cutting MS1 reference
**Depends on:** nothing (can start anytime)
**Used by:** session, movement, visibility, everything

The map server speaks the Ragnarok client protocol. rAthena's `clif.cpp` is 25K lines covering ~700 distinct packet types. We only need a small subset for MS1, but we need to pin the client version we target before writing any new handler — packet IDs and shapes vary across versions.

## Source of truth

- [rathena/src/map/clif_packetdb.hpp](/Volumes/1TB/Projetos/rathena/src/map/clif_packetdb.hpp) — the giant packet-id-per-version table
- [rathena/src/map/clif.cpp](/Volumes/1TB/Projetos/rathena/src/map/clif.cpp) — all handlers and emitters
- [rathena/src/common/mmo.hpp](/Volumes/1TB/Projetos/rathena/src/common/mmo.hpp) — `PACKETVER` macros

## Client version pinning

**Decision needed.** rAthena supports ~30 packet versions stretching from 2004 to 2024. We pick one and stick to it. Candidates:

- **20180621** — kRO main, broad community client support.
- **20200902** — Modern kRO Zero (popular pservers, more packets but more bug-fixes).
- **20211103** — Recent kRO main; many UI updates.

(Pre-renewal client targets like 20180620 zero are excluded — this project is renewal-only.)

**Recommendation:** `PACKETVER = 20211103` — recent enough to have all the modern char-select/map handoff packets we already emit on the char side; old enough that there's well-known rAthena baseline behavior to mirror.

The pinned version goes in [Core.Server/Packets/PacketVersion.cs](../../../Core.Server/Packets) (new file). All handlers and emitters branch on it where rAthena uses `#if PACKETVER >= X`.

## Scope (MS1 packets)

Just the minimum to support enter + walk + see-other-players. Each row maps to a code file path that will hold the packet class.

### Incoming (CZ_) — handlers in Map.Server

| Packet ID | Name | Purpose | Handler |
|---|---|---|---|
| `0x0436` | `CZ_WANT_TO_CONNECTION` (modern) | post char-select TCP connect | [session.md](session.md) |
| `0x007d` | `CZ_NOTIFY_ACTORINIT` | load-end-ack (client ready to spawn) | [session.md](session.md) |
| `0x035f` | `CZ_REQUEST_MOVE2` | walk request (modern) | [movement.md](movement.md) |
| `0x0085` | `CZ_REQUEST_MOVE` | walk request (legacy fallback) | [movement.md](movement.md) |
| `0x007e` | `CZ_REQUEST_TIME` (`PING`) | client keep-alive | TBD — likely echo `ZC_NOTIFY_TIME` |
| `0x0187` | (already used by char) | ignore on map | — |
| `0x00a2` | `CZ_REQ_QUIT` | client-initiated quit (ALT+E) | Disconnect path |

### Outgoing (ZC_) — emitters

| Packet ID | Name | Purpose | Emitter |
|---|---|---|---|
| `0x0283` | `ZC_AID` | tells client its account id | [session.md](session.md) WantToConnectionHandler |
| `0x00eb` (modern) / `0x0073` (legacy) | `ZC_ACCEPT_ENTER` | spawn position + map data ack | [session.md](session.md) |
| `0x0074` | `ZC_REFUSE_ENTER` | auth ticket rejected | [session.md](session.md) |
| `0x09fe` / `0x09ff` / `0x0a30` (varies) | `ZC_NOTIFY_STANDENTRY` | "an entity at standing-rest is here" | [visibility.md](visibility.md) |
| `0x0086` | `ZC_NOTIFY_MOVE` | "this entity is walking from A to B" | [movement.md](movement.md) |
| `0x0087` | `ZC_NOTIFY_PLAYERMOVE` | echo to mover ("walk accepted") | [movement.md](movement.md) |
| `0x0080` | `ZC_NOTIFY_VANISH` | entity leaving view (disconnect, warp, death) | [visibility.md](visibility.md) |
| `0x0091` | `ZC_NPCACK_MAPMOVE` | server-issued map change (warp scroll, walking into warp) | [movement.md](movement.md) |
| `0x0081` | `SC_NOTIFY_BAN` | kick reason (already exists on char side) | shared with char |
| `0x0088` | `ZC_STOPMOVE` | entity stopped walking | [movement.md](movement.md) |
| `0x007f` | `ZC_NOTIFY_TIME` | server time echo | response to `CZ_REQUEST_TIME` |

That's 7 incoming + 11 outgoing = 18 packets for full MS1. Tiny compared to rAthena's total.

## Source of complexity

For some of these packets the shape varies wildly by `PACKETVER`. The C# implementation needs to:
1. Pin the version at top of the file/class
2. Implement the exact shape for that version
3. NOT support multiple shapes in the same packet class — that's rAthena's `#ifdef` hell. Pick one shape per pinned version.

For example, `ZC_NOTIFY_STANDENTRY`:
- Pre-20071113: ~56 bytes
- 2020+: ~109 bytes (added headdir2, robe, body_state, etc.)
- 2024+: even more fields

We pick the 20211103 shape; if we ever change the pinned version, the packet class is the one that gets rewritten.

## Done

- `PACKETVER = 20211103` pinned in [Core.Server/Packets/PacketVersion.cs](../../../Core.Server/Packets/PacketVersion.cs).
- [Core.Server/Packets/PositionPacker.cs](../../../Core.Server/Packets/PositionPacker.cs) — shared WBUFPOS / WBUFPOS2 encoders for 3-byte and 6-byte packed positions.
- Incoming (handlers TBD in [session.md](session.md)/[movement.md](movement.md)):
  - [CZ_WANT_TO_CONNECTION](../../../Core.Server/Packets/In/CZ/CZ_WANT_TO_CONNECTION.cs)
  - [CZ_NOTIFY_ACTORINIT](../../../Core.Server/Packets/In/CZ/CZ_NOTIFY_ACTORINIT.cs)
  - [CZ_REQUEST_MOVE](../../../Core.Server/Packets/In/CZ/CZ_REQUEST_MOVE.cs)
  - [CZ_REQUEST_TIME](../../../Core.Server/Packets/In/CZ/CZ_REQUEST_TIME.cs)
  - [CZ_REQ_QUIT](../../../Core.Server/Packets/In/CZ/CZ_REQ_QUIT.cs)
- Outgoing:
  - [ZC_AID](../../../Core.Server/Packets/Out/ZC/ZC_AID.cs)
  - [ZC_ACCEPT_ENTER_ZONE](../../../Core.Server/Packets/Out/ZC/ZC_ACCEPT_ENTER_ZONE.cs)
  - [ZC_REFUSE_ENTER_ZONE](../../../Core.Server/Packets/Out/ZC/ZC_REFUSE_ENTER_ZONE.cs)
  - [ZC_NOTIFY_TIME](../../../Core.Server/Packets/Out/ZC/ZC_NOTIFY_TIME.cs)
  - [ZC_NOTIFY_VANISH](../../../Core.Server/Packets/Out/ZC/ZC_NOTIFY_VANISH.cs)
  - [ZC_NPCACK_MAPMOVE](../../../Core.Server/Packets/Out/ZC/ZC_NPCACK_MAPMOVE.cs)
  - [ZC_NOTIFY_PLAYERMOVE](../../../Core.Server/Packets/Out/ZC/ZC_NOTIFY_PLAYERMOVE.cs)
  - [ZC_NOTIFY_MOVE](../../../Core.Server/Packets/Out/ZC/ZC_NOTIFY_MOVE.cs)
  - [ZC_STOPMOVE](../../../Core.Server/Packets/Out/ZC/ZC_STOPMOVE.cs)
  - [ZC_NOTIFY_STANDENTRY](../../../Core.Server/Packets/Out/ZC/ZC_NOTIFY_STANDENTRY.cs) — 108-byte `idle_unit` skeleton; cosmetic fields zeroed for MS1, filled out in MS3.
- 26 round-trip / wire-shape tests in [Map.Server.Tests/Packets/](../../../Map.Server.Tests/Packets/) (all passing).

## Pending

### Items

1. **Pin `PACKETVER`.** Add `Core.Server/Packets/PacketVersion.cs` with a single constant; reference it everywhere a version branch matters.

2. **Per-packet class.** For each of the 18 above, add an `IncomingPacket` or `OutgoingPacket` class in `Core.Server/Packets/In/CZ/` or `Core.Server/Packets/Out/ZC/`. Use the existing `Read(BinaryReader)` / `Write(BinaryWriter)` pattern from the char-side packets.

3. **Register sizes** for variable-length packets in `Core.Server/Packets/appsettings.packets.json` (the existing file).

4. **Wire handlers** as `[PacketHandler(...)]` attributes on the new `*Handler.cs` files in `Map.Server/Handlers/`.

5. **Document packet version in the doc.** Every packet class gets a 1-line comment: `// Packet shape: PACKETVER = 20211103`. Easier to audit than digging through rAthena's #ifdefs.

### File layout

```
Core.Server/Packets/
├── PacketVersion.cs              — pinned version constant
├── In/CZ/                        — new
│   ├── CZ_WANT_TO_CONNECTION.cs
│   ├── CZ_NOTIFY_ACTORINIT.cs
│   ├── CZ_REQUEST_MOVE.cs
│   ├── CZ_REQUEST_MOVE2.cs
│   ├── CZ_REQUEST_TIME.cs
│   └── CZ_REQ_QUIT.cs
├── Out/ZC/                       — new
│   ├── ZC_AID.cs
│   ├── ZC_ACCEPT_ENTER.cs
│   ├── ZC_REFUSE_ENTER.cs
│   ├── ZC_NOTIFY_STANDENTRY.cs
│   ├── ZC_NOTIFY_MOVE.cs
│   ├── ZC_NOTIFY_PLAYERMOVE.cs
│   ├── ZC_NOTIFY_VANISH.cs
│   ├── ZC_NPCACK_MAPMOVE.cs
│   ├── ZC_STOPMOVE.cs
│   └── ZC_NOTIFY_TIME.cs
└── appsettings.packets.json      — extend with new variable-length packet sizes
```

### Tests

For each packet, a round-trip test in `Core.Server.Tests/Packets/` (or `Map.Server.Tests/Packets/`):
- Write known field values → read back via `BinaryReader` → asserts match.
- Validates the on-wire layout against a hex fixture from a real client capture if possible.

### Acceptance

- Each of the 18 packets has a class, registered size (where variable), and round-trip test.
- `PACKETVER` is pinned in a single place; every packet class comments which version's shape it targets.

## Cross-cutting decisions deferred to MS2/MS3

- **Encryption / packet shuffling.** rAthena supports per-version obfuscation tables (`clif_obfuscation.hpp`, `clif_shuffle.hpp`). For private/test clients we can leave plaintext; for kRO official-client compatibility, we'd need to port the obfuscation. Document and skip for MS1.
- **Length-prefixed payload integrity.** rAthena calculates lengths at write time. Our existing `OutgoingPacket` writer handles this; just keep the pattern.

## History

- **2026-05-16** — Plan written. No implementation yet. `PACKETVER` not yet pinned.
- **2026-05-16** — All 14 MS1 packets + `PacketVersion` + `PositionPacker` shipped. 26 round-trip / wire-shape tests in [Map.Server.Tests/Packets/](../../../Map.Server.Tests/Packets/) — all passing. Handlers/wire-up still pending (covered by session.md and movement.md).
