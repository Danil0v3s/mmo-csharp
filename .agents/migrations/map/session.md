# MS1 · Player session — TCP enter to map spawn

**Phase:** MS1
**Depends on:** [entities.md](entities.md), IPC (P6 done)
**Blocks:** movement, visibility, all gameplay packets

Bridges what's already wired (char→map gRPC handoff in P6) to actual gameplay: a player TCP-connects to the map server, sends the post-handoff "I'm here" packet, the server validates the auth ticket, spawns a `PlayerEntity`, and sends back the initial map data + visible entities.

## Source of truth

- [rathena/src/map/clif.cpp](/Volumes/1TB/Projetos/rathena/src/map/clif.cpp) — `clif_parse_LoadEndAck` (player sends 0x007d after receiving map data), `clif_parse_WantToConnection` (0x436), `clif_authok`/`clif_authfail`, `clif_set_unit_idle`, `pc_setpos`
- [rathena/src/map/pc.cpp](/Volumes/1TB/Projetos/rathena/src/map/pc.cpp) — `pc_authok`, `pc_setpos`, `pc_setregistry` (registers char data loaded from char server)
- [rathena/src/map/chrif.cpp](/Volumes/1TB/Projetos/rathena/src/map/chrif.cpp) — `chrif_authreq`, `chrif_authok` (we already have the equivalent in P6)

## Scope (MS1)

**In scope:**
- Client TCP packet handler for `CZ_WANT_TO_CONNECTION (0x436)` (post char-select) — validates account+login_id pair against the auth ticket already issued by char server.
- Server response `ZC_AID (0x283)` + `ZC_ACCEPT_ENTER (0x73)` with spawn position.
- Client follow-up `CZ_NOTIFY_ACTORINIT (0x7d)` (LoadEndAck): triggers the actual spawn — player gets added to entity registry, surrounding entities sent via `ZC_NOTIFY_STANDENTRY`.
- Player disconnect path: TCP drop → remove from entity registry → trigger existing P6 `LeaveMap` gRPC flow.
- Map change while connected (warp scroll, walking into a warp): tear-down then re-enter (full handshake repeats).

**Out of scope:**
- Item / skill / status packets at spawn time — MS3.
- Reconnect-after-crash handling (rAthena's auth-after-temporary-disconnect path) — later.

## Done

Map server has a TCP listener ([MapServerImpl.cs](../../../Map.Server/MapServerImpl.cs)) and a `MapSessionData` ([Map.Server/MapSessionData.cs](../../../Map.Server/MapSessionData.cs)) that's mostly empty. There's a placeholder `EnterMapHandler` ([Map.Server/Handlers/EnterMapHandler.cs](../../../Map.Server/Handlers/EnterMapHandler.cs)) that listens on `CZ_HEARTBEAT` (wrong packet) and does fake spawning.

The gRPC-level `EnterMap` flow ([MapGrpcService.cs](../../../Map.Server/MapGrpcService.cs)) is wired but represents the **char→map handoff**, not the **client→map TCP connect**. They're different events; today they're conflated.

## Pending

### Items, in suggested order

1. **Disambiguate the two flows.** Rename the existing gRPC `EnterMap` to make clear it's the char-side ticket issuance. The TCP client flow is its own thing:
   - **Step A (already done):** Char server, on `CH_SELECT_CHAR`, issues a map auth ticket (`IMapAuthTicketService`).
   - **Step B (this doc):** Client TCP-connects to map and sends `CZ_WANT_TO_CONNECTION` with `(account_id, char_id, login_id1, login_id2, sex)`.
   - **Step C (this doc):** Map validates the ticket via `RequestCharacterMapAuthAsync` (already exists), binds the session, sends back account-id + accept-enter.
   - **Step D (this doc):** Client sends `CZ_NOTIFY_ACTORINIT`, map spawns the `PlayerEntity` and broadcasts to view range.

2. **`MapSessionData` extension.** Add `int AccountId`, `int CharacterId`, `int LoginId1`, `int LoginId2`, `byte Sex`, `EntityId? EntityId`, `bool LoadEndAckReceived`. The session is "authenticated" once Step C succeeds; the entity exists once Step D fires.

3. **Map packet IDs.** Add the new packet headers to `Core.Server/Packets/PacketHeader.cs` (some may already exist for char select):
   - `CZ_WANT_TO_CONNECTION = 0x436` (modern), and legacy variants
   - `ZC_AID = 0x283`
   - `ZC_ACCEPT_ENTER = 0xeb` (modern; `0x73` legacy) — note this is also used at char select, double-check the map version
   - `CZ_NOTIFY_ACTORINIT = 0x007d` (LoadEndAck)
   - `ZC_NOTIFY_STANDENTRY = 0x9fe` (varies wildly by client version)
   - `ZC_NPCACK_MAPMOVE = 0x0091` (map change accepted)
   - `ZC_REFUSE_ENTER = 0x074`

   See [packets.md](packets.md) for the full inventory and the client-version pinning discussion.

4. **`WantToConnectionHandler`** (`Map.Server/Handlers/`):
   - Parse the 5 fields from the packet.
   - Call `ICharServerIpcService.RequestCharacterMapAuthAsync` (already exists) with the ticket data.
   - On success: bind the session, send `ZC_AID` + `ZC_ACCEPT_ENTER` with the saved-map position from the character data.
   - On failure: send `ZC_REFUSE_ENTER` + disconnect.
   - Replace the placeholder `EnterMapHandler`.

5. **`LoadEndAckHandler` / `NotifyActorInitHandler`** (`Map.Server/Handlers/`):
   - Requires session is post-`WantToConnection`.
   - Allocate `PlayerEntity` via `IEntityRegistry.Add(...)` at the character's saved position.
   - Send `ZC_NOTIFY_STANDENTRY` to nearby players for this player (visibility — see [visibility.md](visibility.md)).
   - Send the player's view (other entities in range via `ZC_NOTIFY_STANDENTRY`).
   - Mark `session.LoadEndAckReceived = true`.

6. **Disconnect path.** The existing TCP receive loop closes on socket error; we need to hook in:
   - Look up the `PlayerEntity` by `MapSessionData.EntityId`.
   - Remove from `IEntityRegistry`.
   - Broadcast `ZC_NOTIFY_VANISH` to nearby players.
   - Call the existing P6 `LeaveMap` IPC trigger so the char server saves state + flips online flag.

7. **`MapServerImpl` integration.** The current `MapGrpcService.EnterMap` triggers `SetCharacterOnline` post-auth. That's still correct, but we need to make sure the **TCP-connect path** also has the right hooks. The gRPC `EnterMap` should pre-register the auth ticket; the TCP `CZ_WANT_TO_CONNECTION` consumes it.

8. **Move state from `IPlayerMapService` to `IEntityRegistry`.** Once [entities.md](entities.md) is built, the existing `IPlayerMapService` (P6) should fold into the new registry. The MS2 wave doesn't need a separate per-character tracker.

### File layout

```
Map.Server/Handlers/
├── WantToConnectionHandler.cs      — CZ_WANT_TO_CONNECTION (0x436)
└── LoadEndAckHandler.cs            — CZ_NOTIFY_ACTORINIT (0x7d)

Map.Server/MapSessionData.cs        — extend with auth + entity binding
Map.Server/Session/
├── SessionDispatcher.cs            — maps SessionId ↔ EntityId
└── SessionAuthState.cs             — enum: Unauthenticated → Authenticated → Spawned
```

### Tests (Map.Server.Tests)

1. `WantToConnectionHandlerTests`:
   - Valid ticket → session bound, accept-enter sent.
   - Invalid ticket → refuse-enter + disconnect.
   - Replayed ticket (already consumed) → refuse-enter.
2. `LoadEndAckHandlerTests`:
   - Pre-auth state (no WantToConnection yet) → ignored/rejected.
   - Post-auth → entity spawned in registry, view-range broadcast triggered.
3. Integration smoke test (in-process):
   - Char server issues ticket → map server's WantToConnection accepts → LoadEndAck spawns.
   - Verify `IEntityRegistry.Get(charId)` returns the player.

### Acceptance

- A player completing char-select can TCP-connect to map server 5191, send `CZ_WANT_TO_CONNECTION`, receive `ZC_ACCEPT_ENTER`, send `CZ_NOTIFY_ACTORINIT`, and end up as a tracked `PlayerEntity` in the registry.
- Disconnect path: TCP drop → entity removed from registry → P6 `LeaveMap` flow runs → char server sees `online=0`.

### Open decisions

- **Client packet version pinning.** rAthena supports 100+ client versions; packets vary substantially. We must pin one. Likely candidate: 2020-09-23 (popular kRO Zero target). See [packets.md](packets.md).
- **Reconnect handling.** rAthena's auth-after-temporary-disconnect path allows a brief re-auth window. For MS1, treat any disconnect as final. Add the grace window later if user experience needs it.

## History

- **2026-05-16** — Plan written. No implementation yet.
