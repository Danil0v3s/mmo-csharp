# Char client packet handlers

Client (`CH_*`) → Char server packet parity vs rAthena.

**rAthena source:** [rathena/src/char/char_clif.cpp](/Volumes/1TB/Projetos/rathena/src/char/char_clif.cpp)
**C# implementation:** [Char.Server/Handlers/](../../../Char.Server/Handlers/)
**Dispatch:** [Char.Server/CharServerImpl.cs](../../../Char.Server/CharServerImpl.cs) → [Core.Server/Network/PacketHandlerRegistry.cs](../../../Core.Server/Network/PacketHandlerRegistry.cs)

## Done ✅

All 17 client packet handlers exist and dispatch via `[PacketHandler]` attribute + `IPacketHandler<TSession,TPacket>`. Behavior was audited 2026-05-15 against rAthena.

| Packet | rAthena handler | C# handler |
|---|---|---|
| `CH_REQ_TO_CONNECT 0x65` | `chclif_parse_reqtoconnect` | [ClientConnectHandler.cs](../../../Char.Server/Handlers/ClientConnectHandler.cs) |
| `CH_REQ_CHARLIST 0x9a1` | `chclif_parse_req_charlist` | [CharacterListHandler.cs](../../../Char.Server/Handlers/CharacterListHandler.cs) |
| `CH_SELECT_CHAR 0x66` | `chclif_parse_charselect` | [CharacterSelectHandler.cs](../../../Char.Server/Handlers/CharacterSelectHandler.cs) |
| `CH_SELECT_ACCESSIBLE_MAPNAME 0x841` | `chclif_parse_select_accessible_map` | [CharacterSelectAccessibleMapHandler.cs](../../../Char.Server/Handlers/CharacterSelectAccessibleMapHandler.cs) |
| `CH_MAKE_NEW_CHAR 0xa39` | `chclif_parse_createnewchar` | [CharacterCreateHandler.cs](../../../Char.Server/Handlers/CharacterCreateHandler.cs) |
| `CH_DELETE_CHAR 0x1fb` | `chclif_parse_delchar` | [CharacterDeleteHandler.cs](../../../Char.Server/Handlers/CharacterDeleteHandler.cs) |
| `CH_REQ_CHAR_DELETE2 0x827` | `chclif_parse_char_delete2_req` | [CharacterDelete2RequestHandler.cs](../../../Char.Server/Handlers/CharacterDelete2RequestHandler.cs) |
| `CH_REQ_CHAR_DELETE2_ACCEPT 0x829` | `chclif_parse_char_delete2_accept` | [CharacterDelete2AcceptHandler.cs](../../../Char.Server/Handlers/CharacterDelete2AcceptHandler.cs) |
| `CH_REQ_CHAR_DELETE2_CANCEL 0x82b` | `chclif_parse_char_delete2_cancel` | [CharacterDelete2CancelHandler.cs](../../../Char.Server/Handlers/CharacterDelete2CancelHandler.cs) |
| `CH_REQ_IS_VALID_CHARNAME 0x28d` | `chclif_parse_reqrename` | [CharacterRenameValidateHandler.cs](../../../Char.Server/Handlers/CharacterRenameValidateHandler.cs) |
| `CH_REQ_CHANGE_CHARNAME 0x8fc` | `chclif_parse_ackrename` | [CharacterRenameApplyHandler.cs](../../../Char.Server/Handlers/CharacterRenameApplyHandler.cs) |
| `CH_MOVE_CHAR_SLOT 0x8d4` | `chclif_parse_moveCharSlot` | [CharacterMoveSlotHandler.cs](../../../Char.Server/Handlers/CharacterMoveSlotHandler.cs) |
| `CH_KEEP_ALIVE 0x187` | `chclif_parse_keepalive` | [CharKeepAliveHandler.cs](../../../Char.Server/Handlers/CharKeepAliveHandler.cs) |
| `CH_REQ_PINCODE_WINDOW 0x8c5` | `chclif_parse_reqpincode_window` | [PincodeWindowHandler.cs](../../../Char.Server/Handlers/PincodeWindowHandler.cs) |
| `CH_PINCODE_CHECK 0x8b8` | `chclif_parse_pincode_check` | [PincodeCheckHandler.cs](../../../Char.Server/Handlers/PincodeCheckHandler.cs) |
| `CH_PINCODE_CHANGE 0x8be` | `chclif_parse_pincode_change` | [PincodeChangeHandler.cs](../../../Char.Server/Handlers/PincodeChangeHandler.cs) |
| `CH_PINCODE_SETNEW 0x8ba` | `chclif_parse_pincode_setnew` | [PincodeSetNewHandler.cs](../../../Char.Server/Handlers/PincodeSetNewHandler.cs) |

### Parser-level pincode gate ✅

rAthena's `chclif_parse` (lines 1588-1632) rejects non-whitelisted packets while pincode is unverified. C# parity: [CharServerImpl.cs:125-132, 200-205](../../../Char.Server/CharServerImpl.cs). Whitelist: `CH_REQ_TO_CONNECT`, `CH_KEEP_ALIVE`, `CH_PINCODE_CHECK`, `CH_PINCODE_CHANGE`, `CH_REQ_PINCODE_WINDOW`, `CH_REQ_CHARLIST`.

### Earlier parity fixes (preserved from old plan)

- `CH_SELECT_ACCESSIBLE_MAPNAME` rejects forged use when current map is available.
- `CH_SELECT_CHAR` / `CH_SELECT_ACCESSIBLE_MAPNAME` set `online = -2` earlier (ordering parity).
- `CH_MOVE_CHAR_SLOT` failure ACK returns source-slot moves like rAthena.
- `CH_KEEP_ALIVE` performs strict account-id validation (see Pending below — this is a deliberate stricter divergence).
- `CH_REQ_CHANGE_CHARNAME` early-returns with no ACK when char is not owned/found.
- Select handoff resolves map endpoint by map ownership in map registry (not config fallback).
- `CH_PINCODE_CHECK` disconnects when pincode disabled and on malformed payload.
- `CH_REQ_CHAR_DELETE2_ACCEPT` removed extra char-window/list resend (rAthena only sends `HC_CHAR_DELETE2_ACCEPT_ACK`).
- `CH_MAKE_NEW_CHAR` applies rAthena-style name normalization/structural validation.
- Name duplicate checks honor `name_ignoring_case` in create/rename flows.

## Pending ⚠️

### Minor divergences (low priority — current behavior is safe but not exact)

- **`CH_KEEP_ALIVE 0x187` is stricter than rAthena.** rAthena ignores the account_id field on keep-alive ([char_clif.cpp:1328](/Volumes/1TB/Projetos/rathena/src/char/char_clif.cpp)); C# disconnects on mismatch ([CharKeepAliveHandler.cs:12](../../../Char.Server/Handlers/CharKeepAliveHandler.cs)). Decide whether to relax or document the divergence.

- **`CH_REQ_CHANGE_CHARNAME 0x8fc` resend burst differs.** rAthena calls `chclif_mmo_char_send` (full char list); C# calls `ResendCharacterWindowAsync` ([CharacterRenameApplyHandler.cs:146-164](../../../Char.Server/Handlers/CharacterRenameApplyHandler.cs)) which sends `HC_ACCEPT_ENTER` + `HC_CHARLIST_NOTIFY` + `HC_BLOCK_CHARACTER`. Functionally equivalent, structurally different. Verify clients accept both.

## Tests

Per-handler tests in [Char.Server.Tests/Handlers/](../../../Char.Server.Tests/Handlers/). Flow-level tests in [Char.Server.Tests/Services/](../../../Char.Server.Tests/Services/) (notably `PincodeGateParityTests.cs`, `CharacterSelectPacketFlowTests.cs`, `ConnectFlowRegressionGuardTests.cs`).

## History

- **2026-05-15** — Audited all 17 handlers against rAthena `char_clif.cpp`. 15 are exact matches, 2 have benign divergences logged in Pending.
- **(undated, pre-2026-05)** — Initial parity fixes for accessible-map, online-state ordering, move-slot ACK, keep-alive validation, rename early-return, map endpoint resolution, pincode disable/malformed, delete2 ACK trim, new-char normalization, name case rules. Parser-level pincode gate added.
