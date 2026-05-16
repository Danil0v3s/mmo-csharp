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

## Pending — server-side stubs

These return success without performing the routing/persistence rAthena does.

### Whisper / broadcast (no fan-out)

| RPC | rAthena packet | C# location | What's missing |
|---|---|---|---|
| `InterBroadcast` | 0x3000 `mapif_parse_broadcast` | CharGrpcService.cs:1033-1048 | Logs the message; **does not forward to any map server**. rAthena fans out to all map servers. |
| `InterBroadcastItem` | 0x3009 `mapif_parse_broadcast_item` | CharGrpcService.cs:1050-1061 | Logs only; no fan-out. |
| `InterWhisper` | 0x3001 `mapif_parse_WisRequest` | CharGrpcService.cs:1063-1082 | Validates strings, returns `true`; does not route to recipient's map server. |
| `InterWhisperReply` | 0x3002 `mapif_parse_WisReply` | CharGrpcService.cs:1084-1089 | Returns hardcoded success; no routing. |
| `InterWhisperToGm` | 0x3003 `mapif_parse_WisToGM` | CharGrpcService.cs:1091-1097 | Validates, returns `true`; no fan-out to GMs. |

These need:
1. A way to look up which map server a recipient character is currently on (or to broadcast to all if recipient unknown).
2. An outbound RPC from char → map to deliver the broadcast/whisper.
3. (Map-side) handlers in Map.Server to receive and emit to game clients.

### Name change (TODO)

| RPC | rAthena packet | C# location | What's missing |
|---|---|---|---|
| `InterNameChange` | 0x3006 `mapif_parse_NameChangeRequest` | CharGrpcService.cs:1155-1165 | Marked TODO; returns `false`. rAthena resolves char/pet/homun name change via DB update + broadcast. |

## Files

- Server impl: [CharGrpcService.cs](../../../Char.Server/CharGrpcService.cs) (search "Inter")
- Proto: [char_service.proto](../../../Core.Server/Protos/char_service.proto) (Inter section)

## History

- **2026-05-15** — Audit found 5 of 9 inter-base RPCs are server-side stubs. Registry (0x3004/0x3005) and `InterAccountInfo` (0x3007) correctly forward to login. Broadcast (0x3000/0x3009), whisper (0x3001/0x3002/0x3003), and name change (0x3006) need implementation. Map-side senders also missing — see [../map/ipc-integration.md](../map/ipc-integration.md).
