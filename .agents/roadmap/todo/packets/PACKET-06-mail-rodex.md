# PACKET-06-mail-rodex — RODEX mail client→map packet bridge

> **Epic:** Packet bridge · **Status:** ❌ Not started · **Size:** L · **Player-visible:** yes
> **Depends on:** FEATURE-mail (MailService + Intif mail RPCs exist) · **Blocks:** none

## Problem

`Map.Server/Mail/MailService.cs` implements the mail surface (`OpenMail`, `Send`,
`SetAttachment`, `RemoveItem`, `RemoveZeny`, `GetAttachment`, `RefreshRemainingAmount`,
`DeliveryFail`) and `IIntifService` has `MailRequestInbox`, `MailRead`, `MailGetAttach`,
`MailDelete`, `MailSend`, `MailReturn`. But **no client→map mail/RODEX packet is wired**. A
player cannot open the RODEX window, list/read mail, attach items/zeny, send, take attachments,
delete, or return mail.

## Current state (C#)

- No handler exists for any mail packet.
- `Map.Server/Mail/IMailService.cs` — `OpenMail(pc)`, `Clear(pc)`, `Send(pc, recipientName, title, body)`,
  `SetAttachment(pc, inventoryIndex, amount)`, `RemoveItem(pc, inventoryIndex)`,
  `RemoveZeny(pc, amount)`, `GetAttachment(pc, mailId)`, `RefreshRemainingAmount(pc)`,
  `DeliveryFail(pc)`, `InvalidOperation(pc)`.
- `Map.Server/Services/Intif/IIntifService.cs:64-69` — `MailRequestInbox(charId, flag)`,
  `MailRead(mailId)`, `MailGetAttach(charId, mailId, flag)`, `MailDelete(charId, mailId)`,
  `MailSend(senderCharId, toName, title, body, zeny)`, `MailReturn(charId, mailId)`.

## rAthena reference (source of truth)

`rathena/src/map/clif.cpp` — the RODEX system reuses the `clif_parse_Mail_*` family with the
`PACKET_CZ_RODEX_*` / `PACKET_ZC_*_RODEX` structs:

- `clif_parse_Mail_refreshinbox` (`clif.cpp:16240`) → request inbox page (`CZ_REQ_OPEN_MAIL` /
  refresh). type byte selects inbox tab (account / return / all).
- `clif_parse_Mail_read` (`clif.cpp:16422`) → read a mail; emits `ZC_ACK_READ_RODEX` (body + item list).
- `clif_parse_Mail_beginwrite` (`clif.cpp:16451`) → open write window (`rodexopenwrite`).
- `clif_parse_Mail_cancelwrite` (`clif.cpp:16473`).
- `clif_parse_Mail_Receiver_Check` (`clif.cpp:16497`) → validate recipient name / class.
- `clif_parse_Mail_setattach` (`clif.cpp:16702`) → add item to the open mail; emits
  `rodexadditem` ack (`ZC_ACK_ADD_ITEM_RODEX`).
- `clif_parse_Mail_getattach` (`clif.cpp:16520`) → take zeny / items from a read mail; type byte
  selects zeny vs item (`rodexgetzeny` / `rodexgetitem`).
- `clif_parse_Mail_delete` (`clif.cpp:16626`) → delete a mail (`rodexdelete`).
- `clif_parse_Mail_return` (`clif.cpp:16670`, `PACKET_CZ_RODEX_RETURN`) → return to sender.
- `clif_parse_Mail_winopen` (`clif.cpp:16763`) → open RODEX main window.
- `clif_parse_Mail_send` (`clif.cpp:16784`) → send the composed mail; emits `rodexwriteresult`
  (`ZC_WRITE_MAIL_RESULT`).

ZC responses (read the struct names in `clif.cpp` around the line refs above):
`ZC_OPEN_RODEX` (`rodexicon` 0x09e7 new-mail notify), `ZC_MAIL_LIST` / `ZC_RODEX_LIST` (inbox page),
`ZC_ACK_READ_RODEX`, `ZC_ACK_ADD_ITEM_RODEX`, `ZC_ACK_REMOVE_ITEM_RODEX` (`rodexremoveitem`),
`ZC_ACK_GET_ZENY_RODEX` / `ZC_ACK_GET_ITEM_RODEX`, `ZC_ACK_DELETE_RODEX` (`rodexdelete`),
`ZC_WRITE_MAIL_RESULT` (`rodexwriteresult`), `ZC_CHECK_RECEIVE_CHARACTER_NAME` (receiver check).
**Read `clif_packetdb.hpp` for the numeric ids — RODEX ids are heavily PACKETVER-versioned.**

## Scope — every sub-system that must be touched

- [ ] **In packets** (`Core.Server/Packets/In/CZ/`):
  - [ ] `CZ_REQ_OPEN_MAIL` / `CZ_REQ_REFRESH_MAIL_LIST` (`clif_parse_Mail_refreshinbox`) —
        `<open_type>.B <page_mail_id>.Q` (cursor paging).
  - [ ] `CZ_REQ_READ_MAIL` (`clif_parse_Mail_read`) — `<open_type>.B <mail_id>.Q`.
  - [ ] `CZ_REQ_ADD_ITEM_TO_MAIL` (`clif_parse_Mail_setattach`) — `<index>.W <amount>.L` (item) /
        zeny path `<zeny>.Q`.
  - [ ] `CZ_REQ_REMOVE_ITEM_MAIL` — remove a staged attachment.
  - [ ] `CZ_REQ_GET_ITEM_FROM_MAIL` (`clif_parse_Mail_getattach`) — `<mail_id>.Q <type>.B` (zeny/item).
  - [ ] `CZ_REQ_SEND_MAIL` (`clif_parse_Mail_send`) — var-len `<receiver>.24B <sender>.24B <zeny>.Q
        <title_len>.W <body_len>.W <title> <body>`.
  - [ ] `CZ_REQ_DELETE_MAIL` (`clif_parse_Mail_delete`) — `<open_type>.B <mail_id>.Q`.
  - [ ] `CZ_REQ_RETURN_MAIL` (`clif_parse_Mail_return`) — `<mail_id>.Q`.
  - [ ] `CZ_CHECK_RECEIVE_CHARACTER_NAME` (`clif_parse_Mail_Receiver_Check`) — `<receiver>.24B`.
  - [ ] `CZ_REQ_OPEN_WRITE_MAIL` (`clif_parse_Mail_beginwrite`) + `CZ_REQ_CANCEL_WRITE_MAIL`.
- [ ] **Out packets** (`Core.Server/Packets/Out/ZC/`): the full list above.
- [ ] **PacketHeader.cs** + **appsettings.packets.json** (send + list are var-len).
- [ ] **Handlers** (`Map.Server/Handlers/Mail/`):
  - [ ] `MailOpenHandler` / `MailRefreshInboxHandler` → `IMailService.OpenMail` +
        `IIntifService.MailRequestInbox(charId, flag)`; inbox arrives on IPC completion → emit list.
  - [ ] `MailReadHandler` → `IIntifService.MailRead(mailId)`; emit `ZC_ACK_READ_RODEX` on completion.
  - [ ] `MailSetAttachHandler` → `IMailService.SetAttachment` / `RemoveZeny` → add-item ack.
  - [ ] `MailRemoveItemHandler` → `IMailService.RemoveItem` → remove-item ack.
  - [ ] `MailGetAttachHandler` → `IIntifService.MailGetAttach(charId, mailId, flag)` → zeny/item ack.
  - [ ] `MailSendHandler` → `IMailService.Send` → `IIntifService.MailSend(...)` → `ZC_WRITE_MAIL_RESULT`;
        on bad recipient → `DeliveryFail`.
  - [ ] `MailDeleteHandler` → `IIntifService.MailDelete` → delete ack.
  - [ ] `MailReturnHandler` → `IIntifService.MailReturn` → ack.
  - [ ] `MailReceiverCheckHandler` → resolve recipient name, emit `ZC_CHECK_RECEIVE_CHARACTER_NAME`.
  - [ ] `MailBeginWriteHandler` / `MailCancelWriteHandler` → open/close write state via `MailService`.
- [ ] No new char-side RPC — all mail RPCs exist; results land on the IPC completion path.

## Done criteria

- Opening the RODEX window lists the inbox page; reading a mail shows body + attachments and marks
  it read; taking zeny/items removes them from the mail and credits the player (capacity/weight gated).
- Sending a mail with a valid recipient + staged attachments + zeny delivers it (sender's items/zeny
  escrowed) and returns the correct `ZC_WRITE_MAIL_RESULT` code; bad recipient → delivery-fail code.
- Delete and return work and produce the matching acks; staged-but-unsent attachments are refunded
  on cancel-write (rAthena escrow behavior).
- No stub, no `// TODO`.

## Test plan

- Handler tests pinning: send to non-existent recipient → fail code; get-attach over weight cap →
  reject; delete/return call the right RPC with `mail_id`; cancel-write refunds staged items.
- Manual: full send→receive→read→take-attachment→delete cycle between two characters.

## Notes / gotchas

- Mail ids are 64-bit (`.Q`) in RODEX; do not truncate to 32-bit.
- The inbox **list** and **read** results arrive asynchronously from the char server (IPC
  completion), not synchronously in the handler — emit the ZC there, not in the parse handler.
- Attachment staging is server-side escrow; items leave inventory when staged and must be refunded
  on cancel/timeout — mirror `MailService.SetAttachment` / `RemoveItem` semantics, don't shortcut.
- "RODEX" == modern mail in this checkout; there is no separate `rodex.cpp`. Cite `clif_parse_Mail_*`.
