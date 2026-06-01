# FEATURE-05 — Mail service

> **Epic:** Gameplay-Mail · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** none (char-side mail RPCs already real) · **Blocks:** none
> **Related:** PACKET-* (ZC mail UI packets)

## Problem

Mail looks wired but moves nothing of value. `Send` validates the draft, logs,
and clears it — it **never calls the char-server mail RPC, never removes the
attached items/zeny from the sender's inventory**. So a player can "send" mail
and keep the items. `GetAttachment` only logs (no inventory credit on the
receiver). `DeliveryFail` doesn't rebound attachments. The char side has real
persistence (`MailSendAsync`, etc.) and `IntifService` wraps it — but
`MailService.Send` doesn't call `IntifService.MailSend`.

## Current state (C#)

- `Map.Server/Mail/MailService.cs`:
  - `:46 Send(pc, recipientName, title, body)` — validates, then *"Char-server mail RPC handles persistence; we wipe the local draft"* → logs (`:54`) and `Clear(pc)` (`:56`). **No `IntifService.MailSend` call, no inventory/zeny debit.** Returns true.
  - `:110 GetAttachment(pc, mailId)` — *"Inventory mutation happens after the char-server returns..."* → log only (`:114`). No credit.
  - `:101 DeliveryFail(pc)` — *"Real attachments rebound through char-server IPC; here we just clear the local draft"* → `Clear(pc)`. No rebound.
  - `:118 RefreshRemainingAmount(pc)` — empty body, comment only.
  - Working: `OpenMail` (`:27`), `Clear` (`:34`), `SetAttachment` (`:61`), `RemoveItem` (`:72`), `RemoveZeny` (`:82`), `InvalidOperation` (`:91`). Draft state on `PlayerEntity.MailDraftItems` / `MailDraftZeny`.
- `Map.Server/Services/Intif/IntifService.cs`: real wrappers `MailSend` (`:333`), `MailRequestInbox` (`:281`), `MailRead` (`:296`), `MailGetAttach` (`:308`), `MailDelete` (`:319`), `MailReturn` (`:364`) — orphaned (no `MailService` caller).
- Char side: `Char.Server/CharGrpcService.cs` has the Mail RPC overrides; `ICharServerIpcServiceMail` wraps them.

## rAthena reference (source of truth)

- `rathena/src/map/mail.cpp`:
  - `mail_send(sd, dstname, title, body, body_len)` — checks `MAIL_SEND_FAIL` gates (open, not own name, rates/cooldown), then **removes attached items + zeny from the sender's inventory/zeny** (`pc_delitem` per attachment, `pc_payzeny`), packs `struct mail_message`, and `intif_Mail_send(sd->status.char_id, &msg)`. The fee (`MAIL_ZENY` cost) is also deducted. On IPC ack the client gets `clif_Mail_send` (success/fail). On **fail** the items/zeny are returned to the sender.
  - `mail_getattach(sd, mail_id, flag)` → `intif_Mail_getattach` — char side returns the attachment; on success `pc_additem` each item + `pc_getzeny`, mark mail read/attachment-claimed, `clif_Mail_getattachment`. Inventory-full / over-weight rejects.
  - `mail_delete` → `intif_Mail_delete`. `mail_read` → `intif_Mail_read`. `mail_return` → `intif_Mail_return` (bounce to sender).
  - `mail_deliveryfail(sd, msg)` — return the attachments + zeny to the sender's inventory (the IPC reported the send failed).
  - Inbox open (CZ_MAIL_GET_LIST) → `intif_Mail_requestinbox`.

## Scope — every sub-system that must be touched

- [ ] Inject `IIntifService` (+ the inventory/zeny service) into `MailService`.
- [ ] `Send` — after validation: deduct each `MailDraftItems` entry from inventory (`pc_delitem` equivalent), deduct `MailDraftZeny` + the send fee from zeny, build the attachment payload, call `IntifService.MailSend(senderCharId, toName, title, body, zeny)` with the serialized items, then `Clear`. On a **synchronous reject** (inventory check fails) return false and do NOT clear. (Char-side ack handles async fail → rebound via `DeliveryFail`.)
- [ ] `GetAttachment` — call `IntifService.MailGetAttach(charId, mailId, flag)`; on the char-side response, credit items via `pc_additem` + zeny, with inventory-full / overweight rejection (reject keeps the mail unclaimed).
- [ ] `DeliveryFail` — credit the draft items + zeny back to the sender's inventory (true rebound), then clear.
- [ ] `RefreshRemainingAmount` — emit the remaining-mail-count packet from the char-side inbox response.
- [ ] Inbox open / read / delete / return: wire the handlers to `IntifService.MailRequestInbox` / `MailRead` / `MailDelete` / `MailReturn` and emit the inbox list packet on response.
- [ ] **Attachment serialization**: define the wire shape passed to `MailSendAsync` (item id, amount, refine, cards, etc.) — confirm `ICharServerIpcServiceMail.MailSendAsync` already accepts an attachment byte[] (it does — `attachment:` param) and define the codec.
- [ ] **Client packets**: ZC_ACK_MAIL_SEND, ZC_MAIL_REQ_GET_LIST, ZC_MAIL_REQ_OPEN, ZC_ACK_MAIL_GET_ITEM, ZC_ACK_MAIL_DELETE. Define/handle in `Map.Server` or call PACKET-* seam; the **inventory/zeny mutation must occur here**.

## Done criteria

- Sending mail with an item + zeny removes them from the sender's inventory/zeny and dispatches `IntifService.MailSend` with the attachment; the recipient can claim them via `GetAttachment` and receives them.
- A send that fails the inventory gate returns false and does not clear the draft or lose items.
- A char-side delivery failure rebounds the attachment + zeny to the sender (`DeliveryFail`).
- Inbox open populates the list from the char server; read/delete/return reach their RPCs.
- No log-only no-op left in `Send` / `GetAttachment` / `DeliveryFail` / `RefreshRemainingAmount`.

## Test plan

- `Map.Server.Tests` (add) `MailServiceTests`:
  - `Send` debits inventory+zeny and calls `IntifService.MailSend` once;
  - `Send` with insufficient inventory returns false and leaves the draft intact;
  - `GetAttachment` credits items+zeny on a stubbed char response and rejects when inventory full;
  - `DeliveryFail` restores the exact draft items+zeny.
- Integration with char-server mail RPC if a harness exists.
- Manual/live: send mail with an item between two characters; claim it; confirm both inventories.

## Draft validation limits (already correct, keep)

`MailService` already encodes the rAthena caps: `MaxDraftZeny = 1_000_000_000` (`:19`, MAX_ZENY), `MaxAttachments = 5` (`:20`, MAIL_MAX_ITEM), recipient name ≤ 24, title ≤ 40, body ≤ 200 (`Send` :48–51). The draft state lives on `PlayerEntity.MailDraftItems` (index→amount) + `MailDraftZeny`. The gap is purely the *transfer* + *dispatch*, not the validation.

## Send flow (rAthena `mail_send` order)

```
1. gate: MailOpened, recipient valid, title/body length, not self
2. for each MailDraftItems entry: assert inventory still holds index+amount
3. assert zeny >= MailDraftZeny + send_fee
4. debit: pc_delitem(each attachment); pc_payzeny(MailDraftZeny + fee)
5. serialize attachments → attachment byte[]
6. IntifService.MailSend(charId, toName, title, body, MailDraftZeny, attachment)
7. Clear(pc)  // local draft wiped only after dispatch
8. ZC_ACK_MAIL_SEND (success)
```

If step 2/3 fails → reject (ZC_ACK_MAIL_SEND fail), do NOT clear. If the char-side ack later reports failure → `DeliveryFail` rebounds items+zeny.

## Notes / gotchas

- The mail **fee** (rAthena `MAIL_ZENY` / config) is a separate zeny deduction from the attached zeny — apply both.
- Atomicity: debit inventory/zeny only after the gates pass; on async char-side fail, `DeliveryFail` is the compensating action (eventual consistency, matches rAthena).
- Don't reintroduce an in-memory mailbox — persistence is char-side; the map only debits/credits inventory and dispatches IPC.
- `IntifService.MailSend` currently passes `senderName: string.Empty` (char backfills, `:351`) and `receiverCharacterId: 0` (char resolves by name) — keep that contract.
- `GetAttachment` must handle the "claim once" semantic char-side (the char RPC clears the row's attachment+zeny on grab); the map must reject the credit if inventory is full so the char side can keep it unclaimed (don't ack success then drop the item).
