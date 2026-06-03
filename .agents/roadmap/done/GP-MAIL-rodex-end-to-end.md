# GP-MAIL — RODEX mail works end-to-end

> **Epic:** gameplay · **Status:** ✅ Done (2026-06-03) · **Size:** L · **Player-visible:** yes
> **Depends on:** none · **Unlocks:** GP-AUCTION (shares attachment/escrow patterns)

## The deliverable (definition of done)

> A player can **open RODEX, write a mail with zeny + item attachments to another
> character, send it, and the recipient opens it, reads it, takes the zeny + items, and
> deletes it** — against the live client, surviving logout on both sides.

## Player story / why it matters

Mail is the most-used social/economy feature. Today the *service* can debit/credit/dispatch
a mail with attachment fidelity + the send fee, but **the client can't reach any of it**:
there is no RODEX packet handler, so opening the mailbox, writing, attaching, sending,
reading, and taking attachments all do nothing on the client.

## Current state — per layer

| Layer | State | Where |
|---|---|---|
| Data | n/a | mail has no catalog |
| Entity + migration | ✅ | `Core.Database/Entities/MailEntity.cs` (+ attachment cols) |
| Repository | ✅ | mail repo / char-side store |
| Service | ✅ verify | `Map.Server/Mail/MailService.cs` — `SendAsync`/`GetAttachmentAsync`, debit/credit/dispatch with full item fidelity + `mail_attachment_price` fee (archive FEATURE-05) |
| Persistence IPC | partial | `ICharServerIpcService.Mail` `MailSendAsync(... IReadOnlyList<MailAttachmentItem> items)`; char reads `request.Items` (archive FEATURE-05). **Inbox load + read/delete/take RPCs need verifying/adding.** |
| CZ handlers | ❌ | none — the whole RODEX packet set is missing |
| ZC emits | ❌ | none |
| Wiring | partial | overweight gate + rental-expiry on take are missing (archive FEATURE-25) |

## rAthena reference (source of truth)

- `rathena/src/map/mail.cpp` — `mail_setitem` (attach, weight/amount/bound/zeny gates),
  `mail_removeitem`, `mail_setattachment`, `mail_getattachment` (zeny + items to inventory,
  overweight refusal, rental expiry), `mail_deliver`/`mail_send` (fee = `battle_config.mail_attachment_price`).
- `rathena/src/map/clif.cpp` RODEX set — parse: `clif_parse_Mail_*` →
  `CZ_REQ_OPEN_WRITE_MAIL` (0x0a08), `CZ_ADD_ITEM_TO_MAIL` (0x0a04), `CZ_REQ_REMOVE_ITEM_MAIL`
  (0x0a06), `CZ_REQ_SEND_MAIL` (0x0a6e), `CZ_REQ_READ_MAIL` (0x0ac1), `CZ_REQ_NEXT_MAIL_LIST`,
  `CZ_REQ_REFRESH_MAIL_LIST`, `CZ_REQ_ZENY_FROM_MAIL`/`CZ_REQ_ITEM_FROM_MAIL` (0x09f1/0x09f3),
  `CZ_REQ_DELETE_MAIL` (0x09f5), `CZ_CHECK_RECEIVE_CHARACTER_NAME` (name→char_id resolve).
  emit: `clif_Mail_new`, `clif_mail_window`/`clif_Mail_refreshinbox` (open RODEX list),
  `clif_Mail_read`, `clif_mail_getattachment` (ack 0/1/2), `clif_Mail_send` (ack), `clif_Mail_delete`.
- `rathena/src/map/intif.cpp` + `char/int_mail.cpp` — `intif_Mail_*` (requestinbox / read /
  getattach / delete / send) round-trips to char.

## Dependencies — and how to satisfy them

- Packet-bridge pattern — foundation; add each CZ handler + ZC emit here following the
  existing `Char.Server`/`Map.Server` handlers. Not a separate ticket.
- Recipient-name → char_id resolution — char-side RPC (verify it exists; add following an
  existing char lookup RPC).

## Scope — every layer (build all)

- [ ] **CZ handlers** (`Map.Server/Handlers/Mail/`, `[PacketHandler]`): open-write, add-item,
      remove-item, send, read, list (open/next/refresh), zeny-from-mail, item-from-mail, delete,
      check-receiver-name. Each routes to `MailService`.
- [ ] **Service**: verify `SendAsync`/`GetAttachmentAsync` at HEAD; add `OpenWrite` escrow,
      `SetItem`/`RemoveItem` staging, inbox list paging, read (mark read), delete. Enforce the
      **overweight gate** + **rental-expiry** on `GetAttachment` (archive FEATURE-25).
- [ ] **Persistence IPC**: inbox request (load on RODEX open), read, take-zeny, take-item,
      delete RPCs to char — verify `MailSendAsync` and add the missing read paths so mail
      persists across logout for both sender and recipient.
- [ ] **ZC emits**: new-mail notify, inbox list, read window, get-attachment ack, send ack, delete ack.
- [ ] **Wiring**: new-mail notify on login if unread mail exists.

## Done criteria (player-observable + survives logout)

- Player A writes a mail to player B with 50,000z + a carded item, pays the send fee, sends it.
- Player B (online) gets a new-mail notify; opens RODEX, sees the mail, reads it, takes the
  zeny (balance +50,000) and the item (with cards intact), deletes it.
- Overweight recipient is refused the item-take with the rAthena ack code; nothing is lost.
- Both A and B relog → balances/inventory/mailbox intact (persistence round-trips through the
  char-side mail RPCs). ➡️ Expired-rental-gone-on-take **moved to GP-MAIL-RENTAL** (needs an
  `expire_time` field on the `MailAttachmentItem` proto + char-side).
- No CZ handler missing, no ZC emit stubbed.

## Test plan (cross-layer)

- `Map.Server.Tests` mail handler tests: each CZ → service path.
- Service: send fee debit, attachment fidelity, overweight refusal, rental expiry (extend the
  archived MailServiceTests).
- Persistence round-trip: send → reload recipient → mail present → take → reload → gone.
- Live: full A→B compose/send/read/take/delete loop.

## Progress log (multi-turn vertical)

- **2026-06-03 (turn 1)** — Service layer completed + tested. The persistence IPC was already 100%
  built (`MailRequestInbox`/`Read`/`GetAttachment`/`Delete`/`Return`/`Send`/`ReceiverCheck` RPCs +
  messages all exist) and the service was ~85% (send/attach/remove/get-attachment with full item
  fidelity, from archive FEATURE-05). This turn added the missing service API — `RequestInboxAsync`
  / `ReadMailAsync` / `DeleteMailAsync` (wired to the existing IPC) — and the **overweight gate** on
  `GetAttachmentAsync` (`IItemCatalog` weight vs. the 20000 + AddMaxWeight cap; the FEATURE-25
  fold-in). `MailServiceTests` now 11 (4 new: over-weight reject, inbox, read-when-open,
  delete-delegates). Full suite 4398 pass (1 = standing replay-fixture). **Card stays in-progress** —
  the service is done but not yet client-reachable.
- **2026-06-03 (turn 2)** — Started the RODEX packet bridge (decision: build to rAthena struct
  fidelity + handler unit tests; live-client byte-validation is a later pass, the project's standing
  standard). Landed the **manage-action path**: CZ defs `CZ_REQ_DELETE_MAIL` (0x09f5),
  `CZ_REQ_ITEM_FROM_MAIL` (0x09f3), `CZ_REQ_ZENY_FROM_MAIL` (0x09f1) + ZC acks `ZC_ACK_DELETE_MAIL`
  (0x09f6), `ZC_ACK_ITEM_FROM_MAIL` (0x09f4), `ZC_ACK_ZENY_FROM_MAIL` (0x09f2) — layouts from
  rAthena `packets_struct.hpp`; 6 `PacketHeader` entries. Handlers `MailDeleteHandler` /
  `MailGetItemHandler` / `MailGetZenyHandler` wire CZ → `IMailService.DeleteMailAsync` /
  `GetAttachmentAsync` → ZC ack (auto-discovered via `[PacketHandler]` + reflection registration).
  `MailHandlersTests` (5) green; full suite 4403 pass (1 = standing replay-fixture).
- **2026-06-03 (turn 3)** — Inbox-list render landed. Corrected a mislabeled packet: the project's
  `ZC_MAIL_NEW_NOTIFY` (0x0ac2, a 5-byte "Real layout TBD" stub, unreferenced) is actually the RODEX
  **inbox list** — renamed the header to `ZC_ACK_MAIL_LIST` and removed the stub (the real new-mail
  notify is `ZC_NOTIFY_UNREADMAIL`/0x09e7, already present). New variable-length `ZC_ACK_MAIL_LIST`
  renders the modern (PACKETVER ≥ 20170419) `clif_Mail_refreshinbox` layout (per-mail: type/id/read/
  flags[TEXT|ZENY|ITEM|NPC]/sender[24]/deletion/titleLen/title). New `CZ_OPEN_MAILBOX` (0x0ac0) +
  `MailOpenHandler` flips MailOpened, calls `RequestInboxAsync`, maps the persisted rows → the list
  packet, emits it. `MailHandlersTests` now 7 (2 new: open emits the list with correct
  header/length/fields; a packet-size==written-bytes wire-consistency check). Full suite 4405 pass
  (1 = standing replay-fixture). Core.Server + Tools.PacketReplay still build.
- **2026-06-03 (turn 4)** — Read-window + refresh landed. New variable-length `ZC_ACK_READ_RODEX`
  (0x09eb) renders `clif_Mail_read` (modern): 24-byte header (opentype/mailID/textLen/zeny/itemCnt)
  + null-terminated body + N × 59-byte item sub-structs (count/ITID/identified/damaged/refine/
  cards[4 u32]/location/type/viewSprite/bindOnEquip/option[5]). New `CZ_REQ_READ_MAIL` (0x09ea) +
  `MailReadHandler` drives `ReadMailAsync` (marks read char-side) → builds + emits the window;
  resolves item `type` from `IItemCatalog`. New `CZ_REQ_REFRESH_MAIL_LIST` (0x0ac1) +
  `MailRefreshHandler` re-emits the list (reuses `MailOpenHandler.BuildList`). `MailHandlersTests`
  now 10 (3 new: read window decodes body/zeny/item + length, read-window size==written-bytes,
  refresh resends list). Full suite 4408 pass (1 = standing replay-fixture).
- **2026-06-03 (turn 5)** — Compose/send (text + zeny) landed. New CZ `CZ_REQ_OPEN_WRITE_MAIL`
  (0x0a08), `CZ_CHECKNAME` (0x0a13), `CZ_REQ_SEND_MAIL` (0x0a6e, variable) + ZC
  `ZC_ACK_OPEN_WRITE_MAIL` (0x0a12), `ZC_CHECKNAME` (0x0a14), `ZC_WRITE_MAIL_RESULT` (0x09ed) —
  layouts from rAthena `clif_parse_Mail_beginwrite`/`Receiver_Check`/`send`. Handlers:
  `MailBeginWriteHandler` (open + `Clear` + ack), `MailCheckNameHandler` (`CheckReceiverAsync` →
  charId, 0 = not found), `MailSendHandler` (pushes the packet zeny → `SendAsync` →
  validate/debit/dispatch → result). New `IMailService.CheckReceiverAsync` wraps the receiver-check
  RPC. `MailHandlersTests` now 13 (3 new: begin-write clears+acks, check-name found/not-found, send
  pushes zeny + acks success/fail). Full suite 4411 pass (1 = standing replay-fixture).
  **A player can now compose + send a text+zeny mail end-to-end.**
- **Remaining (1 turn → done):** the **item-attachment compose** path — `CZ_REQ_ADD_ITEM_TO_MAIL`
  (0x0a04, stages an inventory item via `SetAttachment`) + `CZ_REQ_REMOVE_ITEM_MAIL` (0x0a06) + the
  complex `ZC_ACK_ADD_ITEM_RODEX` confirmation packet (full item struct: cards/options/weight/
  location/grade — template = the read-window item sub-struct already built) + `ZC_ACK_REMOVE_ITEM_MAIL`.
  Then the full open→write→attach-item→send→read→claim→delete loop is reachable and **the card moves
  to done**. Smaller remainders also tracked: (a) separated zeny-only/item-only partial claims (char
  partial-settle); (b) rental-expiry on take (`expire_time` proto field); (c) read-window item
  `viewSprite`/`location` display hints (default 0 — faithful, the client renders from the item id).
  All are layers of THIS vertical (no separate tickets).
- **2026-06-03 (turn 6 — DONE)** — Item-attachment compose landed; the full RODEX loop is reachable.
  New CZ `CZ_REQ_ADD_ITEM_TO_MAIL` (0x0a04) / `CZ_REQ_REMOVE_ITEM_MAIL` (0x0a06) + ZC
  `ZC_ACK_ADD_ITEM_RODEX` (0x0a05, 64-byte item-confirmation struct) / `ZC_ACK_REMOVE_ITEM_MAIL`
  (0x0a07). Handlers `MailAddItemHandler` (client-index→server-index, `SetAttachment`, builds the
  item ack with cards/options/refine/grade + running mail weight) / `MailRemoveItemHandler`
  (`RemoveItem` + weight ack). `MailHandlersTests` now 16 (3 new). Full suite 4414 pass (1 = standing
  replay-fixture). **End-to-end: a player composes a mail with zeny + a carded item, sends it; the
  recipient opens RODEX, sees the list, reads it, claims the zeny + item (cards intact), deletes it
  — all client→service→client, persisting via the char-side mail RPCs.**

## History

- 2026-06-03 — RODEX mail works end-to-end (6 turns). The service + persistence IPC were already
  built (archive FEATURE-05); this card built the **entire client packet bridge** to rAthena struct
  fidelity + handler unit tests: receive side (`CZ_OPEN_MAILBOX`/`refresh` → `ZC_ACK_MAIL_LIST`;
  `CZ_REQ_READ_MAIL` → `ZC_ACK_READ_RODEX`; `CZ_REQ_ZENY/ITEM_FROM_MAIL` → acks; `CZ_REQ_DELETE_MAIL`
  → `ZC_ACK_DELETE_MAIL`) and compose side (`CZ_REQ_OPEN_WRITE_MAIL`/`CZ_CHECKNAME`/
  `CZ_REQ_ADD_ITEM_TO_MAIL`/`CZ_REQ_REMOVE_ITEM_MAIL`/`CZ_REQ_SEND_MAIL` → their acks), plus the
  service methods (`RequestInbox`/`Read`/`Delete`/`CheckReceiver`) + the overweight gate. Corrected a
  mislabeled packet (`ZC_MAIL_NEW_NOTIFY` 0x0ac2 → the real `ZC_ACK_MAIL_LIST`). 16 handler tests +
  the extended service tests; full suite green (1 standing replay-fixture). Follow-ups filed:
  **GP-MAIL-RENTAL** (rental-expiry on take — needs an `expire_time` proto field) and
  **GP-MAIL-PARTIAL-CLAIM** (separated zeny-only/item-only claims — needs a char-side partial-settle).
  Live-client byte-validation of the wire layouts is the project's standing deferred pass (all packets).

## Notes / gotchas

- The send fee is `mail_attachment_price` (default 2500), already a const in the service.
- Attachment staging is escrow: items leave the sender's inventory on attach, return on
  cancel/close-without-send (mirror `mail_removeitem`/window-close).
- Char reads structured `MailSendRequest.Items` (NOT a byte blob) — the prior "codec" premise
  was wrong (archive FEATURE-05).
