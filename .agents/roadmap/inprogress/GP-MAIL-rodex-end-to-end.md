# GP-MAIL — RODEX mail works end-to-end

> **Epic:** gameplay · **Status:** 🚧 In progress · **Size:** L · **Player-visible:** yes
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
- Both A and B relog → balances/inventory/mailbox intact; an expired rental item is gone on take.
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
- **Remaining (next turns):** (1) the **read-window** ZC render — `ZC_ACK_READ_RODEX` (body text +
  the nested per-attachment item sub-struct: cards/options/grade/location) + the `CZ_REQ_READ_MAIL`
  handler. (2) `CZ_REQ_REFRESH_MAIL_LIST` / next-page handlers (header added; trivial — reuse
  `MailOpenHandler`'s list build). (3) the **compose/send** path — begin-write, add/remove-item,
  send, check-receiver-name CZ + acks. (4) **separated zeny-only / item-only partial claims** (needs
  a char-side partial-settle path; noted in `MailGetZenyHandler`). (5) **rental-expiry on take**
  (needs an `expire_time` field on the `MailAttachmentItem` proto + char-side). All are layers of
  THIS vertical (no separate tickets); the loop resumes this card.

## Notes / gotchas

- The send fee is `mail_attachment_price` (default 2500), already a const in the service.
- Attachment staging is escrow: items leave the sender's inventory on attach, return on
  cancel/close-without-send (mirror `mail_removeitem`/window-close).
- Char reads structured `MailSendRequest.Items` (NOT a byte blob) — the prior "codec" premise
  was wrong (archive FEATURE-05).
