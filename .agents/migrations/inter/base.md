# Inter base IPC (`inter.cpp`)

Cross-server primitives that any server may invoke through the char server: global broadcast, whisper (1:1 message), per-account registry (server-side state vars), name change, account info.

**rAthena source:** [rathena/src/char/inter.cpp](/Volumes/1TB/Projetos/rathena/src/char/inter.cpp)
**C# implementation:** [CharGrpcService.cs](../../../Char.Server/CharGrpcService.cs) lines ~1033-1191
**Proto:** [Core.Server/Protos/char_service.proto](../../../Core.Server/Protos/char_service.proto) section "Inter base"

## Done ✅

### Registry (per-account k/v state)

| RPC | rAthena packet | C# location | Status |
|---|---|---|---|
| `InterRegistryUpdate` | 0x3004 `mapif_parse_Registry` | CharGrpcService.cs:1099-1123 | Forwards to login for persistence |
| `InterRegistryFetch` | 0x3005 `mapif_parse_RegistryRequest` | CharGrpcService.cs:1125-1153 | Fetches from login |

### Account info

| RPC | rAthena packet | C# location | Status |
|---|---|---|---|
| `InterAccountInfo` | 0x3007 `mapif_parse_accinfo` | CharGrpcService.cs:1167-1191 | Forwards to login |

## Pending

None. Whisper / broadcast / name change all close in P5. Map-side
client emission (announce text on screen, whisper bubble UI, etc.) is
gameplay work — map handlers currently log + queue.

## Files

- Server impl: [CharGrpcService.cs](../../../Char.Server/CharGrpcService.cs) (search "Inter")
- Char → map wrapper: [MapServerIpcService.cs](../../../Char.Server/Services/MapServerIpcService.cs)
- Map proto receivers: [map_service.proto](../../../Core.Server/Protos/map_service.proto) (`ReceiveBroadcast`, `ReceiveItemBroadcast`, `ReceiveWhisper`, `ReceiveWhisperToGm`, `NotifyNameChange`, `NotifyAddressSync`)
- Map handlers: [MapGrpcService.cs](../../../Map.Server/MapGrpcService.cs)
- Proto contracts: [char_service.proto](../../../Core.Server/Protos/char_service.proto) (Inter section)

## History

- **2026-05-16** — **P5 complete.** All inter-base routing wired:
  - Added 6 push RPCs to [map_service.proto](../../../Core.Server/Protos/map_service.proto): `ReceiveBroadcast`, `ReceiveItemBroadcast`, `ReceiveWhisper`, `ReceiveWhisperToGm`, `NotifyNameChange`, `NotifyAddressSync`.
  - Added `IMapServerIpcService` + `MapServerIpcService` char-side wrapper that fans out to all connected maps (and aggregates whisper acks).
  - `InterBroadcast` / `InterBroadcastItem` now fan out via the wrapper.
  - `InterWhisper` resolves the target by name (`ICharacterRepository.GetByNameAsync`) and pushes a `MapWhisperNotification` to all maps; the aggregate `delivered` bit is returned.
  - `InterWhisperToGm` fans out; each map filters by group_id locally.
  - `InterNameChange` now validates against `char_name_option` / `char_name_letters` (rAthena `mapif_parse_NameChangeRequest` parity) and fans out a `MapNameChangeNotification`. DB write deferred to map gameplay (same as rAthena TODO).
  - `RequestAddressSync` (P3.3 followup) now also fans out `NotifyAddressSync` to maps.
  - Map handlers log + ack; game-client emission lives in the map gameplay phase.
  - Tests: `InterRoutingTests.cs` covers `IsAllowedCharName` validation matrix + the wrapper's no-maps-connected degradation. 7 new tests; suite at 140.
- **2026-05-15** — Audit found 5 of 9 inter-base RPCs are server-side stubs. Registry (0x3004/0x3005) and `InterAccountInfo` (0x3007) correctly forward to login. Broadcast (0x3000/0x3009), whisper (0x3001/0x3002/0x3003), and name change (0x3006) need implementation. Map-side senders also missing — see [../map/ipc-integration.md](../map/ipc-integration.md).
