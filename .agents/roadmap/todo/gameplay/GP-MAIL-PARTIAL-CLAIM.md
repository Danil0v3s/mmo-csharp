# GP-MAIL-PARTIAL-CLAIM — Separated zeny-only / item-only mail claims

> **Epic:** gameplay · **Status:** ❌ Not started · **Size:** S · **Player-visible:** yes
> **Depends on:** GP-MAIL · **Unlocks:** none

## The deliverable

> Claiming **just the zeny** (`CZ_REQ_ZENY_FROM_MAIL`) leaves the item attachments on the mail, and
> claiming **just the items** (`CZ_REQ_ITEM_FROM_MAIL`) leaves the zeny — matching rAthena's separate
> RODEX claim actions.

## Player story

GP-MAIL wired both `CZ_REQ_ZENY_FROM_MAIL` and `CZ_REQ_ITEM_FROM_MAIL` to the **combined**
`GetAttachmentAsync`, which settles the whole attachment (zeny + items) at once via the char-side
`MailGetAttachment` RPC. So the first of the two requests claims everything and the second acks
"nothing left". Attachments are still delivered (no loss), but the rAthena behaviour is two
independent partial claims (the player can take the zeny now and the items later).

## Current state — per layer

| Layer | State | Where |
|---|---|---|
| Handlers | combined | `Map.Server/Handlers/Mail/MailGetZenyHandler.cs` / `MailGetItemHandler.cs` both call `GetAttachmentAsync` |
| Service | combined | `MailService.GetAttachmentAsync` claims zeny + items together |
| Char RPC | combined | `MailGetAttachment` settles + flips the whole-attachment claimed flag |

## rAthena reference

- `rathena/src/map/clif.cpp` `clif_parse_Mail_getattach` — a `request_type` (0 = zeny, 1 = item)
  routes to `intif_Mail_getattach` with the type; the char side settles only that part and leaves the
  other for a later claim.

## Scope

- [ ] Add a claim-kind (zeny / item) to the `MailGetAttachment` request proto + char-side settle
      (settle only the requested part, leave the other; flip per-part claimed flags).
- [ ] Split `GetAttachmentAsync` into zeny-only / item-only paths; wire `MailGetZenyHandler` →
      zeny-only, `MailGetItemHandler` → item-only.

## Done criteria

- Take-zeny credits only the zeny and leaves the items claimable; take-item credits only the items
  and leaves the zeny claimable; a fully-claimed mail acks "nothing left".

## Test plan

- `MailServiceTests` / `MailHandlersTests`: zeny-only claim leaves items; item-only claim leaves zeny.

## Notes

- Split from GP-MAIL (the combined claim already delivers attachments — this is the partial-claim
  fidelity layer that needs a char-side partial-settle path).
