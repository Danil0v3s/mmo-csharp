# FEATURE-25 — Mail attachment: overweight rejection + rental-item expiry

> **Epic:** Feature unlock · **Status:** ❌ Not started · **Size:** S · **Player-visible:** yes
> **Depends on:** FEATURE-05 (mail attachment transfer) · **Blocks:** none

## Problem

FEATURE-05's `MailService.GetAttachmentAsync` credits claimed attachment items + zeny with full
fidelity (refine / cards / random options / bound / enchant grade), and rejects when the inventory
has no free **slots**. Two rAthena gates are not yet matched:

1. **Overweight rejection** — rAthena `mail_getattachment` also rejects the claim when the items
   would push the PC over `max_weight` (`pc_checkadditem` returns `CHKADDITEM_OVERAMOUNT`). FEATURE-05
   only checks the slot count, not weight.
2. **Rental expiry** — the `MailAttachmentItem` proto carries no `expire_time`, so a rental item
   (`InventoryItem.ExpireTime`) sent through mail loses its remaining duration on claim.

## Current state (C#)

- `Map.Server/Mail/MailService.cs` `GetAttachmentAsync` — free-slot gate only
  (`inv.Count + freshSlots > MaxInventory`); `CreditItem` maps everything except `ExpireTime`
  (no proto field) and `Favorite`/`EquipSwitch` (not relevant to mail).
- `Core.Server/Protos/char_service.proto` `MailAttachmentItem` — has refine/cards/options/bound/
  enchant_grade/unique_id; **no** `expire_time`.

## rAthena reference (source of truth)

- `rathena/src/map/mail.cpp` `mail_getattach` → the `pc_checkadditem` / weight gate before crediting.
- `db` mail_attachment table carries the item's `expire_time` for rentals.

## Scope

- [ ] `GetAttachmentAsync` — inject the item catalog (weights) + read the PC's current/max weight;
      reject the claim (return false, leave the mail unclaimed) when the attachment would exceed
      `max_weight`. Match rAthena's `pc_checkadditem` semantics.
- [ ] Add `expire_time` to `MailAttachmentItem` (proto) + the char-side mail_attachment row mapping
      + map it both ways in `MailService.ToAttachment` / `CreditItem`, so rental items keep their
      remaining duration through mail.

## Done criteria

- A claim that would overweight the PC is rejected and the mail stays claimable.
- A rental item sent + claimed retains its `ExpireTime`.

## Test plan

- `MailServiceTests` — overweight claim rejected; rental item round-trips its expiry.

## Notes / gotchas

- The slot-count + full-attribute fidelity (refine/cards/random options) already land in FEATURE-05;
  this is the weight gate + the rental-expiry proto field only.
