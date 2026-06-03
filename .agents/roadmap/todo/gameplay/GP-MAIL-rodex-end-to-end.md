# GP-MAIL — RODEX mail works end-to-end

> **Epic:** gameplay · **Status:** ❌ Not started · **Size:** L · **Player-visible:** yes
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

## Notes / gotchas

- The send fee is `mail_attachment_price` (default 2500), already a const in the service.
- Attachment staging is escrow: items leave the sender's inventory on attach, return on
  cancel/close-without-send (mirror `mail_removeitem`/window-close).
- Char reads structured `MailSendRequest.Items` (NOT a byte blob) — the prior "codec" premise
  was wrong (archive FEATURE-05).
