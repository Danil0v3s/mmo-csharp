# Map server IPC integration

The biggest open migration surface. The Char server has 118 gRPC RPCs defined and implemented; **the Map server only invokes 2 of them**. Every rAthena packet the map server *sends* to the char server (`chmapif_parse_*` triggers on the char side) needs a corresponding outgoing call from Map.Server, and almost none are wired.

**rAthena map-side senders:** `rathena/src/map/chrif.cpp`, `rathena/src/map/intif.cpp`
**C# map-side wrappers (mostly unused):** [Map.Server/Services/](../../../Map.Server/Services/)
**C# map server impl:** [Map.Server/MapServerImpl.cs](../../../Map.Server/MapServerImpl.cs), [Map.Server/Handlers/](../../../Map.Server/Handlers/)

## Done ✅ (the only 2)

| RPC | Trigger | Map-side caller |
|---|---|---|
| `RequestCharacterMapAuth` (0x2b26) | Player TCP connect to map | [MapGrpcService.cs:34](../../../Map.Server/MapGrpcService.cs) consumer / map handlers |
| `GetCharacterData` | Character payload load | Map service flow |

## Pending — by category

### 🔴 CRITICAL — Character lifecycle (data-loss without these)

These are called by rAthena's `chrif.cpp` on character logout / disconnect / state change. Without them the map server is read-only against the DB.

| RPC | rAthena packet | rAthena trigger | Where to wire in C# |
|---|---|---|---|
| `SaveCharacterState` | 0x2b01 `reqsavechar` | character logout, autosave timer, map change | Map.Server logout path, autosave scheduler |
| `SetCharacterOffline` | 0x2b17 `setcharoffline` | character logs out | Map.Server disconnect handler |
| `SetCharacterOnline` | 0x2b19 `setcharonline` | character enters map | Map.Server post-auth handler |
| `SetAllCharactersOffline` | 0x2b18 `setalloffline` | server shutdown / reset | Map.Server shutdown lifecycle |
| `SaveSkillCooldown` | 0x2b15 | character logout | Map.Server logout path |
| `LoadSkillCooldown` | 0x2b0a | character login | Map.Server post-auth handler |
| `SaveStatusChangeData` | 0x2b1c | character logout (with active SCs) | Map.Server logout path |
| `RequestStatusChangeData` | 0x2afc | character login (load active SCs) | Map.Server post-auth handler |

### 🟠 HIGH — Handshake / topology

Without these the char server doesn't know which maps each map server hosts, or current usercount per server.

| RPC | rAthena packet | rAthena trigger |
|---|---|---|
| `RegisterMapServerMaps` | 0x2afa | map server startup, announces its map list |
| `RegisterMapServerUserCount` | 0x2aff | every 10s, announces user count |
| `GetMapServerUserCount` | 0x2afe | map↔map relay (informational) |
| `UpdateMapServerAddress` | 0x2b13 | map server IP change |
| `KeepAlive` (Char↔Map) | 0x2b23 | periodic heartbeat |

### 🟠 HIGH — Auth handoff continuation

| RPC | rAthena packet | rAthena trigger |
|---|---|---|
| `NotifyCharacterSelectAuthOk` | 0x2b02 | char select success → tell char server auth handoff completed |
| `RequestMapServerChange` | 0x2b05 | warp to a map on a different map server |

### 🟡 MEDIUM — Social ops forwarded via char

| RPC | rAthena packet | rAthena trigger |
|---|---|---|
| `RequestRemoveFriend` | 0x2b07 | player removes friend (cross-server flow) |
| `RequestCharacterName` | 0x2b08 | name lookup for offline char |
| `RequestEmailChange` | 0x2b0c | in-game email change |
| `ForwardAccountStatusChange` | 0x2b0e | in-game state log |
| `RequestDivorce` | 0x2b11 | marriage divorce |
| `RequestCharacterBan` | 0x2b28 | in-game ban command |
| `RequestCharacterUnban` | 0x2b2a | in-game unban command |

### 🟡 MEDIUM — Fame & bonus

| RPC | rAthena packet | rAthena trigger |
|---|---|---|
| `RequestFameList` | 0x2b1a | periodic fame refresh |
| `GetBonusScript` | 0x2b2d | character login |
| `SaveBonusScript` | 0x2b2e | character logout |
| `UpdateFame` | 0x2b10 | quest/PvP fame award (also needs server impl — see [../char/grpc.md](../char/grpc.md)) |

### 🟢 LOWER — Module ops (gameplay features)

All ~75 module RPCs (party / guild / storage / mail / auction / quest / achievement / pet / homunculus / mercenary / elemental / clan / inter-base broadcast/whisper). Char-side impls are mostly complete; each needs a map-side caller wired into the corresponding gameplay command. See [../inter/modules.md](../inter/modules.md) for the per-module RPC list.

## Suggested wiring order

1. **Lifecycle first.** Without these, no other work matters because character state is lost on logout. Order: `SetCharacterOnline` + `LoadSkillCooldown` + `RequestStatusChangeData` on map enter; `SaveCharacterState` + `SaveSkillCooldown` + `SaveStatusChangeData` + `SetCharacterOffline` on map exit / disconnect.
2. **Handshake/topology.** So the char server has accurate map ownership and usercount data for select handoff and load balancing.
3. **Module ops, prioritized by feature.** Party + guild + mail are the most user-visible.
4. **Social / fame.** Lowest priority unless those features are part of the launch scope.

## Map.Server.Services inventory

[Map.Server/Services/](../../../Map.Server/Services/) defines wrapper classes for many of these RPCs (e.g. `CharServerIpcService`), but the wrappers are not currently called from gameplay code. When wiring, prefer extending those wrappers rather than calling `IServerConnectionService` directly from handlers.

## History

- **2026-05-15** — Audit revealed Map.Server invokes only 2 of 118 char RPCs despite old IPC plan marking all `[x]`. Document created to track the gap explicitly.
