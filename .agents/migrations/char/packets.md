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

None. P2 closed the two minor divergence items as deliberate decisions — see History.

## Tests

Per-handler tests in [Char.Server.Tests/Handlers/](../../../Char.Server.Tests/Handlers/). Flow-level tests in [Char.Server.Tests/Services/](../../../Char.Server.Tests/Services/) (notably `PincodeGateParityTests.cs`, `CharacterSelectPacketFlowTests.cs`, `ConnectFlowRegressionGuardTests.cs`).

## History

- **2026-05-22** — **T6.3 audit-doc refresh — verified 0 ❌.** Companion
  to T5.2's map-tree sweep. All 17 client packet handlers ✅; the two
  deliberate divergences (`CH_KEEP_ALIVE` stricter check, `CH_REQ_CHANGE_CHARNAME`
  resend burst) remain resolved in the P2 history entry below. Full audit
  rollup at [../T6-audit-2026-05-22.md](../T6-audit-2026-05-22.md). No
  code changes — this is a checkpoint for future audits.
- **2026-05-16** — **P2 closed for packets:**
  - `CH_KEEP_ALIVE 0x187` stricter check (validate account_id, disconnect on mismatch) kept as a **deliberate divergence** — catches forged keep-alive packets that rAthena lets through. Marked won't-fix.
  - `CH_REQ_CHANGE_CHARNAME 0x8fc` resend burst: re-audited [`CharacterRenameApplyHandler.cs:74-113`](../../../Char.Server/Handlers/CharacterRenameApplyHandler.cs) against rAthena `chclif_mmo_char_send` ([char_clif.cpp:440-453](/Volumes/1TB/Projetos/rathena/src/char/char_clif.cpp)). Both send the same 4 packets in order: `HC_ACCEPT_ENTER2` + `HC_ACCEPT_ENTER` + `HC_CHARLIST_NOTIFY` + `HC_BLOCK_CHARACTER`. Earlier audit was wrong; no change needed.
- **2026-05-15** — Audited all 17 handlers against rAthena `char_clif.cpp`. 15 are exact matches, 2 have benign divergences logged in Pending.
- **(undated, pre-2026-05)** — Initial parity fixes for accessible-map, online-state ordering, move-slot ACK, keep-alive validation, rename early-return, map endpoint resolution, pincode disable/malformed, delete2 ACK trim, new-char normalization, name case rules. Parser-level pincode gate added.
