# GP-MAIL-RENTAL — Rental-item expiry on mail attachments

> **Epic:** gameplay · **Status:** ❌ Not started · **Size:** S · **Player-visible:** yes
> **Depends on:** GP-MAIL · **Unlocks:** none

## The deliverable

> When a player claims a mail attachment that is a **rental item whose timer has already expired**,
> the item is **not granted** (it's gone) — matching rAthena `mail_getattachment`.

## Player story

GP-MAIL delivers mail attachments end-to-end, but rental items (items with an `expire_time`) are
treated as permanent on claim. rAthena drops an expired rental attachment on take. Today the
`MailAttachmentItem` proto has **no `expire_time` field**, so the map side can't know an attachment
is an expired rental and grants it anyway.

## Current state — per layer

| Layer | State | Where |
|---|---|---|
| Service | partial | `Map.Server/Mail/MailService.cs:GetAttachmentAsync` — credits items, no rental check |
| Proto | ❌ | `Core.Server/Protos/char_service.proto` `MailAttachmentItem` lacks `expire_time` |
| Char side | ❌ | the mail attachment store doesn't carry/return the rental expiry |

## rAthena reference

- `rathena/src/map/mail.cpp` `mail_getattachment` — skips/expires a rental attachment whose
  `expire_time <= now`; `pc_additem` with the rental flag otherwise.

## Scope

- [ ] Add `int64 expire_time` (and the bound/rental flag) to `MailAttachmentItem` in the proto +
      char-side store/return path.
- [ ] `GetAttachmentAsync`: if an item's `expire_time` is set and in the past, do not credit it
      (rAthena drops it); credit live rentals with their remaining expiry on the granted `InventoryItem`.
- [ ] Carry the rental expiry through on the **send** path too (attaching a rental keeps its timer).

## Done criteria

- Claiming an expired-rental attachment grants nothing for that item; a live rental is granted with
  its remaining time; non-rental items are unaffected.

## Test plan

- `MailServiceTests`: expired rental → not credited; live rental → credited with expiry.

## Notes

- Split from GP-MAIL (the core mail loop is done; this is the rental-fidelity layer that needs a
  proto + char-side change).
