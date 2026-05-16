# Map server IPC integration

The biggest open migration surface. The Char server has 118 gRPC RPCs defined and implemented; **the Map server only invokes 2 of them**. Every rAthena packet the map server *sends* to the char server (`chmapif_parse_*` triggers on the char side) needs a corresponding outgoing call from Map.Server, and almost none are wired.

**rAthena map-side senders:** `rathena/src/map/chrif.cpp`, `rathena/src/map/intif.cpp`
**C# map-side wrappers (mostly unused):** [Map.Server/Services/](../../../Map.Server/Services/)
**C# map server impl:** [Map.Server/MapServerImpl.cs](../../../Map.Server/MapServerImpl.cs), [Map.Server/Handlers/](../../../Map.Server/Handlers/)

## Done ✅ (P6 — infrastructure-level wiring)

Per-module typed wrappers live in [Map.Server/Services/CharServerIpcService.*.cs](../../../Map.Server/Services/) (Core, Party, Guild, Storage, Mail, Auction, Quest, Pet, Homunculus, Mercenary, Elemental, Clan, Inter). They exist for every char_service RPC; gameplay-driven RPCs (Module ops below) are call-ready but lack triggers until the gameplay phase.

### Lifecycle triggers wired

| Trigger point | rAthena equivalent | C# location | Calls |
|---|---|---|---|
| Map server startup | `chmapif_parse_getmapname` push | [MapServerImpl.EnsureRegisteredOnCharServerAsync](../../../Map.Server/MapServerImpl.cs) | `RegisterMapServerMaps(serverId, mapsFromConfig)` |
| Periodic (30s) | `chmapif_parse_keepalive` push | `MapServerImpl.SendKeepAliveIfDueAsync` | `KeepAlive` |
| Periodic (10s, on change) | `chmapif_parse_regmapuser` push | `MapServerImpl.SyncUserCountIfDueAsync` | `RegisterMapServerUserCount(serverId, count)` |
| Periodic (300s) | `chrif_save` autosave | `MapServerImpl.AutosaveIfDueAsync` | `SaveCharacterState(setOfflineAfterSave: false, finalSave: false)` for each |
| Player connect (post auth-ticket) | rAthena `chrif_authok` follow-ups | [MapGrpcService.EnterMap](../../../Map.Server/MapGrpcService.cs) | `SetCharacterOnline`, `LoadSkillCooldown`, `RequestStatusChangeData`, `GetBonusScript` |
| Player disconnect | rAthena `chrif_save(sd, 1)` + setcharoffline | `MapGrpcService.LeaveMap` | `SaveCharacterState(finalSave: true)`, `SetCharacterOffline` |
| Map server shutdown | rAthena `do_final_chrif` | `MapServerImpl.StopAsync` | `SaveCharacterState` for each online + `SetAllCharactersOffline(serverId)` |

### Map-side receivers (P5 push receivers, plus ForceDisconnectAccount in P6)

[map_service.proto](../../../Core.Server/Protos/map_service.proto) defines: `ReceiveBroadcast`, `ReceiveItemBroadcast`, `ReceiveWhisper`, `ReceiveWhisperToGm`, `NotifyNameChange`, `NotifyAddressSync`, `ForceDisconnectAccount`. Handlers in [MapGrpcService.cs](../../../Map.Server/MapGrpcService.cs); broadcast/whisper currently log + ack (game-client emission is gameplay phase). `ForceDisconnectAccount` removes matching players from `_playerMapService` and replies with the disconnected count.

### Configuration

Map server reads `ServerId`, `Maps`, `KeepAliveInterval`, `UserCountSyncInterval`, `AutosaveInterval` from [Map.Server/appsettings.json](../../../Map.Server/appsettings.json). Defaults match rAthena (`KeepAliveInterval` 30s, `UserCountSyncInterval` 10s, `AutosaveInterval` 300s).

## Pending — by category

### 🟠 Lifecycle work — gameplay-required wrappers (no triggers yet)

`SaveSkillCooldown`, `SaveStatusChangeData` are exposed by the typed wrapper but currently have no trigger because the map server's TCP session-disposal path (where rAthena would persist these on logout) is gameplay work. They'll be wired when the map gameplay loop starts using the data. Likewise `RequestMapServerChange` (map↔map warp) needs the warp-to-other-map flow which is gameplay-only.

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

## Map.Server.Services inventory

[Map.Server/Services/](../../../Map.Server/Services/) provides typed wrapper partials for every char_service RPC, grouped by domain (Core, Party, Guild, Storage, Mail, Auction, Quest, Pet, Homunculus, Mercenary, Elemental, Clan, Inter). When map gameplay needs to call any RPC, depend on `ICharServerIpcService` from DI and call the typed method directly.

## History

- **2026-05-16** — **P6 complete.** Infrastructure-level wiring done:
  - `MapServerImpl` now owns periodic timers for registration retry, keep-alive, user-count sync, and autosave (intervals from config).
  - `EnterMap` triggers `SetCharacterOnline` + `LoadSkillCooldown` + `RequestStatusChangeData` + `GetBonusScript` post-auth.
  - `LeaveMap` triggers `SaveCharacterState(finalSave=true)` + `SetCharacterOffline`.
  - `StopAsync` batch-saves all online players then `SetAllCharactersOffline`.
  - Added map-side `ForceDisconnectAccount` handler; char-side `ForceDisconnectAccount` cascades to all maps via `IMapServerIpcService.ForceDisconnectAccountOnMapsAsync`.
  - Added `MapServerConfiguration` fields: `ServerId`, `Maps`, `KeepAliveInterval`, `UserCountSyncInterval`, `AutosaveInterval`. Defaults match rAthena.
  - `PlayerEntity` extended with `AccountId`; `IPlayerMapService` extended with `Count`, `GetAllPlayers`, `RemovePlayerAndGet`.
  - Integration tests requiring an in-process multi-server harness are deferred to P7 per ROADMAP.
- **2026-05-15** — Audit revealed Map.Server invokes only 2 of 118 char RPCs despite old IPC plan marking all `[x]`. Document created to track the gap explicitly.
