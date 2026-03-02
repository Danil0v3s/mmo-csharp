# Char Clif Packet Parity Plan

Source of truth:
- `rathena/src/char/char_clif.cpp`

Goal:
- Achieve 1:1 client packet behavior parity for char-server packet handlers.
- No additional checks/logic beyond rAthena behavior.

## Packet checklist (client -> char)

- [x] `CH_REQ_TO_CONNECT (0x65)`
- [x] `CH_REQ_CHARLIST (0x09a1)`
- [x] `CH_SELECT_CHAR (0x0066)`
- [x] `CH_SELECT_ACCESSIBLE_MAPNAME (0x0841)`
- [x] `CH_MAKE_NEW_CHAR (0x0a39)` (current packet version path)
- [x] `CH_DELETE_CHAR (0x01fb)`
- [x] `CH_REQ_CHAR_DELETE2 (0x0827)`
- [x] `CH_REQ_CHAR_DELETE2_ACCEPT (0x0829)`
- [x] `CH_REQ_CHAR_DELETE2_CANCEL (0x082b)`
- [x] `CH_REQ_IS_VALID_CHARNAME (0x028d)`
- [x] `CH_REQ_CHANGE_CHARNAME / CH_REQ_CHANGE_CHARACTERNAME (0x08fc)`
- [x] `CH_MOVE_CHAR_SLOT (0x08d4)`
- [x] `CH_KEEP_ALIVE / PING (0x0187)`
- [x] `CH_REQ_PINCODE_WINDOW (0x08c5)`
- [x] `CH_PINCODE_CHECK (0x08b8)`
- [x] `CH_PINCODE_CHANGE (0x08be)`
- [x] `CH_PINCODE_SETNEW (0x08ba)`

## Recent parity fixes already applied

- [x] `CH_SELECT_ACCESSIBLE_MAPNAME`: reject forged use when current map is available.
- [x] `CH_SELECT_CHAR`/`CH_SELECT_ACCESSIBLE_MAPNAME`: online `-2` set earlier (ordering parity).
- [x] `CH_MOVE_CHAR_SLOT`: failure ACK now returns source-slot moves like rAthena.
- [x] `CH_KEEP_ALIVE`: strict account-id validation; no account auto-bind.
- [x] `CH_REQ_CHANGE_CHARNAME`: no ACK when char is not owned/found (early return parity).
- [x] Select handoff now resolves map endpoint by map ownership in map registry (not config fallback).
- [x] `CH_PINCODE_CHECK`: disconnect when pincode disabled; disconnect malformed pin payload.
- [x] `CH_REQ_PINCODE_WINDOW`: disabled pincode path no longer mutates verification state.
- [x] `CH_REQ_CHAR_DELETE2_ACCEPT`: removed extra char-window/list resend on success (rAthena sends only `HC_CHAR_DELETE2_ACCEPT_ACK`).
- [x] `CH_MAKE_NEW_CHAR`: apply rAthena-style name normalization/structural validation before create.
- [x] Name duplicate checks now honor `name_ignoring_case` parity in create/rename flows.

## Next parity items

- [x] Add parser-level pincode gate parity (`chclif_parse` behavior): when pincode is enabled and not yet validated, reject/disconnect unexpected packets before handler dispatch.
- [x] Re-check rename validation parity (`normalize_name`/illegal-char semantics) versus current simplified name rules.
- [x] Re-check remaining delete2 result-code edges against `char_delete` return mapping for full code parity.

## Test status

- Char server tests currently passing after latest parity work.
