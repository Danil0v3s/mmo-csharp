# GP-PARTY-EXPEL-REASON — Expelled-vs-left withdraw reason

> **Epic:** gameplay · **Status:** ✅ Done (2026-06-05) · **Size:** S · **Player-visible:** yes
> **Depends on:** GP-PARTY · **Unlocks:** none

## The deliverable

> When a member is **expelled**, the party's withdraw broadcast carries reason **1 (kicked)** so the
> client shows "X has been expelled", not reason **0 (left)** — matching rAthena
> `e_party_member_withdraw`.

## Player story

GP-PARTY's expel handler removes the target correctly, but it routes through
`IIntifService.LeaveParty`, whose dispatch (`IntifService.DispatchPartyLeaveAsync`) hard-codes
`NotifyMemberWithdraw(..., reason: 0)`. So an expelled member is announced as having *left* rather
than being *kicked* — a UX/fidelity gap.

## Current state — per layer

| Layer | State | Where |
|---|---|---|
| Expel handler | ✅ removes | `Map.Server/Handlers/Party/PartyExpelHandler.cs` → `IIntifService.LeaveParty` |
| Withdraw reason | ❌ always 0 | `Map.Server/Services/Intif/IntifService.cs:209` `NotifyMemberWithdraw(..., reason: 0)` |

## rAthena reference

- `rathena/src/map/party.cpp` `party_member_withdraw` / `clif_party_withdraw` — the reason byte:
  `PARTY_MEMBER_WITHDRAW`=0 (left), `PARTY_MEMBER_EXPEL`=1 (kicked).

## Scope

- [ ] Thread a withdraw-reason through `IIntifService.LeaveParty` (add a `byte reason = 0` param) +
      `DispatchPartyLeaveAsync` → `NotifyMemberWithdraw(reason)`; update the 4 `IIntifService` test
      stubs.
- [ ] `PartyExpelHandler` passes reason 1; `PartyLeaveHandler` passes reason 0.

## Done criteria

- An expelled member's withdraw broadcast carries reason 1; a voluntary leave carries reason 0.

## Test plan

- `PartyJoinReqAckHandlerTests` (manage handlers): expel → reason 1, leave → reason 0.

## Notes

- Split from GP-PARTY: the removal works; this is the kicked-vs-left message distinction, deferred to
  avoid churning the 4 `IIntifService` stubs in the same turn as the core handlers.

## History

- 2026-06-05 — Threaded a `byte reason` through `IIntifService.LeaveParty` → `DispatchPartyLeaveAsync` → `NotifyMemberWithdraw(reason)` (default 0). `PartyExpelHandler` passes reason 1 (PARTY_MEMBER_EXPEL = kicked), `PartyLeaveHandler` passes 0 (left). Updated the 5 IIntifService stubs (the capturing one records `LastLeaveReason`). 2 handler-test assertions added; full Map.Server.Tests 4688 pass (1 standing replay-fixture).
